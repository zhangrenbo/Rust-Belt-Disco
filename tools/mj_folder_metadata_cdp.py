"""Inspect metadata response types for the user's MJ folder in a hidden Chrome target."""
import importlib.util
import json
import sys
import time
from pathlib import Path
import websocket

ROOT=Path(__file__).resolve().parents[1]
sys.stdout.reconfigure(encoding='utf-8')
spec=importlib.util.spec_from_file_location('direct_cdp',ROOT/'scripts/record-catalog-cdp-session.py')
cdp=importlib.util.module_from_spec(spec);spec.loader.exec_module(cdp)
browser=cdp.Connection(cdp.discover('json/version')['webSocketDebuggerUrl'])
target=browser.call('Target.createTarget',{'url':'about:blank#mj-folder-meta','background':True})['targetId']
try:
    tab=next(tab for tab in cdp.discover('json/list') if tab['id']==target)
    ws=websocket.create_connection(tab['webSocketDebuggerUrl'],timeout=12,suppress_origin=True,http_proxy_host=None)
    serial=0
    def call(method,params=None):
        global serial
        serial+=1;rid=serial;ws.send(json.dumps({'id':rid,'method':method,'params':params or {}}))
        while True:
            msg=json.loads(ws.recv())
            if msg.get('id')==rid:return msg.get('result',{})
    try:
        call('Page.enable');call('Network.enable')
        call('Page.navigate',{'url':'https://www.midjourney.com/imagine?folder=%E4%B8%9C%E5%8C%97%E8%BF%AA%E6%96%AF%E7%A7%91%E5%8F%82%E8%80%83%E5%9B%BE'})
        found=[];end=time.monotonic()+10
        while time.monotonic()<end:
            ws.settimeout(max(.1,end-time.monotonic()))
            try:msg=json.loads(ws.recv())
            except websocket.WebSocketTimeoutException:break
            if msg.get('method')=='Network.responseReceived':
                info=msg['params']['response'];url=info.get('url','')
                if '/api/' in url and ('collections' in url or 'jobs' in url):
                    found.append({'path':url.split('?',1)[0].replace('https://www.midjourney.com',''),
                                  'http':info.get('status'),'type':info.get('mimeType')})
        print(json.dumps({'responses':found[:30],'count':len(found)},ensure_ascii=False))
    finally:ws.close()
finally:
    browser.call('Target.closeTarget',{'targetId':target});browser.close()
