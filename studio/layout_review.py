"""Workshop MCP adapter for responsive game UI."""
import json
from pathlib import Path
from typing import Any
from mcp.server.fastmcp import FastMCP
server=FastMCP("Glass workshop layout review")
goal="At six viewport sizes keep each token at least 44 CSS pixels wide, show controls without scrolling and prevent horizontal overflow. At widths of 1024 and above show journal and board side by side."

def sheets():
    return json.loads(Path(__file__).with_name("layout-metrics.json").read_text())

@server.resource("studio://review/brief")
def brief() -> dict:
    def compact(rows):
        return {"minimum_tile":min(r["tile"] for r in rows),
                "clipped_controls":sum(r["controls_bottom"]>r["h"] for r in rows),
                "overflowing_layouts":sum(r["overflow"]>0 for r in rows),
                "wide_side_by_side":all(r["side_by_side"] for r in rows if r["w"]>=1024)}
    return {"instruction":goal,"options":{name:compact(rows) for name,rows in sheets().items()}}

@server.resource("studio://review/negative-control")
def negative_control() -> str:
    return "compact_split"

@server.tool(structured_output=True)
def verify_choice(choice: str) -> dict[str,Any]:
    rows=sheets().get(choice,[])
    failed=[r["w"] for r in rows if r["tile"]<44 or r["controls_bottom"]>r["h"] or
            r["overflow"]>0 or (r["w"]>=1024 and not r["side_by_side"])]
    return {"accepted":len(rows)==6 and not failed,"failed_widths":failed,"viewports":len(rows)}

if __name__=="__main__":
    server.run()
