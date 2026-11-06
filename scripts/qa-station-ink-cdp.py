"""Compare real A000 dialog DOM to the INK source in the fixed work Chrome."""
import ast
import hashlib
import importlib.util
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('ink_cdp',ROOT/'scripts/record-catalog-cdp-session.py')
cdp=importlib.util.module_from_spec(spec);spec.loader.exec_module(cdp)
tree=ast.parse((ROOT/'scripts/qa-character-newspaper-cdp.py').read_text(encoding='utf-8-sig'))
INIT=next(ast.literal_eval(n.value) for n in tree.body if isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='INIT' for t in n.targets))
INIT+=";localStorage.setItem('northeast-disco:campaign:v1:06','preserve damaged manual');"
source=ROOT/'content/story/ink/D0_序章.ink'
lines=[line.strip() for line in source.read_text(encoding='utf-8').split('序章：A000 任务名称：不要左转')[1].split('- (返回)')[0].splitlines()]
page=cdp.CatalogPage(INIT)
observed=[]
checks=[]

def view(name):
    page.wait(f"document.querySelector('[data-view={name}].active')&&!document.querySelector('#app').classList.contains('is-view-switching')")

def inspect(name,speaker=None):
    page.wait("document.querySelector('#station-talk').open&&document.querySelector('[data-view=station]').dataset.storyNode==="+json.dumps(name),timeout=55)
    data=page.evaluate("({node:document.querySelector('[data-view=station]').dataset.storyNode,speaker:document.querySelector('#station-speaker').textContent,paragraphs:[...document.querySelectorAll('#station-dialogue-text p')].map(p=>p.textContent),choices:[...document.querySelectorAll('#station-choices button span')].map(p=>p.textContent)})")
    if speaker: assert data['speaker']==speaker,data
    if name not in ['background','value','fear']:
        assert all(p in lines for p in data['paragraphs']),data
        assert all(c=='继续' or '*'+c in lines for c in data['choices']),data
    observed.append(data)
    print('PASS '+name,flush=True)

try:
    page.resize(1440,810)
    page.cdp.call('Page.navigate',{'url':'http://127.0.0.1:5173/'})
    page.wait("document.querySelector('#app')?.dataset.musicState&&document.querySelectorAll('.record-track').length===2",timeout=25)
    view('mainmenu');page.click('.mainmenu-choice-start');view('character-select')
    page.focus('[data-character-choice="大厦崩塌"]');page.key('Enter');view('station')
    assert not page.evaluate("document.querySelector('#station-talk').open")
    page.click('#station-grandmother-marker')
    for i,(name,speaker) in enumerate([('arrival','老太太'),('greeting','董点事'),('greeting-memory','老太太'),('greeting-question','老太太')]):
        inspect(name,speaker)
        if name=='greeting-memory':
            page.click('#station-talk-close');page.click('#station-pause-button');page.click('#station-pause-menu');view('mainmenu')
            page.click('.mainmenu-choice-continue');view('station')
            inspect(name,speaker);checks.append('Continue restores the corrected survivor speech at its saved node')
        page.click('#station-choices button')
    for name in ['background','value','fear']:
        inspect(name,'老太太');page.click('#station-choices button')
    inspect('warning','老太太');page.click('#station-choices li:nth-child(2) button')
    for name,speaker in [('dont-turn-left','老太太'),('shadow-intro','黑影'),('shadow','黑影')]:
        inspect(name,speaker)
        if name!='shadow':page.click('#station-choices button')
    page.click('#station-choices li:nth-child(4) button')
    page.wait("document.querySelector('[data-view=station]').dataset.storyNode==='ready'")
    checks.append('Authored sequence reaches ready with three answers, warning and shadow')
    assert page.evaluate("localStorage.getItem('northeast-disco:campaign:v1:06')==='preserve damaged manual'")
    checks.append('Damaged manual fixture preserved; all storage isolated in memory')
    assert page.evaluate('window.__characterErrors.length===0')
    checks.append('No browser runtime errors')
    report={'date':'2026-09-15','source':str(source),'sourceSha256':hashlib.sha256(source.read_bytes()).hexdigest(),'transport':'fixed work Google Chrome + direct CDP 9223; background task page','observed':observed,'checks':checks,'audio':'silent output','electronWindowVerified':False,'humanReview':'pending'}
    (ROOT/'docs/station-ink-verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
finally:
    page.close()
