"""Settings stamp UI checks in a disposable background work-Chrome target."""
import argparse
import json
import os
import time
from pathlib import Path
from playwright.sync_api import sync_playwright

os.environ['NO_PROXY'] = 'localhost,127.0.0.1'
parser = argparse.ArgumentParser()
parser.add_argument('--width', type=int, default=1440)
args = parser.parse_args()
width, height = args.width, round(args.width * 9 / 16)
out = Path('D:/CodexData/home')
checks, errors = [], []

with sync_playwright() as pw:
    browser = pw.chromium.connect_over_cdp('http://127.0.0.1:9223', timeout=10000)
    context = browser.contexts[0]
    browser_cdp = browser.new_browser_cdp_session()
    with context.expect_page(timeout=10000) as created:
        browser_cdp.send('Target.createTarget', {'url': 'about:blank', 'background': True})
    page = created.value
    try:
        page.set_default_timeout(10000)
        page.route('**/@vite/client', lambda r: r.fulfill(status=200, content_type='application/javascript', body=''))
        page.add_init_script('''(() => {
          const values = new Map([['northeast-disco.settings.v1', '{"music":0,"film":true,"filmStrength":65,"filmMotion":true}']]);
          Object.defineProperty(window,'localStorage',{value:{
            getItem:k=>values.get(String(k))??null,setItem:(k,v)=>values.set(String(k),String(v)),
            removeItem:k=>values.delete(String(k)),clear:()=>values.clear(),key:i=>[...values.keys()][i]??null,
            get length(){return values.size;}
          }});
          // Keep real audio graphs and settings events, silence only the test target output.
          const connect=AudioNode.prototype.connect, gates=new WeakMap();
          AudioNode.prototype.connect=function(target,...args) {
            if (target instanceof AudioDestinationNode) {
              if (!gates.has(target)) {
                const gate=new GainNode(this.context,{gain:0});
                connect.call(gate,target);gates.set(target,gate);
              }
              target=gates.get(target);
            }
            return connect.call(this,target,...args);
          };
          const play=HTMLMediaElement.prototype.play;
          HTMLMediaElement.prototype.play=function(...args) {this.muted=true;return play.apply(this,args);};
        })();''')
        page.on('pageerror', lambda e: errors.append(str(e)))
        cdp = context.new_cdp_session(page)
        cdp.send('Emulation.setFocusEmulationEnabled', {'enabled': True})
        try:
            cdp.send('Page.setWebLifecycleState', {'state': 'active'})
        except Exception as e:
            print('Lifecycle:', str(e)[:120], flush=True)
        page.emulate_media(reduced_motion='no-preference')
        page.set_viewport_size({'width': width, 'height': height})
        page.goto(f'http://127.0.0.1:5173/?settings-stamps-qa={time.time_ns()}', wait_until='domcontentloaded')

        def ready(view):
            page.wait_for_function('''name => {
              const e=document.querySelector('[data-view="'+name+'"].active');
              return e && !document.querySelector('#app').classList.contains('is-view-switching')
                && getComputedStyle(e).opacity==='1'
                && +getComputedStyle(document.querySelector('.page-shutter')).opacity<.01;
            }''', arg=view)

        ready('mainmenu')
        menu_filter = page.locator('.projector-film').evaluate('e=>getComputedStyle(e).backdropFilter')
        page.locator('.mainmenu-actions [data-open-view=settings]').click()
        ready('settings')
        page.wait_for_function('''[...document.querySelectorAll('.view.active img')].filter(i=>i.getAttribute('src')).every(i=>i.complete&&i.naturalWidth>0)''')
        layout = page.evaluate('''() => {
          const view=document.querySelector('.view-settings');
          const rect=e=>{const r=e.getBoundingClientRect();return {x:r.x,y:r.y,width:r.width,height:r.height};};
          const stamps=[...view.querySelectorAll('.settings-ink-stamp')].map(e=>{
            const c=getComputedStyle(e);return {text:e.textContent.trim(),rect:rect(e),
              background:c.backgroundColor,color:c.color,mask:c.maskImage,border:c.borderTopWidth,
              font:c.fontFamily,transform:c.transform,pointer:c.pointerEvents};
          });
          const controls=[...view.querySelectorAll('button,input')].filter(e=>e.getClientRects().length&&!e.disabled).map(e=>{
            const r=e.getBoundingClientRect();return {name:e.textContent.trim()||e.getAttribute('aria-label'),
              hit:e.contains(document.elementFromPoint(r.x+r.width/2,r.y+r.height/2))};
          });
          return {stamps,controls,heading:rect(document.querySelector('#settings-title')),
            form:rect(document.querySelector('.settings-dossier-form')),
            film:document.querySelector('#app').dataset.film,
            filter:getComputedStyle(document.querySelector('.projector-film')).backdropFilter};
        }''')
        assert [s['text'] for s in layout['stamps']] == ['未查明凶手','恢复默认','应用设置']
        assert all(s['background']=='rgba(0, 0, 0, 0)' and 'save-stamp-ink-mask' in s['mask'] and float(s['border'].replace('px',''))>0 for s in layout['stamps'])
        assert all(c['hit'] for c in layout['controls']), layout['controls']
        assert all(s['rect']['x']>=0 and s['rect']['y']>=0 and s['rect']['x']+s['rect']['width']<=width and s['rect']['y']+s['rect']['height']<=height for s in layout['stamps']), layout
        assert layout['film']=='on' and layout['filter']==menu_filter
        checks += ['three correct stamp labels', 'transparent red ink with distress mask and frames', 'stamps inside viewport', 'all controls reachable', 'shared main-menu film preserved']
        page.mouse.move(width-2,height-2)
        for w in [width+1,width]:
            cdp.send('Emulation.setDeviceMetricsOverride', {'width':w,'height':height,'deviceScaleFactor':1,'mobile':False})
        screenshot=out/f'settings-stamps-{width}.png'
        page.screenshot(path=str(screenshot), timeout=12000)
        print('Captured', screenshot.name, flush=True)

        reset=page.locator('[data-reset-settings]')
        apply=page.locator('[data-save-settings]')
        reset.focus()
        page.keyboard.press('Enter')
        defaults=page.locator('input[data-audio-channel]').evaluate_all('es=>Object.fromEntries(es.map(e=>[e.dataset.audioChannel,+e.value]))')
        assert defaults=={'voice':72,'music':54,'sound':84}, defaults
        assert page.locator('[data-settings-feedback]').inner_text().startswith('已预览默认')
        assert page.evaluate('JSON.parse(localStorage.getItem("northeast-disco.settings.v1")).music')==0
        checks += ['Restore defaults works from keyboard without saving prematurely']
        music=page.locator('input[data-audio-channel=music]')
        music.fill('37')
        music.dispatch_event('input')
        apply.hover()
        page.wait_for_function('getComputedStyle(document.querySelector("[data-save-settings]")).color==="rgb(116, 35, 28)"')
        checks += ['stamp hover feedback visible']
        apply.click()
        page.wait_for_function('JSON.parse(localStorage.getItem("northeast-disco.settings.v1")).music===37')
        assert page.locator('[data-settings-feedback]').inner_text()=='设置已应用并保存。'
        checks += ['Apply settings click saves changed volume']
        page.locator('[data-settings-return]').click()
        ready('mainmenu')
        page.locator('.mainmenu-actions [data-open-view=settings]').click()
        ready('settings')
        assert music.input_value()=='37'
        checks += ['applied settings survive leaving and reopening settings']
        page.locator('[data-music-library-open]').click()
        page.wait_for_function('document.querySelector(".view-settings").classList.contains("is-record-library")')
        assert page.locator('.settings-case-stamp').is_hidden()
        assert reset.is_hidden() and apply.is_hidden()
        page.locator('[data-music-library-close]').click()
        assert page.locator('.settings-case-stamp').is_visible()
        checks += ['settings stamps hide in music library and return correctly']
        assert not errors, errors
        result={'transport':'fixed work Google Chrome, direct CDP 9223; task-created background target',
          'storage':'isolated memory; real player data untouched','audio':'test target output muted',
          'viewport':[width,height],'screenshot':str(screenshot),'layout':layout,'defaults':defaults,
          'checks':checks,'errors':errors,'humanArtReview':'pending'}
        (out/f'settings-stamps-{width}-verification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
        print(json.dumps({'checks':len(checks),'errors':errors}),flush=True)
    finally:
        page.close()
