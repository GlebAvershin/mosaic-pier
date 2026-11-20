# /// script
# requires-python = ">=3.10"
# dependencies = ["mcp==1.26.0"]
# ///
"""Run the native rules through the official Unity MCP server in a live workshop editor."""

import argparse
import asyncio
import csv
import json
from pathlib import Path

from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client

ROOT = Path(__file__).resolve().parents[2]
TOOL = "mosaic_verify_campaign"


async def replay(project):
    server = StdioServerParameters(command="unity", args=["mcp", "--project-path", str(project.resolve())])
    async with stdio_client(server) as (reader, writer), ClientSession(reader, writer) as session:
        info = await session.initialize()
        if TOOL not in {tool.name for tool in (await session.list_tools()).tools}:
            raise RuntimeError("The open editor has not registered the Mosaic campaign command")
        result = await session.call_tool(TOOL, {})
        if result.isError:
            raise RuntimeError(str(result.content))
        payload = json.loads(result.content[0].text)

    for key, filename in [("chapters", "unity-player.csv"), ("commissions", "unity-master.csv")]:
        with (ROOT / "docs/evidence" / filename).open() as file:
            expected = [dict(panes_left=0, **{k: int(v) for k, v in row.items()}) for row in csv.DictReader(file)]
        if payload[key] != expected:
            raise ValueError(f"Editor and release replay diverged for {key}: {payload[key]}")
    return {"server": info.serverInfo.name, "version": info.serverInfo.version, "tool": TOOL, **payload}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("project", type=Path)
    print(json.dumps(asyncio.run(replay(parser.parse_args().project)), indent=2))
