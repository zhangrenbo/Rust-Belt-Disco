"""Rewrite the 40-scene MJ design prompts to the A000 v8 direction:
snowy winter throughout, atmospheric photorealistic UE5 style, strict
orthographic 45-degree top-down camera, 16:9. Never submits MJ jobs."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / '美术概念' / 'MJ剧情场景设计_首轮40景_20260909.json'
OUT_JSON = ROOT / '美术概念' / 'MJ剧情场景40景雪夜_v1_20260917.json'
OUT_MD = ROOT / '美术概念' / 'MJ剧情场景40景雪夜_v1_20260917.md'
V8_RECORD = '美术概念/MJ_A000正交45度写实雪景_v8_20260916_生成记录.json'

CAMERA = {
    'interior': ('STRICT ORTHOGRAPHIC PROJECTION, 45-DEGREE TOP-DOWN CAMERA. '
                 'Camera tilted downward exactly 45 degrees, diagonal view of the scene. '
                 'Parallel projection: parallel edges remain parallel, no vanishing points, '
                 'no perspective foreshortening with distance; identical objects retain the same scale near and far. '
                 'Atmospheric photorealistic 3D environment, Unreal Engine 5 visual quality, '
                 'realistic materials and cinematic lighting. '
                 'Roof and camera-facing walls removed, complete room footprint and doorways visible.'),
    'exterior': ('STRICT ORTHOGRAPHIC PROJECTION, 45-DEGREE TOP-DOWN CAMERA. '
                 'Camera tilted downward exactly 45 degrees, diagonal view of the scene. '
                 'Parallel projection: parallel edges remain parallel, no vanishing points, '
                 'no perspective foreshortening with distance; identical objects retain the same scale near and far. '
                 'Atmospheric photorealistic 3D environment, Unreal Engine 5 visual quality, '
                 'realistic materials and cinematic lighting. '
                 'Complete playable area visible with connected paths, clear entrances and solid architectural depth.'),
}
TAIL = {
    'interior': ('Realistic worn wood, aged plaster, damp concrete and rough brickwork, '
                 'physically based surface shading, bounced light, soft contact shadows, '
                 'subtle volumetric light, strong three-dimensional depth. '
                 'Natural subdued winter colors, quiet, cold, immersive atmosphere.'),
    'exterior': ('Realistic snow crystals, damp concrete, corroded metal and rough brickwork, '
                 'physically based surface shading, bounced light, soft contact shadows, '
                 'subtle volumetric fog, strong three-dimensional depth. '
                 'Natural subdued winter colors, quiet, cold, immersive atmosphere.'),
}
NEG = '--no perspective projection, vanishing point, wide-angle lens, painting, illustration, brushstrokes, cartoon, miniature, interface, text, watermark, extra people'

# id -> {field: [(old, new), ...]} every old string must occur exactly once in the field.
PATCHES = {
 'ND-S01': {
  'layout': [
    ('A small Northeast Chinese town bus stop beside a two-lane road. Corrugated metal shelter, two concrete benches, rusted stop pole, faded timetable board, dusty paving and summer weeds.',
     'A small Northeast Chinese town bus stop beside a two-lane road at night. Corrugated metal shelter with snow on the roof, two concrete benches, rusted stop pole, faded timetable board, snow-dusted paving.'),
    ('A worn bus pulls away at the far edge.',
     'A worn bus pulls away at the far edge, its tire tracks crossing the snow.'),
  ],
  'action': [
    ('One young Chinese man with a small travel bag pauses by the bench; one elderly Chinese woman with a black walking cane sits watching him, her hand slightly extended toward him.',
     'One young Chinese man in a dark padded winter jacket with a small travel bag pauses by the bench; one elderly Chinese woman in a thick winter coat with a black walking cane sits watching him, her hand slightly extended toward him.'),
  ],
  'light': [('Early summer morning with thin pale mist in the distance, sparse warm tones in the shelter; foreground people, bus stop and route remain clear.',
             'Snowy winter night with thin mist in the distance, light snow drifting through the warm streetlamp glow; foreground people, bus stop and route remain clear.')],
 },
 'ND-S02': {
  'layout': [
    ('Charred plaster, ash and glass on the floor, a loosened floorboard at the opposite corner. Preserve the main aisle and all walls in readable solid 3D.',
     'Charred plaster, ash and glass on the floor, a loosened floorboard at the opposite corner, thin snow dusted on the broken windowsill edges. Preserve the main aisle and all walls in a complete readable footprint.'),
  ],
  'action': [
    ('One young Chinese man crouches beside a scorched shelf support, looking closely at the floor with one hand braced on his knee.',
     'One young Chinese man in a padded winter jacket crouches beside a scorched shelf support, looking closely at the floor with one hand braced on his knee.'),
  ],
  'light': [('Pale daylight from the shopfront and damaged ceiling, cool shadows, small amber highlights, no fog.',
             'Pale winter daylight from the shopfront and damaged ceiling, cool shadows, small amber highlights, no fog.')],
 },
 'ND-S03': {
  'light': [('Soft afternoon light through the partly covered window, dusty muted blue shadows and warm paper tones.',
             'Soft afternoon winter light through the partly covered window, thin snow on the outer sill, dusty muted blue shadows and warm paper tones.')],
 },
 'ND-S04': {
  'layout': [
    ('Neighboring residential door to the left, vocational school gate glimpsed across the street, cracked paving and summer weeds.',
     'Neighboring residential door to the left, vocational school gate glimpsed across the street, cracked paving with a thin layer of snow.'),
  ],
  'action': [
    ('One elderly Chinese neighbor carrying a vegetable basket speaks from her doorway to one young man standing outside the shop.',
     'One elderly Chinese neighbor in a padded coat carrying a vegetable basket speaks from her doorway to one young man in a winter jacket standing outside the shop.'),
  ],
  'light': [('Clear overcast daylight, cool grey brick, subdued warm clothing accents, no fog.',
             'Clear overcast winter daylight, snow on the steps and alley ground, cool grey brick, subdued warm clothing accents, no fog.')],
 },
 'ND-S05': {
  'layout': [
    ('Entrance walkway at front right; preserve open ground between chairs and coffin.',
     'A swept entrance walkway at front right; thin snow covers the open lot, preserve clear ground between chairs and coffin.'),
  ],
  'action': [
    ('A small suona band plays on the left, a middle-aged relative receives two arriving mourners by the entry, a young Chinese man stands quietly near the chairs.',
     'A small suona band in thick winter coats plays on the left, a middle-aged relative receives two arriving mourners by the entry, a young Chinese man in a black padded jacket stands quietly near the chairs.'),
  ],
  'light': [('Flat overcast daytime light, pale cloth and muted earth, restrained warm brass highlights, no fog.',
             'Flat overcast winter daytime light, pale cloth and snow-dusted earth, restrained warm brass highlights, no fog.')],
 },
 'ND-S06': {
  'light': [('One warm ceiling bulb at night, amber table island, deep muted teal corners, no supernatural effects.',
             'One warm ceiling bulb on a winter night, amber table island, deep muted teal corners, faint snow drifting past the window, no supernatural effects.')],
 },
 'ND-S07': {
  'light': [('Warm bedside lamp and cool night window light, quiet intimate painted shadows.',
             'Warm bedside lamp and cool winter night window light, thin snow on the outer sill, quiet intimate shadows.')],
 },
 'ND-S08': {
  'layout': [
    ('oil-stained concrete and practical worn tools.',
     'oil-stained concrete, snow piled along the open front edge, and practical worn tools.'),
  ],
  'action': [
    ('A middle-aged mechanic uncle and a young Chinese man lift the same heavy truck tire together, both gripping its sides with bent knees.',
     'A middle-aged mechanic uncle and a young Chinese man in padded work jackets lift the same heavy truck tire together, both gripping its sides with bent knees.'),
  ],
  'light': [('Summer morning daylight through the open front, warm skin and rust accents against cool steel.',
             'Winter morning daylight through the open front, cold blue air mixing with warm skin and rust accents against cool steel.')],
 },
 'ND-S09': {
  'layout': [
    ('Laundry line, washbasin, aluminum pots, one dry potted plant and two low stools. Continuous empty walking space links gate, kitchen and house.',
     'Laundry line, washbasin, aluminum pots, one dry potted plant and two low stools; snow swept into the courtyard corners. Continuous empty walking space links gate, kitchen and house.'),
  ],
  'action': [
    ('An aunt carries an enamel basin from the kitchen while the mechanic uncle washes his hands at the courtyard tap.',
     'An aunt in a thick wool coat carries an enamel basin from the kitchen while the mechanic uncle in a padded jacket washes his hands at the courtyard tap.'),
  ],
  'light': [('Soft summer afternoon daylight, white tile and grey brick with restrained warm highlights.',
             'Soft winter afternoon daylight, snow on the rooftops, white tile and grey brick with restrained warm highlights.')],
 },
 'ND-S10': {
  'light': [('Gentle morning window light, muted blue-green room with soft warm skin tones.',
             'Gentle winter morning window light, pale snow outside the window, muted blue-green room with soft warm skin tones.')],
 },
 'ND-S11': {
  'light': [('Daylight from office windows mixed with cool fluorescent tubes, restrained amber desk-lamp accents.',
             'Winter daylight from office windows mixed with cool fluorescent tubes, snow visible on the outer sills, restrained amber desk-lamp accents.')],
 },
 'ND-S12': {
  'light': [('Late evening desk-lamp pool over warm paper, cool green-grey metal receding into shadow.',
             'Late winter evening desk-lamp pool over warm paper, cool green-grey metal receding into shadow, frost at the small high window.')],
 },
 'ND-S13': {
  'light': [('Pale early morning window light, muted teal shadows and warm domestic wood, no dream apparition.',
             'Pale early winter morning window light, muted teal shadows and warm domestic wood, no dream apparition.')],
 },
 'ND-S14': {
  'light': [('Afternoon shopfront daylight, warm wooden drawers, pale green shadows behind the curtain.',
             'Winter afternoon shopfront daylight, light snow on the street outside the front, warm wooden drawers, pale green shadows behind the curtain.')],
 },
 'ND-S15': {
  'light': [('Night, soft green monitor light balanced with a small warm bedside bulb, deep quiet blue-green shadows.',
             'Winter night, soft green monitor light balanced with a small warm bedside bulb, deep quiet blue-green shadows, faint snow beyond the curtain-drawn window.')],
 },
 'ND-S16': {
  'light': [('Soft morning window light, faded wedding reds against muted teal plaster and warm wood.',
             'Soft winter morning window light, light snow on the sill, faded wedding reds against muted teal plaster and warm wood.')],
 },
 'ND-S17': {
  'light': [('Warm midday window light and amber cooking-area glow, soft steam with clearly readable room edges.',
             'Warm midday window light and amber cooking-area glow, soft steam with clearly defined room edges, snow on the street through the front window.')],
 },
 'ND-S18': {
  'layout': [
    ('A continuous stretch of main street in a declining Northeast Chinese industrial town in July.',
     'A continuous stretch of main street in a declining Northeast Chinese industrial town in winter.'),
    ('Cracked concrete road runs diagonally through the frame, two-storey red-brick and grey plaster shopfronts on both sides, utility poles with sagging wires.',
     'Cracked concrete road with packed snow and tire tracks runs diagonally through the frame, two-storey red-brick and grey plaster shopfronts with snow on the roofs and ledges, utility poles with sagging wires.'),
    ('Noodle shop and repair entrance near a large locust tree junction, shuttered shops farther along, distant factory chimney.',
     'Noodle shop and repair entrance near a large bare locust tree junction, shuttered shops farther along, distant factory chimney.'),
  ],
  'action': [
    ('A few elderly residents sit on stools by their doors, a young man walks along the sidewalk, one shopkeeper lifts a roller shutter.',
     'A few elderly residents in thick coats sit on stools by their doors, a young man in a padded jacket walks along the snow-cleared sidewalk, one shopkeeper lifts a roller shutter.'),
  ],
  'light': [('Overcast summer daylight, dusty green foliage, faded blue-grey walls and restrained warm shop windows.',
             'Overcast winter daylight, pale snow cover, bare trees, faded blue-grey walls and restrained warm shop windows.')],
 },
 'ND-S19': {
  'layout': [
    ('Moist road surface catches amber streetlights.',
     'Snow-dusted road surface catches amber streetlights.'),
  ],
  'action': [
    ('One adult Chinese figure walks home along the near sidewalk carrying a small bag.',
     'One adult Chinese figure in a long winter coat walks home along the near sidewalk carrying a small bag.'),
  ],
  'light': [('Night with warm sodium streetlight pools fading into cool blue-green fog, readable foreground geometry.',
             'Winter night with warm sodium streetlight pools fading into cool blue-green fog, thin snow on the rooflines, readable foreground geometry.')],
 },
 'ND-S20': {
  'layout': [
    ('Older weathered graves continue uphill among summer grass; low retaining stones and scattered trees, distant factory rooftops below.',
     'Older weathered graves continue uphill among snow-dusted winter grass; low retaining stones and scattered bare trees, distant factory rooftops below.'),
  ],
  'action': [
    ('One young Chinese man crouches at the fresh grave, setting down a small bunch of flowers with both hands.',
     'One young Chinese man in a dark padded coat crouches at the fresh grave, setting down a small bunch of flowers with both hands.'),
  ],
  'light': [('Soft overcast afternoon light, subdued green grass and warm earth, distant blue-grey industrial skyline.',
             'Soft overcast winter afternoon light, snow-dusted grass and warm earth, distant blue-grey industrial skyline.')],
 },
 'ND-S21': {
  'layout': [
    ('Beyond the locked gate only factory rooflines, pipes and a tall smoking chimney are visible. Summer weeds along the wall, faded notice board without legible text.',
     'Beyond the locked gate only snow-capped factory rooflines, pipes and a tall smoking chimney are visible. Snow along the wall base, faded notice board without legible text.'),
  ],
  'action': [
    ('One young Chinese man with a shoulder bag stands on the public pavement looking through the gate; a guard is visible inside the booth.',
     'One young Chinese man in a padded jacket with a shoulder bag stands on the public pavement looking through the gate; a guard is visible inside the booth.'),
  ],
  'light': [('Cloudy summer afternoon, pale chimney smoke against grey sky, rusty steel and muted green weeds.',
             'Cloudy winter afternoon, pale chimney smoke against grey sky, rusty steel and snow along the wall.')],
 },
 'ND-S22': {
  'layout': [
    ('Police cordon spans the front yard, access lane enters from front left, neighboring intact house at right.',
     'Police cordon spans the front yard, access lane enters from front left, neighboring intact house with snow on its roof at right.'),
  ],
  'action': [
    ('Two adult investigators examine the doorway from outside the cordon while a resident waits farther back.',
     'Two adult investigators in winter coats examine the doorway from outside the cordon while a resident waits farther back.'),
  ],
  'light': [('Grey morning light, damp ground and thin distant summer fog, restrained warm brick under cold soot.',
             'Grey winter morning light, damp ground with melting snow and thin distant fog, restrained warm brick under cold soot.')],
 },
 'ND-S23': {
  'light': [('Overcast window light mixed with cool fluorescent ceiling light, a small warm patch on the bedside wood.',
             'Overcast winter window light with snow on the sill mixed with cool fluorescent ceiling light, a small warm patch on the bedside wood.')],
 },
 'ND-S24': {
  'light': [('Flat grey daytime light, silent cool fluorescent interior with a small warm reflected window edge.',
             'Flat grey winter daytime light, silent cool fluorescent interior with a small warm reflected window edge.')],
 },
 'ND-S25': {
  'light': [('Soft afternoon window light, warm aged wood, muted teal plaster and gentle shadow.',
             'Soft winter afternoon window light, light snow outside the window, warm aged wood, muted teal plaster and gentle shadow.')],
 },
 'ND-S26': {
  'layout': [
    ('A riverside embankment beside a small Northeast Chinese industrial town in July.',
     'A riverside embankment beside a small Northeast Chinese industrial town in winter.'),
    ('Raised dirt walking path runs diagonally, sloped concrete bank descends to visibly high muddy water close to the lower steps.',
     'Raised dirt walking path with a swept snow strip runs diagonally, sloped concrete bank descends to visibly high dark water close to the lower steps.'),
    ('Short stone stair connects the upper road to a safe fishing platform, willow branches at rear, distant low factory silhouettes.',
     'Short stone stair connects the upper road to a safe fishing platform, bare willow branches at rear, distant low factory silhouettes.'),
  ],
  'action': [
    ('One elderly fisherman sits well back from the waters edge with his rod, while a young man pauses beside him on the upper path.',
     'One elderly fisherman in a thick coat and fur cap sits well back from the waters edge with his rod, while a young man in a padded jacket pauses beside him on the upper path.'),
  ],
  'light': [('Cool overcast afternoon with low fog over distant water, green summer foliage and faint amber reflections.',
             'Cool overcast winter afternoon with low fog over distant water, snow on the banks and faint amber reflections.')],
 },
 'ND-S27': {
  'layout': [
    ('A large old locust tree at a Northeast Chinese town street junction, summer leaves overhead.',
     'A large old locust tree with bare snow-dusted branches at a Northeast Chinese town street junction.'),
    ('Low circular brick tree surround with a worn wooden chair beside it, cracked pavement and two narrow streets receding behind.',
     'Low circular brick tree surround with a worn wooden chair beside it, cracked pavement with thin snow and two narrow streets receding behind.'),
  ],
  'action': [
    ('An elderly Chinese woman sits on the chair with a black walking cane across her knees, raising one hand while speaking to a standing adult Chinese man in modest clinic clothes.',
     'An elderly Chinese woman in a long padded coat sits on the chair with a black walking cane across her knees, raising one hand while speaking to a standing adult Chinese man in a dark winter jacket.'),
  ],
  'light': [('Summer dusk, amber lamp just switched on, cool teal fog beginning beyond the junction while foreground remains clear.',
             'Winter dusk, amber lamp just switched on, light snow falling in the lamp glow, cool teal fog beginning beyond the junction while foreground remains clear.')],
 },
 'ND-S28': {
  'layout': [
    ('Behind the gate a worn basketball court and plain school block are visible, weeds along the fence.',
     'Behind the gate a worn basketball court and plain school block are visible, snow-dusted weeds along the fence.'),
  ],
  'action': [
    ('An elderly guard removes and wipes his reading glasses while a young adult Chinese visitor stands outside the window listening.',
     'An elderly guard in a thick sweater removes and wipes his reading glasses while a young adult Chinese visitor in a padded jacket stands outside the window listening.'),
  ],
  'light': [('Overcast summer afternoon with soft fog inside the distant campus, dusty green weeds, muted red brick and warm light from the guard room.',
             'Overcast winter afternoon with soft fog inside the distant campus, snow-dusted weeds, muted red brick and warm light from the guard room.')],
 },
 'ND-S29': {
  'layout': [
    ('Full rooftop footprint visible, neighboring school roofs and overgrown court beyond.',
     'Full rooftop footprint visible with thin snow patches, neighboring school roofs and overgrown court beyond.'),
  ],
  'light': [('Cloudy summer daylight with distant blue-grey haze, faded warm concrete and muted green courtyard below.',
             'Cloudy winter daylight with distant blue-grey haze, snow patches on the concrete and muted green courtyard below.')],
 },
 'ND-S30': {
  'light': [('Warm overhead bulbs mixed with grey summer window light, restrained red accents over muted teal shadows.',
             'Warm overhead bulbs mixed with grey winter window light, restrained red accents over muted teal shadows.')],
 },
 'ND-S32': {
  'layout': [
    ('Alley opens visibly onto the main street in front and a small rear yard beyond, continuous clear walking strip between them.',
     'Alley opens visibly onto the main street in front and a small rear yard beyond, continuous clear walking strip between them, thin snow on the ground with a few footprints.'),
  ],
  'light': [('Night, a single amber streetlamp at the alley mouth, dim blue-green fog farther down the lane, wet concrete catches a narrow reflection.',
             'Winter night, a single amber streetlamp at the alley mouth, dim blue-green fog farther down the lane, light snow falling in the lamp glow, snow-dusted ground catches a narrow reflection.')],
 },
 'ND-S33': {
  'light': [('Cloudy afternoon shopfront light, warm imitation wood furniture, cool teal wall shadows.',
             'Cloudy winter afternoon shopfront light with snow outside the front glass, warm imitation wood furniture, cool teal wall shadows.')],
 },
 'ND-S34': {
  'light': [('Soft late-afternoon window light, warm domestic wood and faded teal fabric, no fog indoors.',
             'Soft late-afternoon winter window light, warm domestic wood and faded teal fabric, no fog indoors.')],
 },
 'ND-S35': {
  'layout': [
    ('Open concrete apron connects to the road at front; maintain a clear side exit beside the shed.',
     'Open concrete apron with snow piled at its edges connects to the road at front; maintain a clear side exit beside the shed.'),
  ],
  'action': [
    ('Two small groups of fully clothed adult men face each other several steps apart on the apron, one older mechanic holds out a hand between them.',
     'Two small groups of adult men in thick winter coats face each other several steps apart on the apron, one older mechanic holds out a hand between them.'),
  ],
  'light': [('Heavy overcast summer afternoon, dusty amber concrete and cool grey-green shadows beneath the canopy.',
             'Heavy overcast winter afternoon, dusty amber concrete and cool grey-green shadows beneath the canopy.')],
 },
 'ND-S36': {
  'light': [('Dim afternoon light filtered through closed blinds across the desk, warm wood and muted teal steel cabinets.',
             'Dim winter afternoon light filtered through closed blinds across the desk, warm wood and muted teal steel cabinets.')],
 },
 'ND-S37': {
  'layout': [
    ('A small enclosed Northeast Chinese family courtyard in summer.',
     'A small enclosed Northeast Chinese family courtyard in winter.'),
    ('Keep a clear path from gate to steps and the same compact rectangular yard footprint.',
     'Keep a clear swept path from gate to steps and the same compact rectangular yard footprint.'),
  ],
  'action': [
    ('An older Chinese uncle and his adult nephew sit side by side on the stone steps; the uncle passes a small faded photograph of a factory worker to the younger man.',
     'An older Chinese uncle and his adult nephew, both in padded coats, sit side by side on the stone steps; the uncle passes a small faded photograph of a factory worker to the younger man.'),
  ],
  'light': [('Quiet summer night with pale moonlight in the yard and warm kitchen window light on their hands, subdued teal shadows.',
             'Quiet winter night with pale moonlight on the snow-dusted yard and warm kitchen window light on their hands, subdued teal shadows.')],
 },
 'ND-S38': {
  'layout': [
    ('The chair is now empty, a black walking cane rests against it and a plain folded cloth lies on its seat. Foreground walking route stays open, summer leaves hang above.',
     'The chair is now empty, a black walking cane rests against it and a plain folded cloth lies on its seat. Foreground walking route stays open, bare snow-dusted branches hang above.'),
  ],
  'action': [
    ('Two adult townspeople stand a few steps away speaking quietly while looking toward the empty chair.',
     'Two adult townspeople in winter coats stand a few steps away speaking quietly while looking toward the empty chair.'),
  ],
  'light': [('Late summer dusk, one warm lamp pool against cool blue-green mist beyond the crossroads.',
             'Late winter dusk, one warm lamp pool against cool blue-green mist beyond the crossroads, faint snow in the lamp glow.')],
 },
 'ND-S39': {
  'layout': [
    ('Folding stools arranged in sparse rows with an open central aisle, dirt road at front and distant hills clearly visible behind.',
     'Folding stools arranged in sparse rows with an open central aisle, dirt road at front, thin snow patches on the open lot, distant hills clearly visible behind.'),
  ],
  'action': [
    ('Small groups of fully clothed adult mourners stand apart under the canopy; one young man pauses in the central aisle holding white flowers.',
     'Small groups of adult mourners in dark winter coats stand apart under the canopy; one young man pauses in the central aisle holding white flowers.'),
  ],
  'light': [('Humid July morning under heavy low clouds, clear air without fog, dry ground before the rain, muted warm earth and blue-grey sky.',
             'Cold overcast winter morning under heavy low clouds, clear air without fog, dry ground before the rain, muted warm earth and blue-grey sky.')],
 },
 'ND-S40': {
  'action': [
    ('Three fully clothed adult Chinese men stand several steps apart: two near the entrance and central aisle face a third behind the old counter.',
     'Three adult Chinese men in dark winter coats stand several steps apart: two near the entrance and central aisle face a third behind the old counter.'),
  ],
  'light': [('Heavy July night rain falls directly into the roofless ruins, no fog, puddles reflect cool blue-green street light, one restrained warm flashlight beam crosses the counter.',
             'Heavy cold winter night rain falls directly into the roofless ruins, no fog, puddles reflect cool blue-green street light, one restrained warm flashlight beam crosses the counter.')],
 },
}


def main():
    data = json.loads(SRC.read_text(encoding='utf-8'))
    errors = []
    rewrites = []
    for s in data['scenes']:
        sid = s['id']
        changes = []
        for field in ('layout', 'action', 'light'):
            for old, new in PATCHES.get(sid, {}).get(field, []):
                if s[field].count(old) != 1:
                    errors.append(f"{sid} {field}: expected exactly 1 occurrence, found {s[field].count(old)}: {old[:60]!r}")
                    continue
                s[field] = s[field].replace(old, new)
                changes.append((field, old[:48], new[:48]))
        kind = s['kind']
        old_prompt = s['prompt']
        s['prompt'] = ' '.join([CAMERA[kind], s['layout'], s['action'], s['light'],
                                TAIL[kind], '--ar 16:9 --raw --v 8.1 --relax', NEG])
        rewrites.append(dict(id=sid, name=s['name'], changes=changes,
                             old_prompt=old_prompt, new_prompt=s['prompt']))
        s['prompt_v8_direction'] = '2026-09-17 user selected: v8 style (photoreal UE5, strict orthographic 45, snowy winter)'
        s['ar'] = '16:9'
        s['status'] = 'prepared'
        s['job_id'] = None
        s['submitted_at'] = None
        s['submit_receipt'] = None
        s['grouped'] = False
    if errors:
        raise ValueError('\n'.join(errors))
    out = dict(
        schema='northeast-disco-mj-story-scenes-snow-v1',
        created='2026-09-17',
        based_on='美术概念/MJ剧情场景设计_首轮40景_20260909.json',
        direction='user_selected_20260917',
        direction_notes=[
            '全部40景分批提交（D0→D7）。',
            '按A000 v8方向重写：3D写实、UE5质感、氛围光、严格正交45°俯视（无透视、无灭点、同尺寸等比例）。',
            '全游戏统一冬季雪景；各场景保留原文各自的时段与天气（如D7暴雨保留为冷雨）。',
            '画幅16:9；Raw、V8.1、Relax；无sref；个性化关闭。',
            '原文档的油画笔触、青橙配色与夏季设定已被用户否定，不得回退。',
        ],
        style_basis=dict(
            v8_record=V8_RECORD,
            v8_job_id='e7c933c9-2604-4d6f-bb34-d051959e27bf',
            camera='STRICT ORTHOGRAPHIC PROJECTION, 45-DEGREE TOP-DOWN CAMERA; parallel edges remain parallel; no vanishing points; no perspective foreshortening; identical scale near and far.',
            style='Atmospheric photorealistic 3D environment, Unreal Engine 5 visual quality, realistic materials and cinematic lighting.',
        ),
        folder=dict(id='d1ff8d95-e403-45ba-8f03-6991d740a0ce', name='东北迪斯科参考图'),
        params=dict(ar='16:9', raw=True, v='8.1', relax=True, personalization=False, style_references=[]),
        generation_status='not_submitted',
        source_hashes=data['source_hashes'],
        scenes=data['scenes'],
    )
    OUT_JSON.write_text(json.dumps(out, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')

    lines = ['# 东北迪斯科：剧情场景40景 · 雪夜写实 v1', '',
             '依据 40 景设计稿重写，用户 2026-09-17 选定方向：全部 40 景分批提交、按 A000 v8 方向（3D 写实、UE5 质感、严格正交 45° 俯视）、全游戏统一冬季雪景、16:9。', '',
             '风格基础：`MJ_A000正交45度写实雪景_v8_20260916_生成记录.json`（job `e7c933c9-2604-4d6f-bb34-d051959e27bf`）。', '',
             '参数：16:9、Raw、V8.1、Relax、无 sref、个性化关闭。', '',
             '各场景保留原文的时段与天气逻辑（D0/D1 无雾清晰、D2–D6 雾、D7 雾散夜间暴雨——D7 雨保留为冷雨）。', '',
             '## 场景索引', '', '| 编号 | 场景 | 时段 | 类型 |', '|---|---|---|---|']
    for s in data['scenes']:
        lines.append(f"| {s['id']} | {s['name']} | {s['day']} | {s['kind']} |")
    for s in data['scenes']:
        lines += ['', f"## {s['id']} {s['name']}", '',
                  f"剧情时段：{s['day']}。类型：{s['kind']}。", '',
                  '**空间设计：** ' + s['layout'], '',
                  '**人物动作：** ' + s['action'], '',
                  '**光照与天气：** ' + s['light'], '',
                  '**MJ 提示词（雪夜写实 v1）：**', '', '```text', s['prompt'], '```']
    OUT_MD.write_text('\n'.join(lines) + '\n', encoding='utf-8')
    print(json.dumps(dict(scenes=len(data['scenes']), rewritten=len(rewrites),
                          json=str(OUT_JSON), markdown=str(OUT_MD)), ensure_ascii=False))


if __name__ == '__main__':
    main()
