"""Play via visible controls in Chromium, including keyboard, hint, swipe and reload."""
import argparse
import json
from pathlib import Path
from playwright.sync_api import sync_playwright

def board(page):
    return page.locator(".tile")

def number(page,id):
    return int(page.locator("#"+id).inner_text())

def choose_hint(page, keyboard=False):
    page.locator("#hint").click()
    marked=page.locator(".hinted")
    assert marked.count()==2
    a,b=[el.get_attribute("aria-label") for el in marked.all()]
    if keyboard:
        page.get_by_role("button",name=a,exact=True).focus()
        page.keyboard.press("Space")
        page.get_by_role("button",name=b,exact=True).focus()
        page.keyboard.press("Space")
    else:
        for name in (a,b):page.get_by_role("button",name=name,exact=True).click()
    page.wait_for_function("!document.querySelector('#retry').disabled")

def main():
    cli=argparse.ArgumentParser()
    cli.add_argument("url")
    cli.add_argument("--chrome")
    cli.add_argument("--out",type=Path,default=Path("docs/evidence"))
    args=cli.parse_args()
    args.out.mkdir(parents=True,exist_ok=True)
    errors=[]
    with sync_playwright() as pw:
        browser=pw.chromium.launch(executable_path=args.chrome,headless=True)
        page=browser.new_page(viewport={"width":1320,"height":850},device_scale_factor=1,reduced_motion="reduce")
        page.on("pageerror",lambda e:errors.append(str(e)))
        page.goto(args.url)
        page.wait_for_function("!document.querySelector('#hint').disabled")
        page.evaluate("document.fonts.ready")
        page.screenshot(path=args.out/"workshop.jpg",type="jpeg",quality=88)
        page.locator("#guide").click()
        assert page.get_by_text("The glazier’s guide").is_visible()
        page.keyboard.press("Escape")
        page.locator("#sound").click()
        assert page.locator("#sound").get_attribute("aria-pressed")=="true"
        # An equal-colour adjacent pair can never be a legal swap.
        labels=[c.get_attribute("aria-label").split(", ")[2] for c in board(page).all()]
        a,b=next((i,j) for i in range(36) for j in (i+1,i+6) if j<36 and
                 abs(i%6-j%6)+abs(i//6-j//6)==1 and labels[i]==labels[j])
        before=number(page,"moves")
        board(page).nth(a).click();board(page).nth(b).click()
        assert number(page,"moves")==before
        chapters=[]
        for ch in range(6):
            title=page.locator("#chapter").inner_text()
            turns=0
            while not page.locator("dialog").is_visible():
                choose_hint(page,keyboard=ch==0 and turns==0)
                turns+=1
                assert turns<=24
                if ch==0 and turns==1:
                    score=number(page,"score");labels=board(page).evaluate_all("(x)=>x.map(e=>e.ariaLabel)")
                    page.reload();page.wait_for_function("!document.querySelector('#hint').disabled")
                    assert board(page).evaluate_all("(x)=>x.map(e=>e.ariaLabel)")==labels and number(page,"score")==score
            assert page.locator("#modalAction").inner_text()=="Restore "+title
            chapters.append({"chapter":title,"turns":turns,"remaining":number(page,"moves"),"score":number(page,"score")})
            page.locator("#modalAction").click()
        assert page.locator("#modalTitle").inner_text()=="The glasshouse is open"
        page.locator("#modalAction").click()
        assert page.locator("#progress").inner_text()=="6 of 6 sections restored"
        page.reload();page.get_by_text("The glasshouse is open",exact=True).wait_for()
        mobile=browser.new_context(viewport={"width":390,"height":844},device_scale_factor=1,is_mobile=True,has_touch=True,reduced_motion="reduce")
        m=mobile.new_page();m.on("pageerror",lambda e:errors.append(str(e)))
        m.goto(args.url);m.wait_for_function("!document.querySelector('#hint').disabled")
        m.evaluate("document.fonts.ready");m.screenshot(path=args.out/"mobile.jpg",type="jpeg",quality=85)
        assert m.evaluate("document.documentElement.scrollWidth<=innerWidth")
        m.locator("#hint").tap()
        points=m.locator(".hinted").evaluate_all("(xs)=>xs.map(x=>{let r=x.getBoundingClientRect(); return {x:r.x+r.width/2,y:r.y+r.height/2}})")
        cdp=mobile.new_cdp_session(m)
        for event,pts in [("touchStart",points[:1]),("touchMove",points[1:]),("touchEnd",[])]:
            cdp.send("Input.dispatchTouchEvent",{"type":event,"touchPoints":pts})
        m.wait_for_function("Number(document.querySelector('#score').textContent)>0")
        assert number(m,"moves")==9
        assert not errors,errors
        (args.out/"browser.json").write_text(json.dumps({"engine":browser.version,"chapters":chapters,"viewport":[1320,850],
            "mobile_viewport":[390,844],"checks":["six restorations","keyboard match","invalid swap","saved board replay","completed campaign reload","sound control","guide","touch swipe","mobile overflow"],"page_errors":errors},indent=2)+"\n")
        browser.close()
if __name__=="__main__":
    main()
