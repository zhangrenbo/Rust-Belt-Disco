"""Check reference artwork against live archive slots in an isolated CDP page."""
import ast
import importlib.util
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('save_cdp', ROOT/'scripts/record-catalog-cdp-session.py')
cdp = importlib.util.module_from_spec(spec)
spec.loader.exec_module(cdp)
tree = ast.parse((ROOT/'scripts/qa-save-slot-detail-cdp.py').read_text(encoding='utf-8-sig'))
INIT = next(ast.literal_eval(n.value) for n in tree.body if isinstance(n, ast.Assign)
            and any(isinstance(t, ast.Name) and t.id == 'INIT' for t in n.targets))
INIT += ";window.__saveArtErrors=[];addEventListener('error',e=>{if(e.error)window.__saveArtErrors.push(String(e.error));});"
SNAPSHOT = "JSON.stringify(Object.fromEntries(Array.from({length:localStorage.length},(_,i)=>localStorage.key(i)).sort().map(k=>[k,localStorage.getItem(k)])))"
checks, screens, layouts = [], [], []
page = cdp.CatalogPage(INIT)

def check(name, expression):
    page.wait('Boolean('+expression+')')
    checks.append(name)
    print('PASS '+name, flush=True)

def settled():
    page.wait("!document.querySelector('#app').classList.contains('is-view-switching')")

def enter():
    settled()
    page.click('.mainmenu-actions [data-open-view=entry]')
    page.wait("document.querySelector('.view-entry.active')&&!document.querySelector('#app').classList.contains('is-view-switching')")

def capture(state, width, height):
    page.resize(width, height)
    settled()
    page.pointer(width-2, height-2)
    path = ROOT/f'docs/save-reference-{state}-{width}.png'
    bounds = page.capture(path)
    screens.append({'state':state, 'viewport':[width,height], 'path':str(path), 'gameBounds':bounds})

def open_slot(slot):
    settled()
    page.click(f'.view-entry [data-slot="{slot}"]')
    page.wait("document.querySelector('#slot-dialog').open")

try:
    page.resize(1440, 810)
    page.cdp.call('Page.navigate', {'url':'http://127.0.0.1:5173/'})
    page.wait("document.querySelectorAll('.record-track').length===2&&document.querySelector('#app').dataset.musicState&&!document.querySelector('#app').classList.contains('is-view-switching')")
    before = page.evaluate(SNAPSHOT)
    enter()
    check('Edited reference decodes at 1672 by 941', "document.querySelector('.entry-reference-art').complete&&document.querySelector('.entry-reference-art').naturalWidth===1672&&document.querySelector('.entry-reference-art').naturalHeight===941")
    for width, height in [(1440,810),(1058,595),(844,475)]:
        page.resize(width, height)
        page.pointer(width-2,height-2)
        check(f'{width}: all six artwork positions receive clicks', """(()=>{
          const art=document.querySelector('.entry-reference-art').getBoundingClientRect();
          const faces=[[538,331],[1126,331],[538,545],[1126,545],[540,742],[1123,742]];
          return faces.every(([x,y],i)=>document.querySelector('.view-entry [data-slot="0'+(i+1)+'"]')
            .contains(document.elementFromPoint(art.x+x/1672*art.width,art.y+y/941*art.height)));})()""")
        check(f'{width}: titles and descriptions fit their printed fields', """[...document.querySelectorAll('.view-entry [data-save-title],.view-entry [data-save-description],#entry-save-heading')].every(e=>{
          const r=e.getBoundingClientRect(),range=document.createRange();range.selectNodeContents(e);const t=range.getBoundingClientRect();
          return t.left>=r.left-1&&t.right<=r.right+1&&t.top>=r.top-2&&t.bottom<=r.bottom+2;
        })""")
        check(f'{width}: independent masked ticket art with no legacy borders', """[...document.querySelectorAll('.view-entry [data-slot]')].every(e=>{
          const c=getComputedStyle(e);return c.backgroundImage==='none'&&c.transform==='none'&&c.borderWidth==='0px'
            &&getComputedStyle(e,'::before').backgroundImage.includes('save-archive-reference-v1.png')
            &&getComputedStyle(e,'::before').maskImage.includes('save-ticket-mask-'+e.dataset.slot+'.svg')
            &&getComputedStyle(e,'::after').content==='none';})""")
        check(f'{width}: live empty, saved and damaged states', """document.querySelector('.view-entry [data-slot="01"]').dataset.saveStatus==='empty'
          &&document.querySelector('.view-entry [data-slot="02"]').dataset.saveStatus==='saved'
          &&document.querySelector('.view-entry [data-slot="04"]').dataset.saveStatus==='error'
          &&document.querySelector('.view-entry [data-slot="01"] .entry-save-preview').hidden
          &&!document.querySelector('.view-entry [data-slot="02"] .entry-save-preview').hidden""")
        check(f'{width}: existing film layers stay above artwork without intercepting input', "document.querySelector('#app').dataset.film==='on'&&document.querySelectorAll('.projector-edge-snow>i').length===53&&getComputedStyle(document.querySelector('.grain')).pointerEvents==='none'")
        capture('mixed', width, height)
        layouts.append(page.evaluate("""[...document.querySelectorAll('.view-entry [data-slot]')].map(e=>{
          const r=e.getBoundingClientRect();return {slot:e.dataset.slot,state:e.dataset.saveStatus,title:e.querySelector('[data-save-title]').textContent,
            rect:{x:r.x,y:r.y,width:r.width,height:r.height}};})"""))
        for slot in ['01','02','03','04','05','06']:
            page.pointer(width-2,height-2)
            page.wait(f"getComputedStyle(document.querySelector('.view-entry [data-slot=\"{slot}\"]')).transform==='none'")
            rest=page.rect(f'.view-entry [data-slot="{slot}"]')
            page.pointer(rest['x']+rest['width']/2,rest['y']+rest['height']/2)
            check(f'{width}: slot {slot} lifts its paper and exposes its action', f"""(()=>{{
              const b=document.querySelector('.view-entry [data-slot="{slot}"]'),a=b.querySelector('[data-save-action]');
              return b.getBoundingClientRect().y<{rest['y']}-.5&&getComputedStyle(b).filter.includes('brightness')
                &&getComputedStyle(a).clipPath==='none'&&a.getBoundingClientRect().width>30;
            }})()""")
            open_slot(slot)
            check(f'{width}: slot {slot} opens the correct detail and permitted actions', f"""document.querySelector('#dialog-slot').textContent==='{slot}'
              &&document.querySelectorAll('.station-slot-actions button')[0].disabled==={'false' if slot=='02' else 'true'}
              &&document.querySelectorAll('.station-slot-actions button')[1].disabled""")
            page.click('.slot-detail-close')
    page.resize(1440,810)
    settled()
    page.pointer(1438,808)
    page.wait("getComputedStyle(document.querySelector('.view-entry [data-slot=\"01\"]')).transform==='none'")
    r=page.rect('.view-entry [data-slot="01"]')
    page.pointer(r['x']+r['width']/2,r['y']+r['height']/2)
    check('Hover clearly changes the paper shadow', "getComputedStyle(document.querySelector('.view-entry [data-slot=\"01\"]')).filter.includes('brightness(1.07)')")
    path=ROOT/'docs/save-reference-hover-1440.png'
    page.capture(path);screens.append({'state':'hover','path':str(path),'viewport':[1440,810]})
    page.cdp.call('Input.dispatchMouseEvent',{'type':'mousePressed','x':r['x']+r['width']/2,'y':r['y']+r['height']/2,'button':'left','clickCount':1})
    check('Pressing the paper gives a distinct pressed state', "document.querySelector('.view-entry [data-slot=\"01\"]').matches(':active')&&getComputedStyle(document.querySelector('.view-entry [data-slot=\"01\"]')).filter.includes('brightness(0.98)')")
    page.cdp.call('Input.dispatchMouseEvent',{'type':'mouseReleased','x':r['x']+r['width']/2,'y':r['y']+r['height']/2,'button':'left','clickCount':1})
    check('Releasing opens the actual archive detail', "document.querySelector('#slot-dialog').open")
    page.click('.slot-detail-close')
    page.cdp.call('Emulation.setEmulatedMedia',{'features':[{'name':'prefers-reduced-motion','value':'reduce'}]})
    page.pointer(r['x']+r['width']/2,r['y']+r['height']/2)
    check('Reduced motion retains feedback without moving paper', "getComputedStyle(document.querySelector('.view-entry [data-slot=\"01\"]')).transform==='none'&&getComputedStyle(document.querySelector('.view-entry [data-slot=\"01\"]')).filter.includes('brightness')")
    page.cdp.call('Emulation.setEmulatedMedia',{'features':[{'name':'prefers-reduced-motion','value':'no-preference'}]})
    page.focus('.view-entry [data-slot="01"]')
    page.key('Tab')
    check('Keyboard focus is visible on the next ticket', "document.activeElement.matches('.view-entry [data-slot="+'"02"'+"]:focus-visible')&&getComputedStyle(document.activeElement).outlineStyle==='solid'")
    page.key('Enter')
    check('Keyboard opens the saved ticket', "document.querySelector('#slot-dialog').open&&document.querySelector('#dialog-slot').textContent==='02'")
    page.key('Escape')
    check('Escape closes detail and retains archive page', "!document.querySelector('#slot-dialog').open&&document.querySelector('.view-entry.active')")
    open_slot('02')
    page.click('.station-slot-actions button:first-child')
    check('Reading an effective save enters the existing UI-only scene', "document.querySelector('[data-view=ui-only].active')")
    settled()
    page.click('.ui-only-notice-button')
    check('UI-only return goes to main menu', "document.querySelector('.view-mainmenu.active')")
    enter()
    settled()
    page.click('.entry-main-return')
    check('Reference return button goes to main menu', "document.querySelector('.view-mainmenu.active')")
    check('Browsing and reading preserved all fixture data', SNAPSHOT+'==='+json.dumps(before))
    # Change only isolated fixture state to inspect the complete empty reference.
    page.evaluate("window.__saved02=localStorage.getItem('northeast-disco:campaign:v1:02');localStorage.removeItem('northeast-disco:campaign:v1:02');localStorage.removeItem('northeast-disco:campaign:v1:04');")
    enter()
    check('All six empty states are live', "[...document.querySelectorAll('.view-entry [data-slot]')].every(e=>e.dataset.saveStatus==='empty')")
    for width, height in [(1440,810),(1058,595),(844,475)]:
        capture('empty', width, height)
    page.evaluate("localStorage.setItem('northeast-disco:campaign:v1:02',window.__saved02);localStorage.setItem('northeast-disco:campaign:v1:04','damaged save')")
    check('Fixture state restored; unrelated data intact', SNAPSHOT+'==='+json.dumps(before))
    page.key('Escape')
    check('Escape from the archive returns to main menu', "document.querySelector('.view-mainmenu.active')")
    check('No JavaScript exceptions', 'window.__saveArtErrors.length===0')
    report={'date':'2026-09-14','transport':'fixed work Chrome, background target-specific direct CDP 9223',
            'checks':checks,'screens':screens,'layouts':layouts,'errors':page.evaluate('window.__saveArtErrors'),
            'storage':'isolated in-memory fixtures; actual player saves untouched','audio':'silent QA output',
            'humanArtReview':'pending','electronWindowVerified':False}
    (ROOT/'docs/save-reference-verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({'passed':len(checks),'screens':len(screens)}),flush=True)
except Exception:
    if page.evaluate("!!document.querySelector('[data-screenshot-root]')"):
        print(json.dumps(page.evaluate("""[...document.querySelectorAll('.view-entry [data-save-title],.view-entry [data-save-description],#entry-save-heading')].map(e=>{
          const r=e.getBoundingClientRect(),range=document.createRange();range.selectNodeContents(e);const t=range.getBoundingClientRect();
          return {text:e.textContent,font:getComputedStyle(e).font,spacing:getComputedStyle(e).letterSpacing,
            box:[r.x,r.y,r.width,r.height],textBox:[t.x,t.y,t.width,t.height]};})"""),ensure_ascii=True))
        page.capture(ROOT/'docs/save-reference-qa-failure.png')
    raise
finally:
    page.close()
