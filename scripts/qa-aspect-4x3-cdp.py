"""Inspect 4:3 layout in the persistent work Chrome via target-specific CDP."""
import importlib.util
import json
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('cdp', ROOT/'scripts/record-catalog-cdp-session.py')
cdp = importlib.util.module_from_spec(spec)
spec.loader.exec_module(cdp)
OUT = ROOT/'docs'
INIT = """if(!sessionStorage.qaReady){localStorage.clear();sessionStorage.qaReady='1'} Object.defineProperty(HTMLMediaElement.prototype,'play',{configurable:true,value:function(){return Promise.resolve()}});"""

def snapshot(page, name):
    page.capture(OUT/f'aspect-4x3-{name}.png')
    result = page.evaluate("""(() => {
      const stage = document.querySelector('#app').getBoundingClientRect();
      const view = document.querySelector('.view.active');
      const rows = [...view.querySelectorAll('button, [role=button], .event-center-menu, .entry-directory, .settings-dossier, .character-camera, .station-talk, .station-toolbar')]
        .filter(e => { const r=e.getBoundingClientRect(); return r.width>1 && r.height>1 && getComputedStyle(e).visibility!=='hidden'; })
        .map(e => { const r=e.getBoundingClientRect(); return {name:e.id||e.className||e.tagName,x:Math.round(r.x-stage.x),y:Math.round(r.y-stage.y),w:Math.round(r.width),h:Math.round(r.height),outside:r.right<stage.left||r.left>stage.right||r.bottom<stage.top||r.top>stage.bottom}; });
      return {stage:{w:stage.width,h:stage.height},view:view.dataset.view,items:rows};
    })()""")
    assert abs(result['stage']['w']/result['stage']['h']-4/3)<.002,result['stage']
    assert not [item for item in result['items'] if item['outside']],name
    return result

page=cdp.CatalogPage(INIT)
try:
    page.resize(1200,900)
    page.cdp.call('Page.navigate',{'url':'http://127.0.0.1:5173/'})
    page.wait('document.querySelector(".view-mainmenu.active") && document.querySelector("#app").clientWidth===1200',20)
    report={'menu':snapshot(page,'menu')}
    page.evaluate('document.querySelector(".mainmenu-choice-start").click()')
    page.wait('!!document.querySelector(".view-character-select.active")')
    report['character']=snapshot(page,'character')
    page.evaluate('document.querySelector(".character-return").click()')
    page.wait('!!document.querySelector(".view-mainmenu.active")')
    page.evaluate('document.querySelector(".mainmenu-choice-compact[data-open-view=settings]").click()')
    page.wait('!!document.querySelector(".view-settings.active")')
    report['settings']=snapshot(page,'settings')
    page.evaluate('document.querySelector("[data-settings-return]").click()')
    page.wait('!!document.querySelector(".view-mainmenu.active")')
    page.evaluate('document.querySelector(".mainmenu-choice-compact[data-open-view=entry]").click()')
    page.wait('!!document.querySelector(".view-entry.active")')
    report['saves']=snapshot(page,'saves')
    for name in ['gallery','profile','dialogue','exploration']:
        page.cdp.call('Page.navigate',{'url':'http://127.0.0.1:5173/?qa='+name+'#'+name})
        page.wait('!!document.querySelector("[data-view='+name+'].active")')
        report[name]=snapshot(page,name)
    page.cdp.call('Page.navigate',{'url':'http://127.0.0.1:5173/?qa=entry#entry'})
    page.wait('!!document.querySelector(".view-entry.active")')
    page.evaluate('document.querySelector(".view-entry [data-open-view=mainmenu]").click()')
    page.wait('!!document.querySelector(".view-mainmenu.active")')
    page.evaluate('document.querySelector(".mainmenu-choice-start").click()')
    page.wait('!!document.querySelector(".view-character-select.active")')
    page.evaluate('document.querySelector(".character-choice-center").click()')
    page.wait('!!document.querySelector(".view-station.active")')
    time.sleep(3)
    report['station']=snapshot(page,'station')
    world=page.evaluate('(()=>{const r=document.querySelector("#station-world").getBoundingClientRect(),s=document.querySelector(".station-night-shade").getBoundingClientRect();return {world:[r.width,r.height,r.y],shade:[s.width,s.height,s.y]}})()')
    assert abs(world['world'][0]/world['world'][1]-16/9)<.002,world
    assert all(abs(a-b)<1 for a,b in zip(world['world'],world['shade'])),world
    report['stationWorld']=world
    (OUT/'aspect-4x3-verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps({k:{'stage':v['stage'],'view':v['view'],'outside':[i['name'] for i in v['items'] if i['outside']]} for k,v in report.items() if 'stage' in v},ensure_ascii=False))
finally:
    page.close()

page=cdp.CatalogPage(INIT)
try:
    page.resize(1280,720)
    page.cdp.call('Page.navigate',{'url':'http://127.0.0.1:5173/'})
    page.wait('!!document.querySelector(".view-mainmenu.active")')
    wide=page.evaluate('({w:document.querySelector("#app").clientWidth,h:document.querySelector("#app").clientHeight})')
    assert abs(wide['w']/wide['h']-16/9)<.002,wide
    page.resize(800,600)
    compact=page.evaluate('({w:document.querySelector("#app").clientWidth,h:document.querySelector("#app").clientHeight})')
    assert abs(compact['w']/compact['h']-4/3)<.002,compact
    report['responsive']={'wide':wide,'compact':compact}
    (OUT/'aspect-4x3-verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    print('responsive',report['responsive'])
finally:
    page.close()

fixture=dict(version=1,character='dong-dianshi',scene='A000',node='greeting',answers={},inspected=[],position=dict(x=800,y=500),warningHeard=False,shadow=None,completed=False,updatedAt='2026-10-04T00:00:00.000Z')
dialogue_init=INIT+'localStorage.setItem("northeast-disco:campaign:v1:auto",'+json.dumps(json.dumps(fixture))+');'
page=cdp.CatalogPage(dialogue_init)
try:
    page.resize(1200,900)
    page.cdp.call('Page.navigate',{'url':'http://127.0.0.1:5173/'})
    page.wait('!!document.querySelector(".mainmenu-choice-continue")',20)
    page.evaluate('document.querySelector(".mainmenu-choice-continue").click()')
    page.wait('document.querySelector("#station-talk").open',20)
    report['dialogue']=snapshot(page,'dialogue')
    (OUT/'aspect-4x3-verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    print('dialogue',report['dialogue']['stage'])
finally:
    page.close()
