"""Remove the neutral checkerboard from the generated settings ticket."""
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage

root = Path(__file__).resolve().parent
source = Image.open(root / 'settings-ticket-textured-preview-v1.png').convert('RGB')
rgb = np.asarray(source).astype(np.int16)
warmth = rgb[:, :, 0] - rgb[:, :, 2]
foreground = (warmth > 10) | (rgb.max(axis=2) < 160)
labels, _ = ndimage.label(foreground)
sizes = np.bincount(labels.ravel())
sizes[0] = 0
body = labels == sizes.argmax()
body = ndimage.binary_fill_holes(body)

# Keep the film perforations open, while filling tiny bright paper-fiber gaps.
holes = (rgb.min(axis=2) > 198) & (np.abs(warmth) < 9)
holes[:, 315:] = False
hole_labels, count = ndimage.label(holes)
perforations = []
for label in range(1, count + 1):
    ys, xs = np.where(hole_labels == label)
    if 100 < xs.size < 700 and 55 < xs.mean() < 300 and 45 < ys.mean() < 930:
        perforations.append(label)
        body[hole_labels == label] = False
assert len(perforations) == 30, f'Expected 30 film perforations, found {len(perforations)}'

# A narrow matte transition removes checkerboard contamination at the boundary.
inside = ndimage.distance_transform_edt(body)
outside = ndimage.distance_transform_edt(~body)
alpha = np.clip((inside - outside - .35) / 1.8 + .5, 0, 1)
alpha = np.rint(alpha * 255).astype(np.uint8)
rgba = np.dstack((np.asarray(source), alpha))
image = Image.fromarray(rgba)
target = root / 'settings-ticket-textured-v1.png'

assert image.mode == 'RGBA'
assert alpha[0, 0] == 0 and alpha[72, 80] == 0
assert alpha[300:800, 400:1400].min() == 255
assert np.count_nonzero(alpha == 0) > 100000
assert np.count_nonzero((alpha > 0) & (alpha < 255)) > 1000
# Trim only transparent padding around this independent asset. No UI is cropped.
left, top, right, bottom = image.getbbox()
image = image.crop((max(0, left - 2), max(0, top - 2),
                    min(image.width, right + 2), min(image.height, bottom + 2)))
image.save(target, optimize=True)
alpha = np.asarray(image.getchannel('A'))
preview = Image.new('RGBA', image.size, '#302f2b')
preview.alpha_composite(image)
preview.convert('RGB').save(root / '_qa_settings_alpha.png')
print({'file': target.name, 'size': image.size, 'mode': image.mode,
       'transparent_pixels': int((alpha == 0).sum()),
       'edge_pixels': int(((alpha > 0) & (alpha < 255)).sum()),
       'perforations': len(perforations)})
