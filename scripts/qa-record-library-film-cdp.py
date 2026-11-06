"""Shared film pass on record library: actual motion, compositor and input checks."""
import ast
import json
import os
import sys
import uuid
from pathlib import Path
from PIL import Image, ImageStat
from playwright.sync_api import sync_playwright

ROOT=Path(__file__).resolve().parents[1]
os.environ['NO_PROXY']='localhost,127.0.0.1'
before_mode='--before' in sys.argv
module=ast.parse((ROOT/'scripts/qa-record-library-cdp.py').read_text(encoding='utf-8-sig'))
INIT=next(ast.literal_eval(n.value) for n in module.body if isinstance(n,ast.Assign)
  and any(isinstance(t,ast.Name) and t.id=='INIT' for t in n.targets))
checks,screens,errors,metrics=[],[],[],[]
LAYERS="""() => {
  const stage=document.querySelector('#app');
  return {film:stage.dataset.film,motion:stage.dataset.filmMotion,strength:stage.style.getPropertyValue('--film-strength'),
    nodes:document.querySelectorAll('.projector-edge-snow>i').length,
    layers:['.projector-film','.grain','.projector-edge-snow','.projector-edge-flicker'].map(selector=>{
      const e=document.querySelector(selector),c=getComputedStyle(e);
      return {selector,display:c.display,zIndex:c.zIndex,opacity:c.opacity,filter:c.filter,
        mask:c.maskImage,backdrop:c.backdropFilter,pointerEvents:c.pointerEvents};}),
    dustScale:getComputedStyle(document.querySelector('.film-dust')).scale};
}"""
with sync_playwright() as p:
    browser=p.chromium.connect_over_cdp('http://127.0.0.1:9223',timeout=10000)
    context=browser.contexts[0]
    bc=browser.new_browser_cdp_session()
    task_url='about:blank#record-film-qa-'+str(uuid.uuid4())
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
        except Exception as e: print('Lifecycle:',str(e)[:160])
        page.route('**/@vite/client',lambda r:r.fulfill(status=200,content_type='application/javascript',body=''))
        page.add_init_script(INIT)
        page.on('pageerror',lambda e:errors.append(str(e)))
        def check(name,expression):
            page.wait_for_function(expression,polling=100,timeout=10000)
            checks.append(name);print('PASS '+name,flush=True)
        def capture(name,width,height):
            page.set_viewport_size({'width':width,'height':height})
            page.wait_for_function("!document.querySelector('#app').classList.contains('is-view-switching')")
            page.mouse.move(width-2,height-2)
            path=ROOT/f'docs/record-library-film-{name}-{width}.png'
            rect=page.locator('[data-screenshot-root]').bounding_box()
            page.screenshot(path=str(path),clip=rect,timeout=10000)
            screens.append({'path':str(path),'viewport':[width,height],'gameBounds':rect})
            return path
        def open_library():
            page.locator('[data-music-library-open]').click()
            page.locator('.record-library:not([hidden])').wait_for(state='visible')
        def close_library():
            page.locator('[data-music-library-close]').click()
        page.set_viewport_size({'width':1440,'height':810})
        page.emulate_media(reduced_motion='no-preference')
        page.goto('http://127.0.0.1:5173/',wait_until='networkidle')
        storage_before=page.evaluate("Object.fromEntries(Array.from({length:localStorage.length},(_,i)=>{const k=localStorage.key(i);return [k,localStorage.getItem(k)];}))")
        page.locator('.mainmenu-actions [data-open-view=settings]').click()
        open_library()
        page.wait_for_function("document.querySelector('#app').dataset.musicState==='playing'")
        if before_mode:
            capture('before',1440,810)
            (ROOT/'docs/record-library-film-before.json').write_text(json.dumps(page.evaluate(LAYERS),ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
            print('Saved previous library filter evidence.')
            sys.exit(0)
        check('Shared enabled film has only the original 53 damage nodes',
          "document.querySelector('#app').dataset.film==='on'&&document.querySelector('#app').dataset.filmMotion==='on'&&document.querySelectorAll('.projector-edge-snow>i').length===53")
        for width,height in [(1440,810),(1058,595),(844,475)]:
            page.set_viewport_size({'width':width,'height':height})
            data=page.evaluate(LAYERS);metrics.append({'viewport':[width,height],**data})
            assert data['layers'][3]['zIndex']=='90' and data['layers'][1]['filter']=='contrast(2.25)'
            assert abs(float(data['layers'][1]['opacity'])-.39)<.001 and data['dustScale']=='1.2'
            check(f'{width}: full-stage layers overlay the catalog and player without blocking controls',"""() =>
              [...document.querySelectorAll('.record-library button,.record-library input')].filter(e=>!e.disabled&&e.getClientRects().length)
                .every(e=>{const r=e.getBoundingClientRect();return e.contains(document.elementFromPoint(r.x+r.width/2,r.y+r.height/2));})
              && ['.projector-film','.grain','.projector-edge-snow','.projector-edge-flicker'].every(s=>{
                const e=document.querySelector(s),c=getComputedStyle(e);return c.display==='block'&&c.pointerEvents==='none'&&e.parentElement.id==='app';})""")
            capture('on',width,height)
        page.locator('[data-record-play]').click()
        check('Music pause works under the film and leaves film animation running',
          "document.querySelector('#app').dataset.musicState==='paused'&&document.querySelector('#app').dataset.filmMotion==='on'")
        page.wait_for_timeout(800)
        motion=page.evaluate("""() => new Promise(resolve=>{
          const stage=document.querySelector('#app'),damage=document.querySelector('.projector-edge-snow'),samples=[];
          const observer=new MutationObserver(()=>{
            const position=getComputedStyle(document.querySelector('.grain')).backgroundPosition;
            if(samples.at(-1)?.position===position)return;
            samples.push({position,overlay:getComputedStyle(damage).translate,
              dust:damage.querySelectorAll('.film-dust:not([hidden])').length,
              scratches:damage.querySelectorAll('.film-scratch:not([hidden])').length,
              boxes:['.record-player-paper','.record-catalog','.record-now','.record-transport'].map(s=>{
                const r=document.querySelector(s).getBoundingClientRect();return [r.x,r.y,r.width,r.height];})});
          });
          observer.observe(stage,{attributes:true,attributeFilter:['style']});
          setTimeout(()=>{observer.disconnect();resolve(samples);},9000);
        })""")
        assert len(motion)>30 and max(s['scratches'] for s in motion)>0
        assert sum(s['dust'] for s in motion)/len(motion)>25
        assert all(s['boxes']==motion[0]['boxes'] for s in motion)
        checks.append('Observed 9 seconds of changing grain, dust and real scratches with fixed artwork and text')
        # Isolate the vignette for pixel QA, pausing only QA-visible noise, not changing production settings.
        hide_noise=page.add_style_tag(content='.stage>.grain,.stage>.projector-edge-snow{visibility:hidden!important}')
        pair_on=capture('vignette-isolated',1440,810)
        hide_edge=page.add_style_tag(content='.stage>.projector-edge-flicker{visibility:hidden!important}')
        pair_off=capture('without-vignette',1440,810)
        hide_edge.evaluate('(e)=>e.remove()');hide_noise.evaluate('(e)=>e.remove()')
        on=Image.open(pair_on).convert('RGB');off=Image.open(pair_off).convert('RGB')
        pixel_results={}
        for name,x,y in [('center',.5,.5),('topLeft',.04,.04),('topRight',.96,.04),('bottomLeft',.04,.96),('bottomRight',.96,.96)]:
            cx,cy=int(on.width*x),int(on.height*y);box=(cx-3,cy-3,cx+4,cy+4)
            a=sum(ImageStat.Stat(on.crop(box)).mean)/3;b=sum(ImageStat.Stat(off.crop(box)).mean)/3
            pixel_results[name]={'on':a,'withoutVignette':b,'ratio':a/max(b,1)}
        assert abs(pixel_results['center']['on']-pixel_results['center']['withoutVignette'])<2,pixel_results
        assert all(pixel_results[k]['ratio']<.7 for k in pixel_results if k!='center'),pixel_results
        checks.append('Actual rendered vignette darkens all four corners while preserving the center')
        page.locator('[data-record-play]').click()
        check('Playback resumes with the filter on',"document.querySelector('#app').dataset.musicState==='playing'&&window.__recordQA.rms()>.001")
        seek=page.locator('[data-record-seek]');r=seek.bounding_box()
        page.mouse.click(r['x']+r['width']*.4,r['y']+r['height']/2)
        check('Progress remains draggable under all film layers',"Number(document.querySelector('[data-record-seek]').value)>85&&Number(document.querySelector('[data-record-seek]').value)<110")
        close_library()
        page.locator('[data-film-toggle]').click();open_library()
        check('Shared off switch completely hides film on the library',"""() =>
          document.querySelector('#app').dataset.film==='off'&&['.projector-film','.grain','.projector-edge-snow','.projector-edge-flicker']
            .every(s=>getComputedStyle(document.querySelector(s)).display==='none')""")
        capture('off',844,475)
        close_library();page.locator('[data-film-toggle]').click()
        page.locator('[data-film-strength]').press('Home');open_library()
        check('Strength zero is also a complete bypass',"document.querySelector('#app').dataset.film==='off'&&getComputedStyle(document.querySelector('.projector-edge-flicker')).display==='none'")
        close_library();page.locator('[data-film-strength]').press('End');open_library()
        check('Maximum strength follows the existing control',"document.querySelector('#app').style.getPropertyValue('--film-strength')==='1'")
        close_library();page.locator('button[data-film-motion]').click();open_library()
        check('Static option keeps colour and dark corners but stops moving damage',"document.querySelector('#app').dataset.film==='on'&&document.querySelector('#app').dataset.filmMotion==='off'&&getComputedStyle(document.querySelector('.projector-edge-snow')).display==='none'&&getComputedStyle(document.querySelector('.projector-edge-flicker')).display==='block'")
        close_library();page.locator('[data-reset-settings]').click();open_library()
        check('Default reset restores the same 65 percent filter',"document.querySelector('#app').style.getPropertyValue('--film-strength')==='0.65'&&document.querySelector('#app').dataset.filmMotion==='on'")
        page.emulate_media(reduced_motion='reduce')
        check('System reduced motion stops damage on this screen too',"document.querySelector('#app').dataset.filmMotion==='off'&&document.querySelectorAll('.projector-edge-snow>i:not([hidden])').length===0")
        page.emulate_media(reduced_motion='no-preference')
        check('Motion resumes with the same damage nodes',"document.querySelector('#app').dataset.filmMotion==='on'&&document.querySelectorAll('.projector-edge-snow>i').length===53")
        page.evaluate("Object.defineProperty(document,'hidden',{configurable:true,value:true});document.dispatchEvent(new Event('visibilitychange'))")
        check('Page-hidden stops the existing film animation',"document.querySelector('#app').dataset.filmMotion==='off'&&document.querySelectorAll('.projector-edge-snow>i:not([hidden])').length===0")
        page.evaluate("delete document.hidden;document.dispatchEvent(new Event('visibilitychange'))")
        check('Page-visible resumes film animation',"document.querySelector('#app').dataset.filmMotion==='on'")
        page.keyboard.press('Escape')
        check('Escape returns with the identical settings-page filter',"document.querySelector('#settings-music-library').hidden&&getComputedStyle(document.querySelector('.projector-edge-flicker')).zIndex==='90'")
        page.locator('[data-settings-return]').click()
        check('Main menu retains its previous film treatment',"document.querySelector('.view.active').dataset.view==='mainmenu'&&getComputedStyle(document.querySelector('.projector-edge-flicker')).zIndex==='71'")
        storage_after=page.evaluate("Object.fromEntries(Array.from({length:localStorage.length},(_,i)=>{const k=localStorage.key(i);return [k,localStorage.getItem(k)];}))")
        assert storage_before==storage_after,(storage_before,storage_after)
        checks.append('Unsaved previews leave all stored player and settings data identical')
        assert not errors,errors
        report={'date':'2026-09-14','transport':'fixed work Google Chrome + direct CDP 9223','checks':checks,'screens':screens,
          'layers':metrics,'motion':{'samples':len(motion),'grainPositions':len(set(s['position'] for s in motion)),
            'overlayPositions':len(set(s['overlay'] for s in motion)),'meanVisibleDust':sum(s['dust'] for s in motion)/len(motion),
            'scratchFrames':sum(s['scratches']>0 for s in motion),'artworkStable':True,'recordWasPaused':True},
          'pixelComparison':pixel_results,'errors':errors,'storage':'isolated in-memory storage; physical audio output muted',
          'humanArtApproval':'pending','electronWorkbench':'not directly inspected'}
        (ROOT/'docs/record-library-film-verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
        print(json.dumps({'checks':len(checks),'screens':len(screens),'motion':report['motion'],'errors':errors}))
    except Exception:
        if page.locator('[data-screenshot-root]').count():
            page.screenshot(path=str(ROOT/'docs/record-library-film-qa-failure.png'),clip=page.locator('[data-screenshot-root]').bounding_box())
        raise
    finally: page.close()

