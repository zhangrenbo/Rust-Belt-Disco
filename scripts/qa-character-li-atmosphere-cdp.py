"""Verify Li Zheng's source layers share world lighting and foreground fog."""
import ast
import importlib.util
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('li_cdp',ROOT/'scripts/record-catalog-cdp-session.py')
cdp=importlib.util.module_from_spec(spec);spec.loader.exec_module(cdp)
tree=ast.parse((ROOT/'scripts/qa-character-newspaper-cdp.py').read_text(encoding='utf-8-sig'))
INIT=next(ast.literal_eval(n.value) for n in tree.body if isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='INIT' for t in n.targets))
page=cdp.CatalogPage(INIT)
checks,screens,metrics=[],[],[]

def check(name,expression):
    page.wait('Boolean('+expression+')')
    checks.append(name);print('PASS '+name,flush=True)

def key(name,vk):
    for kind in ('keyDown','keyUp'):
        page.cdp.call('Input.dispatchKeyEvent',{'type':kind,'key':name,'code':name,'windowsVirtualKeyCode':vk})

def ready(view):
    page.wait(f"document.querySelector('[data-view={view}].active')&&!document.querySelector('#app').classList.contains('is-view-switching')")

def capture(name,width,height):
    path=ROOT/f'docs/character-li-atmosphere-{name}-{width}.png'
    bounds=page.capture(path)
    screens.append({'path':str(path),'viewport':[width,height],'gameBounds':bounds})
    return path

try:
    page.resize(1440,810)
    page.cdp.call('Page.navigate',{'url':'http://127.0.0.1:5173/'})
    page.wait("document.querySelector('#app')?.dataset.musicState&&document.querySelectorAll('.record-track').length===2",timeout=20)
    page.click('.mainmenu-actions [data-open-view=character-select]');ready('character-select')
    page.wait("[...document.querySelectorAll('.character-art')].every(i=>i.complete&&i.naturalWidth>0)",timeout=20)
    check('All six original Li layers are present and fully opaque', "document.querySelectorAll('.character-card-li').length===6&&[...document.querySelectorAll('.character-card-li')].every(i=>getComputedStyle(i).opacity==='1')")
    check('Both fog planes reuse the original PNG above and below the Li stack', """(()=>{
      const a=document.querySelector('.character-fog-rear'),b=document.querySelector('.character-fog-li-front');
      return a.src===b.src&&a.naturalWidth===3916&&b.naturalHeight===3238
        &&getComputedStyle(a).zIndex==='2'&&getComputedStyle(b).zIndex==='26'
        &&[...document.querySelectorAll('.character-card-li')].every(i=>Number(getComputedStyle(i).zIndex)<26)
        &&Number(getComputedStyle(document.querySelector('.character-choice-bottom')).zIndex)>26;
    })()""")
    check('Complementary masks avoid doubling the lower fog', "getComputedStyle(document.querySelector('.character-fog-rear')).maskImage.includes('60%')&&getComputedStyle(document.querySelector('.character-fog-li-front')).maskImage.includes('70%')")
    check('All three papers, photo and ink have coordinated grading', "document.querySelectorAll('.character-card-li-paper').length===3&&[...document.querySelectorAll('.character-card-li')].every(i=>getComputedStyle(i).filter.includes('saturate')&&getComputedStyle(i).filter.includes('brightness'))")
    for width,height in [(1440,810),(1058,595),(844,475)]:
        page.resize(width,height);page.pointer(width/2,height/2);key('Home',36)
        capture('default',width,height)
        for _ in range(7): key('ArrowDown',40)
        check(f'{width}: both fog planes follow the same camera geometry', """(()=>{
          const a=document.querySelector('.character-fog-rear').getBoundingClientRect(),b=document.querySelector('.character-fog-li-front').getBoundingClientRect();
          return ['x','y','width','height'].every(k=>a[k]===b[k]);})()""")
        check(f'{width}: Li is clickable through fog and fully in view', """(()=>{
          const b=document.querySelector('.character-choice-bottom'),r=b.getBoundingClientRect(),s=document.querySelector('#app').getBoundingClientRect();
          return b.contains(document.elementFromPoint(r.x+r.width/2,r.y+r.height/2))
            &&r.top>=s.top&&r.bottom<=s.bottom&&r.left>=s.left&&r.right<=s.right;
        })()""")
        capture('lower',width,height)
        metrics.append(page.evaluate("""(()=>({width:innerWidth,li:[...document.querySelectorAll('.character-card-li')].map(i=>({src:i.getAttribute('src'),filter:getComputedStyle(i).filter,z:getComputedStyle(i).zIndex})),fog:getComputedStyle(document.querySelector('.character-fog-li-front')).maskImage}))()"""))
    page.focus('.character-choice-bottom');page.key('Enter');ready('ui-only')
    check('Li keyboard selection enters the existing UI showcase',"document.querySelector('[data-view=ui-only].active')")
    page.click('.ui-only-notice-button');ready('mainmenu')
    page.click('.mainmenu-actions [data-open-view=character-select]');ready('character-select')
    page.click('.character-return');ready('mainmenu')
    check('Return and storage protection remain intact',"localStorage.length===0")
    check('No runtime errors',"window.__characterErrors.length===0")
    report={'date':'2026-09-14','transport':'fixed work Chrome, direct target-specific background CDP 9223','checks':checks,'screens':screens,'metrics':metrics,
      'errors':page.evaluate('window.__characterErrors'),'storage':'isolated memory; player data untouched','audio':'silent QA output',
      'humanArtReview':'pending','electronWindowVerified':False,
      'pixelComparison':'Not completed: Page.captureScreenshot timed out during isolated fog-on/off controls in three attempts. Normal scene captures succeeded. No numeric fog pixel claim; attempt script preserved in .local.'}
    (ROOT/'docs/character-li-atmosphere-verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({'checks':len(checks),'screens':len(screens)}),flush=True)
finally:
    page.close()
