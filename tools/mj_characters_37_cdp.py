"""Submit the 37-character portrait batch (38 jobs) to Midjourney through the fixed
work Chrome + direct CDP. Same proven path as mj_scenes_40_snow_cdp.py (2026-09-17):
synthetic keydown on the prompt textarea for the React handler, job ids captured from
the /api/submit-jobs response body via the Network domain. Each character submits
exactly once; unknown receipts stop the batch for manual verification."""
import argparse, importlib.util, json, sys, time
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RECORD = ROOT / '美术概念' / 'MJ人物37个_v1_20260917.json'
sys.stdout.reconfigure(encoding='utf-8')
spec = importlib.util.spec_from_file_location('direct_cdp', ROOT / 'scripts/record-catalog-cdp-session.py')
cdp = importlib.util.module_from_spec(spec)
spec.loader.exec_module(cdp)
import websocket

parser = argparse.ArgumentParser()
parser.add_argument('action', choices=['inspect', 'submit', 'status'])
args = parser.parse_args()

version = cdp.discover('json/version')
assert version['Browser'].startswith('Chrome/'), version
tabs = [t for t in cdp.discover('json/list') if t.get('type') == 'page' and t['url'].startswith('https://www.midjourney.com/')]
assert tabs, 'No Midjourney page tab found'
tab = next((t for t in tabs if '/imagine' in t['url']), tabs[0])
if '/imagine' not in tab['url'] or 'folder=' not in tab['url']:
    ws0 = cdp.Connection(tab['webSocketDebuggerUrl'])
    ws0.call('Page.enable')
    ws0.call('Page.navigate', {'url': 'https://www.midjourney.com/imagine?folder=%E4%B8%9C%E5%8C%97%E8%BF%AA%E6%96%AF%E7%A7%91%E5%8F%82%E8%80%83%E5%9B%BE'})
    deadline = time.monotonic() + 40
    while time.monotonic() < deadline:
        try:
            r = ws0.call('Runtime.evaluate', {'expression': '!!document.querySelector("textarea#desktop_input_bar")', 'returnByValue': True})
            if r['result'].get('value'):
                break
        except Exception:
            pass
        time.sleep(1.5)
    ws0.close()
    tabs = [t for t in cdp.discover('json/list') if t.get('type') == 'page' and t['url'].startswith('https://www.midjourney.com/')]
    tab = next((t for t in tabs if '/imagine' in t['url']), tabs[0])
print('target tab:', tab['id'], tab['url'][:130])
ws = websocket.create_connection(tab['webSocketDebuggerUrl'], timeout=12, suppress_origin=True, http_proxy_host=None)


def call(method, params=None, _id=[0]):
    _id[0] += 1
    ws.send(json.dumps({'id': _id[0], 'method': method, 'params': params or {}}))
    deadline = time.monotonic() + 20
    while time.monotonic() < deadline:
        r = json.loads(ws.recv())
        if r.get('id') == _id[0]:
            if 'error' in r:
                raise RuntimeError(method + ': ' + str(r['error']))
            return r.get('result', {})
    raise TimeoutError(method)


def ev(js):
    r = call('Runtime.evaluate', {'expression': js, 'returnByValue': True, 'awaitPromise': True, 'userGesture': True})
    if r.get('exceptionDetails'):
        raise RuntimeError('JS: ' + str(r['exceptionDetails']))
    return r['result'].get('value')


def drain_until(predicate, timeout, extra=None):
    events = []
    ws.settimeout(timeout)
    end = time.monotonic() + timeout
    try:
        while time.monotonic() < end:
            r = json.loads(ws.recv())
            if 'id' not in r:
                events.append(r)
                if predicate(r):
                    break
            elif r.get('id') == 999999:
                pass
    except Exception:
        pass
    return events


def load_record():
    return json.loads(RECORD.read_text(encoding='utf-8'))


def save_record(data):
    RECORD.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


def submit_one(c, data):
    cid = c['id']
    prompt = c['prompt']
    deadline = time.monotonic() + 20
    while time.monotonic() < deadline:
        v = ev('document.querySelector("textarea#desktop_input_bar")?.value ?? null')
        if v == '':
            break
        if v is not None:
            # clear leftover draft text through the React-native value setter
            ev('''(()=>{const ta=document.querySelector('textarea#desktop_input_bar');const setter=Object.getOwnPropertyDescriptor(HTMLTextAreaElement.prototype,'value').set;setter.call(ta,'');ta.dispatchEvent(new Event('input',{bubbles:true}));return true})()''')
        time.sleep(.5)
    else:
        raise RuntimeError(f'{cid}: textarea not empty before fill')
    ev('document.querySelector("textarea#desktop_input_bar").focus()')
    call('Input.insertText', {'text': prompt})
    assert ev('document.querySelector("textarea#desktop_input_bar").value') == prompt, f'{cid}: exact prompt mismatch'
    preflight = ev('''(()=>{const a=document.querySelector('textarea#desktop_input_bar').parentElement.parentElement.parentElement,p=[...document.querySelectorAll('button')].find(e=>e.textContent==='PPersonalize');return {folder:a.innerText.includes('东北迪斯科参考图'),images:a.querySelectorAll('img').length,personalization:p?.getAttribute('data-active')}})()''')
    assert preflight == {'folder': True, 'images': 0, 'personalization': 'false'}, f'{cid} preflight {preflight}'
    c['preflight'] = preflight
    c['submitted_at'] = datetime.now(timezone.utc).isoformat()
    save_record(data)
    ev('''(()=>{const ta=document.querySelector('textarea#desktop_input_bar');const o={key:'Enter',code:'Enter',keyCode:13,which:13,charCode:13,bubbles:true,cancelable:true,view:window};ta.dispatchEvent(new KeyboardEvent('keydown',o));return true})()''')
    deadline = time.monotonic() + 30
    submit_body = None
    collections_bodies = []
    while time.monotonic() < deadline:
        events = drain_until(lambda e: e.get('method') in ('Network.responseReceived', 'Network.loadingFailed'), 8)
        hit = False
        for e in events:
            m = e['method']
            d = e.get('params', {})
            url = d.get('response', {}).get('url', '')
            rid = d.get('requestId')
            if m == 'Network.loadingFailed' and '/api/submit-jobs' in url:
                raise RuntimeError(f'{cid}: submit-jobs failed: {d.get("errorText")}')
            if m == 'Network.responseReceived' and '/api/submit-jobs' in url:
                try:
                    body = call('Network.getResponseBody', {'requestId': rid})
                    submit_body = json.loads(body.get('body') or '{}')
                except Exception as ex:
                    submit_body = {'body_read_error': str(ex)[:120]}
                hit = True
                break
            if m == 'Network.responseReceived' and '/api/collections-images' in url:
                try:
                    body = call('Network.getResponseBody', {'requestId': rid})
                    collections_bodies.append(json.loads(body.get('body') or '{}'))
                except Exception:
                    pass
        if hit:
            break
    if submit_body is None:
        c['status'] = 'submission_unknown'
        save_record(data)
        print(json.dumps({'character': cid, 'status': 'submission_unknown', 'STOPPED': True}, ensure_ascii=False))
        raise SystemExit(2)
    c['submit_receipt'] = {'http': 200, 'collections_snapshots': len(collections_bodies)}
    ids = []
    def collect(n):
        if isinstance(n, dict):
            if isinstance(n.get('job_id'), str):
                ids.append(n['job_id'])
            for v in n.values():
                collect(v)
        elif isinstance(n, list):
            for v in n:
                collect(v)
    collect(submit_body)
    ids = list(dict.fromkeys(ids))
    if len(ids) != 1:
        c['status'] = 'submission_unknown'
        c['submit_receipt']['job_ids_found'] = ids
        save_record(data)
        print(json.dumps({'character': cid, 'status': 'submission_unknown', 'ids': ids, 'STOPPED': True}, ensure_ascii=False))
        raise SystemExit(2)
    job_id = ids[0]
    c['job_id'] = job_id
    c['status'] = 'accepted'
    c['result_url'] = f'https://www.midjourney.com/jobs/{job_id}?index=0'
    c['service_rephrased_prompt'] = None
    for k in ('prompt', 'full_command', 'raw_prompt'):
        if isinstance(submit_body.get(k), str):
            c['service_rephrased_prompt'] = submit_body[k][:400]
            break
    if c['service_rephrased_prompt'] is None:
        c['service_rephrased_prompt'] = json.dumps(submit_body, ensure_ascii=False)[:400]
    save_record(data)
    print(json.dumps({'character': cid, 'status': 'accepted', 'job_id': job_id}, ensure_ascii=False), flush=True)


try:
    call('Runtime.enable')
    call('Page.enable')
    call('Network.enable')
    try:
        call('Page.setWebLifecycleState', {'state': 'active'})
    except Exception as e:
        print('Lifecycle:', str(e)[:150])

    if args.action == 'inspect':
        print(json.dumps(ev('''(()=>{const ta=document.querySelector('textarea#desktop_input_bar');return {url:location.href,textarea:ta?ta.value.slice(0,80):null,vis:document.visibilityState,body:document.body.innerText.slice(0,400)}})()'''), ensure_ascii=False))
    elif args.action == 'submit':
        data = load_record()
        # self-heal: if the tab died or the textarea is missing (ERR_CONNECTION_CLOSED
        # leaves no input), re-navigate to the folder URL once and wait for the app.
        if ev('!!document.querySelector("textarea#desktop_input_bar")') is not True:
            print('textarea missing; re-navigating to folder URL')
            call('Page.navigate', {'url': 'https://www.midjourney.com/imagine?folder=%E4%B8%9C%E5%8C%97%E8%BF%AA%E6%96%AF%E7%A7%91%E5%8F%82%E8%80%83%E5%9B%BE'})
            deadline = time.monotonic() + 90
            while time.monotonic() < deadline:
                try:
                    if ev('!!document.querySelector("textarea#desktop_input_bar")') is True:
                        break
                except Exception:
                    pass
                time.sleep(2)
            else:
                print(json.dumps({'error': 'textarea did not appear after re-navigation (MJ unreachable?)'}, ensure_ascii=False))
                raise SystemExit(3)
        results = []
        for c in data['characters']:
            cid = c['id']
            if c.get('job_id') or c.get('status') == 'accepted':
                results.append({cid: 'skipped_already_accepted'})
                continue
            submit_one(c, data)
            results.append({cid: 'accepted', 'job_id': c['job_id']})
            time.sleep(3)
        data['generation_status'] = 'all_submitted_20260917' if all(c.get('status') == 'accepted' for c in data['characters']) else 'partial_submitted'
        save_record(data)
        print(json.dumps({'results': results, 'generation_status': data['generation_status']}, ensure_ascii=False))
    elif args.action == 'status':
        data = load_record()
        out = []
        for c in data['characters']:
            if not c.get('job_id'):
                out.append({c['id']: {'status': c['status']}})
                continue
            value = ev('''(id=>{const links=[...document.querySelectorAll('a[href*="/jobs/'+id+'"]')];let section=links[0];for(let n=0;n<8&&section&&!section.innerText.includes('Rerun');n++)section=section.parentElement;const folder=[...document.querySelectorAll('div')].find(e=>e.childElementCount===0&&e.textContent==='东北迪斯科参考图');return {job_id:id,links:links.map(a=>({href:a.getAttribute('href'),images:[...a.querySelectorAll('img')].map(i=>({loaded:i.complete,width:i.naturalWidth,height:i.naturalHeight}))})),text:section?.innerText?.slice(0,600)||document.body.innerText.slice(0,400),folderSelected:folder?.parentElement?.parentElement?.className?.includes('bg-splash')}})(''' + json.dumps(c['job_id']) + ')')
            c['last_status'] = value
            c['status_checked_at'] = datetime.now(timezone.utc).isoformat()
            out.append({c['id']: {'job_id': c['job_id'], 'links': len(value['links']), 'folderSelected': value['folderSelected'], 'text': value['text'][:200]}})
        save_record(data)
        print(json.dumps({'characters': out}, ensure_ascii=False))
finally:
    ws.close()
