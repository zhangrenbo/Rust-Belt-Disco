"""Verify actual station single-line reading in isolated, silent Chrome CDP pages."""
import ast, importlib.util, json, sys, time
from pathlib import Path
R=Path(__file__).resolve().parents[1];sys.stdout.reconfigure(encoding='utf-8')
s=importlib.util.spec_from_file_location('cdp',R/'scripts/record-catalog-cdp-session.py');m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
tree=ast.parse((R/'scripts/qa-character-newspaper-cdp.py').read_text(encoding='utf-8-sig'));init=next(ast.literal_eval(n.value) for n in tree.body if isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='INIT' for t in n.targets))
fixture=dict(version=1,character='dong-dianshi',scene='A000',node='greeting',answers={},inspected=[],position=dict(x=800,y=500),warningHeard=False,shadow=None,completed=False,updatedAt='2026-09-16T00:00:00.000Z')
init+=';localStorage.setItem("northeast-disco:campaign:v1:auto",'+json.dumps(json.dumps(fixture))+');localStorage.setItem("qa-unrelated","preserved");'
results=[];screens=[]
def choose_first(p):
 for kind in ['rawKeyDown','keyUp']:
  p.cdp.call('Input.dispatchKeyEvent',{'type':kind,'key':'1','code':'Digit1','windowsVirtualKeyCode':49})
def metrics(p):
 return p.evaluate('''(()=>{const t=document.querySelector('#station-dialogue-text'),c=t.parentElement,b=document.querySelector('#station-talk .disco-dialog-panel'),r=e=>{const b=e.getBoundingClientRect();return {x:b.x,y:b.y,w:b.width,h:b.height}},range=document.createRange();range.selectNodeContents(t);return {text:t.textContent,textRect:r(t),panel:r(b),copy:r(c),rectCount:range.getClientRects().length,font:getComputedStyle(c).fontFamily,fontSize:parseFloat(getComputedStyle(c).fontSize),scrollWidth:c.scrollWidth,clientWidth:c.clientWidth,more:!document.querySelector('#station-line-next').hidden,choicesHidden:document.querySelector('#station-choices').hidden}})()''')
for width,height in [(1440,810),(844,475)]:
 p=m.CatalogPage(init)
 try:
  p.resize(width,height);p.cdp.call('Page.navigate',{'url':'http://127.0.0.1:5173/'})
  p.wait("document.querySelector('#app')?.dataset.musicState&&document.querySelectorAll('.record-track').length===2",20)
  p.click('.mainmenu-choice-continue');p.wait("document.querySelector('#station-talk').open&&!document.querySelector('#app').classList.contains('is-view-switching')")
  p.evaluate('document.fonts.ready.then(()=>true)');time.sleep(.25)
  story=p.evaluate("import('/src/station/story.js').then(m=>({nodes:m.storyNodes,questions:m.questions}))")
  route=['greeting','greeting-memory','greeting-question','background','value','fear','warning','doubt','alive','dont-turn-left','shadow-intro','shadow']
  previous=None
  for name in route:
   p.wait('document.querySelector("[data-view=station]").dataset.storyNode==='+json.dumps(name))
   expected=(story['questions'].get(name) or story['nodes'][name])['text']
   if name in ['value','fear','warning']:
    q={'value':'background','fear':'value','warning':'fear'}[name]
    expected='她说：「'+story['questions'][q]['choices'][0]['reply']+'」\n\n'+expected
   collected=[];count=0
   while True:
    a=metrics(p);count+=1;assert count<50
    assert a['rectCount']==1,a
    assert a['textRect']['w']<=a['copy']['w']+.5,a
    assert abs(a['textRect']['y']+a['textRect']['h']/2-a['panel']['y']-a['panel']['h']/2)<1,a
    assert 'SimSun' in a['font'] and a['fontSize']>=17
    assert a['more']==a['choicesHidden']
    collected.append(a['text'])
    if name=='greeting' and count==1:
     assert a['more'];choose_first(p);assert p.evaluate('document.querySelector("[data-view=station]").dataset.storyNode')==name
     assert metrics(p)['text']==a['text']
    if name=='warning' and count==2:
     p.click('#station-talk-close');p.wait('!document.querySelector("#station-talk").open&&!document.querySelector("#station-talk-resume").hidden')
     p.click('#station-talk-resume');p.wait('document.querySelector("#station-talk").open');assert metrics(p)['text']==a['text'],'resume lost current line'
     p.pointer(width*.96,height*.1);p.evaluate('document.activeElement?.blur()')
     path=R/f'docs/dialogue-single-line-warning-{width}.png';p.capture(path);screens.append(str(path))
    if not a['more']:break
    if count%2:
     p.focus('.station-dialogue-copy');p.key('Space')
    else:
     time.sleep(.55) # Separate deliberate clicks from the UI's double-click guard.
     p.click('#station-line-next')
    p.wait('document.querySelector("#station-dialogue-text").textContent!=='+json.dumps(a['text']))
   assert ''.join(collected)==expected.replace('\n',''),{'node':name,'got':collected,'expected':expected}
   if name=='background':
    assert p.evaluate('document.querySelectorAll("#station-choices button:not(:disabled)").length')==4
    a=p.rect('#station-choices button');p.pointer(a['x']+a['width']/2,a['y']+a['height']/2);time.sleep(.2)
    assert p.rect('#station-choices button')==a,'hover moved the choice'
    p.pointer(width*.96,height*.1);p.evaluate('document.activeElement?.blur()')
    path=R/f'docs/dialogue-single-line-choices-{width}.png';p.capture(path);screens.append(str(path))
   results.append(dict(width=width,node=name,lineCount=count,exactSourcePreserved=True,oneLineAndCentered=True))
   choose_first(p)
  p.wait('document.querySelector("[data-view=station]").dataset.storyNode==="ready"')
  assert not p.evaluate('document.querySelector("#station-talk").open')
  assert p.evaluate('window.__characterErrors.length===0&&localStorage.getItem("qa-unrelated")==="preserved"')
  print(f'PASS {width}: 12 story nodes, exact text, centered single lines, gated choices, keyboard/mouse, resume and no hover movement.',flush=True)
 finally:p.close()
report=dict(date='2026-09-16',results=results,screens=screens,storage='isolated memory; real player data untouched',audio='silent',transport='fixed work Google Chrome direct CDP',electronWindowVerified=False,humanReview='pending',font='SimSun; provisional default, no explicit user font selection yet',advance='click/Space/Enter; no automatic timer')
(R/'docs/dialogue-single-line-verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
