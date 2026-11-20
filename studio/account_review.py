"""Use the subscription CLI adapter from the same pinned workshop platform as CI."""
import argparse
import asyncio
import json
from pathlib import Path
import sys
from tempfile import TemporaryDirectory
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client

async def review(platform, provider):
    sys.path.insert(0, str(platform.resolve() / "studio" / "agents"))
    from subscription_review import ask, DEFAULT_MODELS
    params = StdioServerParameters(command=sys.executable, args=[str(Path(__file__).with_name("agent_review.py").resolve())])
    async with stdio_client(params) as io, ClientSession(*io) as mcp:
        await mcp.initialize()
        resource = await mcp.read_resource("studio://review/brief")
        brief = json.loads(resource.contents[0].text)
        with TemporaryDirectory() as tmp:
            answer = await asyncio.to_thread(ask, provider, DEFAULT_MODELS[provider], brief, Path(tmp))
        result = await mcp.call_tool("verify_choice", {"choice": answer["choice"]})
        if result.isError or not result.structuredContent["accepted"]:
            raise ValueError("The proposed finale failed release review")
        return {"provider": provider, "model": DEFAULT_MODELS[provider], "brief": brief,
                "decision": answer, "verdict": result.structuredContent}

if __name__ == "__main__":
    cli = argparse.ArgumentParser()
    cli.add_argument("platform", type=Path)
    cli.add_argument("provider", choices=["codex","gemini"])
    args = cli.parse_args()
    print(json.dumps(asyncio.run(review(args.platform, args.provider)), indent=2))
