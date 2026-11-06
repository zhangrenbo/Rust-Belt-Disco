"""Target-specific Chrome CDP for one D1 scene. Metadata only; no image transport."""
import argparse,importlib.util,json,sys,time,hashlib
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
sys.stdout.reconfigure(encoding='utf-8')
spec=importlib.util.spec_from_file_location('direct_cdp',ROOT/'scripts/record-catalog-cdp-session.py')
cdp=importlib.util.module_from_spec(spec);spec.loader.exec_module(cdp)
parser=argparse.ArgumentParser();parser.add_argument('action',choices=['inspect','eval','click','fill','submit','status']);parser.add_argument('--file');parser.add_argument('--selector');args=parser.parse_args()
version=cdp.discover('json/version');assert version['Browser'].startswith('Chrome/')
tabs=[t for t in cdp.discover('json/list') if t['id']=='97D1C30E256BE7F9AE334AB7AB0E6983' and t['url'].startswith('https://www.midjourney.com/')]
assert len(tabs)==1,'The verified original MJ target is unavailable'
session=cdp.Connection(tabs[0]['webSocketDebuggerUrl'])
def evaluate(js):
    r=session.call('Runtime.evaluate',{'expression':js,'returnByValue':True,'awaitPromise':True,'userGesture':True})
    if r.get('exceptionDetails'):raise RuntimeError(str(r['exceptionDetails']))
    return r['result'].get('value')
def click(selector):
    r=evaluate('(()=>{const e=document.querySelector('+json.dumps(selector)+');if(!e||e.disabled)throw Error("Missing/disabled control");const r=e.getBoundingClientRect();return {x:r.x+r.width/2,y:r.y+r.height/2,w:r.width,h:r.height}})()')
    assert r['w']>0 and r['h']>0
    for kind in ['mouseMoved','mousePressed','mouseReleased']:
        p={'type':kind,'x':r['x'],'y':r['y']}
        if kind!='mouseMoved':p.update(button='left',clickCount=1)
        session.call('Input.dispatchMouseEvent',p)
try:
    session.call('Runtime.enable');session.call('Page.enable')
    try:session.call('Page.setWebLifecycleState',{'state':'active'})
    except Exception as e:print('Lifecycle: '+str(e)[:150])
    if args.action=='inspect':
        value=evaluate('''(()=>{const visible=e=>e.getBoundingClientRect().width>0;return {url:location.href,text:document.body.innerText.slice(0,3800),inputs:[...document.querySelectorAll('textarea,input')].filter(visible).map(e=>({tag:e.tagName,placeholder:e.placeholder,value:e.value?.slice(0,150)})),buttons:[...document.querySelectorAll('button')].filter(visible).slice(0,45).map(e=>({text:e.innerText.slice(0,80),title:e.title,aria:e.getAttribute('aria-label'),pressed:e.getAttribute('aria-pressed')})),links:[...document.querySelectorAll('a[href*="/jobs/"]')].slice(0,8).map(e=>e.getAttribute('href'))}})()''')
    elif args.action=='eval':value=evaluate(Path(args.file).read_text(encoding='utf-8-sig'))
    elif args.action=='click':click(args.selector);value={'clicked':args.selector}
    elif args.action=='fill':
        record=json.loads(Path(args.file).read_text(encoding='utf-8'))
        assert evaluate('document.querySelector("textarea").value')=='','Preserve existing draft'
        click('textarea');session.call('Input.insertText',{'text':record['prompt']})
        value={'exact':evaluate('document.querySelector("textarea").value')==record['prompt']}
    elif args.action=='submit':
        path=Path(args.file);record=json.loads(path.read_text(encoding='utf-8'))
        assert record['status']=='prepared' and not record.get('job_id'),'Prevent duplicate submission'
        assert evaluate('document.querySelector("textarea").value')==record['prompt'],'Exact prompt mismatch'
        preflight=evaluate('''(()=>{const a=document.querySelector('textarea').parentElement.parentElement.parentElement,p=[...document.querySelectorAll('button')].find(e=>e.textContent==='PPersonalize');return {folder:a.innerText.includes('东北迪斯科参考图'),images:a.querySelectorAll('img').length,personalization:p?.getAttribute('data-active')}})()''')
        assert preflight=={'folder':True,'images':0,'personalization':'false'},preflight
        evaluate('''(()=>{if(window.__d1Submission)throw Error('Existing submission observer');const original=window.fetch;window.__d1Submission={accepted:[],http:null};window.fetch=async function(...args){const r=await original.apply(this,args);const path=new URL(typeof args[0]==='string'?args[0]:args[0].url,location.href).pathname;if(path==='/api/submit-jobs'){window.__d1Submission.http=r.status;if((r.headers.get('content-type')||'').includes('application/json')){try{const json=await r.clone().json();const collect=n=>{if(!n||typeof n!=='object')return;if(n.job_id)window.__d1Submission.accepted.push(Object.fromEntries(['job_id','prompt','is_queued','job_type','flags','meta','personalization_codes'].map(k=>[k,n[k]])));for(const v of Object.values(n))if(v&&typeof v==='object')collect(v)};collect(json)}catch(e){window.__d1Submission.error=String(e)}}window.fetch=original;}return r;};return true})()''')
        record['status']='submission_pending_verification';record['preflight']=preflight
        record['submitted_at']=datetime.now(timezone.utc).isoformat()
        record['source_sha256']={p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest() for p in ['content/story/ink/D1_第一天.ink','content/story/source-documents/D1_剧本_董点事线.md']}
        path.write_text(json.dumps(record,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
        click('textarea')
        for kind in ['keyDown','keyUp']:
            p={'type':kind,'key':'Enter','code':'Enter','windowsVirtualKeyCode':13}
            if kind=='keyDown':p.update(text='\r',unmodifiedText='\r')
            session.call('Input.dispatchKeyEvent',p)
        deadline=time.monotonic()+30;receipt={}
        while time.monotonic()<deadline:
            receipt=evaluate('window.__d1Submission')
            if receipt.get('http') is not None:break
            time.sleep(.4)
        record['submit_receipt']=receipt
        if len(receipt.get('accepted',[]))==1:
            record['job_id']=receipt['accepted'][0]['job_id'];record['status']='accepted'
        record['post_submit_text']=evaluate('document.body.innerText.slice(0,2400)')
        path.write_text(json.dumps(record,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
        value={'status':record['status'],'receipt':receipt,'text':record['post_submit_text'][:700]}
    elif args.action=='status':
        path=Path(args.file);record=json.loads(path.read_text(encoding='utf-8'));assert record.get('job_id')
        value=evaluate('''(id=>{const links=[...document.querySelectorAll('a[href*="/jobs/'+id+'"]')];let section=links[0];for(let n=0;n<8&&section&&!section.innerText.includes('Rerun');n++)section=section.parentElement;const folder=[...document.querySelectorAll('div')].find(e=>e.childElementCount===0&&e.textContent==='东北迪斯科参考图');return {job_id:id,links:links.map(a=>({href:a.getAttribute('href'),images:[...a.querySelectorAll('img')].map(i=>({loaded:i.complete,width:i.naturalWidth,height:i.naturalHeight}))})),text:section?.innerText?.slice(0,2300)||document.body.innerText.slice(0,1800),folder:folder?.parentElement?.parentElement?.innerText,folderSelected:folder?.parentElement?.parentElement?.className.includes('bg-splash')}})('''+json.dumps(record['job_id'])+')')
        record['last_status']=value;record['status_checked_at']=datetime.now(timezone.utc).isoformat()
        if len(value['links'])==4 and all(any(i['loaded'] and i['width']>0 for i in a['images']) for a in value['links']) and 'Cancel' not in value['text']:
            record['status']='completed'
            record['group_membership_verified']=bool(value['folderSelected'])
        path.write_text(json.dumps(record,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');value['record_status']=record['status']
    print(json.dumps(value,ensure_ascii=False))
finally:session.close()
