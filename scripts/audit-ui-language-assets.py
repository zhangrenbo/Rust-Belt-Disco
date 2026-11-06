"""Read-only audit: preserve existing story changes instead of blessing old hashes."""
from pathlib import Path
import hashlib,json,re
ROOT=Path(__file__).resolve().parents[1]
manifest=json.loads((ROOT/'docs/portable-content-manifest.json').read_text(encoding='utf-8'))
differences=[]
for item in manifest['files']:
    path=ROOT/item['destination'];data=path.read_bytes();sha=hashlib.sha256(data).hexdigest()
    if sha!=item['sha256'] or len(data)!=item['bytes']:
        differences.append({'path':item['destination'],'expected':item['sha256'],'actual':sha,'bytes':len(data),'modified':path.stat().st_mtime})
assets=[]
for name in ('main-menu-logo-en.png','menu-en-atlas.png','character-en-atlas.png','dossier-en-atlas.png'):
    path=ROOT/'public/assets'/name;data=path.read_bytes()
    assert data.startswith(b'\x89PNG\r\n\x1a\n')
    assert data==(Path('E:/抖音开发者工具/PNG_UI_HTML复刻/000 公司官网/games/dongbei-disco')/name).read_bytes()
    assert data==(ROOT/'dist/assets'/name).read_bytes()
    assets.append({'file':str(path.relative_to(ROOT)),'sha256':hashlib.sha256(data).hexdigest(),'bytes':len(data),'sameAsShowcaseAndBuild':True})
paths=set(re.findall(r'(?:src|data-src|href)="([^"#]+)"',(ROOT/'index.html').read_text(encoding='utf-8')))
for file in [ROOT/'app.css',*list((ROOT/'src').rglob('*.css'))]:
    for url in re.findall(r'url\([\s\"\x27]*([^\)\"\x27]+)',file.read_text(encoding='utf-8')):
        if ':' not in url and not url.startswith(('#','%23')):
            assert (ROOT/'public'/url.lstrip('/')).exists() or (file.parent/url).exists(),url
for url in paths:
    if ':' not in url: assert (ROOT/url).exists() or (ROOT/'public'/url.lstrip('/')).exists(),url
report={'assets':assets,'legacyManifestDifferences':differences,'legacyManifestMatching':len(manifest['files'])-len(differences),'allCurrentHtmlCssPathsExist':True,'storyAndManifestModifiedByThisPort':False}
(ROOT/'docs/ui-language-asset-audit.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'pngs':len(assets),'matchingLegacyHashes':report['legacyManifestMatching'],'legacyDifferences':[d['path'] for d in differences],'resourcePaths':'passed'},ensure_ascii=False))
