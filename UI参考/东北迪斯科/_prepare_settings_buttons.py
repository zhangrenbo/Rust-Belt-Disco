"""Prepare generated button cutouts without changing their painted materials."""
from pathlib import Path
import numpy as np
from PIL import Image
from scipy import ndimage

root = Path(__file__).resolve().parent
for kind in ('reset', 'apply'):
    source = Image.open(root / f'settings-{kind}-button-source-v1.png')
    if 'A' in source.getbands() and source.getchannel('A').getextrema()[0] == 0:
        result = source.convert('RGBA')
    else:
        rgb = np.asarray(source.convert('RGB'))
        labels, _ = ndimage.label(rgb.max(axis=2) > 12)
        areas = np.bincount(labels.ravel())
        areas[0] = 0
        body = ndimage.binary_fill_holes(labels == areas.argmax())
        distance = ndimage.distance_transform_edt(body)
        alpha = np.rint(np.clip(distance / 1.4, 0, 1) * 255).astype(np.uint8)
        result = Image.fromarray(np.dstack((rgb, alpha)))
    x0, y0, x1, y1 = result.getbbox()
    result = result.crop((max(0, x0 - 4), max(0, y0 - 4),
                          min(result.width, x1 + 4), min(result.height, y1 + 4)))
    alpha = np.asarray(result.getchannel('A'))
    assert alpha[0, 0] == 0 and alpha[alpha.shape[0] // 2, alpha.shape[1] // 2] == 255
    assert (alpha == 0).sum() > 1000
    assert ((alpha > 0) & (alpha < 255)).sum() > 500
    target = root / f'settings-{kind}-button-v1.png'
    result.save(target, optimize=True)
    print({'file': target.name, 'mode': result.mode, 'size': result.size,
           'transparent': int((alpha == 0).sum()),
           'soft_edges': int(((alpha > 0) & (alpha < 255)).sum())})
