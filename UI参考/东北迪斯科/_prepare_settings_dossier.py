"""Remove only the connected black backdrop of the generated dossier assembly."""
from pathlib import Path
import numpy as np
from PIL import Image
from scipy import ndimage

root = Path(__file__).resolve().parent
source = Image.open(root / 'settings-dossier-source-v3.png').convert('RGB')
rgb = np.asarray(source)
dark = rgb.max(axis=2) <= 12
seed = np.zeros(dark.shape, dtype=bool)
seed[0] = dark[0]
seed[-1] = dark[-1]
seed[:, 0] = dark[:, 0]
seed[:, -1] = dark[:, -1]
outside = ndimage.binary_propagation(seed, mask=dark)
distance = ndimage.distance_transform_edt(~outside)
alpha = np.rint(np.clip(distance / 1.4, 0, 1) * 255).astype(np.uint8)
result = Image.fromarray(np.dstack((rgb, alpha)))
assert result.mode == 'RGBA' and alpha[0, 0] == 0
assert alpha[200:650, 400:1200].min() == 255
assert (alpha == 0).sum() > 30000
assert ((alpha > 0) & (alpha < 255)).sum() > 1000
result.save(root / 'settings-dossier-art-v3.png', optimize=True)
print({'mode': result.mode, 'size': result.size,
       'transparent': int((alpha == 0).sum()),
       'soft_edges': int(((alpha > 0) & (alpha < 255)).sum())})
