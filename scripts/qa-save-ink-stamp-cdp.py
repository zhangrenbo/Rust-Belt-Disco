"""Reuse the actual six-ticket interaction checks for the stamped title treatment."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
source_path = ROOT / 'scripts/qa-save-reference-cdp.py'
source = source_path.read_text(encoding='utf-8-sig')
# Retain the reference revision's evidence; only change this run's output names.
source = source.replace('docs/save-reference-', 'docs/save-ink-stamp-')
source = source.replace("    before = page.evaluate(SNAPSHOT)", """
    for asset in ['save-ticket-ink-mask.svg', 'save-ticket-stamp-rim.svg']:
        check('Stamp resource loads: '+asset,
              "fetch('/assets/"+asset+"').then(r=>r.ok&&r.headers.get('content-type').includes('image/svg+xml'))")
    before = page.evaluate(SNAPSHOT)""")
source = source.replace("        capture('mixed', width, height)", """
        check(f'{width}: six complete stamps retain live titles and nonblocking rims', '''
          [...document.querySelectorAll('.view-entry [data-save-title]')].every(e=>{
            const ink=getComputedStyle(e),rim=getComputedStyle(e,'::before');
            return e.textContent.trim().length>0&&ink.maskImage.includes('save-ticket-ink-mask.svg')
              &&rim.maskImage.includes('save-ticket-stamp-rim.svg')&&rim.pointerEvents==='none';
          })''')
        capture('mixed', width, height)""")
exec(compile(source, str(source_path), 'exec'), {'__file__': str(source_path), '__name__': '__main__'})
