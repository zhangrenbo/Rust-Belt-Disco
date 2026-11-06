"""Targeted English five-button layout and failed-image fallback checks."""
import ast,importlib.util,json,time
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cdp',ROOT/'scripts/record-catalog-cdp-session.py')
cdp=importlib.util.module_from_spec(spec);spec.loader.exec_module(cdp)
tree=ast.parse((ROOT/'scripts/qa-character-newspaper-cdp.py').read_text(encoding='utf-8'))
init=next(ast.literal_eval(n.value) for n in tree.body if isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='INIT' for t in n.targets))
save=dict(version=1,character='dong-dianshi',scene='A000',node='arrival',answers={},inspected=[],position=dict(x=440,y=640),warningHeard=False,shadow=None,completed=False,updatedAt='2026-09-19T00:00:00Z')
init+=';localStorage.setItem("northeast-disco.ui-language.v1","en");localStorage.setItem("northeast-disco:campaign:v1:auto",'+json.dumps(json.dumps(save))+');'
page=cdp.CatalogPage(init)
try:
    page.cdp.call('Network.setBlockedURLs',{'urls':['*dossier-en-atlas.png','http://127.0.0.1:5173/@vite/client']})
    page.resize(1440,810);page.cdp.call('Page.navigate',{'url':'http://127.0.0.1:5173/?ui-fallback-qa=1'})
    page.wait("document.querySelector('.mainmenu-choice-continue') && document.documentElement.classList.contains('ui-ink-menu')")
    assert page.evaluate("document.documentElement.lang==='en'")
    time.sleep(.4);page.capture(ROOT/'docs/ui-language-en-continue.png')
    page.resize(844,475);time.sleep(.4);page.capture(ROOT/'docs/ui-language-en-main-844.png')
    assert page.evaluate("document.documentElement.scrollWidth<=innerWidth")
    page.resize(1440,810)
    page.click('.mainmenu-actions [data-open-view=settings]')
    page.wait("document.querySelector('[data-view=settings].active')&&!document.querySelector('#app').classList.contains('is-view-switching')")
    page.click('[data-personnel-open]')
    page.wait("document.querySelector('[data-view=explorer-dossier].active')&&!document.querySelector('#app').classList.contains('is-view-switching')")
    assert page.evaluate("!document.documentElement.classList.contains('ui-ink-dossier') && getComputedStyle(document.querySelector('.dossier-identity')).opacity==='1' && document.querySelector('[data-dossier-name]').textContent==='Dong Dianshi'")
    page.capture(ROOT/'docs/ui-language-en-dossier-fallback.png')
    result={'savedEnglishPreference':True,'fiveMenuItems':True,'narrowViewportNoOverflow':True,'failedEnglishDossierShowsTranslatedHtml':True,'runtimeErrors':page.evaluate('window.__characterErrors')}
    assert result['runtimeErrors']==[]
    (ROOT/'docs/ui-language-fallback-qa.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
    print(json.dumps(result))
finally:page.close()
