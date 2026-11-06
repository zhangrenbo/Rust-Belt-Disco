"""Read one Midjourney job page's text and image metadata in a background target."""
import importlib.util
import json
import sys
import time
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
sys.stdout.reconfigure(encoding='utf-8')
spec=importlib.util.spec_from_file_location('direct_cdp',ROOT/'scripts/record-catalog-cdp-session.py')
cdp=importlib.util.module_from_spec(spec);spec.loader.exec_module(cdp)
job_id=sys.argv[1]
wait_seconds=float(sys.argv[2]) if len(sys.argv)>2 else 5
browser=cdp.Connection(cdp.discover('json/version')['webSocketDebuggerUrl'])
target=None
try:
    target=browser.call('Target.createTarget',{'url':'about:blank#mj-job-probe','background':True})['targetId']
    descriptor=next(tab for tab in cdp.discover('json/list') if tab['id']==target)
    page=cdp.Connection(descriptor['webSocketDebuggerUrl'])
    try:
        page.call('Page.enable');page.call('Runtime.enable')
        page.call('Page.navigate',{'url':f'https://www.midjourney.com/jobs/{job_id}?index=0'})
        time.sleep(wait_seconds)
        value=page.call('Runtime.evaluate',{'expression':'''(()=>({
          url:location.href,visibility:document.visibilityState,title:document.title,
          text:document.body?.innerText.slice(-650),images:document.images.length,
          loadedImages:[...document.images].filter(i=>i.complete&&i.naturalWidth>0).length,
          apiPaths:[...new Set(performance.getEntriesByType('resource').map(e=>e.name)
            .filter(u=>u.includes('/api/')).map(u=>new URL(u).pathname))].slice(0,18)
        }))()''','returnByValue':True})['result']['value']
        print(json.dumps({'job_id':job_id,**value},ensure_ascii=False))
    finally: page.close()
finally:
    if target:browser.call('Target.closeTarget',{'targetId':target})
    browser.close()
