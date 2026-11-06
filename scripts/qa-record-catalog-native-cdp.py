"""Right catalog visual and interaction QA over target-specific Chrome CDP."""
import ast
import importlib.util
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('catalog_cdp',ROOT/'scripts/record-catalog-cdp-session.py')
cdp=importlib.util.module_from_spec(spec);spec.loader.exec_module(cdp)
tree=ast.parse((ROOT/'scripts/qa-record-library-cdp.py').read_text(encoding='utf-8-sig'))
INIT=next(ast.literal_eval(n.value) for n in tree.body if isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='INIT' for t in n.targets))
INIT+=";window.__catalogQAErrors=[];addEventListener('error',e=>{if(e.error)window.__catalogQAErrors.push(String(e.error));});"
checks,screens,layouts=[],[],[]
page=cdp.CatalogPage(INIT)
def check(name,expression):
    page.wait(expression)
    checks.append(name);print('PASS '+name,flush=True)
def capture(name,width,height):
    page.resize(width,height)
    page.wait("!document.querySelector('#app').classList.contains('is-view-switching')")
    page.pointer(width-2,height-2)
    path=ROOT/f'docs/record-catalog-no-tabs-{name}-{width}.png'
    bounds=page.capture(path)
    screens.append({'path':str(path),'viewport':[width,height],'gameBounds':bounds})
try:
    page.resize(1440,810)
    page.cdp.call('Page.navigate',{'url':'http://127.0.0.1:5173/'})
    page.wait("document.querySelectorAll('.record-track').length===2&&document.querySelector('#app').dataset.musicState&&!document.querySelector('#app').classList.contains('is-view-switching')")
    page.click('.mainmenu-actions [data-open-view=settings]')
    page.wait("document.querySelector('.view-settings.active')&&!document.querySelector('#app').classList.contains('is-view-switching')")
    page.click('[data-music-library-open]')
    check('Actual theme recording plays through the silent QA sink',"document.querySelector('#app').dataset.musicState==='playing'&&window.__recordQA.rms()>.001")
    check('Reference-based paper and both real game thumbnails decode',"""(async()=>{
      const i=new Image();i.src='/record-catalog-paper-reference-v4.png';await i.decode();
      return i.naturalWidth===1122&&i.naturalHeight===1402
        && [...document.querySelectorAll('.record-track-cover')].every(i=>i.complete&&i.naturalWidth>0);
    })()""")
    check('Catalog always shows all tracks without category tabs',"!document.querySelector('.record-catalog-tabs,[data-record-all],[data-record-favorites-only]')&&document.querySelectorAll('.record-track:not([hidden])').length===2")
    check('Left player artwork and needle projection remain intact',"""getComputedStyle(document.querySelector('.record-vinyl')).backgroundImage.includes('record-vinyl-painted-v1.png')
      && getComputedStyle(document.querySelector('.record-player-paper')).backgroundImage.includes('record-player-paper-v1.png')
      && getComputedStyle(document.querySelector('.record-tonearm'),'::before').backgroundImage.includes('record-tonearm.png')""")
    for width,height in [(1440,810),(1058,595),(844,475)]:
        page.resize(width,height)
        check(f'{width}: all visible controls receive pointer input inside the game',"""(()=>{
          const stage=document.querySelector('#app').getBoundingClientRect();
          return [...document.querySelectorAll('.record-library button,.record-library input')].filter(e=>!e.disabled&&e.getClientRects().length).every(e=>{
            const r=e.getBoundingClientRect();return r.left>=stage.left&&r.right<=stage.right&&r.top>=stage.top&&r.bottom<=stage.bottom
              && e.contains(document.elementFromPoint(r.x+r.width/2,r.y+r.height/2));});})()""")
        check(f'{width}: heading, both credits and footer fit their printed areas',"""[...document.querySelectorAll('.record-catalog-heading,.record-track-main,.record-track-copy,.record-catalog-footer')]
          .every(e=>e.scrollWidth<=e.clientWidth+1)""")
        check(f'{width}: old-animation film and two paper shadows cover the catalog',"""document.querySelector('#app').dataset.film==='on'
          && getComputedStyle(document.querySelector('.projector-edge-flicker')).zIndex==='90'
          && getComputedStyle(document.querySelector('.grain')).filter==='contrast(2.25)'
          && document.querySelectorAll('.projector-edge-snow>i').length===53
          && getComputedStyle(document.querySelector('.record-catalog'),'::after').backgroundImage.includes('162deg')""")
        capture('playing',width,height)
        layouts.append(page.evaluate("""(()=>({width:innerWidth,height:innerHeight,
          heading:getComputedStyle(document.querySelector('#music-library-title')).font,
          paper:getComputedStyle(document.querySelector('.record-catalog-paper')).backgroundImage,
          selected:document.querySelector('.record-track.is-current [data-preview-music]').dataset.previewMusic,
          visibleTracks:document.querySelectorAll('.record-track:not([hidden])').length}))()"""))
    start=page.evaluate("Number(document.querySelector('[data-record-seek]').value)")
    check('Playback advances the progress slider',f"Number(document.querySelector('[data-record-seek]').value)>{start}+.4")
    page.click('[data-record-play]')
    check('Pause stops actual audio and the record rotation',"document.querySelector('#app').dataset.musicState==='paused'&&window.__recordQA.rms()<.000001&&getComputedStyle(document.querySelector('.record-vinyl')).animationPlayState==='paused'")
    rect=page.rect('[data-record-seek]')
    page.click_at(rect['x']+rect['width']*.4,rect['y']+rect['height']/2)
    check('Seeking while paused changes the real playback position',"document.querySelector('#app').dataset.musicState==='paused'&&Number(document.querySelector('[data-record-seek]').value)>85&&Number(document.querySelector('[data-record-seek]').value)<110")
    capture('paused',1440,810)
    offset=page.evaluate("Number(document.querySelector('[data-record-seek]').value)")
    page.click('[data-record-play]')
    check('Resume starts at the selected position',f"document.querySelector('#app').dataset.musicState==='playing'&&Math.abs(window.__recordQA.sources.at(-1).offset-{offset})<1")
    page.click('[data-preview-music=station] .record-track-cover')
    check('Clicking the cover selects the actual station recording',"document.querySelector('#app').dataset.musicTrack==='station'&&document.querySelector('#app').dataset.musicState==='playing'&&window.__recordQA.sources.at(-1).node.buffer.duration<60")
    check('The red strip follows the selected recording',"document.querySelectorAll('.record-track.is-current').length===1&&document.querySelector('.record-track.is-current [data-preview-music=station]')&&getComputedStyle(document.querySelector('.record-track.is-current'),'::before').backgroundColor==='rgb(132, 46, 50)'")
    capture('station',1058,595)
    page.click('[data-preview-music=menu]')
    check('Clicking a title returns to the theme',"document.querySelector('#app').dataset.musicTrack==='menu'&&document.querySelector('#app').dataset.musicState==='playing'")
    page.click('[data-record-next]')
    check('Next track remains functional',"document.querySelector('#app').dataset.musicTrack==='station'&&document.querySelector('#app').dataset.musicState==='playing'")
    page.click('[data-record-previous]')
    check('Previous track remains functional',"document.querySelector('#app').dataset.musicTrack==='menu'&&document.querySelector('#app').dataset.musicState==='playing'")
    page.click('[data-record-favorite]')
    check('Adding a favorite preserves the complete track list',"JSON.parse(localStorage.getItem('northeast-disco.music-favorites.v1')).includes('menu')&&document.querySelectorAll('.record-track:not([hidden])').length===2")
    capture('favorite',844,475)
    page.focus('.record-track:not([hidden]) .record-track-heart')
    page.key('Enter')
    check('Removing a favorite keeps its row visible and keyboard focus intact',"document.activeElement.matches('.record-track-heart')&&document.activeElement.getAttribute('aria-pressed')==='false'&&document.querySelectorAll('.record-track:not([hidden])').length===2")
    page.click('[data-record-repeat]')
    check('Loop button changes the real source loop state',"!window.__recordQA.sources.at(-1).node.loop")
    page.focus('[data-record-previous]')
    page.key('Tab')
    check('Keyboard focus remains visible on the player',"document.activeElement.matches('[data-record-play]:focus-visible')&&getComputedStyle(document.activeElement).outlineStyle==='solid'")
    page.key('Enter')
    check('Enter pauses actual playback',"document.querySelector('#app').dataset.musicState==='paused'")
    page.cdp.call('Emulation.setEmulatedMedia',{'features':[{'name':'prefers-reduced-motion','value':'reduce'}]})
    check('Reduced motion disables record rotation',"getComputedStyle(document.querySelector('.record-vinyl')).animationName==='none'")
    page.cdp.call('Emulation.setEmulatedMedia',{'features':[{'name':'prefers-reduced-motion','value':'no-preference'}]})
    page.click('[data-music-library-close]')
    check('Return restores settings and scene music',"document.querySelector('#settings-music-library').hidden&&!document.querySelector('[data-settings-controls]').hidden&&document.querySelector('#app').dataset.musicTrack==='menu'")
    page.click('[data-music-library-open]')
    page.key('Escape')
    check('Escape returns to settings',"document.querySelector('#settings-music-library').hidden")
    check('Unrelated settings and player saves remain untouched',"localStorage.getItem('qa-unrelated')==='preserve'&&localStorage.getItem('northeast-disco.settings.v1')==='{\"music\":54}'&&![...Array(localStorage.length)].some((_,i)=>localStorage.key(i).startsWith('northeast-disco:campaign:'))")
    check('No JavaScript errors',"window.__catalogQAErrors.length===0")
    report={'date':'2026-09-14','transport':'fixed work Google Chrome, direct target-specific CDP 9223; unique background page',
      'connectionNote':'Reused the working target-specific CDP method from the previous catalog revision; no restart or unrelated tab manipulation.',
      'checks':checks,'screens':screens,'layouts':layouts,'errors':page.evaluate('window.__catalogQAErrors'),
      'storage':'isolated in-memory fixtures; real player data untouched','audio':'real Web Audio before silent QA output; speakers not inspected',
      'humanArtReview':'pending','electronWindowVerified':False}
    (ROOT/'docs/record-catalog-no-tabs-verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({'passed':len(checks),'screens':len(screens),'errors':report['errors']}),flush=True)
except Exception:
    if page.evaluate("!!document.querySelector('[data-screenshot-root]')"):
        page.capture(ROOT/'docs/record-catalog-no-tabs-qa-failure.png')
    raise
finally:
    page.close()
