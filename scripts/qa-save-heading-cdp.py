"""Check the dedicated archive heading artwork in an isolated Chrome task page."""
import json
import os
import time
from pathlib import Path
from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[1]
os.environ['NO_PROXY'] = 'localhost,127.0.0.1'
with sync_playwright() as playwright:
    browser = playwright.chromium.connect_over_cdp('http://127.0.0.1:9223', timeout=10000)
    context = browser.contexts[0]
    control = browser.new_browser_cdp_session()
    with context.expect_page(timeout=10000) as created:
        target = control.send('Target.createTarget', {'url':'about:blank', 'background':True})['targetId']
    page = created.value
    errors, screens = [], []
    page.on('pageerror', lambda error: errors.append(str(error)))
    try:
        page.set_default_timeout(10000)
        session = context.new_cdp_session(page)
        assert session.send('Target.getTargetInfo')['targetInfo']['targetId'] == target
        session.send('Emulation.setFocusEmulationEnabled', {'enabled':True})
        try:
            session.send('Page.setWebLifecycleState', {'state':'active'})
        except Exception as error:
            print('Lifecycle activation:', str(error)[:160])
        page.add_init_script('''(() => {
          const save=JSON.stringify({version:1,character:'dong-dianshi',scene:'A000',node:'arrival',answers:{},inspected:[],
            position:{x:440,y:640},warningHeard:false,shadow:null,completed:false,updatedAt:'2026-09-14T00:00:00.000Z'});
          const values=new Map([['northeast-disco:campaign:v1:auto',save],['northeast-disco:campaign:v1:01',save],
            ['northeast-disco:campaign:v1:04','damaged save']]);
          Object.defineProperty(window,'localStorage',{configurable:true,value:{getItem:k=>values.get(String(k))??null,
            setItem:(k,v)=>values.set(String(k),String(v)),removeItem:k=>values.delete(String(k)),clear:()=>values.clear(),
            key:i=>[...values.keys()][i]??null,get length(){return values.size;}}});
        })();''')
        page.set_viewport_size({'width':1440,'height':810})
        page.goto(f'http://127.0.0.1:5173/?save-heading-qa={time.time_ns()}', wait_until='domcontentloaded')
        page.locator('.mainmenu-actions [data-open-view="entry"]').click()
        page.locator('[data-view=entry].active').wait_for(state='visible')
        page.wait_for_function("() => !document.querySelector('#app').classList.contains('is-view-switching') && document.querySelector('.page-shutter').getAnimations().every(a => a.playState === 'finished')")
        before=page.evaluate('Object.fromEntries(Array.from({length:localStorage.length},(_,i)=>{const k=localStorage.key(i);return [k,localStorage.getItem(k)];}))')
        page.wait_for_function("() => document.querySelector('.entry-heading-panel')?.complete && document.querySelector('.entry-heading-panel').naturalWidth > 0")
        page.evaluate(r'''async () => {
          const urls=[...document.querySelectorAll('[data-view=entry], [data-view=entry] *')]
            .flatMap(e=>[...getComputedStyle(e).backgroundImage.matchAll(/url\("?([^"()]+)"?\)/g)].map(m=>m[1]));
          await Promise.all([...new Set(urls)].map(url=>new Promise((resolve,reject)=>{
            const image=new Image();image.onload=resolve;image.onerror=reject;image.src=url;
          })));
        }''')
        for width,height in [(1440,810),(1920,1080),(844,475)]:
            page.set_viewport_size({'width':width,'height':height})
            page.mouse.move(width-4,height-4)
            metrics=page.evaluate('''() => {
              const h=document.querySelector('.entry-directory > header'),img=h.querySelector('img'),r=h.getBoundingClientRect();
              const title=h.querySelector('b'),tr=document.createRange();tr.selectNodeContents(title);const t=tr.getBoundingClientRect();
              return {asset:img.getAttribute('src'),naturalSize:[img.naturalWidth,img.naturalHeight],title:title.textContent,
                titleInside:t.left>r.left&&t.right<r.right&&t.top>r.top&&t.bottom<r.bottom,
                bounds:{x:r.x,y:r.y,width:r.width,height:r.height},background:getComputedStyle(h).backgroundImage,
                slots:[...document.querySelectorAll('[data-view=entry] [data-slot]')].map(b=>{
                  const br=b.getBoundingClientRect();return {slot:b.dataset.slot,status:b.dataset.saveStatus,
                    reachable:b.contains(document.elementFromPoint(br.x+br.width/2,br.y+br.height/2))};})};
            }''')
            assert metrics['asset'].lstrip('/')=='assets/save-archive-heading-panel-v1.png',metrics
            assert metrics['background']=='none' and metrics['title']=='选择你要读取的档案' and metrics['titleInside'],metrics
            assert len(metrics['slots'])==6 and all(s['reachable'] for s in metrics['slots']),metrics
            path=ROOT/f'docs/save-heading-panel-{width}.png'
            frame = page.locator('[data-screenshot-root]')
            frame.screenshot(path=str(path),timeout=10000)
            screens.append({'path':str(path),'viewport':[width,height],
                            'captureBounds':frame.bounding_box(),'captureSelector':'[data-screenshot-root]',**metrics})
        page.locator('[data-view=entry] [data-slot="01"]').click()
        assert page.locator('.station-slot-actions button').nth(1).is_disabled()
        page.locator('.station-slot-actions button').first.click()
        page.locator('[data-view="ui-only"].active').wait_for(state='visible')
        page.locator('.ui-only-notice-button').click()
        assert page.locator('.view.active').get_attribute('data-view')=='mainmenu'
        after=page.evaluate('Object.fromEntries(Array.from({length:localStorage.length},(_,i)=>{const k=localStorage.key(i);return [k,localStorage.getItem(k)];}))')
        assert before==after and not errors,(after,errors)
        report={'date':'2026-09-14','transport':'fixed work Google Chrome + direct CDP 9223','browser':browser.version,
          'storage':'task-created page; isolated memory fixtures; real saves untouched','dedicatedImage':True,
          'sixSlotsReachable':True,'loadAndReturn':True,'savedDataUnchanged':True,'screens':screens,'errors':errors,
          'visualAcceptance':'pending user review'}
        (ROOT/'docs/save-heading-panel-verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
        print(json.dumps({'screens':len(screens),'sixSlotsReachable':True,'loadAndReturn':True,'errors':errors}))
    finally:
        page.close()
