"""Cut five original glass tokens from radial polygons. Requires Pillow."""
import math
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter

root = Path(__file__).resolve().parents[1] / "web/assets"
shapes = [
    ("lagoon", [(50,5),(70,34),(80,55),(77,72),(65,84),(48,88),(29,82),(19,66),(20,49),(33,30)], (23,177,218)),
    ("honey", [(50,6),(79,22),(89,54),(73,81),(43,91),(16,70),(12,38),(28,17)], (244,165,31)),
    ("coral", [(20,15),(50,10),(80,18),(92,42),(50,89),(8,42)], (234,77,73)),
    ("orchid", [(50,5),(86,47),(51,94),(14,50)], (156,102,210)),
    ("fern", [(83,10),(86,40),(79,65),(59,81),(30,87),(14,74),(13,52),(28,27),(54,16)], (52,174,114))
]
N=400
def xy(p): return tuple(round(v*4) for v in p)
def tint(rgb, mult, extra=0):
    return tuple(max(0,min(255,round(v*mult+extra))) for v in rgb)
for name, outline, rgb in shapes:
    shadow=Image.new("RGBA",(N,N))
    ImageDraw.Draw(shadow).polygon([xy((x,y+4)) for x,y in outline],fill=(3,25,34,130))
    shadow=shadow.filter(ImageFilter.GaussianBlur(6))
    img=Image.new("RGBA",(N,N))
    d=ImageDraw.Draw(img)
    center=(49,48)
    inner=[(center[0]+(x-center[0])*.59,center[1]+(y-center[1])*.59) for x,y in outline]
    d.polygon(list(map(xy,outline)), fill=tint(rgb,.43))
    for i,p in enumerate(outline):
        n=(i+1)%len(outline)
        light=(1-(p[0]+p[1])/200)
        d.polygon(list(map(xy,[p,outline[n],inner[n],inner[i]])), fill=tint(rgb,.48+light*.72,light*39))
    d.polygon(list(map(xy,inner)), fill=tint(rgb,.96,16))
    d.polygon(list(map(xy,inner[:2]+[(48,54),inner[-1]])), fill=tint(rgb,1.03,32))
    for pts,color,w in [(outline,tint(rgb,.47),2),(inner,tint(rgb,1.15,34),.65)]:
        d.line(list(map(xy,pts+[pts[0]])),fill=color,width=round(w*4),joint="curve")
    d.line(list(map(xy,[outline[-1],outline[0],outline[1]])),fill=tint(rgb,1.1,73),width=4)
    shadow.alpha_composite(img)
    shadow.resize((128,128),Image.Resampling.LANCZOS).save(root/(name+".png"))
