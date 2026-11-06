"""Submit the map matrix one job at a time, preserving each CDP receipt."""
import json
import subprocess
import sys
import time
from collections import Counter
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
sys.stdout.reconfigure(encoding='utf-8')
ART=ROOT/'美术概念'
MANIFEST=ART/'MJ_2.5D地图矩阵_三图SR_20261018.json'
SUBMIT=Path(__file__).with_name('mj_environment_sample_cdp.py')
max_jobs=int(sys.argv[1]) if len(sys.argv)>1 else 60

def tally(jobs):
    return dict(Counter(json.loads((ART/j['record']).read_text(encoding='utf-8'))['status'] for j in jobs))

jobs=json.loads(MANIFEST.read_text(encoding='utf-8'))['jobs']
done=0
for job in jobs:
    record=json.loads((ART/job['record']).read_text(encoding='utf-8'))
    if record['status']=='accepted' and record.get('job_id'):
        continue
    if record['status']!='draft':
        print(json.dumps({'stopped_on':job['id'],'status':record['status'],'tally':tally(jobs)},ensure_ascii=False),flush=True)
        raise SystemExit(2)
    result=subprocess.run([sys.executable,str(SUBMIT),job['record']],cwd=ROOT,
                          text=True,encoding='utf-8',errors='replace',capture_output=True)
    if result.returncode:
        print(json.dumps({'stopped_on':job['id'],'returncode':result.returncode,
                          'stdout':(result.stdout or '')[-1000:],'stderr':(result.stderr or '')[-1000:],
                          'tally':tally(jobs)},ensure_ascii=False),flush=True)
        raise SystemExit(result.returncode)
    print(json.dumps({'id':job['id'],'receipt':result.stdout.splitlines()[-1]},ensure_ascii=False),flush=True)
    done+=1
    if done>=max_jobs:break
    time.sleep(.5)
print(json.dumps({'submitted_this_run':done,'tally':tally(jobs)},ensure_ascii=False),flush=True)
