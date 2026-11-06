"""Direct, target-specific CDP for background record-catalog QA; no browser-wide attach."""
import base64
import json
import os
import time
import urllib.request
import uuid
from pathlib import Path
import websocket

os.environ['NO_PROXY']='localhost,127.0.0.1'
HTTP=urllib.request.build_opener(urllib.request.ProxyHandler({}))

def discover(path):
    with HTTP.open('http://127.0.0.1:9223/'+path,timeout=5) as response:
        return json.load(response)

class Connection:
    def __init__(self,url):
        self.ws=websocket.create_connection(url,timeout=12,suppress_origin=True,http_proxy_host=None)
        self.counter=0
    def call(self,method,params=None):
        self.counter+=1
        self.ws.send(json.dumps({'id':self.counter,'method':method,'params':params or {}}))
        deadline=time.monotonic()+15
        while time.monotonic()<deadline:
            result=json.loads(self.ws.recv())
            if result.get('id')==self.counter:
                if 'error' in result: raise RuntimeError(method+': '+str(result['error']))
                return result.get('result',{})
        raise TimeoutError(method)
    def close(self): self.ws.close()

class CatalogPage:
    def __init__(self,init):
        version=discover('json/version')
        assert version['Browser'].startswith('Chrome/')
        self.browser=Connection(version['webSocketDebuggerUrl'])
        task_url='about:blank#record-catalog-native-'+str(uuid.uuid4())
        self.target=self.browser.call('Target.createTarget',{'url':task_url,'background':True})['targetId']
        descriptor=next(t for t in discover('json/list') if t['id']==self.target)
        assert descriptor['url']==task_url
        self.cdp=Connection(descriptor['webSocketDebuggerUrl'])
        self.cdp.call('Page.enable')
        self.cdp.call('Runtime.enable')
        self.cdp.call('Network.enable')
        self.cdp.call('Network.setBlockedURLs',{'urls':['http://127.0.0.1:5173/@vite/client']})
        self.cdp.call('Page.addScriptToEvaluateOnNewDocument',{'source':init})
        self.cdp.call('Emulation.setFocusEmulationEnabled',{'enabled':True})
        try: self.cdp.call('Page.setWebLifecycleState',{'state':'active'})
        except Exception as e: print('Lifecycle:',str(e)[:100])
    def evaluate(self,expression):
        result=self.cdp.call('Runtime.evaluate',{'expression':expression,'returnByValue':True,'awaitPromise':True,'userGesture':True})
        if 'exceptionDetails' in result: raise RuntimeError(str(result['exceptionDetails']))
        return result['result'].get('value')
    def wait(self,expression,timeout=10):
        deadline=time.monotonic()+timeout
        while time.monotonic()<deadline:
            if self.evaluate(expression): return
            time.sleep(.1)
        raise TimeoutError(expression)
    def resize(self,width,height):
        self.cdp.call('Emulation.setDeviceMetricsOverride',{'width':width,'height':height,'deviceScaleFactor':1,'mobile':False})
    def rect(self,selector):
        return self.evaluate('(()=>{const r=document.querySelector('+json.dumps(selector)+').getBoundingClientRect();return {x:r.x,y:r.y,width:r.width,height:r.height};})()')
    def pointer(self,x,y):
        self.cdp.call('Input.dispatchMouseEvent',{'type':'mouseMoved','x':x,'y':y})
    def click(self,selector):
        r=self.rect(selector)
        self.click_at(r['x']+r['width']/2,r['y']+r['height']/2)
    def click_at(self,x,y):
        self.pointer(x,y)
        for kind in ['mousePressed','mouseReleased']:
            self.cdp.call('Input.dispatchMouseEvent',{'type':kind,'x':x,'y':y,'button':'left','clickCount':1})
    def focus(self,selector):
        self.evaluate('document.querySelector('+json.dumps(selector)+').focus()')
    def key(self,key):
        code,vk={'Enter':('Enter',13),'Space':('Space',32),'Tab':('Tab',9),'Escape':('Escape',27)}[key]
        for kind in ['keyDown','keyUp']:
            event={'type':kind,'key':' ' if key=='Space' else key,'code':code,'windowsVirtualKeyCode':vk}
            if kind=='keyDown' and key in ['Enter','Space']:
                event['text']=event['unmodifiedText']='\r' if key=='Enter' else ' '
            self.cdp.call('Input.dispatchKeyEvent',event)
    def capture(self,path):
        r=self.rect('[data-screenshot-root]')
        result=self.cdp.call('Page.captureScreenshot',{'format':'png','fromSurface':True,'captureBeyondViewport':False,'clip':{**r,'scale':1}})
        Path(path).write_bytes(base64.b64decode(result['data']))
        return r
    def close(self):
        info=self.browser.call('Target.getTargetInfo',{'targetId':self.target})['targetInfo']
        assert info['targetId']==self.target
        self.cdp.close()
        self.browser.call('Target.closeTarget',{'targetId':self.target})
        self.browser.close()
