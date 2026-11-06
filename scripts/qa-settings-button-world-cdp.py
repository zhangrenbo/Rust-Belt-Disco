"""Check paper-stamp proportions and real controls in an isolated background Chrome tab."""
import ast
import json
import os
import uuid
from pathlib import Path
from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[1]
os.environ['NO_PROXY'] = 'localhost,127.0.0.1'
module = ast.parse((ROOT/'scripts/qa-record-library-cdp.py').read_text(encoding='utf-8-sig'))
INIT = next(ast.literal_eval(n.value) for n in module.body if isinstance(n, ast.Assign)
    and any(isinstance(t, ast.Name) and t.id == 'INIT' for t in n.targets))
checks, screens, errors, layouts = [], [], [], []
PROTECTED = """() => {
  const selectors=['.settings-board-actions','.settings-board-actions [data-reset-settings]',
    '.settings-board-actions [data-save-settings]','.settings-case-stamp','.settings-sheet-heading h1',
    '.settings-sheet-row h2','.settings-row-label','.settings-art-backdrop','.settings-dossier-form'];
  return selectors.map(selector=>({selector,items:[...document.querySelectorAll(selector)].map(e=>{
    const c=getComputedStyle(e),r=e.getBoundingClientRect();
    return {text:e.textContent.trim().slice(0,30),color:c.color,background:c.backgroundColor,
      image:c.backgroundImage,mask:c.maskImage,mixBlendMode:c.mixBlendMode,
      font:c.fontFamily,fontSize:c.fontSize,fontWeight:c.fontWeight,
      border:c.borderTopWidth,inner:getComputedStyle(e,'::before').borderTopWidth,
      transform:c.transform,bounds:[r.x,r.y,r.width,r.height]};})}));
}"""
with sync_playwright() as p:
    browser = p.chromium.connect_over_cdp('http://127.0.0.1:9223', timeout=10000)
    context = browser.contexts[0]
    bc = browser.new_browser_cdp_session()
    task_url = 'about:blank#settings-button-world-'+str(uuid.uuid4())
    with context.expect_page(lambda c:c.url == task_url, timeout=10000) as created:
        target = bc.send('Target.createTarget', {'url':task_url,'background':True})['targetId']
    page = created.value
    assert page.url == task_url
    try:
        page.set_default_timeout(10000)
        cdp = context.new_cdp_session(page)
        assert cdp.send('Target.getTargetInfo')['targetInfo']['targetId'] == target
        cdp.send('Emulation.setFocusEmulationEnabled', {'enabled':True})
        try: cdp.send('Page.setWebLifecycleState', {'state':'active'})
        except Exception as e: print('Lifecycle: '+str(e)[:120])
        page.route('**/@vite/client', lambda r:r.fulfill(status=200,content_type='application/javascript',body=''))
        page.add_init_script(INIT)
        page.on('pageerror', lambda e:errors.append(str(e)))
        page.emulate_media(reduced_motion='no-preference')
        page.set_viewport_size({'width':1058,'height':595})

        def ready(view='settings'):
            page.wait_for_function("""view => document.querySelector('[data-view="'+view+'"].active')
              && !document.querySelector('#app').classList.contains('is-view-switching')""", arg=view)

        def check(name, condition):
            assert condition, name
            checks.append(name)
            print('PASS '+name, flush=True)

        def open_settings():
            page.goto('http://127.0.0.1:5173/', wait_until='networkidle')
            ready('mainmenu')
            page.locator('.mainmenu-actions [data-open-view=settings]').click()
            ready()
            page.mouse.move(1055,592)

        def capture(width, height, suffix=''):
            page.set_viewport_size({'width':width,'height':height})
            ready()
            page.mouse.move(width-2,height-2)
            path = ROOT/f'docs/settings-button-world{suffix}-{width}.png'
            bounds = page.locator('[data-screenshot-root]').bounding_box()
            page.screenshot(path=str(path),clip=bounds)
            screens.append({'path':str(path),'viewport':[width,height],'gameBounds':bounds})

        # Compare the already accepted stamps and left labels against the saved original CSS.
        baseline_css = (ROOT/'.local/app-before-settings-button-world-20260914.css').read_text(encoding='utf-8-sig')
        route_old = lambda r:r.fulfill(status=200,content_type='text/css',body=baseline_css)
        page.route('**/app.css',route_old)
        open_settings()
        baseline = page.evaluate(PROTECTED)
        page.unroute('**/app.css',route_old)
        open_settings()
        protected = page.evaluate(PROTECTED)
        check('Approved action stamps, left labels and form placement match the previous CSS', protected == baseline)
        for width,height in [(1440,810),(1058,595),(844,475)]:
            capture(width,height)
            layout = page.evaluate("""() => {
              const stage=document.querySelector('#app').getBoundingClientRect();
              const controls=[...document.querySelectorAll('.view-settings button,.view-settings input')]
                .filter(e=>e.getClientRects().length&&!e.disabled).map(e=>{
                  const r=e.getBoundingClientRect();
                  return {name:e.getAttribute('aria-label')||e.textContent.trim(),
                    hit:e.contains(document.elementFromPoint(r.x+r.width/2,r.y+r.height/2)),
                    inside:r.left>=stage.left&&r.right<=stage.right&&r.top>=stage.top&&r.bottom<=stage.bottom,
                    overflow:e.scrollWidth>e.clientWidth+1};});
              const stamps=[...document.querySelectorAll('.settings-dossier-form .setting-choice,.settings-dossier-form .setting-toggle')].map(e=>{
                const c=getComputedStyle(e),mark=getComputedStyle(e,'::before'),inner=getComputedStyle(e,'::after');
                return {name:e.textContent.trim(),width:e.getBoundingClientRect().width,
                  font:c.fontSize,background:c.backgroundColor,border:c.borderTopWidth,inner:inner.borderTopWidth,
                  mask:c.maskImage,selected:e.matches('.active,[aria-checked="true"]'),tick:mark.visibility};});
              return {controls,stamps,film:document.querySelector('#app').dataset.film};
            }""")
            layouts.append({'viewport':[width,height],**layout})
            check(f'{width}: every visible control is inside the game and receives clicks',all(e['hit'] and e['inside'] and not e['overflow'] for e in layout['controls']))
            check(f'{width}: all ten stamps expose paper and selected marks remain distinct',len(layout['stamps']) == 10 and all(e['background']=='rgba(0, 0, 0, 0)' and 'save-stamp-ink-mask' in e['mask'] and (e['tick']=='visible')==e['selected'] and float(e['border'][:-2])>=2 and float(e['inner'][:-2])>=2 for e in layout['stamps']))
            check(f'{width}: shared film remains enabled',layout['film']=='on')

        # Exercise the smaller game window, including real hit targets and keyboard paths.
        for group in ['data-mode','data-quality']:
            for button in page.locator('.settings-dossier-form ['+group+']').all():
                value = button.get_attribute(group)
                button.click()
                check(group+' choice '+value,button.get_attribute('aria-pressed')=='true' and page.locator('.settings-dossier-form ['+group+'][aria-pressed=true]').count()==1 and button.evaluate("e=>getComputedStyle(e,'::before').visibility==='visible'"))
        page.locator('[data-reset-settings]').click()
        for selector in ['button[data-film-toggle]','button[data-film-motion]','[data-game-toggle=name]','[data-game-toggle=replay]']:
            button=page.locator(selector)
            before=button.get_attribute('aria-checked')
            button.click()
            check(selector+' mouse toggles state and ink mark',button.get_attribute('aria-checked')!=before and button.evaluate("e=>(getComputedStyle(e,'::before').visibility==='visible')===(e.getAttribute('aria-checked')==='true')"))
            button.focus()
            page.keyboard.press('Space')
            check(selector+' keyboard restores state',button.get_attribute('aria-checked')==before)
        for selector in ['[data-film-strength]','[data-audio-channel=voice]','[data-audio-channel=music]','[data-audio-channel=sound]']:
            slider=page.locator(selector)
            slider.focus()
            page.keyboard.press('Home')
            zero=slider.input_value()=='0' and slider.evaluate("e=>e.parentElement.querySelector('output').textContent==='0'")
            page.keyboard.press('End')
            full=slider.input_value()=='100' and slider.evaluate("e=>e.parentElement.querySelector('output').textContent==='100'")
            check(selector+' keyboard reaches both limits and displays values',zero and full)
        page.locator('[data-reset-settings]').focus()
        page.keyboard.press('Enter')
        check('Restore previews defaults without writing changes',page.evaluate("JSON.parse(localStorage.getItem('northeast-disco.settings.v1')).music===54") and page.locator('[data-settings-feedback]').inner_text().startswith('已预览默认'))
        music=page.locator('[data-audio-channel=music]')
        rect=music.bounding_box()
        page.mouse.click(rect['x']+rect['width']*.38,rect['y']+rect['height']/2)
        value=int(music.input_value())
        check('Volume track receives real pointer input',32<=value<=44)
        page.locator('[data-save-settings]').click()
        page.wait_for_function("value=>JSON.parse(localStorage.getItem('northeast-disco.settings.v1')).music===value",arg=value)
        check('Apply saves the selected value',page.locator('[data-settings-feedback]').inner_text()=='设置已应用并保存。')
        page.locator('[data-settings-return]').click()
        ready('mainmenu')
        page.locator('.mainmenu-actions [data-open-view=settings]').click()
        ready()
        check('Saved settings survive returning and reopening',music.input_value()==str(value))
        page.locator('[data-music-library-open]').click()
        page.locator('.record-library:not([hidden])').wait_for(state='visible')
        check('Record library hides settings stamps',page.locator('.settings-dossier-form').is_hidden() and page.locator('[data-save-settings]').is_hidden())
        page.locator('[data-music-library-close]').click()
        check('Return restores the form and applied value',music.is_visible() and music.input_value()==str(value))
        page.emulate_media(reduced_motion='reduce')
        check('Reduced motion disables stamp transitions',page.locator('.setting-toggle').first.evaluate("e=>getComputedStyle(e).transitionDuration==='0s'"))
        page.emulate_media(reduced_motion='no-preference')
        page.locator('[data-reset-settings]').click()
        capture(844,475,'-keyboard')
        check('Unrelated player data remains intact in the isolated test store',page.evaluate("localStorage.getItem('qa-unrelated')==='preserve'&&localStorage.length===2"))
        check('No JavaScript errors',not errors)
        report={'transport':'fixed work Google Chrome, direct CDP 9223; unique background test target',
            'storage':'isolated in-memory store, real player data untouched','audio':'real graph with silent test output',
            'checks':checks,'errors':errors,'screenshots':screens,'layouts':layouts,'protectedBefore':baseline,
            'protectedAfter':protected,'humanArtReview':'pending','electronWindowVerified':False}
        (ROOT/'docs/settings-button-world-verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
        print(json.dumps({'checks':len(checks),'screenshots':len(screens),'errors':errors}),flush=True)
    finally:
        assert cdp.send('Target.getTargetInfo')['targetInfo']['targetId']==target
        page.close()
