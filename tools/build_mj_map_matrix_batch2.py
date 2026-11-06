"""Create a second, nonoverlapping Midjourney map matrix in the existing art folder."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
art = root / '\u7f8e\u672f\u6982\u5ff5'
batch = [
    ('ND-X61', '客运站车辆院', 'exterior', 'A provincial bus depot vehicle yard in a Northeast Chinese town: three bus bays, a low dispatch hut, a fenced snow storage corner and a wide turning lane. A pedestrian gate connects the waiting room to the bays; keep bus and pedestrian routes legible.'),
    ('ND-X62', '火车站前广场', 'exterior', 'A small 1980s railway station forecourt: a low brick station facade, taxi stand, concrete planters, bicycle racks and two streets connecting left and right. Preserve a broad walkable approach to the entrance.'),
    ('ND-X63', '火车站站台', 'exterior', 'A weathered railway platform with two parallel tracks, an overhead footbridge landing, benches under a metal canopy and a station exit at one end. The safe platform path and its edges are visibly distinct.'),
    ('ND-X64', '铁路桥下通道', 'exterior', 'A rail underpass crossing beneath a brick railway embankment: concrete retaining walls, one road lane, a separate narrow pedestrian path and stairs up to street level. Show the entire connection between both sides.'),
    ('ND-X65', '老住宅楼院落', 'exterior', 'A shared courtyard between three aging brick apartment blocks: entrances, bicycle sheds, coal bins, drying lines and a snow-cleared central path. Keep doorways and passages to the street visible.'),
    ('ND-X66', '老楼夹缝台阶巷', 'exterior', 'A stepped alley squeezed between old Northeast Chinese housing blocks: brick side walls, drain channel, overhead pipes, two landings and a passage to the next courtyard. Make each stair and traversable landing clear.'),
    ('ND-X67', '冬季露天集市', 'exterior', 'An outdoor winter market square: empty produce stalls with canvas roofs, wooden crates, a central pedestrian aisle, side lanes and an entrance arch without readable lettering. No vendors or shoppers.'),
    ('ND-X68', '批发市场后巷', 'exterior', 'A wholesale market service alley: loading doors, stacked empty pallets, metal dumpsters, a handcart and a narrow clear route from the street to the rear warehouse gate.'),
    ('ND-X69', '公共浴池门前', 'exterior', 'The exterior of a municipal bathhouse: brick facade, steam vent, concrete steps, side boiler access, bicycle parking and a clear public entrance connected to the street.'),
    ('ND-X70', '老电影院门前', 'exterior', 'A worn town cinema frontage at dusk: shallow entry steps, ticket window, empty poster cases, two side alleys and a broad forecourt. No legible movie titles or signs.'),
    ('ND-X71', '镇政府广场', 'exterior', 'A modest municipal square: low government building, bare flagpole, long benches, stone paving, winter trees and three clear pedestrian approaches. Keep the building entrance and square boundary readable.'),
    ('ND-X72', '饭馆后院', 'exterior', 'A small restaurant service courtyard: back kitchen door, coal-fired stove chimney, crates, covered refuse area, a delivery gate and a walkable loop around a low storage shed.'),
    ('ND-X73', '街角诊所门外', 'exterior', 'A street-corner neighborhood clinic exterior: brick storefront, covered steps, ambulance stopping bay, side passage to a rear door and snow-dusted pavement. No readable signage.'),
    ('ND-X74', '职校教学楼院', 'exterior', 'A vocational school courtyard between a workshop wing and classroom block: covered walkway, outdoor equipment plinths, two stair entrances, a gate and a clear crossing route.'),
    ('ND-X75', '旧操场看台', 'exterior', 'An aging school athletic ground: compact running track edge, concrete spectator terraces, equipment shed, fence opening and stairs connecting field level to the upper path.'),
    ('ND-X76', '运河步行桥', 'exterior', 'A narrow pedestrian bridge over an icy drainage canal: metal railings, embankment paths on both banks, concrete stair landings and clear safe walking surfaces.'),
    ('ND-X77', '河边阶梯码头', 'exterior', 'A river landing with terraced concrete steps, mooring bollards, a small maintenance hut and upper levee path. Show the height difference and route from street level down toward the water.'),
    ('ND-X78', '工业铁路支线', 'exterior', 'An industrial rail spur beside a steel works: tracks, signal hut, crossing plates, a fenced pedestrian passage and a loading siding. Preserve readable walkable crossing geometry.'),
    ('ND-X79', '废铁回收场', 'exterior', 'A scrapyard: piles of cut steel, stripped truck chassis, gantry crane base, weighbridge and fenced lanes. Keep a safe route through the yard and a clear entrance.'),
    ('ND-X80', '仓库装卸月台', 'exterior', 'A logistics warehouse loading yard: three roll-up doors, a raised loading platform, forklift lanes, pallet stacks and a separate pedestrian stair and path to the office door.'),
    ('ND-X81', '煤场分拣区', 'exterior', 'A coal distribution yard: black coal heaps, low concrete partition walls, conveyor support, truck weighbridge and a narrow but unobstructed worker path to the gatehouse.'),
    ('ND-X82', '长途货车停靠站', 'exterior', 'A highway truck stop on the town edge: parking bays, low diner, service shed, fuel canopy and separate footpath from the road shoulder to the diner entrance.'),
    ('ND-X83', '旧加油站', 'exterior', 'A small aging filling station: two pumps under a low canopy, attached cashier room, side maintenance pit, road access and a clear walking route around the pumps.'),
    ('ND-X84', '东山山坡小路', 'exterior', 'A winding hillside footpath among leafless birch trees and rock outcrops: stone steps, two switchback landings and a visible connection between lower village path and upper cemetery trail.'),
    ('ND-X85', '山坡阶梯墓区', 'exterior', 'A terraced cemetery on a cold hillside: low retaining walls, modest gravestones without legible inscriptions, stone stairways and clear paths between three elevation levels.'),
    ('ND-X86', '居民菜园暖棚', 'exterior', 'Winter allotment gardens: small plastic greenhouses, narrow raised beds under snow, water barrels, a tool shed and a connected footpath from the gate through the plots.'),
    ('ND-X87', '老住宅屋顶平台', 'exterior', 'A flat apartment rooftop: stairwell housing, water tank, ventilation ducts, laundry lines, low parapet and a navigable loop with clear roof access and safe edges.'),
    ('ND-X88', '排水渠隧道入口', 'exterior', 'An open storm drain leading into a short service tunnel: concrete channel, maintenance walkway, two bank access ramps, metal grating and a clear change from daylight to tunnel.'),
    ('ND-X89', '停工工地', 'exterior', 'A halted construction site: concrete foundation grid, half-built stair core, rebar stacks, fenced access, temporary office cabin and a connected ground-level route around excavation pits.'),
    ('ND-X90', '镇外公路检查点', 'exterior', 'A remote road checkpoint: small brick guardhouse, barrier arm, roadside pullout, snowbank, wire fence and pedestrian passage connecting both sides of the barrier.'),
    ('ND-X91', '街角糕点店', 'interior', 'A compact Northeast Chinese bakery: glass display counter, two customer tables, rear preparation doorway, oven room threshold and an unobstructed aisle from street entrance to counter.'),
    ('ND-X92', '粮油杂货店', 'interior', 'A family grocery selling rice and cooking oil: low shelves, sacks, weighing counter, back storage door and a clear loop around the central shelf. No readable product labels.'),
    ('ND-X93', '五金店', 'interior', 'A small hardware store: bins of screws, coils of wire, hanging hand tools, service counter, rear stockroom doorway and two navigable aisles. No readable labels.'),
    ('ND-X94', '社区药房', 'interior', 'A neighborhood pharmacy: glass medicine cabinets, service counter, small consultation corner, storage door and clear customer floor. No readable prescriptions or labels.'),
    ('ND-X95', '公共浴池更衣室', 'interior', 'A municipal bathhouse changing room: rows of metal lockers, wooden benches, towel counter, dry entry and wet-room doorway. Keep a clear route between both doors.'),
    ('ND-X96', '公共浴池湿区', 'interior', 'An old public bathhouse washing area: tiled floor with drains, showers, low partition walls, a steam room door and a dry exit threshold. Make wet and walkable areas legible.'),
    ('ND-X97', '老电影院大厅', 'interior', 'A town cinema lobby: ticket booth, worn terrazzo floor, concession counter, stairs to balcony, double doors to the auditorium and a clear central queue and circulation area.'),
    ('ND-X98', '老电影院放映厅', 'interior', 'A small cinema auditorium: staggered seat rows, central and side aisles, projection booth at rear, screen wall and two clear emergency exits. No audience.'),
    ('ND-X99', '火车站候车室', 'interior', 'An old railway waiting hall: rows of wooden benches, ticket windows, luggage corner, station entry and platform exit. Preserve obvious paths through the benches.'),
    ('ND-X100', '火车站值班室', 'interior', 'A station duty office: dispatch desk, wall clock, paper schedules without legible text, radio cabinet, rear archive nook and a clear doorway to the platform.'),
    ('ND-X101', '职校教室', 'interior', 'A 1980s vocational school classroom: paired desks, blackboard without writing, teacher platform, coat hooks, front corridor door and rear service door. Keep aisles between desk rows.'),
    ('ND-X102', '职校实验室', 'interior', 'A modest technical school laboratory: workbenches, sinks, locked chemical cabinet, ventilation hood, storage door and wide walkable aisles. No students.'),
    ('ND-X103', '职校宿舍', 'interior', 'A shared vocational school dormitory: four metal bunk beds, narrow wardrobes, washbasin, window and corridor door. Leave clear walkable space between beds and to the door.'),
    ('ND-X104', '镇图书室', 'interior', 'A small public reading room: parallel bookshelves, librarian desk, two reading tables, archive door and continuous aisles through the shelves. No readable book titles.'),
    ('ND-X105', '老邮局', 'interior', 'A local post office: service counter with metal grille, mail sorting cubbies, parcel shelves, public entrance and rear staff passage. No readable postage signs.'),
    ('ND-X106', '信用社柜台', 'interior', 'A small town credit union: teller windows, waiting benches, side office door, secure back room threshold and open customer floor. No readable documents.'),
    ('ND-X107', '印刷复印店', 'interior', 'A print and copy shop: photocopiers, paper racks, binding table, cashier desk, rear machine room and clear access to each work area. No readable printouts.'),
    ('ND-X108', '收音机维修铺', 'interior', 'A radio repair workshop: electronics workbench, parts drawers, shelves of old radios, customer counter, back door and an uncluttered work aisle.'),
    ('ND-X109', '楼房锅炉房', 'interior', 'A residential boiler room: large cylindrical boiler, exposed pipes, coal feed, maintenance platform, service door and safe walking routes around the machinery.'),
    ('ND-X110', '钢厂车间', 'interior', 'An industrial steelworks shop floor: heavy press, overhead crane rails, work platforms, safety rails, loading door and a clearly separated pedestrian route.'),
    ('ND-X111', '工厂门卫室', 'interior', 'A factory gatehouse interior: desk, stove, coat rack, large window facing the gate, back storage niche and front and side exits with clear sightlines.'),
    ('ND-X112', '工厂更衣室', 'interior', 'An industrial workers changing room: metal lockers, long benches, boot racks, wash sink, factory floor door and street corridor door. Keep walkable aisles.'),
    ('ND-X113', '仓库货架通道', 'interior', 'A warehouse interior: tall shelving bays, pallet jack, receiving dock, small clerk station, rear fire door and a connected network of clear aisles.'),
    ('ND-X114', '居民楼楼梯间', 'interior', 'An old apartment stairwell: two flights around a landing, mailboxes, basement door, upper corridor entrance and visible level changes. Show the entire walkable circulation.'),
    ('ND-X115', '老居民厨房', 'interior', 'A modest lived-in apartment kitchen: enamel stove, sink, narrow dining table, pantry shelves, hall doorway and a clear compact route between work surfaces.'),
    ('ND-X116', '居民楼地下储物间', 'interior', 'A basement storage room: brick partition cubicles, old bicycles, pipes, fuse box, stair entrance and a continuous narrow central passage.'),
    ('ND-X117', '社区活动室', 'interior', 'A community hall: folding chairs along the walls, central open floor, tea table, small stage, entry doors and side kitchen access. No people or readable banners.'),
    ('ND-X118', '东北迪斯科舞厅', 'interior', 'A late 1980s Northeast Chinese town dance hall: worn wooden dance floor, low stage, mirrored column, booth seating around the perimeter, entrance and service door. Empty room, clear circulation.'),
    ('ND-X119', '舞厅后台机房', 'interior', 'A dance hall back room: analog audio rack, stacked speakers, spare lights, cable coils, staff door to the stage and a clear working route around the equipment.'),
    ('ND-X120', '屋顶出入口机房', 'interior', 'A compact rooftop access room: concrete stair exit, ventilation equipment, maintenance cabinet, roof door and a clear route between stairs and exterior threshold.'),
]

assert len(batch) == 60 and len({row[0] for row in batch}) == 60
style = '--sref https://s.mj.run/A6_BtdGjcfo https://s.mj.run/l2YC6AxG5Es https://s.mj.run/tHRcou4Txfk --sw 150'
base = ('STRICT ORTHOGRAPHIC PROJECTION, 45-DEGREE TOP-DOWN CAMERA. Parallel edges remain parallel, '
        'no vanishing points or perspective foreshortening; objects retain identical scale near and far. '
        'Photorealistic 3D game environment with realistic materials, physically based shading, cinematic '
        'winter lighting and clear architectural depth. A playable 2.5D map in a Northeast Chinese '
        'industrial town. No characters or people. Show full connected walking surfaces, entrances, '
        'exits, wall thickness, object bases and traversable gaps. ')
finish = (' Realistic snow outside, damp concrete, worn brick, aged wood and corroded metal where appropriate; '
          'soft contact shadows and readable foreground and background depth. No signs with readable words. '
          + style + ' --ar 16:9 --raw --v 8.1 --relax --no perspective projection, vanishing point, '
          'wide-angle lens, painting, illustration, brushstrokes, cartoon, miniature, interface, text, watermark, people')
jobs = []
for task_id, name, kind, scene in batch:
    filename = f'MJ\u5730\u56fe\u77e9\u9635_{task_id}_\u4e09\u56feSR_20261018.json'
    target = art / filename
    if target.exists():
        raise SystemExit(f'Will not overwrite {target}')
    prompt = base + scene + finish
    record = {'schema': 'northeast-disco-mj-map-job-v2', 'project': '东北迪斯科', 'id': task_id,
              'name': name, 'kind': kind, 'prompt': prompt, 'status': 'draft',
              'human_approved': False}
    target.write_text(json.dumps(record, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    jobs.append({'id': task_id, 'name': name, 'kind': kind, 'record': filename, 'status': 'draft'})
manifest = {'schema': 'northeast-disco-mj-map-matrix-sr-v2', 'batch': 2,
            'style_reference_source': 'MJ_用户指定三张极乐迪斯科_风格引用索引.json',
            'style_references': style, 'camera': 'strict 45-degree orthographic',
            'human_review': 'pending', 'jobs': jobs}
manifest_path = art / 'MJ_2.5D\u5730\u56fe\u77e9\u9635_\u7b2c\u4e8c\u6279_\u4e09\u56feSR_20261018.json'
if manifest_path.exists():
    raise SystemExit(f'Will not overwrite {manifest_path}')
manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print(json.dumps({'created': len(jobs), 'interior': sum(j['kind']=='interior' for j in jobs),
                  'exterior': sum(j['kind']=='exterior' for j in jobs), 'manifest': str(manifest_path)}, ensure_ascii=False))
