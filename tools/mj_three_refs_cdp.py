"""Native Chrome CDP helpers for the user's three A000 style-reference images."""
import argparse, importlib.util, json, sys, hashlib, re, time
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.stdout.reconfigure(encoding='utf-8')
spec = importlib.util.spec_from_file_location('direct_cdp', ROOT/'scripts/record-catalog-cdp-session.py')
cdp = importlib.util.module_from_spec(spec)
spec.loader.exec_module(cdp)
parser = argparse.ArgumentParser()
parser.add_argument('action', choices=['inspect', 'eval', 'click', 'upload', 'prepare', 'submit', 'receipt', 'status'])
parser.add_argument('--file')
parser.add_argument('--selector')
args = parser.parse_args()
assert cdp.discover('json/version')['Browser'].startswith('Chrome/')
tabs = [t for t in cdp.discover('json/list') if t['id'] == 'D45719CABD13D5BA069ADDA0963D1703' and t['url'].startswith('https://www.midjourney.com/')]
assert len(tabs) == 1, 'Reacquire the intended MJ target; do not restart Chrome'
c = cdp.Connection(tabs[0]['webSocketDebuggerUrl'])
RECORD=ROOT/'美术概念/MJ_A000三张用户实机风格参考_v3_20260915_生成记录.json'
INDEX=ROOT/'美术概念/MJ_用户指定三张极乐迪斯科_风格引用索引.json'

def evaluate(js):
    r = c.call('Runtime.evaluate', {'expression':js, 'returnByValue':True, 'awaitPromise':True, 'userGesture':True})
    if r.get('exceptionDetails'): raise RuntimeError(str(r['exceptionDetails']))
    return r['result'].get('value')

def save(path, data):
    path.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')

def refs():
    return evaluate((ROOT/'.local/mj-three-upload-data.js').read_text(encoding='utf-8'))

def receipt(record):
    result=evaluate('window.__a000ThreeStyleSubmission')
    record['submit_receipt']=result
    if result and result.get('http')==200 and len(result.get('accepted',[]))==1:
        job=result['accepted'][0]
        assert not record.get('job_id') or record['job_id']==job['job_id']
        record.update(job_id=job['job_id'],status='accepted',accepted_at=datetime.now(timezone.utc).isoformat())
    save(RECORD,record)
    return result

try:
    c.call('Runtime.enable')
    c.call('Page.enable')
    try: c.call('Page.setWebLifecycleState', {'state':'active'})
    except Exception as e: print('Lifecycle:', str(e)[:120])
    c.call('Emulation.setFocusEmulationEnabled', {'enabled':True})
    if args.action == 'inspect':
        value = evaluate('''(()=>{const visible=e=>e.getBoundingClientRect().width>0;return {url:location.href,text:document.body.innerText.slice(0,3200),inputs:[...document.querySelectorAll('textarea,input')].map(e=>({tag:e.tagName,type:e.type,placeholder:e.placeholder,value:e.type==='file'?null:e.value,accept:e.accept,multiple:e.multiple,html:e.type==='file'?e.outerHTML:null})),buttons:[...document.querySelectorAll('button')].filter(visible).slice(0,55).map(e=>({text:e.innerText.slice(0,80),title:e.title,aria:e.getAttribute('aria-label'),'data-active':e.getAttribute('data-active')}))}})()''')
    elif args.action == 'eval':
        value = evaluate(Path(args.file).read_text(encoding='utf-8-sig'))
    elif args.action == 'click':
        r = evaluate('''(s=>{const e=document.querySelector(s);if(!e||e.disabled)throw Error('Missing/disabled control');const r=e.getBoundingClientRect();return {x:r.x+r.width/2,y:r.y+r.height/2,w:r.width,h:r.height}})('''+json.dumps(args.selector)+')')
        assert r['w']>0 and r['h']>0
        for kind in ['mouseMoved','mousePressed','mouseReleased']:
            p={'type':kind,'x':r['x'],'y':r['y']}
            if kind!='mouseMoved':p.update(button='left',clickCount=1)
            c.call('Input.dispatchMouseEvent',p)
        value={'clicked':args.selector}
    elif args.action == 'upload':
        files=json.loads(Path(args.file).read_text(encoding='utf-8'))
        assert len(files)==3 and all(Path(f).is_file() for f in files)
        root=c.call('DOM.getDocument',{})['root']['nodeId']
        node=c.call('DOM.querySelector',{'nodeId':root,'selector':args.selector})['nodeId']
        assert node
        c.call('DOM.setFileInputFiles',{'files':files,'nodeId':node})
        value={'native_file_input_set':files}
    elif args.action == 'prepare':
        assert not RECORD.exists(), 'Preserve existing record'
        assert evaluate('document.querySelector("textarea").value')=='', 'Preserve existing draft'
        files=json.loads((ROOT/'.local/mj-three-files.json').read_text(encoding='utf-8'))
        ids=[hashlib.sha256(Path(f).read_bytes()).hexdigest() for f in files]
        state=refs()
        assert state['category']=='SR' and not state['imageErrors']
        assert [i['image']['imageData']['id'] for i in state['items']]==ids
        assert all(i['image']['sw']>0 and all(i['image'][k]==0 for k in ['iw','cw','ow','ew']) for i in state['items'])
        dom=evaluate((ROOT/'.local/mj-three-ref-metadata.js').read_text(encoding='utf-8'))
        assert len(dom['groups'][1]['refs'])==3 and not dom['groups'][0]['refs'] and not dom['groups'][2]['refs']
        assert dom['personalization']==[{'text':'PPersonalize','active':'false'}]
        assert '东北迪斯科参考图' in dom['tail']
        previews=[r['background'][5:-2] for r in dom['groups'][1]['refs']]
        index={'purpose':'用户指定的三张极乐迪斯科实机截图，共同用作 Midjourney Style References', 'category':'SR','references':[{'source_path':f,'sha256':id,'mj_upload_id':id,'preview_url':p,'role':'style_reference','image_weight':0,'style_weight':100} for f,id,p in zip(files,ids,previews)], 'verified_at':datetime.now(timezone.utc).isoformat()}
        save(INDEX,index)
        previous=json.loads((ROOT/'美术概念/MJ_A000极乐迪斯科实机风格参考_v2_20260915_生成记录.json').read_text(encoding='utf-8'))
        prompt=re.sub(r' --sref \S+','',previous['prompt'])
        assert '--sref' not in prompt and '--sw 150' in prompt
        record={'status':'prepared','scene':'A000 夜间公交站','prompt':prompt,'folder':'东北迪斯科参考图','folder_id':'d1ff8d95-e403-45ba-8f03-6991d740a0ce','source_index':str(INDEX.relative_to(ROOT)),'references':index['references'],'preflight':{'dom':dom,'style_items':state},'human_review':'pending','in_game_integration':'not_applied','downloads':[]}
        save(RECORD,record)
        evaluate('document.querySelector("textarea").focus()')
        c.call('Input.insertText',{'text':prompt})
        assert evaluate('document.querySelector("textarea").value')==prompt
        value={'prepared':True,'file':str(RECORD),'three_source_hashes_match_upload_ids':True}
    elif args.action == 'submit':
        record=json.loads(RECORD.read_text(encoding='utf-8'))
        assert record['status']=='prepared' and not record.get('job_id')
        assert evaluate('document.querySelector("textarea").value')==record['prompt']
        assert [i['image']['imageData']['id'] for i in refs()['items']]==[r['sha256'] for r in record['references']]
        evaluate('''(()=>{if(window.__a000ThreeStyleSubmission)throw Error('Existing observer');const original=window.fetch;window.__a000ThreeStyleSubmission={accepted:[],http:null};window.fetch=async function(...args){const path=new URL(typeof args[0]==='string'?args[0]:args[0].url,location.href).pathname;const r=await original.apply(this,args);if(path==='/api/submit-jobs'){const o=window.__a000ThreeStyleSubmission;o.http=r.status;if((r.headers.get('content-type')||'').includes('application/json')){try{const j=await r.clone().json();const collect=n=>{if(!n||typeof n!=='object')return;if(n.job_id)o.accepted.push(Object.fromEntries(['job_id','prompt','is_queued','job_type','flags','meta','personalization_codes'].map(k=>[k,n[k]])));for(const v of Object.values(n))if(v&&typeof v==='object')collect(v)};collect(j);if(!o.accepted.length)o.errorMetadata=j}catch(e){o.error=String(e)}}window.fetch=original}return r};return true})()''')
        record.update(status='submission_pending_verification',submitted_at=datetime.now(timezone.utc).isoformat())
        save(RECORD,record)
        evaluate('document.querySelector("textarea").focus()')
        for kind in ['keyDown','keyUp']:
            p={'type':kind,'key':'Enter','code':'Enter','windowsVirtualKeyCode':13}
            if kind=='keyDown':p.update(text='\r',unmodifiedText='\r')
            c.call('Input.dispatchKeyEvent',p)
        deadline=time.monotonic()+25
        while time.monotonic()<deadline:
            result=evaluate('window.__a000ThreeStyleSubmission')
            if result.get('http') is not None:break
            time.sleep(.5)
        value={'status':record['status'],'receipt':receipt(record)}
    elif args.action == 'receipt':
        record=json.loads(RECORD.read_text(encoding='utf-8'))
        value={'receipt':receipt(record),'status':record['status']}
    elif args.action == 'status':
        record=json.loads(RECORD.read_text(encoding='utf-8'))
        assert record.get('job_id')
        value=evaluate('''(id=>{const links=[...document.querySelectorAll('a[href*="/jobs/'+id+'"]')];let section=links[0];for(let n=0;n<8&&section&&!section.innerText.includes('Rerun');n++)section=section.parentElement;return {job_id:id,links:links.map(a=>({href:a.getAttribute('href'),images:[...a.querySelectorAll('img')].map(i=>({loaded:i.complete,width:i.naturalWidth,height:i.naturalHeight}))})),text:section?.innerText?.slice(0,2400)||document.body.innerText.slice(0,1600),folderSelected:decodeURIComponent(location.search).includes('folder=东北迪斯科参考图')}})('''+json.dumps(record['job_id'])+')')
        record['last_status']=value
        record['status_checked_at']=datetime.now(timezone.utc).isoformat()
        if len(value['links'])==4 and 'Cancel' not in value['text'] and 'Trash' in value['text']:
            record['status']='completed'
            record['group_membership_verified']=value['folderSelected']
        save(RECORD,record)
        value['record_status']=record['status']
    print(json.dumps(value,ensure_ascii=False))
finally:
    c.close()
