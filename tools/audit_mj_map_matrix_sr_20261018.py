"""Verify whether each submitted job has a reachable detail page; metadata only."""
import json
import subprocess
import sys
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
sys.stdout.reconfigure(encoding='utf-8')
MANIFEST=ROOT/'美术概念/MJ_2.5D地图矩阵_三图SR_20261018.json'
AUDIT=ROOT/'美术概念/MJ_2.5D地图矩阵_三图SR_状态审计_20261018.json'
PROBE=Path(__file__).with_name('mj_probe_job_cdp.py')
jobs=json.loads(MANIFEST.read_text(encoding='utf-8'))['jobs']
results=[]
for job in jobs:
    call=subprocess.run([sys.executable,str(PROBE),job['job_id'],'2'],cwd=ROOT,
                        text=True,encoding='utf-8',errors='replace',capture_output=True)
    if call.returncode:
        status='probe_error'; evidence=(call.stderr or '')[-250:]
    else:
        info=json.loads(call.stdout)
        if 'This page has not been /imagine' in info['text']:
            status='detail_404';evidence='job detail unavailable at audit time'
        elif 'Creation actions' in info['text']:
            status='detail_ready';evidence=f"image_elements={info['images']};loaded={info['loadedImages']}"
        else:
            status='detail_unknown';evidence=info['text'][-180:]
    results.append({'id':job['id'],'job_id':job['job_id'],'status':status,'evidence':evidence})
    print(json.dumps({'id':job['id'],'status':status},ensure_ascii=False),flush=True)
    AUDIT.write_text(json.dumps({'checked_at':datetime.now(timezone.utc).isoformat(),
                                 'summary':dict(Counter(x['status'] for x in results)),
                                 'results':results},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'summary':dict(Counter(x['status'] for x in results)),'audit':str(AUDIT)},ensure_ascii=False))
