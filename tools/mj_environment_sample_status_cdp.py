"""Read the one-shot environment sample's job metadata from the background MJ tab."""
import importlib.util
import json
import sys
import time
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RECORD = ROOT / '美术概念' / (sys.argv[1] if len(sys.argv) > 1 else 'MJ环境内外衔接样图_20261018.json')
sys.stdout.reconfigure(encoding='utf-8')
spec = importlib.util.spec_from_file_location('direct_cdp', ROOT / 'scripts/record-catalog-cdp-session.py')
cdp = importlib.util.module_from_spec(spec)
spec.loader.exec_module(cdp)
record = json.loads(RECORD.read_text(encoding='utf-8'))
job_id = record.get('job_id')
assert job_id, 'The sample has no accepted job ID'
tabs = [tab for tab in cdp.discover('json/list') if tab.get('id') == 'C6DEFB270997741F7D303C62535AEA75'
        and tab.get('type') == 'page' and tab['url'].startswith('https://www.midjourney.com/imagine')]
assert tabs, 'No existing Midjourney imagine tab in the work Chrome'
connection = cdp.Connection(tabs[0]['webSocketDebuggerUrl'])
try:
    connection.call('Runtime.enable')
    try:
        connection.call('Page.setWebLifecycleState', {'state': 'active'})
    except RuntimeError:
        pass
    js = '''(id=>{const links=[...document.querySelectorAll('a[href*="/jobs/'+id+'"]')];
      let section=links[0];for(let n=0;n<8&&section&&!section.innerText.includes('Rerun');n++)section=section.parentElement;
      return {job_id:id,link_count:links.length,links:links.map(a=>a.getAttribute('href')),
        text:(section?.innerText||document.body.innerText).slice(0,450),
        images:links.flatMap(a=>[...a.querySelectorAll('img')].map(i=>({loaded:i.complete,width:i.naturalWidth,height:i.naturalHeight}))),
        visibility:document.visibilityState}})(''' + json.dumps(job_id) + ')'
    response = connection.call('Runtime.evaluate', {'expression': js, 'returnByValue': True})
    if response.get('exceptionDetails'):
        raise RuntimeError(str(response['exceptionDetails']))
    status = response['result']['value']
    if not status['links']:
        version = cdp.discover('json/version')
        browser = cdp.Connection(version['webSocketDebuggerUrl'])
        target = None
        detail = None
        try:
            target = browser.call('Target.createTarget', {
                'url': 'about:blank#mj-environment-sample-status', 'background': True})['targetId']
            descriptor = next(tab for tab in cdp.discover('json/list') if tab['id'] == target)
            detail = cdp.Connection(descriptor['webSocketDebuggerUrl'])
            detail.call('Page.enable')
            detail.call('Runtime.enable')
            detail.call('Page.navigate', {'url': record['result_url']})
            deadline = time.monotonic() + 20
            while time.monotonic() < deadline:
                page = detail.call('Runtime.evaluate', {'expression': '''(()=>({
                    url:location.href,visibility:document.visibilityState,
                    text:document.body.innerText.slice(0,700),
                    images:[...document.querySelectorAll('img')].slice(0,8).map(i=>({
                      loaded:i.complete,width:i.naturalWidth,height:i.naturalHeight}))
                  }))()''', 'returnByValue': True})
                if not page.get('exceptionDetails'):
                    value = page['result'].get('value')
                    if value and '/jobs/' in value['url'] and (len(value['text']) > 100 or value['images']):
                        status['detail'] = value
                        break
                time.sleep(.5)
        finally:
            if detail:
                detail.close()
            if target:
                browser.call('Target.closeTarget', {'targetId': target})
            browser.close()
    record['last_status'] = status
    record['status_checked_at'] = datetime.now(timezone.utc).isoformat()
    RECORD.write_text(json.dumps(record, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(status, ensure_ascii=False))
finally:
    connection.close()
