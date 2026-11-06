"""Inspect official screenshot thumbnails in a task-only Chrome page; never use the sheet as an upload."""
import importlib.util,json,urllib.request,time,base64
from urllib.parse import quote
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cdp',ROOT/'scripts/record-catalog-cdp-session.py');cdp=importlib.util.module_from_spec(spec);spec.loader.exec_module(cdp)
with urllib.request.urlopen('https://store.steampowered.com/api/appdetails?appids=632470',timeout=12) as r:data=json.load(r)['632470']['data']
shots=data['screenshots'];(ROOT/'docs/disco-steam-reference-sources.json').write_text(json.dumps(shots,indent=2),encoding='utf-8')
v=cdp.discover('json/version');b=cdp.Connection(v['webSocketDebuggerUrl']);target=b.call('Target.createTarget',{'url':'about:blank#disco-ref-inspection','background':True})['targetId'];t=next(t for t in cdp.discover('json/list') if t['id']==target);c=cdp.Connection(t['webSocketDebuggerUrl'])
try:
 c.call('Page.enable');c.call('Emulation.setFocusEmulationEnabled',{'enabled':True});c.call('Emulation.setDeviceMetricsOverride',{'width':1440,'height':1120,'deviceScaleFactor':1,'mobile':False})
 html='<body style="background:#252525;color:white;margin:10px;display:grid;grid-template-columns:repeat(4,1fr);gap:8px;font:16px sans-serif">'+''.join('<div><b>'+str(s['id'])+'</b><img style="display:block;width:100%" src="'+s['path_thumbnail']+'"></div>' for s in shots)+'</body>'
 c.call('Page.navigate',{'url':'data:text/html;charset=utf-8,'+quote(html)})
 deadline=time.monotonic()+20
 while time.monotonic()<deadline:
  r=c.call('Runtime.evaluate',{'expression':'document.images.length>0&&[...document.images].every(i=>i.complete)','returnByValue':True})
  if r['result'].get('value'):break
  time.sleep(.3)
 c.call('Page.bringToFront')
 png=c.call('Page.captureScreenshot',{'format':'png','fromSurface':True})
 (ROOT/'docs/disco-steam-reference-inspection.png').write_bytes(base64.b64decode(png['data']))
 print('Reference inspection screenshot ready; original image URLs retained separately.')
finally:c.close();b.call('Target.closeTarget',{'targetId':target});b.close()
