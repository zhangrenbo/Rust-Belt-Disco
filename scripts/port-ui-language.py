"""Import approved showcase artwork; never replace gameplay or story modules."""
from pathlib import Path
import shutil
ROOT=Path(__file__).resolve().parents[1]
SOURCE=Path('E:/抖音开发者工具/PNG_UI_HTML复刻/000 公司官网/games/dongbei-disco')
for name in ('index.html','scene.js'):
    backup=ROOT/'.local'/f'{name}.before-ui-language-20260919'
    if not backup.exists(): shutil.copy2(ROOT/name,backup)
for name in ('main-menu-logo-en.png','menu-en-atlas.png','dossier-en-atlas.png','character-en-atlas.png'):
    shutil.copy2(SOURCE/name,ROOT/'public/assets'/name)
source=(SOURCE/'showcase-language.js').read_text(encoding='utf-8')
dictionary=source[source.index('  const copy = {'):source.index('  const attrs =')]
(ROOT/'src/core/ui-language-copy.js').write_text('// Presentation copy only; story and save identities are untouched.\nexport function createUiTranslator() {\n'+dictionary+'  return translate;\n}\n',encoding='utf-8')
css=(SOURCE/'showcase-language.css').read_text(encoding='utf-8')
for name in ('main-menu-logo-en.png','menu-en-atlas.png','dossier-en-atlas.png','character-en-atlas.png'):
    css=css.replace("url('"+name+"')","url('/assets/"+name+"')")
# Only replace original ink once the corresponding English PNG has decoded.
lines=css.splitlines()
group=''
for i,line in enumerate(lines):
    if 'html[data-showcase-language="en"]' in line:
        group=('logo' if '.mainmenu-logo' in line or '.english-logo' in line else
               'menu' if '.mainmenu-choice' in line or '.english-menu-label' in line else
               'character' if any(s in line for s in ('.english-character-label','.english-character-synopsis','.english-replaced-ink')) else
               'dossier' if '#dossier-record' in line else '')
        if group: lines[i]=line.replace('html[data-showcase-language="en"]',f'html[data-showcase-language="en"].ui-ink-{group}')
css='\n'.join(lines)+'\n'
css+='''
.english-logo,.english-menu-label,.english-character-label,.english-character-synopsis {display:none;}
html[data-showcase-language="en"].ui-ink-logo .english-logo,
html[data-showcase-language="en"].ui-ink-menu .english-menu-label,
html[data-showcase-language="en"].ui-ink-character .english-character-label,
html[data-showcase-language="en"].ui-ink-character .english-character-synopsis {display:block;}
html[data-showcase-language="en"]:not(.ui-ink-dossier) #dossier-record > :is(.dossier-identity,.dossier-record-fields,.dossier-background) {opacity:1;visibility:visible;}
.ui-language-switch {position:absolute;right:2cqw;bottom:1.5cqw;z-index:40;border:0;padding:.7cqw 1.2cqw;background:#d8c49a;color:#372b20;font:700 1cqw Georgia,serif;box-shadow:1px 2px 3px #0005;transform:rotate(-1deg);cursor:pointer;}
.ui-language-switch:focus-visible {outline:2px solid #a24028;outline-offset:3px;}
.ui-language-switch small {display:block;font:.68cqw/1.5 Georgia,serif;letter-spacing:0;}
.settings-language-tab {top:29cqw;}
html[data-showcase-language="en"] .settings-language-tab {font-size:.9cqw;}
'''
if not (ROOT/'src/core/ui-language.css').exists():
    (ROOT/'src/core/ui-language.css').write_text(css,encoding='utf-8')
index=(ROOT/'index.html').read_text(encoding='utf-8')
if 'src/core/ui-language.css' not in index:
    index=index.replace('  <link rel="stylesheet" href="src/core/save-reference.css">','  <link rel="stylesheet" href="src/core/save-reference.css">\n  <link rel="stylesheet" href="src/core/ui-language.css">')
    (ROOT/'index.html').write_text(index,encoding='utf-8')
scene=(ROOT/'scene.js').read_text(encoding='utf-8')
if 'installUiLanguage' not in scene:
    scene="import { installUiLanguage } from './src/core/ui-language.js';\n"+scene+"\ninstallUiLanguage({window,document});\n"
    (ROOT/'scene.js').write_text(scene,encoding='utf-8')
print('Imported four English PNGs and presentation styles; gameplay modules preserved.')
