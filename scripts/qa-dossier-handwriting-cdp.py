"""Check handwritten character records and dossier props with direct Chrome CDP."""
import base64
import json
import os
import sys
import time
import urllib.request
from contextlib import closing
from pathlib import Path

import websocket
from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[1]
os.environ['NO_PROXY'] = 'localhost,127.0.0.1'
checks, screens, errors = [], [], []
INIT = '''(() => {
  const values = new Map([['northeast-disco.settings.v1','{"music":0}'],['qa-unrelated','preserve']]);
  Object.defineProperty(window,'localStorage',{value:{
    getItem:k=>values.get(String(k))??null,setItem:(k,v)=>values.set(String(k),String(v)),
    removeItem:k=>values.delete(String(k)),clear:()=>values.clear(),key:i=>[...values.keys()][i]??null,
    get length(){return values.size}
  }});
})();'''

with sync_playwright() as p:
    browser = p.chromium.connect_over_cdp('http://127.0.0.1:9223', timeout=10000)
    context = browser.contexts[0]
    bc = browser.new_browser_cdp_session()
    old=set(context.pages)
    target=bc.send('Target.createTarget',{'url':'about:blank','background':True})['targetId']
    page=None;deadline=time.monotonic()+10
    while page is None and time.monotonic()<deadline:
        bc.send('Target.getTargets')
        for candidate in context.pages:
            if candidate in old or candidate.is_closed():continue
            probe=context.new_cdp_session(candidate)
            try:
                if probe.send('Target.getTargetInfo')['targetInfo']['targetId']==target:page=candidate;break
            finally:probe.detach()
        if page is None:time.sleep(.1)
    assert page is not None,'Task-created background target did not become ready'
    try:
        page.set_default_timeout(10000)
        page.route('**/@vite/client', lambda r:r.fulfill(status=200,content_type='application/javascript',body=''))
        page.add_init_script(INIT)
        page.on('pageerror', lambda error:errors.append(str(error)))
        cdp = context.new_cdp_session(page)
        try:
            cdp.send('Page.setWebLifecycleState', {'state':'active'})
        except Exception as error:
            print('Lifecycle:', str(error)[:120])
        cdp.send('Emulation.setFocusEmulationEnabled', {'enabled':True})

        def check(name, expression):
            try:
                page.wait_for_function(expression, polling=100, timeout=8000)
            except Exception:
                print('DIAGNOSTIC', page.evaluate("document.querySelector('.view.active').dataset.view"), errors, flush=True)
                raise
            checks.append(name)
            print(f'{len(checks)} PASS {name}', flush=True)

        def capture(name, width=1440, height=810):
            page.set_viewport_size({'width':width, 'height':height})
            page.wait_for_function("[...document.querySelectorAll('.view.active img')].filter(i=>i.hasAttribute('src')&&!i.hidden).every(i=>i.complete&&i.naturalWidth>0)")
            page.wait_for_function('document.querySelector("#dossier-record").classList.contains("has-handwritten-ink")')
            page.wait_for_function("getComputedStyle(document.querySelector('.view.active')).opacity==='1'",polling=100)
            page.mouse.move(width-2,height-2)
            for w in (width+1,width):
                cdp.send('Emulation.setDeviceMetricsOverride', {'width':w,'height':height,'deviceScaleFactor':1,'mobile':False})
            path = f'docs/dossier-handwriting-{name}-{width}.png'
            page.locator('#app.stage').screenshot(path=str(ROOT/path),timeout=12000)
            screens.append({'path':path,'viewport':[width,height]})
            print('Captured',path,flush=True)

        page.set_viewport_size({'width':1440,'height':810})
        page.goto('http://127.0.0.1:5173/',wait_until='domcontentloaded')
        page.locator('.mainmenu-actions [data-open-view="settings"]').click()
        page.locator('[data-film-strength]').fill('31')
        page.locator('[data-personnel-open]').click()
        check('original book is retained and four side buttons remain', "getComputedStyle(document.querySelector('.explorer-dossier-book')).backgroundImage.includes('exploration-dossier.png')&&document.querySelectorAll('.explorer-dossier-tabs button').length===4")
        people=page.locator('[data-dossier-person]').evaluate_all('(tabs)=>tabs.map(t=>({id:t.dataset.dossierPerson,name:t.textContent.trim()}))')
        portraits=[]
        for person in people:
            key,name=person['id'],person['name']
            page.locator(f'[data-dossier-person="{key}"]').click()
            check(f'{key}: side button selects its name and portrait', f'''async () => {{
              const face=document.querySelector('[data-dossier-face]');
              const photo=face.hidden?document.querySelector('[data-dossier-portrait]'):face;
              await photo.decode();
              return photo.naturalWidth>0&&photo.alt.includes({json.dumps(name)})&&document.querySelector('[data-dossier-name]').textContent==={json.dumps(name)}&&document.querySelector('.explorer-dossier-book').dataset.dossierCharacter==='{key}'&&document.querySelectorAll('.explorer-dossier-tabs [aria-selected=true]').length===1;
            }}''')
            portraits.append(page.evaluate("document.querySelector('[data-dossier-face]').hidden?document.querySelector('[data-dossier-portrait]').src:document.querySelector('[data-dossier-face]').src"))
            check(f'{key}: generated ink matches selected record and retains accessible text',f'''() => {{
              const paper=document.querySelector('#dossier-record'),ink=paper.querySelector('[data-dossier-handwriting]');
              return paper.classList.contains('has-handwritten-ink')&&!ink.hidden&&ink.naturalWidth>500
                &&ink.src.endsWith('dossier-handwriting-{key}-v1.png')&&ink.getAttribute('aria-hidden')==='true'
                &&getComputedStyle(paper).mixBlendMode==='multiply'
                &&getComputedStyle(paper.querySelector('.dossier-identity')).opacity==='0'
                &&getComputedStyle(paper.querySelector('.dossier-verification-stamp')).opacity==='1';
            }}''')
            capture(key)
            capture(key,844,475)
            if key=='dong':capture(key,1058,595)
            page.set_viewport_size({'width':1440,'height':810})
        assert len(set(portraits))==len(people),'Portraits must actually be different sources'
        check('dossier contains only identity, location and biography', "!document.querySelector('#dossier-record').hidden&&!document.querySelector('[data-attribute-actions]')&&!document.querySelector('[data-explorer-inc]')&&!/生命|速度|暴击|攻击|防御|等级|经验|3000/.test(document.querySelector('.view-explorer-dossier').textContent)")
        check('paper page covers tab roots but names remain clickable', '''() => {
          const book=document.querySelector('.explorer-dossier-book').getBoundingClientRect();
          return [...document.querySelectorAll('.explorer-dossier-tabs button')].every(tab=>{
            const r=tab.getBoundingClientRect(), range=document.createRange();range.selectNodeContents(tab);
            const name=range.getBoundingClientRect(), root=document.elementFromPoint(book.x+book.width*.85,r.y+r.height/2);
            return root?.classList.contains('dossier-book-foreground')&&tab.contains(document.elementFromPoint(name.x+name.width/2,name.y+name.height/2));
          });
        }''')
        page.locator('[data-dossier-person="dong"]').focus()
        page.keyboard.press('ArrowDown')
        check('keyboard arrows change character and actual portrait together', "document.activeElement.dataset.dossierPerson==='li'&&document.querySelector('.explorer-dossier-book').dataset.dossierCharacter==='li'&&document.querySelector('[data-dossier-face]').hidden")
        page.keyboard.press('End')
        check('End selects the last available character', f"document.activeElement.dataset.dossierPerson==='{people[-1]['id']}'")
        page.set_viewport_size({'width':844,'height':475})
        check('compact role buttons and return are reachable and labels fit', '''() => [...document.querySelectorAll('[data-dossier-return],.explorer-dossier-tabs button:not(:disabled)')].every(e=>{const r=e.getBoundingClientRect(),x=r.x+r.width/2,y=r.y+r.height/2,range=document.createRange();range.selectNodeContents(e);const text=range.getBoundingClientRect();return x>=0&&y>=0&&x<innerWidth&&y<innerHeight&&e.contains(document.elementFromPoint(x,y))&&text.width<r.width&&text.height<r.height})''')
        check('return and all four side tabs use the original independent paper PNG', "getComputedStyle(document.querySelector('[data-dossier-return]')).backgroundImage.includes('entry-title-slip-v2.png')&&[...document.querySelectorAll('.explorer-dossier-tabs button')].every(e=>getComputedStyle(e,'::before').backgroundImage.includes('entry-title-slip-v2.png'))")
        check('Chinese record has a distinct name, metadata and biography', "document.querySelectorAll('.dossier-record-fields dt').length===2&&document.querySelector('.dossier-background h3').textContent==='人物简介'&&Number.parseFloat(getComputedStyle(document.querySelector('[data-dossier-name]')).fontSize)>Number.parseFloat(getComputedStyle(document.querySelector('[data-dossier-note]')).fontSize)*2")
        check('record text fits on the original paper', "[...document.querySelectorAll('.dossier-identity,.dossier-record-fields,.dossier-character-note')].every(e=>e.scrollWidth<=e.clientWidth+1)&&document.querySelector('[data-dossier-note]').getBoundingClientRect().bottom<document.querySelector('#dossier-record').getBoundingClientRect().bottom")
        check('identity, place and introductory text match the selected character', "document.querySelector('[data-dossier-role]').textContent==='诊所经营者'&&document.querySelector('[data-dossier-place]').textContent==='家中／诊所'&&document.querySelector('[data-dossier-note]').textContent.includes('诊所')")
        check('new props are bounded, noninteractive and preserve original book', '''() => {
          const book=document.querySelector('.explorer-dossier-book'),a=document.querySelector('#dossier-record').getBoundingClientRect();
          const key=book.querySelector('.dossier-key-prop'),flower=book.querySelector('.dossier-redflower-prop');
          return getComputedStyle(book).backgroundImage.includes('exploration-dossier.png')
            &&[key,flower].every(e=>getComputedStyle(e).pointerEvents==='none'&&getComputedStyle(e).maskImage.includes('.svg'))
            &&key.getBoundingClientRect().right<a.left&&flower.getBoundingClientRect().left>a.left+a.width*.7
            &&flower.getBoundingClientRect().width<a.width*.2;
        }''')
        page.evaluate("for(const id of ['dong','li','dong','wang'])document.querySelector('[data-dossier-person='+id+']').click()")
        check('rapid switching keeps latest portrait, text and ink together','document.querySelector("#dossier-record").classList.contains("has-handwritten-ink")&&document.querySelector("[data-dossier-handwriting]").src.endsWith("dossier-handwriting-wang-v1.png")&&document.querySelector("[data-dossier-name]").textContent==="王少鹏"')
        page.locator('[data-dossier-return]').click()
        check('return preserves settings draft', "document.querySelector('.view.active').dataset.view==='settings'&&document.querySelector('[data-film-strength]').value==='31'")
        page.locator('[data-personnel-open]').click()
        page.keyboard.press('Escape')
        check('Escape returns to settings', "document.querySelector('.view.active').dataset.view==='settings'")
        page.locator('[data-music-library-open]').click()
        check('music still opens with mute preserved', "!document.querySelector('#settings-music-library').hidden&&document.querySelector('#app').dataset.musicState==='muted'")
        page.locator('[data-music-library-close]').click()
        page.locator('[data-settings-return]').click()
        check('nested return reaches the original main menu', "document.querySelector('.view.active').dataset.view==='mainmenu'")
        check('character browsing preserves storage', "localStorage.getItem('northeast-disco:campaign:v1')===null&&localStorage.getItem('qa-unrelated')==='preserve'&&localStorage.length===2")
        assert not errors,errors
        # Simulate a real cold image request failure, then recover without any
        # missing record text or a stale character's handwriting showing.
        page.route('**/dossier-handwriting-li-v1.png',lambda r:r.abort())
        page.reload(wait_until='domcontentloaded')
        page.locator('.mainmenu-actions [data-open-view=settings]').click()
        page.locator('[data-personnel-open]').click()
        page.locator('[data-dossier-person=li]').click()
        check('failed ink load retains visible correct HTML record', '''() => {
          const e=document.querySelector('[data-dossier-handwriting]');
          return e.complete&&e.naturalWidth===0&&e.hidden&&!document.querySelector('#dossier-record').classList.contains('has-handwritten-ink')
            &&getComputedStyle(document.querySelector('.dossier-identity')).opacity==='1'&&document.querySelector('[data-dossier-name]').textContent==='李正';
        }''')
        page.unroute('**/dossier-handwriting-li-v1.png')
        page.locator('[data-dossier-person=dong]').click()
        page.locator('[data-dossier-person=li]').click()
        check('ink recovers after failed download','document.querySelector("#dossier-record").classList.contains("has-handwritten-ink")&&!document.querySelector("[data-dossier-handwriting]").hidden&&document.querySelector("[data-dossier-handwriting]").src.endsWith("dossier-handwriting-li-v1.png")')
        report={'ui':'inspected #dossier-record handwritten lettering plus red flower and ordinary key','characters':people,'characterCount':len(people),'checks':checks,'screenshots':screens,'errors':errors,'allFourConfigured':len(people)==4,'scope':'Original character records and portraits; no playable content.','transport':'fixed work Google Chrome + direct CDP 9223','targetId':target,'storage':'isolated memory; player data unchanged','humanArtReview':'pending','assistantVisualReview':'pending'}
        (ROOT/'docs/dossier-handwriting-verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
        print(f'COMPLETE {len(checks)} checks, {len(screens)} screenshots; configured characters: {len(people)}',flush=True)
    finally:
        bc.send('Target.closeTarget',{'targetId':target})
