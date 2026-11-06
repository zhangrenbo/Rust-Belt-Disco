"""Restored A000 gameplay, real pointer movement and isolated save roundtrips."""
import ast
import importlib.util
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('game_cdp',ROOT/'scripts/record-catalog-cdp-session.py')
cdp=importlib.util.module_from_spec(spec);spec.loader.exec_module(cdp)
tree=ast.parse((ROOT/'scripts/qa-character-newspaper-cdp.py').read_text(encoding='utf-8-sig'))
INIT=next(ast.literal_eval(n.value) for n in tree.body if isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='INIT' for t in n.targets))
INIT+=";localStorage.setItem('northeast-disco:campaign:v1:06','preserved damaged manual save');localStorage.setItem('qa-unrelated','preserved');"
page=cdp.CatalogPage(INIT)
checks,screens=[],[]
def check(name,exp,timeout=15):
    page.wait('Boolean('+exp+')',timeout=timeout);checks.append(name);print('PASS '+name,flush=True)
def view(name):
    page.wait(f"document.querySelector('[data-view={name}].active')&&!document.querySelector('#app').classList.contains('is-view-switching')")
def node(name):
    page.wait("document.querySelector('[data-view=station]').dataset.storyNode==="+json.dumps(name))
def menu():
    page.click('#station-pause-button');page.wait("document.querySelector('#station-pause').open")
    page.click('#station-pause-menu');view('mainmenu')
def capture(name,w,h):
    page.resize(w,h);view('station')
    page.pointer(w/2,h/2)
    page.wait("[...document.querySelectorAll('#station-world img')].every(i=>i.complete&&i.naturalWidth>0)")
    p=ROOT/f'docs/gameplay-resume-{name}-{w}.png'
    screens.append({'path':str(p),'viewport':[w,h],'gameBounds':page.capture(p)})
try:
    page.resize(1440,810);page.cdp.call('Page.navigate',{'url':'http://127.0.0.1:5173/'})
    page.wait("document.querySelector('#app')?.dataset.musicState&&document.querySelectorAll('.record-track').length===2",timeout=20)
    view('mainmenu')
    check('Launch stays on the homepage and does not write an auto save',"document.querySelector('.view-mainmenu.active')&&!localStorage.getItem('northeast-disco:campaign:v1:auto')")
    page.click('.mainmenu-choice-start');view('character-select')
    check('Start first opens character selection without starting a journey',"!localStorage.getItem('northeast-disco:campaign:v1:auto')")
    page.focus('[data-character-choice="大厦崩塌"]');page.key('Enter');view('station');node('arrival')
    check('Confirmed Dong starts the existing playable night opening',"!document.querySelector('#ui-only-notice').classList.contains('active')&&!document.querySelector('#station-talk').open&&!document.querySelector('#station-grandmother-marker').hidden")
    for w,h in [(1440,810),(844,475)]:
        capture('opening',w,h)
        check(f'{w}: seated grandmother and marker accept input',"""(()=>{const b=document.querySelector('[data-hotspot=grandmother]'),m=document.querySelector('#station-grandmother-marker').getBoundingClientRect();return b.contains(document.elementFromPoint(m.x+m.width/2,m.y+m.height/2));})()""")
    page.resize(1440,810)
    x=page.evaluate("Number(document.querySelector('#station-player-vision').getAttribute('cx'))")
    page.click('#station-grandmother-marker')
    check('Actual walking moves the player and local light',f"Number(document.querySelector('#station-player-vision').getAttribute('cx'))>{x}+30",25)
    check('Arrival dialogue opens only after approaching the grandmother',"document.querySelector('#station-talk').open&&document.querySelector('[data-view=station]').dataset.storyNode==='arrival'",55)
    for expected in ['greeting','greeting-memory','greeting-question','background','value','fear','warning']:
        page.click('#station-choices button');node(expected)
    check('Original questions advance and retain answers',"Object.keys(JSON.parse(localStorage.getItem('northeast-disco:campaign:v1:auto')).answers).length===3")
    page.click('#station-choices li:nth-child(2) button');node('dont-turn-left')
    page.click('#station-choices button');node('shadow-intro')
    page.click('#station-choices button');node('shadow')
    page.click('#station-choices li:nth-child(4) button');node('ready')
    check('Warning and shadow choices unlock the exit',"!document.querySelector('#station-talk').open&&!document.querySelector('[data-hotspot=exit]').hidden")
    page.click('#station-pause-button');page.click('#station-pause-settings');view('settings')
    page.click('[data-settings-return]');view('station');node('ready')
    check('Settings returns to the same active journey',"document.querySelector('[data-view=station]').dataset.storyNode==='ready'")
    page.click('#station-save-button');view('entry')
    page.click('.view-entry [data-slot="01"]');page.wait("document.querySelector('#slot-dialog').open")
    check('Saving is enabled for an active journey',"!document.querySelectorAll('.station-slot-actions button')[1].disabled")
    page.click('.station-slot-actions button:nth-child(2)')
    check('Manual slot stores the actual progress',"JSON.parse(localStorage.getItem('northeast-disco:campaign:v1:01')).node==='ready'")
    manual=page.evaluate("localStorage.getItem('northeast-disco:campaign:v1:01')")
    page.click('#station-archive-return');view('station');menu()
    page.click('.mainmenu-choice-continue');view('station');node('ready')
    check('Continue restores real progress',"document.querySelector('#station-grandmother-marker').hidden")
    menu();page.click('.mainmenu-choice-start');view('character-select')
    page.focus('[data-character-choice="大厦崩塌"]');page.key('Enter');view('station');node('arrival')
    check('New game keeps existing manual slots',"localStorage.getItem('northeast-disco:campaign:v1:01')==="+json.dumps(manual))
    menu();page.click('.mainmenu-actions [data-open-view=entry]');view('entry')
    page.click('.view-entry [data-slot="06"]');page.wait("document.querySelector('#slot-dialog').open")
    check('Damaged manual data cannot be overwritten or loaded',"[...document.querySelectorAll('.station-slot-actions button')].slice(0,2).every(b=>b.disabled)")
    page.click('.slot-detail-close')
    page.click('.view-entry [data-slot="01"]');page.wait("document.querySelector('#slot-dialog').open")
    page.click('.station-slot-actions button:first-child');view('station');node('ready')
    check('Reading a manual slot restores the playable scene',"!document.querySelector('#ui-only-notice').classList.contains('active')")
    page.click('[data-hotspot=exit]')
    check('Walking to the exit completes the existing A000 chapter',"document.querySelector('[data-view=station]').dataset.storyNode==='complete'&&!document.querySelector('#station-completion').hidden",55)
    capture('complete',1440,810)
    check('Unrelated and damaged manual data remain untouched',"localStorage.getItem('qa-unrelated')==='preserved'&&localStorage.getItem('northeast-disco:campaign:v1:06')==='preserved damaged manual save'")
    check('No runtime exceptions',"window.__characterErrors.length===0")
    for route in ['ui-only','station']:
        page.cdp.call('Page.navigate',{'url':'http://127.0.0.1:5173/#'+route})
        # A hash navigation is same-document; reload to test cold startup routing.
        page.cdp.call('Page.reload',{'ignoreCache':True})
        page.wait("document.querySelector('#app')?.dataset.musicState&&document.querySelectorAll('.record-track').length===2",timeout=20)
        view('mainmenu')
        check('Direct '+route+' route returns safely to the menu without a campaign',"document.querySelector('.view-mainmenu.active')&&!document.querySelector('#ui-only-notice').classList.contains('active')")
    report={'date':'2026-09-15','transport':'fixed work Chrome, direct background target CDP 9223','checks':checks,'screens':screens,
      'storage':'isolated in-memory fixtures; real player saves untouched','audio':'silent QA output','electronWindowVerified':False,
      'scope':'Existing Dong/A000 restored. Li/Wang playable routes and subsequent chapters remain unimplemented.'}
    (ROOT/'docs/gameplay-resume-verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({'checks':len(checks),'screens':len(screens)}),flush=True)
except Exception:
    print(json.dumps(page.evaluate("({hidden:document.hidden,view:document.querySelector('.view.active')?.dataset.view,node:document.querySelector('[data-view=station]')?.dataset.storyNode,talk:document.querySelector('#station-talk')?.open,path:document.querySelector('#station-path')?.getAttribute('points'),position:document.querySelector('#station-player-vision')?.getAttribute('cx'),status:document.querySelector('#station-status')?.textContent,errors:window.__characterErrors})"),ensure_ascii=True),flush=True)
    raise
finally:
    page.close()
