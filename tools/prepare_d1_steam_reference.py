import json,urllib.request
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
old=ROOT/'美术概念/MJ_D1叔叔家实机风格参考_v2_20260915_生成记录.json'
r=json.loads(old.read_text(encoding='utf-8'));assert r['job_id'] is None
r['status']='failed_before_job_acceptance';r['failure']='MJ UI: Creation failed / Failed to fetch. No submit response or job ID. Remote reference host has no Access-Control-Allow-Origin header.'
old.write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
shot=next(s for s in json.loads((ROOT/'docs/disco-steam-reference-sources.json').read_text()) if s['id']==18)
url=shot['path_full'].split('?')[0]
with urllib.request.urlopen(urllib.request.Request(url,method='HEAD'),timeout=10) as resp:
 assert resp.status==200 and resp.headers.get('Access-Control-Allow-Origin')=='*'
reference={'type':'style_reference','url':url,'source_page':'https://store.steampowered.com/app/632470/Disco_Elysium__The_Final_Cut/','steam_screenshot_id':18,'subject':'官方实机：斜俯视的小型居住室内，木地板、桌椅、窗光与暗色剖开边界，无大块游戏UI遮挡','visual_inspection':'Observed in docs/disco-steam-reference-inspection.png; that inspection sheet is not uploaded or used as a reference','access_verified':'HTTP HEAD 200; image/jpeg; Access-Control-Allow-Origin: *'}
r['prompt']=r['prompt'].replace(r['references'][0]['url'],url)
r['references']=[reference];r['status']='prepared';r['previous_failed_attempt']=old.name;r['human_review']='pending'
for key in ['failure','preflight','submitted_at','source_sha256','submit_receipt','post_submit_text','retry_diagnostics']:r.pop(key,None)
out=ROOT/'美术概念/MJ_D1叔叔家Steam实机参考_v3_20260915_生成记录.json';assert not out.exists()
out.write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('Prepared Steam screenshot 18 reference; HEAD and CORS verified.')
