"""Measure candidate responsive layouts against the rendered game."""
import argparse
import json
from pathlib import Path
from playwright.sync_api import sync_playwright

cli=argparse.ArgumentParser()
cli.add_argument("url")
cli.add_argument("--chrome")
cli.add_argument("--out",type=Path,default=Path("studio/layout-metrics.json"))
args=cli.parse_args()
source=Path("web/style.css").read_text()
options={
    "board_first":source,
    "journal_first":source.replace("order:2","order:-1"),
    "compact_split":source.replace("max-width:800px","max-width:540px"),
}
metrics={}
with sync_playwright() as pw:
    browser=pw.chromium.launch(executable_path=args.chrome)
    for name,css in options.items():
        page=browser.new_page()
        page.goto(args.url)
        page.wait_for_function("!document.querySelector('#hint').disabled")
        page.evaluate("document.fonts.ready")
        page.evaluate("document.querySelector('link[rel=stylesheet]').disabled=true")
        page.add_style_tag(content=css)
        rows=[]
        for w,h in [(360,740),(390,844),(600,800),(750,800),(1024,768),(1320,850)]:
            page.set_viewport_size({"width":w,"height":h})
            rows.append(page.evaluate("""() => {
                const rect=s=>document.querySelector(s).getBoundingClientRect();
                return {w:innerWidth,h:innerHeight,tile:Math.floor(rect('.tile').width),
                  controls_bottom:Math.ceil(rect('.tools').bottom),overflow:document.documentElement.scrollWidth-innerWidth,
                  side_by_side: rect('.art').right<=rect('.workshop').left};
            }"""))
        metrics[name]=rows
        page.close()
    browser.close()
args.out.write_text(json.dumps(metrics,indent=2)+"\n")
