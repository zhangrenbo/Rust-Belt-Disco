"""Preserve the generated panel's material and prepare its exterior alpha matte."""
from pathlib import Path
import numpy as np
from PIL import Image
from scipy import ndimage

root = Path(__file__).resolve().parent
source = Image.open(root / 'exit-dialog-panel-source-v1.png')
if 'A' in source.getbands() and source.getchannel('A').getextrema()[0] == 0:
    result = source.convert('RGBA')
else:
    rgb = np.asarray(source.convert('RGB'))
    dark = rgb.max(axis=2) <= 12
    seed = np.zeros(dark.shape, dtype=bool)
    seed[0], seed[-1] = dark[0], dark[-1]
    seed[:, 0], seed[:, -1] = dark[:, 0], dark[:, -1]
    outside = ndimage.binary_propagation(seed, mask=dark)
    alpha = np.rint(np.clip(ndimage.distance_transform_edt(~outside) / 1.4, 0, 1) * 255).astype(np.uint8)
    result = Image.fromarray(np.dstack((rgb, alpha)))
x0, y0, x1, y1 = result.getbbox()
result = result.crop((max(0, x0 - 5), max(0, y0 - 5), min(result.width, x1 + 5), min(result.height, y1 + 5)))
alpha = np.asarray(result.getchannel('A'))
assert alpha[0, 0] == 0 and (alpha == 0).sum() > 10000
assert alpha[200:700, 300:1200].min() == 255
assert ((alpha > 0) & (alpha < 255)).sum() > 1000
result.save(root / 'exit-dialog-panel-v1.png', optimize=True)
print({'size': result.size, 'mode': result.mode, 'transparent': int((alpha == 0).sum()),
       'soft_edges': int(((alpha > 0) & (alpha < 255)).sum())})
