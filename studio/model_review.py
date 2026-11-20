"""Run a bounded model/tool loop against either game workshop."""
import argparse
import asyncio
import json
from pathlib import Path
import sys
import httpx
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client

async def review(adapter,endpoint,model):
    params=StdioServerParameters(command=sys.executable,args=[str(adapter.resolve())])
    async with stdio_client(params) as io, ClientSession(*io) as mcp, httpx.AsyncClient(timeout=60) as client:
        await mcp.initialize()
        brief=json.loads((await mcp.read_resource("studio://review/brief")).contents[0].text)
        remaining=dict(brief["options"])
        history=[]
        while remaining:
            context=json.dumps({**brief,"options":remaining,"feedback":history},sort_keys=True)
            assert len(context.encode())<=8192
            schema={"type":"object","properties":{"choice":{"type":"string","enum":list(remaining)}},"required":["choice"],"additionalProperties":False}
            response=await client.post(endpoint.rstrip("/")+"/v1/chat/completions",json={
                "model":model,"seed":42,"temperature":0,"max_tokens":48,"messages":[
                    {"role":"system","content":"Select the one proposal that satisfies the design brief."},
                    {"role":"user","content":context}],"response_format":{"type":"json_schema","json_schema":{"name":"decision","schema":schema}}})
            response.raise_for_status()
            payload=response.json()
            choice=json.loads(payload["choices"][0]["message"]["content"])["choice"]
            del remaining[choice]
            verdict=await mcp.call_tool("verify_choice",{"choice":choice})
            if verdict.isError:raise RuntimeError("Tool failed")
            history.append({"choice":choice,"context_bytes":len(context.encode()),"usage":payload["usage"],"verdict":verdict.structuredContent})
            if verdict.structuredContent["accepted"]:
                return {"adapter":adapter.name,"model":model,"brief":brief,"history":history,"accepted":choice}
        raise ValueError("No acceptable proposal")

if __name__=="__main__":
    cli=argparse.ArgumentParser()
    cli.add_argument("adapter",type=Path)
    cli.add_argument("--endpoint",default="http://127.0.0.1:18181")
    cli.add_argument("--model",default="qwen")
    args=cli.parse_args()
    print(json.dumps(asyncio.run(review(args.adapter,args.endpoint,args.model)),indent=2))
