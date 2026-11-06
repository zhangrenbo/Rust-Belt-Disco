"""Build resumable empty 2.5D environment jobs from the story's 40 scene anchors."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / '美术概念'
SOURCE = OUT / 'MJ剧情场景40景雪夜_v1_20260917.json'
MANIFEST = OUT / 'MJ_2.5D地图矩阵_三图SR_20261018.json'
SREF = '--sref https://s.mj.run/A6_BtdGjcfo https://s.mj.run/l2YC6AxG5Es https://s.mj.run/tHRcou4Txfk --sw 150'
BASE = ('STRICT ORTHOGRAPHIC PROJECTION, 45-DEGREE TOP-DOWN CAMERA. '
        'Parallel edges remain parallel, no vanishing points or perspective foreshortening; '
        'objects retain identical scale near and far. Photorealistic 3D environment with '
        'realistic materials, physically based shading, cinematic winter lighting and clear '
        'architectural depth. A playable 2.5D game map of a Northeast Chinese industrial town '
        'in winter. No characters or people. Show the full connected walking surface, entrances, '
        'exits, wall thickness, furniture bases and traversable gaps. ')
TAIL = ('Realistic snow, damp concrete, worn brick, corroded metal, soft contact shadows, '
        'readable foreground and background depth. No signs with readable words. '
        + SREF + ' --ar 16:9 --raw --v 8.1 --relax '
        '--no perspective projection, vanishing point, wide-angle lens, painting, illustration, '
        'brushstrokes, cartoon, miniature, interface, text, watermark, people')

# These add reusable rooms, thresholds and street links to the 40 narrative locations.
EXTRA = [
  ('ND-M41','exterior','公交站到主街', 'The bus-stop shelter, road crossing, snow-covered sidewalk and first main-street corner as one continuous playable route.'),
  ('ND-M42','interior','便利店一楼通用空景', 'Roof-cutaway ruined convenience store ground floor, entrance threshold, aisle shelving, service counter, rear door and stair landing.'),
  ('ND-M43','interior','便利店二楼通用空景', 'Roof-cutaway convenience-store second floor, storeroom, narrow corridor, old office and a visible stair connection.'),
  ('ND-M44','exterior','便利店后院与逃生巷', 'Rear service yard of the ruined convenience store connecting its back door to a narrow snowy escape alley.'),
  ('ND-M45','interior','叔叔家起居室通用空景', 'Modest family living and dining room, kitchen doorway, bedroom doors and an exterior entrance, roof removed.'),
  ('ND-M46','exterior','叔叔家与修车棚连通', 'Snow-covered family courtyard connected by a gate and footpath to a roadside vehicle repair shed.'),
  ('ND-M47','interior','修车棚操作区空景', 'Vehicle repair workshop with one lift bay, tool benches, parts shelves, service pit and a clear walkable route to the yard.'),
  ('ND-M48','interior','派出所走廊与办公室', 'Small local police station roof-cutaway, lobby, office desks, interview room and connected records-room door.'),
  ('ND-M49','interior','档案室书架迷宫', 'Police records room with rows of steel shelving, a central desk, filing cabinets and unobstructed aisles.'),
  ('ND-M50','interior','中医馆前厅后屋连通', 'Traditional Chinese clinic as one cutaway map: public consultation room, medicine cabinets, curtained doorway and rear sickroom.'),
  ('ND-M51','interior','县医院走廊病房', 'County hospital corridor with two patient rooms, nurse station, stair access and clear door thresholds.'),
  ('ND-M52','interior','面馆前厅后厨', 'Small noodle shop dining room, service counter, kitchen doorway and street-facing entrance, roof removed.'),
  ('ND-M53','exterior','主街完整路段', 'Continuous snowy main street with two shopfront blocks, side alley, sidewalk crossings and a clear route toward the bus stop.'),
  ('ND-M54','exterior','主街雾雪路口', 'Orthographic four-way junction in the old town, snow-covered crossings, low shopfronts, lampposts and branching routes.'),
  ('ND-M55','exterior','东山墓地入口到新坟', 'Cemetery gate, snowy stone path, low retaining walls and connected new grave area with sufficient walking gaps.'),
  ('ND-M56','exterior','钢铁厂门前到厂院', 'Locked steel-plant gate and security booth, adjacent fence and an industrial service yard visible beyond the entrance.'),
  ('ND-M57','exterior','镇西火场街区', 'Fire-damaged residential block with sealed house, intact neighboring facades, street access and rear service lane.'),
  ('ND-M58','exterior','河堤上下层通路', 'Winter river embankment with upper road, descending concrete steps, lower riverside path and clearly separated elevations.'),
  ('ND-M59','interior','地产公司办公区', 'Small property-company office, reception, corridor, private negotiation room and connected archive storage.'),
  ('ND-M60','interior','婚礼厅与偏房连通', 'Rural wedding banquet hall connected to a quieter side room and rear exit, roof-cutaway playable interior.'),
]

def main():
    data = json.loads(SOURCE.read_text(encoding='utf-8'))
    jobs = []
    for scene in data['scenes']:
        jobs.append((scene['id'],scene['kind'],scene['name'],scene['layout'],scene['id']))
    jobs += [(id,kind,name,layout,None) for id,kind,name,layout in EXTRA]
    output = {'schema':'northeast-disco-mj-map-matrix-sr-v1',
              'source':SOURCE.name, 'style_reference_source':'MJ_用户指定三张极乐迪斯科_风格引用索引.json',
              'style_references':SREF,'camera':'strict 45-degree orthographic',
              'human_review':'pending','jobs':[]}
    for id,kind,name,layout,source_id in jobs:
        filename=f'MJ地图矩阵_{id}_三图SR_20261018.json'
        path=OUT/filename
        prompt=BASE+layout+' '+TAIL
        if path.exists():
            record=json.loads(path.read_text(encoding='utf-8'))
            assert record['prompt']==prompt, f'Existing record changed: {filename}'
        else:
            record={'schema':'northeast-disco-mj-map-job-v1','project':'东北迪斯科','id':id,
                    'name':name,'kind':kind,'source_scene_id':source_id,'prompt':prompt,
                    'status':'draft','human_approved':False}
            path.write_text(json.dumps(record,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
        output['jobs'].append({'id':id,'name':name,'kind':kind,'record':filename,
                               'status':record['status'],'job_id':record.get('job_id'),
                               'result_url':record.get('result_url')})
    output['submission_summary']={'accepted':sum(job['status']=='accepted' for job in output['jobs']),
                                  'total':len(output['jobs']),'image_review':'pending'}
    audit=OUT/'MJ_2.5D地图矩阵_三图SR_状态审计_20261018.json'
    if audit.exists():
        checked=json.loads(audit.read_text(encoding='utf-8'))
        output['detail_audit']={'file':audit.name,'checked_at':checked['checked_at'],
                                'summary':checked['summary'],
                                'note':'404 means the detail page was unavailable at audit time; it is not proof of a failed generation.'}
    MANIFEST.write_text(json.dumps(output,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({'manifest':str(MANIFEST),'total':len(jobs),'story':len(data['scenes']),
                      'additional':len(EXTRA)},ensure_ascii=False))

if __name__=='__main__': main()
