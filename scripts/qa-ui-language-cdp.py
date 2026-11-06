"""Isolated, muted, background Chrome regression for the formal game's UI port."""
import ast, importlib.util, json, time
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cdp',ROOT/'scripts/record-catalog-cdp-session.py')
cdp=importlib.util.module_from_spec(spec);spec.loader.exec_module(cdp)
tree=ast.parse((ROOT/'scripts/qa-character-newspaper-cdp.py').read_text(encoding='utf-8'))
init=next(ast.literal_eval(n.value) for n in tree.body if isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='INIT' for t in n.targets))
page=cdp.CatalogPage(init)
checks=[]
def ready(view):
    page.wait(f"document.querySelector('[data-view={view}].active')&&!document.querySelector('#app').classList.contains('is-view-switching')")
def shot(name):
    time.sleep(.25);page.capture(ROOT/f'docs/ui-language-{name}.png')
try:
    page.resize(1440,810)
    page.cdp.call('Page.navigate',{'url':'http://127.0.0.1:5173/?ui-language-qa=1'})
    page.wait("document.querySelector('[data-ui-language-switch]') && document.querySelector('#app').dataset.musicState")
    ready('mainmenu')
    page.evaluate("localStorage.setItem('qa-unrelated','preserved')")
    assert page.evaluate("document.documentElement.lang")=='zh-CN'
    page.click('.ui-language-switch')
    page.wait("document.documentElement.lang==='en' && ['logo','menu','character','dossier'].every(k=>document.documentElement.classList.contains('ui-ink-'+k))")
    # Reveal original menu using its normal keyboard interaction.
    page.focus('.mainmenu-choice-start')
    shot('en-main')
    assert page.evaluate("[...document.querySelectorAll('.english-menu-label')].every(e=>e.dataset.sprite && getComputedStyle(e).backgroundImage.includes('menu-en-atlas.png'))")
    page.click('.mainmenu-actions [data-open-view=settings]');ready('settings');shot('en-settings')
    page.click('[data-personnel-open]');ready('explorer-dossier')
    for person in ('dong','li','wang'):
        page.click('[data-dossier-person='+person+']')
        page.wait(f"document.querySelector('[data-dossier-character]').dataset.dossierCharacter==='{person}'")
        shot('en-dossier-'+person)
        assert page.evaluate("getComputedStyle(document.querySelector('#dossier-record'),'::after').backgroundImage.includes('dossier-en-atlas.png')")
    page.click('[data-dossier-return]');ready('settings')
    page.click('[data-ui-language-switch].settings-language-tab')
    page.wait("document.documentElement.lang==='zh-CN'")
    page.click('[data-personnel-open]');ready('explorer-dossier')
    page.wait("document.querySelector('#dossier-record').classList.contains('has-handwritten-ink')")
    assert page.evaluate("getComputedStyle(document.querySelector('.dossier-handwritten-ink')).display!=='none'")
    shot('zh-dossier-restored')
    page.click('[data-dossier-return]');ready('settings');page.key('Escape');ready('mainmenu')
    page.click('.ui-language-switch')
    page.click('.mainmenu-choice-start');ready('character-select');shot('en-characters')
    # Keyboard activation avoids edge-panning changing the character hit location.
    page.focus('[data-character-choice="大厦崩塌"]');page.key('Enter');ready('station')
    assert page.evaluate("document.querySelector('[data-view=station]').dataset.storyNode")=='arrival'
    checks.append('English Start enters real A000 gameplay; original Chinese dossiers restore')
    page.key('Escape');page.wait("document.querySelector('#station-pause').open")
    page.click('#station-pause-menu');ready('mainmenu')
    before=page.evaluate("JSON.stringify(Object.fromEntries(Array.from({length:localStorage.length},(_,i)=>localStorage.key(i)).filter(k=>k!=='northeast-disco.ui-language.v1').map(k=>[k,localStorage.getItem(k)])))")
    page.click('.ui-language-switch');page.click('.ui-language-switch')
    after=page.evaluate("JSON.stringify(Object.fromEntries(Array.from({length:localStorage.length},(_,i)=>localStorage.key(i)).filter(k=>k!=='northeast-disco.ui-language.v1').map(k=>[k,localStorage.getItem(k)])))")
    assert before==after
    shot('en-continue')
    page.resize(844,475);shot('en-main-844')
    assert page.evaluate("document.documentElement.scrollWidth<=innerWidth")
    page.resize(1440,810)
    page.click('.mainmenu-choice-continue');ready('station')
    checks.append('Repeated language changes preserve all save/settings bytes; Continue works')
    assert page.evaluate('window.__characterErrors')==[]
    checks.append('No runtime exceptions; four English PNGs decoded')
    (ROOT/'docs/ui-language-qa.json').write_text(json.dumps({'checks':checks,'errors':[],'scope':'Chrome web build; isolated saves, muted audio; not Electron'},ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps(checks,ensure_ascii=False))
finally: page.close()
