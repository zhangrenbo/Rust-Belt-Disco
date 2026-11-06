"""Generated music-player artwork QA in one isolated background Chrome target."""
import ast
import json
import os
import uuid
from pathlib import Path
from playwright.sync_api import sync_playwright

ROOT=Path(__file__).resolve().parents[1]
os.environ['NO_PROXY']='localhost,127.0.0.1'
module=ast.parse((ROOT/'scripts/qa-record-library-cdp.py').read_text(encoding='utf-8-sig'))
INIT=next(ast.literal_eval(n.value) for n in module.body if isinstance(n,ast.Assign)
  and any(isinstance(t,ast.Name) and t.id=='INIT' for t in n.targets))
checks,screens,errors=[],[],[]
with sync_playwright() as p:
    browser=p.chromium.connect_over_cdp('http://127.0.0.1:9223',timeout=10000)
    context=browser.contexts[0]
    bc=browser.new_browser_cdp_session()
    task_url='about:blank#record-image-qa-'+str(uuid.uuid4())
    with context.expect_page(lambda candidate:candidate.url==task_url,timeout=10000) as created:
        target=bc.send('Target.createTarget',{'url':task_url,'background':True})['targetId']
    page=created.value
    assert page.url==task_url
    try:
        page.set_default_timeout(10000)
        cdp=context.new_cdp_session(page)
        assert cdp.send('Target.getTargetInfo')['targetInfo']['targetId']==target
        cdp.send('Emulation.setFocusEmulationEnabled',{'enabled':True})
        try: cdp.send('Page.setWebLifecycleState',{'state':'active'})
        except Exception as e: print('Lifecycle:',str(e)[:120])
        page.add_init_script(INIT)
        page.on('pageerror',lambda e:errors.append(str(e)))
        def check(name,expression):
            page.wait_for_function(expression,polling=100,timeout=10000)
            checks.append(name)
            print('PASS '+name,flush=True)
        def capture(name,width,height):
            page.set_viewport_size({'width':width,'height':height})
            page.wait_for_function("() => !document.querySelector('#app').classList.contains('is-view-switching')")
            page.mouse.move(width-2,height-2)
            path=ROOT/f'docs/record-library-image-{name}-{width}.png'
            frame=page.locator('[data-screenshot-root]').bounding_box()
            page.screenshot(path=str(path),clip=frame,timeout=10000)
            metrics=page.evaluate("""() => {
              const rect=e=>{const r=e.getBoundingClientRect();return {x:r.x,y:r.y,width:r.width,height:r.height};};
              return {paper:rect(document.querySelector('.record-player-paper')),title:rect(document.querySelector('.record-now')),
                timeline:rect(document.querySelector('.record-timeline')),controls:rect(document.querySelector('.record-transport')),
                vinyl:getComputedStyle(document.querySelector('.record-vinyl')).backgroundImage,
                paperAsset:getComputedStyle(document.querySelector('.record-player-paper')).backgroundImage,
                paperMask:getComputedStyle(document.querySelector('.record-player-paper')).maskImage,
                titleColor:getComputedStyle(document.querySelector('.record-now')).color,
                track:document.querySelector('#app').dataset.musicTrack};
            }""")
            screens.append({'path':str(path),'viewport':[width,height],'gameBounds':frame,**metrics})
        page.set_viewport_size({'width':1440,'height':810})
        page.goto('http://127.0.0.1:5173/',wait_until='networkidle')
        page.locator('.mainmenu-actions [data-open-view=settings]').click()
        page.locator('[data-music-library-open]').click()
        check('Real theme audio plays through muted QA sink',"document.querySelector('#app').dataset.musicState==='playing'&&window.__recordQA.rms()>.001")
        check('Both generated assets decode and replace procedural vinyl',"""async () => {
          const selectors=['.record-vinyl','.record-player-paper'];
          const urls=selectors.map(s=>getComputedStyle(document.querySelector(s)).backgroundImage);
          if(!urls[0].includes('record-vinyl-painted-v1.png')||urls[0].includes('gradient')||!urls[1].includes('record-player-paper-v1.png'))return false;
          return (await Promise.all(urls.map(async u=>{const i=new Image();i.src=u.slice(5,-2);await i.decode();return i.naturalWidth>1000;}))).every(Boolean);
        }""")
        check('Original catalog artwork, paper shadows and separate tonearm remain',"""() =>
          getComputedStyle(document.querySelector('.record-catalog-paper')).backgroundImage.includes('record-catalog-paper-light-v3.png')
          && getComputedStyle(document.querySelector('.record-catalog'),'::after').backgroundImage.includes('162deg')
          && getComputedStyle(document.querySelector('.record-tonearm'),'::before').backgroundImage.includes('record-tonearm.png')
          && getComputedStyle(document.querySelector('.record-player-paper')).pointerEvents==='none'""")
        for width,height in [(1440,810),(1058,595),(844,475)]:
            page.set_viewport_size({'width':width,'height':height})
            check(f'{width}: all visible controls are reachable',"""() => [...document.querySelectorAll('.record-library button,.record-library input')]
              .filter(e=>!e.disabled&&e.getClientRects().length).every(e=>{const r=e.getBoundingClientRect();
              return r.left>=0&&r.top>=0&&r.right<=innerWidth&&r.bottom<=innerHeight
                && e.contains(document.elementFromPoint(r.x+r.width/2,r.y+r.height/2));})""")
            check(f'{width}: title, slider and buttons fit on generated paper',"""() => {
              const r=document.querySelector('.record-player-paper').getBoundingClientRect();
              return [...document.querySelectorAll('.record-now,.record-timeline,.record-transport')].every(e=>{
                const b=e.getBoundingClientRect();return b.left>r.left&&b.right<r.right&&b.top>r.top&&b.bottom<r.bottom&&e.scrollWidth<=e.clientWidth+1;})
                && [...document.querySelectorAll('.record-track-main,.record-catalog-heading,.record-catalog-footer')].every(e=>e.scrollWidth<=e.clientWidth+1);
            }""")
            capture('playing',width,height)
        check('Painted record rotates during actual playback',"getComputedStyle(document.querySelector('.record-vinyl')).animationPlayState==='running'")
        start=float(page.locator('[data-record-seek]').input_value())
        check('Real playback advances the slider',f"Number(document.querySelector('[data-record-seek]').value)>{start}+.4")
        page.locator('[data-record-play]').click()
        check('Pause stops audio and rotation',"document.querySelector('#app').dataset.musicState==='paused'&&window.__recordQA.rms()<.000001&&getComputedStyle(document.querySelector('.record-vinyl')).animationPlayState==='paused'")
        capture('paused',1440,810)
        seek=page.locator('[data-record-seek]')
        r=seek.bounding_box()
        page.mouse.click(r['x']+r['width']*.4,r['y']+r['height']/2)
        check('Paper slider seeks while preserving pause',"document.querySelector('#app').dataset.musicState==='paused'&&Number(document.querySelector('[data-record-seek]').value)>85&&Number(document.querySelector('[data-record-seek]').value)<110")
        offset=float(seek.input_value())
        page.locator('[data-record-play]').click()
        check('Resume starts from the selected audio position',f"document.querySelector('#app').dataset.musicState==='playing'&&Math.abs(window.__recordQA.sources.at(-1).offset-{offset})<1")
        page.locator('[data-record-next]').click()
        check('Next selects actual station recording',"document.querySelector('#app').dataset.musicTrack==='station'&&document.querySelector('#app').dataset.musicState==='playing'&&window.__recordQA.sources.at(-1).node.buffer.duration<60")
        page.locator('[data-record-previous]').click()
        check('Previous restores theme',"document.querySelector('#app').dataset.musicTrack==='menu'&&document.querySelector('#app').dataset.musicState==='playing'")
        page.locator('[data-record-favorites-only]').check()
        check('Empty collection remains usable',"!document.querySelector('[data-record-empty]').hidden&&document.querySelector('[data-record-next]').disabled")
        capture('empty-favorites',844,475)
        page.locator('[data-record-favorite]').click()
        check('Favorite updates the real filter and isolated storage',"JSON.parse(localStorage.getItem('northeast-disco.music-favorites.v1')).includes('menu')&&document.querySelectorAll('.record-track:not([hidden])').length===1")
        page.locator('[data-record-favorites-only]').uncheck()
        page.locator('[data-record-repeat]').click()
        check('Repeat control changes actual looping',"!window.__recordQA.sources.at(-1).node.loop")
        page.locator('[data-record-previous]').focus()
        page.keyboard.press('Tab')
        check('Keyboard focus is visible on paper',"document.activeElement.matches('[data-record-play]:focus-visible')&&getComputedStyle(document.activeElement).outlineStyle==='solid'")
        page.keyboard.press('Enter')
        check('Keyboard pauses playback',"document.querySelector('#app').dataset.musicState==='paused'")
        page.emulate_media(reduced_motion='reduce')
        check('Reduced motion disables painted-record rotation',"getComputedStyle(document.querySelector('.record-vinyl')).animationName==='none'")
        page.emulate_media(reduced_motion='no-preference')
        page.locator('[data-music-library-close]').click()
        check('Return restores settings and scene theme',"document.querySelector('#settings-music-library').hidden&&!document.querySelector('[data-settings-controls]').hidden&&document.querySelector('#app').dataset.musicTrack==='menu'")
        page.locator('[data-music-library-open]').click()
        page.keyboard.press('Escape')
        check('Escape returns to settings',"document.querySelector('#settings-music-library').hidden")
        check('Player saves and unrelated settings are untouched',"localStorage.getItem('qa-unrelated')==='preserve'&&localStorage.getItem('northeast-disco.settings.v1')==='{\"music\":54}'&&![...Array(localStorage.length)].some((_,i)=>localStorage.key(i).startsWith('northeast-disco:campaign:'))")
        assert not errors,errors
        report={'date':'2026-09-14','transport':'persistent work Google Chrome + direct CDP 9223','checks':checks,
          'screens':screens,'errors':errors,'storage':'isolated memory fixtures; real player data untouched',
          'audio':'real Web Audio analyser before silent QA output sink; physical speaker not inspected',
          'visualAcceptance':'pending user review','electronWorkbench':'not directly inspected'}
        (ROOT/'docs/record-library-image-verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
        print(json.dumps({'passed':len(checks),'screens':len(screens),'errors':errors}))
    except Exception:
        if page.locator('[data-screenshot-root]').count():
            page.screenshot(path=str(ROOT/'docs/record-library-image-qa-failure.png'),clip=page.locator('[data-screenshot-root]').bounding_box())
        raise
    finally:
        page.close()
