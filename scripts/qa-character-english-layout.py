import ast,importlib.util,json,time,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cdp',ROOT/'scripts/record-catalog-cdp-session.py');cdp=importlib.util.module_from_spec(spec);spec.loader.exec_module(cdp)
tree=ast.parse((ROOT/'scripts/qa-character-newspaper-cdp.py').read_text(encoding='utf-8'))
init=next(ast.literal_eval(n.value) for n in tree.body if isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='INIT' for t in n.targets))
page=cdp.CatalogPage(init)
phase=sys.argv[1] if len(sys.argv)>1 else 'after'
checks=[]
try:
 for lang in ('en','zh'):
  page.resize(1440,810);page.cdp.call('Page.navigate',{'url':'http://127.0.0.1:5173/?lang='+lang})
  page.wait("document.querySelector('[data-ui-language-switch]') && document.documentElement.classList.contains('ui-ink-character')")
  page.click('.mainmenu-choice-start');page.wait("document.querySelector('[data-view=character-select].active')&&!document.querySelector('#app').classList.contains('is-view-switching')")
  page.wait("[...document.querySelectorAll('.character-art')].every(i=>i.complete&&i.naturalWidth)")
  page.pointer(720,405);time.sleep(.3)
  page.capture(ROOT/f'docs/character-english-{phase}-{lang}.png')
  print(json.dumps(page.evaluate("[...document.querySelectorAll('.english-character-label,.english-character-synopsis')].map(e=>({text:e.dataset.characterInk,rect:{w:e.offsetWidth,h:e.offsetHeight},bg:getComputedStyle(e).backgroundSize}))")))
  for key,vk,name in [('ArrowUp',38,'top'),('ArrowDown',40,'bottom')]:
   for _ in range(18):
    for kind in ['keyDown','keyUp']:page.cdp.call('Input.dispatchKeyEvent',{'type':kind,'key':key,'code':key,'windowsVirtualKeyCode':vk})
   time.sleep(.3);page.capture(ROOT/f'docs/character-english-{phase}-{lang}-{name}.png')
  if phase=='after':
   for width,height in [(1440,810),(844,475)]:
    page.resize(width,height);page.pointer(width/2,height/2);time.sleep(.3)
    result=page.evaluate("""(()=>{const items=[...document.querySelectorAll('.english-character-label,.english-character-synopsis')];return items.map(e=>{const s=getComputedStyle(e),b=s.backgroundSize.split(' ').map(parseFloat),r=e.getBoundingClientRect();return {name:e.dataset.characterInk||'synopsis',parent:e.parentElement.className,display:s.display,uniform:s.display==='none'||Math.abs(parseFloat(s.width)*b[0]/(parseFloat(s.height)*b[1])-1)<.003,visibleWidth:r.width,hitTest:s.pointerEvents};});})()""")
    assert all(x['uniform'] for x in result),result
    assert all(x['parent']=='character-world' for x in result),result
    assert all(x['display']==('none' if lang=='zh' else 'block') for x in result),result
    if lang=='en':assert all(x['hitTest']=='none' for x in result),result
    checks.append({'language':lang,'width':width,'uniformPngScaling':True,'worldAnchors':True,'correctVisibility':True})
    if width==844:page.capture(ROOT/f'docs/character-english-after-{lang}-844.png')
  assert page.evaluate('window.__characterErrors')==[]
 if phase=='after':
  (ROOT/'docs/character-english-layout-qa.json').write_text(json.dumps({'checks':checks,'errors':[],'storage':'isolated; muted Chrome direct CDP','humanReview':'pending'},indent=2),encoding='utf-8')
finally:page.close()
