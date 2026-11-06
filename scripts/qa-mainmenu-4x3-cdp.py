"""Capture the main menu layout at both supported stage ratios in work Chrome."""
import importlib.util
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('cdp', root / 'scripts/record-catalog-cdp-session.py')
cdp = importlib.util.module_from_spec(spec)
spec.loader.exec_module(cdp)
page = cdp.CatalogPage("Object.defineProperty(HTMLMediaElement.prototype,'play',{configurable:true,value:function(){return Promise.resolve()}})")
report = {}
try:
    for label, width, height in [('4x3', 1200, 900), ('16x9', 1280, 720), ('4x3-small', 800, 600)]:
        page.resize(width, height)
        page.cdp.call('Page.navigate', {'url': 'http://127.0.0.1:5173/'})
        page.wait(f'document.querySelector(".view-mainmenu.active") && Math.abs(document.querySelector("#app").clientWidth-{width})<2', 20)
        page.capture(root / f'docs/mainmenu-{label}-adjusted.png')
        report[label] = page.evaluate("""(() => {
          const stage=document.querySelector('#app').getBoundingClientRect();
          return Object.fromEntries(['.event-center-menu','.mainmenu-logo','.mainmenu-actions','.event-dossier-bag','.event-center-strip']
            .map(selector=>{const r=document.querySelector(selector).getBoundingClientRect();return [selector,
              {x:Math.round(r.x-stage.x),y:Math.round(r.y-stage.y),w:Math.round(r.width),h:Math.round(r.height)}]}));
        })()""")
    page.resize(1200, 900)
    page.cdp.call('Page.navigate', {'url': 'http://127.0.0.1:5173/?lang=en'})
    page.wait('document.documentElement.classList.contains("ui-ink-logo") && document.documentElement.dataset.showcaseLanguage==="en"', 20)
    page.capture(root / 'docs/mainmenu-4x3-en-adjusted.png')
    report['4x3-en'] = page.evaluate("""(() => {
      const stage=document.querySelector('#app').getBoundingClientRect();
      return Object.fromEntries(['.english-logo','.mainmenu-actions','.event-dossier-bag'].map(selector=>{
        const r=document.querySelector(selector).getBoundingClientRect();
        return [selector,{x:Math.round(r.x-stage.x),y:Math.round(r.y-stage.y),w:Math.round(r.width),h:Math.round(r.height)}];
      }));
    })()""")
    (root / 'docs/mainmenu-4x3-adjusted.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps(report, ensure_ascii=False))
finally:
    page.close()
