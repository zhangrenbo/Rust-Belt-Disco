"""Build source-linked Midjourney prompts for playable scene-map gaps after ND-S01..40."""
from pathlib import Path
import json

root = Path(__file__).resolve().parents[1]
out = root / '美术概念' / 'MJ剧情场景补充20景_v1_20261018.json'
camera = ('STRICT ORTHOGRAPHIC PROJECTION, 45-DEGREE TOP-DOWN CAMERA. Camera tilted downward exactly 45 degrees, diagonal view of the scene. '
          'Parallel projection: parallel edges remain parallel, no vanishing points, no perspective foreshortening with distance; identical objects retain the same scale near and far. '
          'Atmospheric photorealistic 3D environment, Unreal Engine 5 visual quality, realistic materials and cinematic lighting. '
          'Complete playable area visible with connected paths, clear entrances and solid architectural depth. ')
finish = (' Realistic damp concrete, worn wood, corroded metal and rough brickwork as applicable, physically based surface shading, bounced light and soft contact shadows. '
          'Natural subdued winter colors, clear readable walking area. '
          '--ar 16:9 --raw --v 8.1 --relax --no perspective projection, vanishing point, wide-angle lens, painting, illustration, brushstrokes, cartoon, miniature, interface, text, watermark, extra people')

# id, map section, name, day, layout and action, light/weather. Keep navigation and shared architectural anchors legible.
items = [
 ('ND-S41','S01','叔叔家客厅·主要室内枢纽','D1–D7',
  'Roof and near wall removed from a modest working-class Northeast Chinese home living room. Worn sofa against the left wall, old CRT television opposite, family photograph of two factory-worker brothers at the rear, doorway to kitchen on the left, staircase to the upstairs bedrooms on the right, front door opening to the same small courtyard as scene ND-S09. Leave continuous walking routes between all three exits. An adult uncle in a padded jacket sits on the sofa with an enamel tea mug.',
  'Late winter afternoon, pale light through courtyard-facing windows, warm lamp beside the sofa, thin snow visible beyond the front door.'),
 ('ND-S42','S01','叔叔家厨房·灶台与院门','D1–D7',
  'Cutaway of the small kitchen joined to the uncle home living room. White lower ceramic wall tiles, coal stove, dented aluminum pots, enamel basin, narrow dining counter, open doorway at right to the living room and service door at front left to the courtyard from scene ND-S09. One aunt in a thick cardigan carries a pot toward the stove. Keep both doors and the floor passage clear.',
  'Cold winter morning through the courtyard door, warm stove and ceiling bulb, snow just beyond the threshold.'),
 ('ND-S43','S01','叔叔家阁楼·母亲遗物','D2',
  'Small dusty attic above the uncle family house, accessible from a narrow stair at front right. Low sloping rafters, two stacked trunks, cloth-wrapped cardboard boxes, an old factory-worker suitcase, family photographs and a folded coat laid on a wooden crate. No legible written text. One young adult man in a dark padded coat kneels beside an open box. Preserve access to the stair and a small rear window.',
  'Late winter daylight enters the little window, faint dust in the beam, snow on the roof outside, no theatrical spotlight.'),
 ('ND-S44','S02','省道修车店外空地·通往叔叔家','D1–D7',
  'Exterior of the corrugated metal roadside tire repair shed from scene ND-S08. Open work bay at left, stacked truck tires and a small sign frame by the entrance, gravel forecourt with one parked motorcycle, two-lane provincial road beyond, footpath toward the uncle family courtyard at right. Keep the connection between road, shed, and house legible. One bundled mechanic rolls a tire across the forecourt.',
  'Overcast winter daytime, thin snow along road edges, wet tire tracks and grey slush, clear near-ground navigation.'),
 ('ND-S45','S05','公安局大厅·接待入口','D1–D7',
  'Roofless cutaway of the entrance lobby of an older three-storey Chinese small-town police station. Glass front door at front, plain reception counter left, benches and notice boards along the right wall, central corridor leading toward the office area of scene ND-S11, stairs at back right. No readable text or emblems. One bundled visitor speaks with a desk officer. Open walking route from the front door to corridor and stair.',
  'Cold winter daylight at the entrance, ordinary fluorescent ceiling light, damp boot marks near the door.'),
 ('ND-S46','S05','公安局会议室·案情通报','D2',
  'Police station meeting room beside the corridor from scene ND-S11. Long rectangular table with simple chairs, folders and enamel mugs, district map pinned on rear wall without readable labels, plain whiteboard at left without legible text, door at front right back to the corridor. Three adult Chinese officers in winter work clothes sit spaced along the table discussing files, faces secondary to the spatial layout.',
  'Overcast winter daylight through two windows, cool fluorescent light and quiet grey-green walls.'),
 ('ND-S47','S05','公安局楼下停车场·私下会面','D2–D6',
  'Small outdoor parking court directly below the old police station from scenes ND-S11 and ND-S45. Rear building entrance, short concrete stair, three worn parked cars and one motorcycle, fence and gate to town street. Two adult men in dark winter coats speak beside the farthest car while keeping a clear path between stair, gate and corridor entrance.',
  'Winter evening after light snow, sodium streetlamp reflected in damp asphalt, distant town fog restrained.'),
 ('ND-S48','S06','李正家客厅与厨房·家庭日常','D1–D7',
  'Cutaway of the modest apartment connected to the bedroom from scene ND-S13. Small living room with worn sofa, low tea table, family photograph and coat rack; tiny practical kitchen through an open doorway at left with breakfast table, kettle and enamel bowls; bedroom doorway at rear right and entrance at front. One fully clothed adult woman prepares tea in the kitchen, one adult man hangs a winter coat near the entrance.',
  'Pale winter morning window light with one warm kitchen bulb, no dream or apparition.'),
 ('ND-S49','S07','镇中心十字路口·空间枢纽','D1–D7',
  'Orthographic view of the main Northeast Chinese small-town crossroads connecting four walkable streets. Worn two-storey shops, bus stop direction on one branch, clinic direction on another, road toward police station, and pedestrian lane toward the burned family shop. Traffic light, patched asphalt, snowbanks kept off the walking routes. Two bundled pedestrians cross at different corners, no crowd.',
  'Late winter overcast daylight, sparse snow, wet road, clear silhouettes and navigable sight lines.'),
 ('ND-S50','S07','供销社·主街旁旧商店','D1–D7',
  'Exterior and shallow cutaway of an old supply cooperative store on the same town main street as scenes ND-S18 and ND-S19. Faded sign area without readable characters, broad shop doorway, old wooden counter and shelves just inside, small alley at right linking back to the crossroads. One bundled shopkeeper stands behind the counter, one passerby approaches from the street. Keep both shop entry and alley open.',
  'Clear winter daytime before the later heavy fog, dry snow along curb, cool natural light with restrained warm interior bulbs.'),
 ('ND-S51','S09','东山坟地·旧墓区','D2–D7',
  'Older section of the East Hill cemetery adjoining the father grave area from scene ND-S20. Uneven stone steps enter from the front, weathered small graves among leafless trees, low retaining wall and a distant view down toward the newer graves. No readable inscriptions. One adult visitor in a padded coat walks between graves with hands at sides. Keep a visible path back to the new grave section.',
  'Winter afternoon, snow caught on stone edges, low grey sky and quiet diffuse light.'),
 ('ND-S52','S10','职高校内教学楼·通往天台','D2–D5',
  'Cutaway of a modest Chinese vocational school inner courtyard and ground-floor corridor, beyond the locked gate from scene ND-S28. Classroom doors on the left, stairwell toward roof access at rear, plain workshop doorway on the right, fence and gate visible toward the front. Two fully clothed students in winter jackets cross the courtyard. Clear path from gate to stair and classroom corridor.',
  'Cloudy winter daytime, patches of snow along courtyard walls, ordinary fluorescent light inside corridor.'),
 ('ND-S53','S11','县医院走廊·烧伤科门外','D2–D4',
  'Long older county hospital corridor leading to the burn ward from scene ND-S23. Reception counter at near left, metal bench on the right, closed ward door at the far end with a plain guard chair beside it, stair door at front right. No readable signs. One seated attendant in winter uniform watches the corridor while one adult visitor carrying a coat waits near the bench. Keep route to ward and stair legible.',
  'Cold winter daylight through high windows, fluorescent ceiling tubes, clean but worn cream and green surfaces.'),
 ('ND-S54','S13','婚礼饭店门口·入场','D3',
  'Exterior entrance to a modest small-town wedding restaurant, leading directly into the banquet hall from scene ND-S30. Shallow steps, plain glass doors, cloth wedding decorations without readable writing, courtyard parking at left and a side passage at right toward the service rooms. A small group of bundled adult guests enters; no crowd blocking doors or side passage.',
  'Winter afternoon with thickening fog and snow piled beside the steps, warm indoor light visible through glass.'),
 ('ND-S55','S13','婚礼后屋·奶奶所在房间','D3',
  'Quiet back room behind the wedding banquet hall, reachable by a service passage from scene ND-S30. Single bed against left wall, wooden chair and small bedside table, enamel basin, folded coat, little rear window, fabric curtain at the doorway. An elderly woman in a thick cardigan rests seated on the bed while one adult man in a winter shirt stands by the chair. Doorway and space around the bed clear; no medical apparatus or spectacle.',
  'Muted winter daylight through the small window, distant warm banquet light from the passage, gentle shadows.'),
 ('ND-S56','S14','便利店旁胡同深处·夜间调查','D3',
  'Deep end of the narrow brick alley connecting to the burned convenience shop exterior from scenes ND-S04 and ND-S32. Shop wall and rear service doorway at left, boundary wall at right, old utility pole and drainage channel leading to the back, dead-end fence far away. Empty of people, no violence or victim. Maintain a visible walkable corridor from the street entry to the far end.',
  'Late winter night, dense drifting fog, patchy snow and wet brick, one distant streetlamp with restrained light.'),
 ('ND-S57','S16','镇东河边空地·D7水位','D7',
  'Open ground at the east edge of the town beside the swollen river, connected to the main street by one raised muddy path. Wide bank, low embankment, leafless trees and funeral area position from scene ND-S39 visible at a distance but without a duplicate canopy. Waterline clearly high against the embankment; broad free space for player movement. Empty of people.',
  'Cold heavy night rain after fog clears, wind-ruffled river water, pale storm cloud and restrained town lights, no snow falling.'),
 ('ND-S58','S17','刘小红母亲家门口·街巷入口','D4–D5',
  'Exterior of the modest east-town home whose interior appears in scene ND-S34. Narrow brick lane at front, worn gate, two entrance steps, low wall, small window with curtains and a side route back toward the main street. One middle-aged woman in a padded winter coat stands at the half-open door speaking to one adult visitor outside. Keep the gate and lane passable.',
  'Winter late afternoon with dense fog between buildings, damp stone steps and snow remnants by wall base.'),
 ('ND-S59','S04','王氏中医馆门面·前后入口','D1–D7',
  'Exterior of the small traditional medicine clinic whose front room is scene ND-S14 and back room is scene ND-S15. Street-facing wooden shop door, front windows revealing medicine drawers, narrow side passage to a rear service door, upstairs apartment window above. Adjacent low shops connect it to the town main street. One adult patient in a padded coat approaches the front door. Keep front entry and side passage distinct.',
  'Overcast winter daytime, thin snow along curb, modest warm interior light behind glass.'),
 ('ND-S60','S07','D6主街·无火无信的空镇','D6',
  'The same Northeast town main street footprint as scenes ND-S18 and ND-S19, with the cooperative storefront, clinic turnoff and crossroads aligned consistently. No pedestrians or traffic. Bare trees, closed shutters, uncollected snow along curbs, clear central walking route. A single empty chair near a storefront, no supernatural figure.',
  'Very late winter dusk, fog thinning after several dense days, cold blue-grey light and a few ordinary streetlamps.'),
]

scenes=[]
for sid, map_id, name, day, layout, light in items:
    assert len(layout)>100 and len(light)>50
    scenes.append({'id':sid,'map_id':map_id,'name':name,'day':day,'source':'content/story/source-documents/场景地图.md',
                   'prompt':camera+layout+' '+light+finish,'status':'planned','job_id':None,'variants':[],
                   'grouped':False,'human_review':'pending'})
assert len(scenes)==20 and len({s['id'] for s in scenes})==20
data={'schema':'northeast-disco-mj-story-scenes-gap-v1','created':'2026-10-18',
      'basis':'content/story/source-documents/场景地图.md and 美术概念/MJ剧情场景40景雪夜_v1_20260917.json',
      'direction':'winter orthographic 45 degree photorealistic 3D UE5 quality; D7 cold rain',
      'folder':{'id':'d1ff8d95-e403-45ba-8f03-6991d740a0ce','name':'东北迪斯科参考图'},
      'params':{'ar':'16:9','raw':True,'v':'8.1','relax':True,'personalization':False,'style_references':[]},
      'scenes':scenes}
if out.exists():
    old=json.loads(out.read_text(encoding='utf-8'))
    prior={s['id']:s for s in old['scenes']}
    assert set(prior)=={s['id'] for s in scenes}, 'existing batch differs; review before rebuilding'
    for scene in scenes:
        previous=prior[scene['id']]
        assert scene['prompt']==previous['prompt'], f"prompt changed for {scene['id']}; review before rebuilding"
    print(out, len(scenes), 'existing submission record preserved')
    raise SystemExit(0)
out.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(out, len(scenes))
