"""Check actual screen-relative tonearm projection with fixed work Chrome CDP."""
import ast
import json
import os
import time
from pathlib import Path
from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[1]
os.environ['NO_PROXY'] = 'localhost,127.0.0.1'
tree = ast.parse((ROOT / 'scripts/qa-record-library-cdp.py').read_text(encoding='utf-8'))
INIT = ast.literal_eval(next(n.value for n in tree.body if isinstance(n, ast.Assign)
                            and any(isinstance(t, ast.Name) and t.id == 'INIT' for t in n.targets)))
INIT = INIT.replace('{"music":54}', '{"music":54,"filmMotion":false}')
checks, shots, measurements, errors = [], [], [], []

GEOMETRY = '''() => {
  const e=document.querySelector('.record-tonearm'), s=getComputedStyle(e), p=getComputedStyle(e,'::before');
  const arm=new DOMMatrix(s.transform), shadow=new DOMMatrix(p.transform);
  const origin=p.transformOrigin.split(' ').map(parseFloat), own=s.transformOrigin.split(' ').map(parseFloat);
  const width=parseFloat(s.width),height=parseFloat(s.height), parent=e.offsetParent.getBoundingClientRect();
  const screen=(x,y)=>{const q=arm.transformPoint(new DOMPoint(x-own[0],y-own[1]));return {
    x:parent.x+parseFloat(s.left)+own[0]+q.x,y:parent.y+parseFloat(s.top)+own[1]+q.y};};
  const points=[['head',970/1453,540/1083],['pipe',550/1453,200/1083],['stylus',.485,.912]].map(([name,x,y])=>{
    x*=width;y*=height;
    const q=shadow.transformPoint(new DOMPoint(x-origin[0],y-origin[1]));
    const body=screen(x,y),cast=screen(q.x+origin[0],q.y+origin[1]);
    return {name,body,cast,dx:cast.x-body.x,dy:cast.y-body.y};
  });
  return {points,filter:p.filter,opacity:p.opacity,pointer:p.pointerEvents,mask:p.maskImage,
    transform:p.transform,origin:p.transformOrigin,playing:document.querySelector('.record-library').classList.contains('is-playing')};
}'''

with sync_playwright() as pw:
    browser = pw.chromium.connect_over_cdp('http://127.0.0.1:9223', timeout=10000)
    context = browser.contexts[0]
    bc = browser.new_browser_cdp_session()
    previous_pages = set(context.pages)
    target = bc.send('Target.createTarget', {'url':'about:blank', 'background':True})['targetId']
    page = None
    deadline = time.monotonic()+10
    try:
        while page is None and time.monotonic()<deadline:
            bc.send('Target.getTargets')
            for candidate in context.pages:
                if candidate in previous_pages or candidate.is_closed():
                    continue
                probe=context.new_cdp_session(candidate)
                try:
                    if probe.send('Target.getTargetInfo')['targetInfo']['targetId']==target:
                        page=candidate
                        break
                finally:
                    probe.detach()
            if page is None:
                time.sleep(.1)
        assert page is not None, 'Task-created target did not become ready'
        page.set_default_timeout(10000)
        page.route('**/@vite/client', lambda r:r.fulfill(status=200,content_type='application/javascript',body=''))
        page.add_init_script(INIT)
        page.on('pageerror',lambda e:errors.append(str(e)))
        cdp=context.new_cdp_session(page)
        try:
            cdp.send('Page.setWebLifecycleState',{'state':'active'})
        except Exception as e:
            print('Lifecycle:',str(e)[:120],flush=True)
        cdp.send('Emulation.setFocusEmulationEnabled',{'enabled':True})
        page.set_viewport_size({'width':1440,'height':810})
        page.goto('http://127.0.0.1:5173/',wait_until='domcontentloaded')

        def check(name,expression):
            page.wait_for_function(expression,polling=100,timeout=10000)
            checks.append(name)

        check('main menu ready', 'document.querySelector("[data-view=mainmenu].active") && !document.querySelector("#app").classList.contains("is-view-switching")')
        page.locator('.mainmenu-actions [data-open-view=settings]').click()
        check('settings transition finished','document.querySelector("[data-view=settings].active") && !document.querySelector("#app").classList.contains("is-view-switching")')
        page.locator('[data-music-library-open]').click()
        check('real theme audio plays through silent QA sink', 'document.querySelector("#app").dataset.musicState==="playing" && window.__recordQA.rms()>.001')
        page.locator('.record-tonearm img').evaluate('e=>e.decode()')

        for width,height in [(1440,810),(844,475)]:
            page.set_viewport_size({'width':width,'height':height})
            for state in ['playing','paused']:
                if state=='paused':
                    page.locator('[data-record-play]').click()
                    check(f'{width}: pause stops real audio', 'document.querySelector("#app").dataset.musicState==="paused" && window.__recordQA.rms()<.000001')
                for render_width in (width+1,width):
                    cdp.send('Emulation.setDeviceMetricsOverride',{'width':render_width,'height':height,'deviceScaleFactor':1,'mobile':False})
                page.wait_for_function('target=>Math.abs(+getComputedStyle(document.querySelector(".record-tonearm"),"::before").opacity-target)<.0001',arg=.72 if state=='playing' else .55,polling=100)
                page.wait_for_function('document.querySelector(".record-tonearm").getAnimations({subtree:true}).every(a=>a.playState!=="running")',polling=100)
                m=page.evaluate(GEOMETRY)
                m.update(viewport=[width,height],state=state)
                measurements.append(m)
                assert m['playing']==(state=='playing'),m
                for p in m['points'][:2]:
                    assert p['dx'] < -width*.035 and p['dy'] > width*.065,p
                    assert 0<p['cast']['x']<width*.55 and 0<p['cast']['y']<width*.35,p
                assert m['pointer']=='none' and 'linear-gradient' in m['mask'],m
                if state=='playing':
                    tip=m['points'][2]
                    assert abs(tip['dx'])<width*.005 and 0<tip['dy']<width*.005,tip
                checks.append(f'{width} {state}: head and pipe project visibly down-left onto platter; stylus contact retained')
                page.mouse.move(width-2,height-2)
                path=f'docs/record-shadow-left-down-{state}-{width}.png'
                page.locator('#app.stage').screenshot(path=str(ROOT/path),timeout=12000)
                shots.append(path)
                print('Captured',path,flush=True)
            check(f'{width}: all player controls remain reachable', '''() => [...document.querySelectorAll('.record-library button,.record-library input')].filter(e=>!e.disabled&&e.getClientRects().length).every(e=>{const r=e.getBoundingClientRect();return e.contains(document.elementFromPoint(r.x+r.width/2,r.y+r.height/2));})''')
            seek=page.locator('[data-record-seek]');box=seek.bounding_box()
            page.mouse.click(box['x']+box['width']*.4,box['y']+box['height']/2)
            check(f'{width}: seek works beneath projection without resuming', 'Number(document.querySelector("[data-record-seek]").value)>85 && document.querySelector("#app").dataset.musicState==="paused"')
            page.locator('[data-record-play]').click()
            check(f'{width}: resume restarts actual audio', 'document.querySelector("#app").dataset.musicState==="playing" && window.__recordQA.rms()>.001')

        page.emulate_media(reduced_motion='reduce')
        check('reduced motion disables projection transitions','getComputedStyle(document.querySelector(".record-tonearm"),"::before").transitionDuration==="0s"')
        page.locator('[data-music-library-close]').click()
        check('return restores settings and preserves storage', 'document.querySelector("#settings-music-library").hidden && localStorage.getItem("qa-unrelated")==="preserve"')
        assert not errors,errors
        result={'date':'2026-09-14','checks':checks,'screenshots':shots,'measurements':measurements,
                'errors':errors,'targetId':target,'transport':'fixed work Google Chrome + direct CDP 9223',
                'storage':'isolated memory; player data untouched','audio':'real analyser before silent QA sink',
                'humanArtReview':'pending','assistantVisualReview':'pending'}
        (ROOT/'docs/record-shadow-left-down-verification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
        print('COMPLETE',len(checks),'checks',flush=True)
    finally:
        bc.send('Target.closeTarget',{'targetId':target})
