"""Cut five original glass tokens from radial polygons. Requires Pillow."""
import math
import shutil
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter, ImageOps

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
    # Rippled glass comes from the same backlit surface as the restored window.
    grain = Image.open(root / "kingfisher.jpg").crop((90, 945, 290, 1145)).resize(img.size)
    material = ImageOps.colorize(ImageOps.grayscale(grain), tint(rgb, .44), tint(rgb, 1.12, 58))
    enamel = Image.blend(img.convert("RGB"), material, .24).convert("RGBA")
    enamel.putalpha(img.getchannel("A"))
    pen = ImageDraw.Draw(enamel)
    gold, glint = (255, 230, 157), (255, 246, 205)
    def line(points, width=1.5, color=gold):
        pen.line(list(map(xy, points)), fill=color, width=round(width*4), joint="curve")
    def arc(box, start, end):
        pen.arc(tuple(round(v*4) for v in box), start, end, fill=gold, width=6)
    line(outline+[outline[0]], 1.6, (121,90,49))
    line(outline[-2:]+outline[:2], 1.2, glint)
    if name == "lagoon":
        arc((34,39,62,68),15,153)
        arc((39,48,59,65),20,145)
    elif name == "honey":
        pen.ellipse((168,156,224,212),outline=glint,width=6)
        for k in range(8):
            t=k*math.pi/4
            line([(49+10*math.cos(t),46+10*math.sin(t)),(49+15*math.cos(t),46+15*math.sin(t))])
    elif name == "coral":
        for x,y in [(35,38),(42,33),(50,31),(58,33),(65,38)]:
            line([(50,60),(x,y)])
        arc((34,31,66,48),180,360)
    elif name == "orchid":
        line([(50,32),(54,45),(62,50),(54,54),(50,68),(46,54),(38,50),(46,45),(50,32)])
    else:
        line([(32,69),(64,32)])
        line([(45,53),(44,39)])
        line([(49,49),(63,47)])
    shadow.alpha_composite(enamel)
    destination = root / (name+".png")
    shadow.resize((160,160),Image.Resampling.LANCZOS).save(destination)
    for client in ("engines/Unity/Assets/Resources", "engines/Unreal/Content/Glass"):
        shutil.copyfile(destination, root.parents[1]/client/destination.name)
