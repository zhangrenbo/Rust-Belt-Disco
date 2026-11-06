"""Check the real station dialogue with memory-only campaign fixtures."""
import ast
import importlib.util
import json
import sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('dialog_cdp',ROOT/'scripts/record-catalog-cdp-session.py')
cdp=importlib.util.module_from_spec(spec);spec.loader.exec_module(cdp)
tree=ast.parse((ROOT/'scripts/qa-character-newspaper-cdp.py').read_text(encoding='utf-8-sig'))
INIT=next(ast.literal_eval(n.value) for n in tree.body if isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='INIT' for t in n.targets))
fixture=dict(version=1,character='dong-dianshi',scene='A000',node='greeting',answers={},inspected=[],position=dict(x=800,y=500),warningHeard=False,shadow=None,completed=False,updatedAt='2026-09-15T00:00:00.000Z')
INIT+=';localStorage.setItem("northeast-disco:campaign:v1:auto",'+json.dumps(json.dumps(fixture))+');localStorage.setItem("qa-unrelated","preserved");'
page=cdp.CatalogPage(INIT)
before='--before' in sys.argv
checks=[];screens=[];measurements=[]
def check(name,expression):
    page.wait('Boolean('+expression+')');checks.append(name);print('PASS '+name,flush=True)
def node(name):
    page.wait('document.querySelector("[data-view=station]").dataset.storyNode==='+json.dumps(name))
def capture(name,w,h):
    page.resize(w,h);page.pointer(w*.95,h*.2)
    if name=='warning':
        page.focus('.station-dialogue-copy')
        for kind in ['rawKeyDown','keyUp']:page.cdp.call('Input.dispatchKeyEvent',{'type':kind,'key':'Home','code':'Home','windowsVirtualKeyCode':36})
        check(f'{w}: Home returns to the first dialogue line',"document.querySelector('.station-dialogue-copy').scrollTop===0")
    page.wait('Boolean([...document.querySelectorAll("#station-talk img")].every(i=>i.complete&&i.naturalWidth>0))')
    path=ROOT/f'docs/dialogue-layout-{name}-{w}.png';page.capture(path);screens.append(str(path))
    metrics=page.evaluate('''(()=>{const rect=e=>{const r=e.getBoundingClientRect();return {x:r.x,y:r.y,w:r.width,h:r.height}};const q=s=>document.querySelector(s),body=q('.station-dialogue-copy');return {panel:rect(q('#station-talk .disco-dialog-panel')),copy:rect(body),font:parseFloat(getComputedStyle(body).fontSize),scrollHeight:body.scrollHeight,clientHeight:body.clientHeight,choices:[...q('#station-choices').children].map(e=>({rect:rect(e),font:parseFloat(getComputedStyle(e.querySelector('span')).fontSize)}))}})()''')
    measurements.append(dict(name=name,width=w,metrics=metrics))
    if not before:
        assert metrics['font']>=14
        assert all(c['font']>=14 for c in metrics['choices'])
        assert metrics['panel']['w']<=w*.8
        assert metrics['choices'][0]['rect']['y']>=w*.17
        check(f'{w}/{name}: all choices accept input and stay above the panel', '''[...document.querySelectorAll('#station-choices button')].every(b=>{const r=b.getBoundingClientRect(),p=document.querySelector('#station-talk .disco-dialog-panel').getBoundingClientRect();return r.bottom<=p.top&&r.left>=0&&r.right<=innerWidth&&b.contains(document.elementFromPoint(r.x+r.width/2,r.y+r.height/2));})''')
try:
    page.resize(1440,810);page.cdp.call('Page.navigate',{'url':'http://127.0.0.1:5173/'})
    page.wait("document.querySelector('#app')?.dataset.musicState&&document.querySelectorAll('.record-track').length===2",20)
    page.click('.mainmenu-choice-continue')
    check('Continue opens actual saved dialogue',"document.querySelector('#station-talk').open&&!document.querySelector('#app').classList.contains('is-view-switching')")
    if before:
        capture('before',1440,810)
    else:
        for w,h in [(1440,810),(844,475)]:capture('greeting',w,h)
    for expected in ['greeting-memory','greeting-question','background']:
        page.click('#station-choices button');node(expected)
    if not before:
        for w,h in [(1440,810),(1058,595),(844,475)]:capture('four-choices',w,h)
        for expected in ['value','fear','warning']:
            page.focus('#station-choices button');page.key('Enter');node(expected)
        for w,h in [(1440,810),(844,475)]:
            capture('warning',w,h)
            page.focus('.station-dialogue-copy')
            for kind in ['rawKeyDown','keyUp']:page.cdp.call('Input.dispatchKeyEvent',{'type':kind,'key':'End','code':'End','windowsVirtualKeyCode':35,'nativeVirtualKeyCode':35})
            check(f'{w}: long dialogue is fully reachable by keyboard',"(()=>{const e=document.querySelector('.station-dialogue-copy');return e.scrollHeight-e.clientHeight-e.scrollTop<2})()")
        page.click('#station-talk-close');check('Close collapses dialogue and exposes Resume',"!document.querySelector('#station-talk').open&&!document.querySelector('#station-talk-resume').hidden")
        page.click('#station-talk-resume');check('Resume restores dialogue',"document.querySelector('#station-talk').open")
        page.key('Escape');check('Escape collapses dialogue',"!document.querySelector('#station-talk').open")
        check('Unrelated data preserved and no exceptions',"localStorage.getItem('qa-unrelated')==='preserved'&&window.__characterErrors.length===0")
    report=dict(date='2026-09-15',checks=checks,screens=screens,measurements=measurements,storage='isolated memory fixture; real saves untouched',transport='fixed work Chrome direct CDP 9223',audio='silent',electronWindowVerified=False,artReview='pending user review')
    (ROOT/('docs/dialogue-layout-before.json' if before else 'docs/dialogue-layout-verification.json')).write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(dict(checks=len(checks),screens=len(screens))),flush=True)
finally:page.close()
