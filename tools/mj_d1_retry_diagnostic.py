"""One native retry of the explicitly failed reference submission; network metadata only."""
import importlib.util,json,sys,time
from pathlib import Path
from urllib.parse import urlsplit
from websocket import WebSocketTimeoutException
ROOT=Path(__file__).resolve().parents[1]
sys.stdout.reconfigure(encoding='utf-8')
spec=importlib.util.spec_from_file_location('cdp',ROOT/'scripts/record-catalog-cdp-session.py');cdp=importlib.util.module_from_spec(spec);spec.loader.exec_module(cdp)
tab=next(t for t in cdp.discover('json/list') if t['id']=='97D1C30E256BE7F9AE334AB7AB0E6983')
c=cdp.Connection(tab['webSocketDebuggerUrl'])
def ev(js):
 r=c.call('Runtime.evaluate',{'expression':js,'returnByValue':True});assert not r.get('exceptionDetails'),r;return r['result'].get('value')
try:
 assert ev("document.body.innerText.includes('Creation failed\\nFailed to fetch\\nIllustrated isometric game environment')")
 c.call('Network.enable')
 r=ev("(()=>{const b=[...document.querySelectorAll('button')].find(b=>b.innerText==='Rerun'),r=b.getBoundingClientRect();return {x:r.x+r.width/2,y:r.y+r.height/2}})()")
 for kind in ['mousePressed','mouseReleased']:c.call('Input.dispatchMouseEvent',{'type':kind,'x':r['x'],'y':r['y'],'button':'left','clickCount':1})
 c.ws.settimeout(2);deadline=time.monotonic()+18;events=[]
 while time.monotonic()<deadline:
  try:m=json.loads(c.ws.recv())
  except WebSocketTimeoutException:continue
  p=m.get('params',{});kind=m.get('method')
  if kind=='Network.requestWillBeSent':
   u=urlsplit(p['request']['url']);events.append({'kind':'request','id':p['requestId'],'url':u.scheme+'://'+u.netloc+u.path,'method':p['request']['method']})
  elif kind=='Network.loadingFailed':events.append({'kind':'failure','id':p['requestId'],'error':p.get('errorText'),'cors':p.get('corsErrorStatus')})
 c.ws.settimeout(12)
 receipt=ev('window.__d1Submission');text=ev('document.body.innerText.slice(0,1300)')
 path=ROOT/'美术概念/MJ_D1叔叔家实机风格参考_v2_20260915_生成记录.json';record=json.loads(path.read_text(encoding='utf-8'))
 record.setdefault('retry_diagnostics',[]).append({'events':events,'receipt':receipt,'ui':text})
 if len(receipt.get('accepted',[]))==1:record.update(status='accepted',job_id=receipt['accepted'][0]['job_id'],submit_receipt=receipt)
 path.write_text(json.dumps(record,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
 print(json.dumps({'events':events,'receipt':receipt,'ui':text},ensure_ascii=False))
finally:c.close()
