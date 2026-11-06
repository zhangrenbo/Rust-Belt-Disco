"""Draw a deterministic orthographic 45-degree camera blockout for MJ composition tests."""
from pathlib import Path
from PIL import Image, ImageDraw
import math

root=Path(__file__).resolve().parents[1]
out=root/'美术概念'/'固定机位_正交45度_场景构图参考.png'
W,H=1600,900
img=Image.new('RGB',(W,H),(31,39,49));d=ImageDraw.Draw(img)
scale=78
def p(x,y,z=0):
    # Camera azimuth 45 degrees, elevation 45 degrees; orthographic projection.
    return (round(W*.5+(x-y)*math.sqrt(.5)*scale),
            round(H*.54+(x+y)*.5*scale-z*math.sqrt(.5)*scale))
def poly(points,color,outline=None,width=1):
    pts=[p(*v) for v in points]
    d.polygon(pts,fill=color)
    if outline:d.line(pts+[pts[0]],fill=outline,width=width)
def line(points,color,width=2): d.line([p(*v) for v in points],fill=color,width=width)
def box(x0,y0,x1,y1,h,color):
    top=tuple(min(255,c+35) for c in color)
    side=tuple(max(0,c-22) for c in color)
    poly([(x0,y0,0),(x1,y0,0),(x1,y1,0),(x0,y1,0)],(47,59,69),(90,105,114),2)
    poly([(x1,y0,0),(x1,y1,0),(x1,y1,h),(x1,y0,h)],side,(140,153,158),2)
    poly([(x0,y1,0),(x1,y1,0),(x1,y1,h),(x0,y1,h)],color,(140,153,158),2)
    poly([(x0,y0,h),(x1,y0,h),(x1,y1,h),(x0,y1,h)],top,(167,182,184),2)

# Fixed 16:9 ground footprint and consistent floor-axis directions.
poly([(-5,-3,0),(5,-3,0),(5,3,0),(-5,3,0)],(72,82,87),(181,193,192),4)
for x in range(-5,6):line([(x,-3,0),(x,3,0)],(104,119,122),2)
for y in range(-3,4):line([(-5,y,0),(5,y,0)],(104,119,122),2)
# Short wall segments at the far edges leave the roof and camera-facing walls open.
poly([(-5,-3,0),(5,-3,0),(5,-3,2.3),(-5,-3,2.3)],(81,97,105),(169,183,186),3)
poly([(-5,-3,0),(-5,3,0),(-5,3,2.3),(-5,-3,2.3)],(68,85,95),(169,183,186),3)
box(-3.8,-1.8,-2.6,-.6,.9,(127,101,78))
box(1.6,-1.9,3.5,-.8,.85,(112,121,113))
box(.3,.4,1.8,1.5,.65,(128,110,85))
# Four equal-height posts in different ground positions should project at the same scale.
for x,y in [(-3.7,1.9),(-.9,-1.2),(2.1,1.6),(3.9,-.6)]:
    line([(x,y,0),(x,y,1.9)],(222,207,157),11)
    px,py=p(x,y,1.9);d.ellipse((px-8,py-8,px+8,py+8),fill=(247,226,176))
out.parent.mkdir(parents=True,exist_ok=True)
img.save(out)
print(out)
