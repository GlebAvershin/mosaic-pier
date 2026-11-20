"""MCP bridge from release engineering to the playable C++ board."""
import json
import os
from pathlib import Path
import subprocess
from typing import Any
from mcp.server.fastmcp import FastMCP

server = FastMCP("Mosaic Pier level workshop")
CANDIDATES = {"kingfisher": 900, "pelican": 901, "sandpiper": 902}
MOVES, PANES = 14, 13


def simulate(seed):
    default = Path(__file__).resolve().parents[1] / "build" / "balance"
    result = subprocess.run([os.environ.get("ATLAS_ORACLE") or str(default),
                             str(seed), str(MOVES), str(PANES)],
                            capture_output=True, text=True, check=True, timeout=5)
    return json.loads(result.stdout)


@server.resource("studio://review/brief")
def brief() -> dict:
    return {"instruction": "Choose a solvable finale with 2 to 5 moves left, at least two line sweeps and a cascade depth of at least three under the hint policy.",
            "options": {name: simulate(seed) for name, seed in CANDIDATES.items()}}


@server.resource("studio://review/negative-control")
def negative_control() -> str:
    return "sandpiper"


@server.tool(structured_output=True)
def verify_choice(choice: str) -> dict[str, Any]:
    if choice not in CANDIDATES:
        return {"accepted": False, "reason": "Unknown candidate"}
    seed = CANDIDATES[choice]
    result = simulate(seed)
    return {"accepted": bool(result["won"] and 2 <= result["spare"] <= 5
                             and result["sweeps"] >= 2 and result["cascade"] >= 3),
            "seed": seed, "moves": MOVES, "panes": PANES, "replay": result}


if __name__ == "__main__":
    server.run()
