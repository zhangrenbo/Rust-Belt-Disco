"""Download one verified MJ candidate via its native Download Image control."""
import argparse,importlib.util,json,sys,time
from websocket import WebSocketTimeoutException
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
sys.stdout.reconfigure(encoding='utf-8')
spec=importlib.util.spec_from_file_location('direct_cdp',ROOT/'scripts/record-catalog-cdp-session.py')
cdp=importlib.util.module_from_spec(spec);spec.loader.exec_module(cdp)
parser=argparse.ArgumentParser();parser.add_argument('index',type=int,choices=range(4));args=parser.parse_args()
record_path=ROOT/'美术概念/MJ_D1叔叔家清晨_20260915_生成记录.json'
record=json.loads(record_path.read_text(encoding='utf-8'));assert record['status'] in ['completed','downloaded']
folder=ROOT/'美术概念';job=record['job_id'];target_file=folder/f'MJ_D1_A003_{job}_i{args.index}.png'
previous=next((d for d in record['downloads'] if d['index']==args.index),None)
if previous and target_file.is_file() and target_file.stat().st_size==previous['bytes']:
    print(json.dumps({'skipped_valid':target_file.name,'bytes':previous['bytes']}));sys.exit(0)
assert not target_file.exists(),'Preserve unverified existing file'
version=cdp.discover('json/version');assert version['Browser'].startswith('Chrome/')
browser=cdp.Connection(version['webSocketDebuggerUrl']);session=None;target=None;behavior_set=False
try:
    target=browser.call('Target.createTarget',{'url':'about:blank#d1-native-download','background':True})['targetId']
    tab=next(t for t in cdp.discover('json/list') if t['id']==target)
    session=cdp.Connection(tab['webSocketDebuggerUrl']);session.call('Page.enable');session.call('Runtime.enable')
    def evaluate(js):
        r=session.call('Runtime.evaluate',{'expression':js,'returnByValue':True,'awaitPromise':True})
        if r.get('exceptionDetails'):raise RuntimeError(str(r['exceptionDetails']))
        return r['result'].get('value')
    url=f'https://www.midjourney.com/jobs/{job}?index={args.index}'
    session.call('Page.navigate',{'url':url})
    try:session.call('Page.setWebLifecycleState',{'state':'active'})
    except Exception as e:print('Lifecycle '+str(e)[:100])
    deadline=time.monotonic()+25;r=None
    while time.monotonic()<deadline:
        r=evaluate('''(()=>{const e=document.querySelector('button[title="Download Image"]');if(!e)return null;const props=e[Object.keys(e).find(k=>k.startsWith('__reactProps'))],r=e.getBoundingClientRect();return r.width>0&&typeof props?.onClick==='function'&&e.contains(document.elementFromPoint(r.x+r.width/2,r.y+r.height/2))?{x:r.x+r.width/2,y:r.y+r.height/2,url:location.href}:null})()''')
        if r:break
        time.sleep(.3)
    assert r and r['url']==url,'Native download control not ready on intended image'
    browser.call('Browser.setDownloadBehavior',{'behavior':'allowAndName','downloadPath':str(folder),'eventsEnabled':True});behavior_set=True
    for kind in ['mouseMoved','mousePressed','mouseReleased']:
        p={'type':kind,'x':r['x'],'y':r['y']}
        if kind!='mouseMoved':p.update(button='left',clickCount=1)
        session.call('Input.dispatchMouseEvent',p)
    deadline=time.monotonic()+45;begin=None;complete=None
    while time.monotonic()<deadline:
        try:message=json.loads(browser.ws.recv())
        except WebSocketTimeoutException:continue
        method=message.get('method');params=message.get('params',{})
        if method=='Browser.downloadWillBegin':
            assert begin is None,'Unexpected concurrent native download'
            begin=params
            assert job in params['url'],'Download URL belongs to a different job'
        elif method=='Browser.downloadProgress' and begin and params['guid']==begin['guid']:
            assert params['state']!='canceled','Native download canceled'
            if params['state']=='completed':complete=params;break
    if not complete or not begin:
        print(json.dumps({'begin':{k:begin.get(k) for k in ['guid','suggestedFilename']} if begin else None,'detail':evaluate('({url:location.href,text:document.body.innerText.slice(-1000)})')},ensure_ascii=False))
    assert complete and begin,'Native completion missing'
    downloaded=folder/begin['guid'];assert downloaded.is_file() and downloaded.stat().st_size>0
    assert Path(begin['suggestedFilename']).suffix.lower()=='.png'
    size=downloaded.stat().st_size;assert size==int(complete['receivedBytes'])
    downloaded.rename(target_file)
    record['downloads'].append({'index':args.index,'path':str(target_file),'bytes':size,'native_state':'completed','guid':begin['guid'],'suggested_filename':begin['suggestedFilename'],'job_url':url,'completed_at':datetime.now(timezone.utc).isoformat()})
    if len(record['downloads'])==4:record['status']='downloaded'
    record_path.write_text(json.dumps(record,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({'file':target_file.name,'bytes':size,'native_state':'completed','download_count':len(record['downloads'])},ensure_ascii=False))
finally:
    if behavior_set:browser.call('Browser.setDownloadBehavior',{'behavior':'default','eventsEnabled':False})
    if session:session.close()
    if target:browser.call('Target.closeTarget',{'targetId':target})
    browser.close()
