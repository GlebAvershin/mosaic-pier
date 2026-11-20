"""Bake the enamel sockets and chipped ice rims at 2x for Unity and the web."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter
import math
import random
import shutil

assets = Path(__file__).resolve().parents[1] / 'web/assets'
unity = assets.parents[1] / 'engines/Unity/Assets/Resources'
for layers, name in enumerate(('enamel', 'rime', 'double-rime')):
    rng = random.Random(155 + layers)
    size = 144
    base = (37, 72, 78) if not layers else (118, 180, 184)
    img = Image.new('RGBA', (size, size))
    alpha = Image.new('L', img.size)
    ImageDraw.Draw(alpha).rounded_rectangle((1, 1, 142, 142), radius=16, fill=255)
    pix = img.load()
    for y in range(size):
        for x in range(size):
            edge = min(x, y, size-x, size-y)
            light = 14 * (1-y/size) + 4 * math.sin((x+2*y)*.065)
            light += .8*rng.uniform(-4,4) - 12 * math.exp(-abs(edge-5)/2.2)
            pix[x,y] = (*[max(0,min(255,round(v+light))) for v in base],255)
    img.putalpha(alpha)
    pen = ImageDraw.Draw(img)
    pen.rounded_rectangle((3,3,140,140), 15, outline=(116,150,145) if not layers else (224,249,239),width=2)
    pen.arc((5,5,136,136),214,238,fill=(153,181,170),width=1)
    if layers:
        for x,y,dx,dy in [(9,11,1,1),(133,132,-1,-1),(134,12,-1,1)]:
            end=(x+dx*30,y+dy*14)
            pen.line([(x,y),(x+dx*13,y+dy*16),end,(end[0]+dx*7,end[1]+dy*10)],fill=(233,255,248),width=2)
            pen.line([(x+dx*13,y+dy*16),(x+dx*10,y+dy*25)],fill=(214,244,237),width=2)
        if layers==2:
            pen.rounded_rectangle((9,9,134,134),11,outline=(255,228,160),width=4)
    img.resize((96,96),Image.Resampling.LANCZOS).save(assets/(name+'.png'))
    shutil.copyfile(assets/(name+'.png'),unity/(name+'.png'))
