"""User requested extraction of the exact selected portrait, with no generated repainting."""
import hashlib,json
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]
source=ROOT/'public/assets/character-select/006_T_A000照片.png'
png=source.with_name('game-icon-dong-crop-v1.png')
ico=source.with_name('game-icon-dong-crop-v1.ico')
assert not png.exists() and not ico.exists(),'Preserve previous outputs'
before=hashlib.sha256(source.read_bytes()).hexdigest()
box=(145,175,495,525)
with Image.open(source) as original:
    crop=original.crop(box)
    assert crop.size==(350,350) and crop.getchannel('A').getextrema()==(255,255)
    crop.save(png)
    crop.save(ico,format='ICO',sizes=[(16,16),(24,24),(32,32),(48,48),(64,64),(128,128),(256,256)])
    with Image.open(png) as saved:assert saved.tobytes()==crop.tobytes()
with Image.open(ico) as encoded:sizes=sorted(encoded.ico.sizes());assert len(sizes)==7
assert hashlib.sha256(source.read_bytes()).hexdigest()==before
record={
 'date':'2026-09-15','request':'从指认的董点事选人卡原画剪出游戏icon',
 'selected_element':'button.character-choice.character-choice-center[data-character-choice="大厦崩塌"]',
 'source':str(source),'source_sha256':before,'source_size':[644,839],
 'crop_box_left_top_right_bottom':list(box),'png':str(png),'png_size':[350,350],
 'png_exact_source_pixels':True,'ico':str(ico),'ico_sizes':sizes,
 'processing':'PNG only crops existing pixels; ICO derives standard resized frames from that crop. No generation, repainting, recoloring or source edits.',
 'human_review':'pending','application_integration':'not_applied'
}
(ROOT/'docs/game-icon-dong-crop-v1.json').write_text(json.dumps(record,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'png':png.name,'size':[350,350],'ico':ico.name,'sizes':sizes,'exact_source_pixels':True}))
