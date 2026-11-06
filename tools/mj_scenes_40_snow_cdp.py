"""Submit the 40-scene snow-v1 batch to Midjourney through the fixed work Chrome + direct CDP.
Metadata only; no image transport. The MJ tab is backgrounded (visibilityState hidden) so
CDP raw input is dropped; submissions use synthetic keydown on the prompt textarea (React
handler path, verified 2026-09-17) and job ids are captured from the /api/submit-jobs
response body via the Network domain. Each scene submits exactly once; unknown receipts
stop the batch for manual verification."""
import argparse, importlib.util, json, sys, time
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RECORD = ROOT / '美术概念' / 'MJ剧情场景40景雪夜_v1_20260917.json'
sys.stdout.reconfigure(encoding='utf-8')
spec = importlib.util.spec_from_file_location('direct_cdp', ROOT / 'scripts/record-catalog-cdp-session.py')
cdp = importlib.util.module_from_spec(spec)
spec.loader.exec_module(cdp)
import websocket

DAYS = {
    'D0': ['ND-S01', 'ND-S02', 'ND-S03', 'ND-S04', 'ND-S05', 'ND-S06', 'ND-S07'],
    'D1': ['ND-S08', 'ND-S09', 'ND-S10', 'ND-S11', 'ND-S13', 'ND-S14', 'ND-S15', 'ND-S17', 'ND-S18', 'ND-S20', 'ND-S21', 'ND-S33'],
    'D2': ['ND-S12', 'ND-S19', 'ND-S22', 'ND-S23', 'ND-S25', 'ND-S26', 'ND-S27', 'ND-S29'],
    'D3': ['ND-S30', 'ND-S31', 'ND-S32'],
    'D4': ['ND-S24', 'ND-S34'],
    'D5': ['ND-S16', 'ND-S28', 'ND-S35', 'ND-S36'],
    'D6': ['ND-S37', 'ND-S38'],
    'D7': ['ND-S39', 'ND-S40'],
}
assert sum(len(v) for v in DAYS.values()) == 40, sum(len(v) for v in DAYS.values())

parser = argparse.ArgumentParser()
parser.add_argument('action', choices=['inspect', 'submit-day', 'submit-ids', 'status-day', 'capture-lists'])
parser.add_argument('--day', choices=sorted(DAYS))
parser.add_argument('--record', type=Path, default=RECORD)
parser.add_argument('--ids', nargs='+')
parser.add_argument('--calibration', action='store_true', help='Allow exactly one camera test before visual approval')
args = parser.parse_args()
RECORD = args.record

version = cdp.discover('json/version')
assert version['Browser'].startswith('Chrome/'), version
tabs = [t for t in cdp.discover('json/list') if t.get('type') == 'page' and t['url'].startswith('https://www.midjourney.com/')]
assert tabs, 'No Midjourney page tab found'
tab = next((t for t in tabs if '/imagine' in t['url']), tabs[0])
if '/imagine' not in tab['url']:
    # SPA wandered off to another MJ view; restore the imagine page with the target folder.
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

# ---- raw CDP helpers (events needed for Network capture) ----
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
    """Read raw WS events until predicate(event) or timeout; return collected events."""
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
            elif r.get('id') == 999999:  # sentinel never used
                pass
    except Exception:
        pass
    return events


def load_record():
    return json.loads(RECORD.read_text(encoding='utf-8'))


def save_record(data):
    RECORD.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


def submit_one(scene, data):
    sid = scene['id']
    prompt = scene['prompt']
    # textarea must be empty (previous submission cleared it); wait for the app to settle
    deadline = time.monotonic() + 20
    while time.monotonic() < deadline:
        if ev('document.querySelector("textarea#desktop_input_bar")?.value ?? ""') == '':
            break
        time.sleep(.5)
    else:
        raise RuntimeError(f'{sid}: textarea not empty before fill')
    ev('document.querySelector("textarea#desktop_input_bar").focus()')
    call('Input.insertText', {'text': prompt})
    assert ev('document.querySelector("textarea#desktop_input_bar").value') == prompt, f'{sid}: exact prompt mismatch'
    preflight = ev('''(()=>{const a=document.querySelector('textarea#desktop_input_bar').parentElement.parentElement.parentElement,p=[...document.querySelectorAll('button')].find(e=>e.textContent==='PPersonalize');return {folder:a.innerText.includes('东北迪斯科参考图'),images:a.querySelectorAll('img').length,personalization:p?.getAttribute('data-active')}})()''')
    assert preflight == {'folder': True, 'images': 0, 'personalization': 'false'}, f'{sid} preflight {preflight}'
    scene['preflight'] = preflight
    scene['submitted_at'] = datetime.now(timezone.utc).isoformat()
    save_record(data)
    # synthetic Enter on the textarea (React keydown handler path)
    ev('''(()=>{const ta=document.querySelector('textarea#desktop_input_bar');const o={key:'Enter',code:'Enter',keyCode:13,which:13,charCode:13,bubbles:true,cancelable:true,view:window};ta.dispatchEvent(new KeyboardEvent('keydown',o));return true})()''')
    # wait for the submit-jobs response
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
                raise RuntimeError(f'{sid}: submit-jobs failed: {d.get("errorText")}')
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
        scene['status'] = 'submission_unknown'
        save_record(data)
        print(json.dumps({'scene': sid, 'status': 'submission_unknown', 'STOPPED': True}, ensure_ascii=False))
        raise SystemExit(2)
    scene['submit_receipt'] = {'http': 200, 'collections_snapshots': len(collections_bodies)}
    # extract job id: search the whole response tree for job_id keys
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
        scene['status'] = 'submission_unknown'
        scene['submit_receipt']['job_ids_found'] = ids
        save_record(data)
        print(json.dumps({'scene': sid, 'status': 'submission_unknown', 'ids': ids, 'STOPPED': True}, ensure_ascii=False))
        raise SystemExit(2)
    job_id = ids[0]
    scene['job_id'] = job_id
    scene['status'] = 'accepted'
    scene['result_url'] = f'https://www.midjourney.com/jobs/{job_id}?index=0'
    scene['service_rephrased_prompt'] = None
    # find the rephrased prompt string inside the response
    for k in ('prompt', 'full_command', 'raw_prompt'):
        if isinstance(submit_body.get(k), str):
            scene['service_rephrased_prompt'] = submit_body[k][:400]
            break
    if scene['service_rephrased_prompt'] is None:
        scene['service_rephrased_prompt'] = json.dumps(submit_body, ensure_ascii=False)[:400]
    save_record(data)
    print(json.dumps({'scene': sid, 'status': 'accepted', 'job_id': job_id}, ensure_ascii=False), flush=True)


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
    elif args.action == 'capture-lists':
        # trigger the app to refresh its recent list by focusing + nudge, then capture any /api/* lists
        events = drain_until(lambda e: e.get('method') == 'Network.responseReceived' and '/api/collections-images' in e.get('params', {}).get('response', {}).get('url', ''), 10)
        print(json.dumps({'captured': len(events)}, ensure_ascii=False))
    elif args.action == 'submit-day':
        data = load_record()
        assert data.get('camera_calibration', {}).get('visual_pass') is True, 'Camera calibration has no verified visual result; batch submission is locked'
        scenes = {s['id']: s for s in data['scenes']}
        results = []
        for sid in DAYS[args.day]:
            scene = scenes[sid]
            if scene.get('job_id') or scene.get('status') == 'accepted':
                results.append({sid: 'skipped_already_accepted'})
                continue
            submit_one(scene, data)
            results.append({sid: 'accepted', 'job_id': scene['job_id']})
            time.sleep(3)
        print(json.dumps({'day': args.day, 'results': results}, ensure_ascii=False))
    elif args.action == 'submit-ids':
        assert args.ids, '--ids required'
        data = load_record()
        if data.get('camera_calibration', {}).get('visual_pass') is not True:
            assert args.calibration and len(args.ids) == 1, 'Camera calibration has no verified visual result; only one --calibration scene is allowed'
        scenes = {s['id']: s for s in data['scenes']}
        assert len(set(args.ids)) == len(args.ids), 'duplicate ids'
        for sid in args.ids:
            scene = scenes[sid]
            if scene.get('job_id') or scene.get('status') == 'accepted':
                print(json.dumps({'scene': sid, 'status': 'skipped_already_accepted'}, ensure_ascii=False), flush=True)
                continue
            if scene.get('status') not in ('planned', 'not_submitted'):
                raise RuntimeError(f'{sid}: unresolved prior status {scene.get("status")}')
            submit_one(scene, data)
            time.sleep(3)
    elif args.action == 'status-day':
        data = load_record()
        scenes = {s['id']: s for s in data['scenes']}
        out = []
        for sid in DAYS[args.day]:
            scene = scenes[sid]
            if not scene.get('job_id'):
                out.append({sid: {'status': scene['status']}})
                continue
            value = ev('''(id=>{const links=[...document.querySelectorAll('a[href*="/jobs/'+id+'"]')];let section=links[0];for(let n=0;n<8&&section&&!section.innerText.includes('Rerun');n++)section=section.parentElement;const folder=[...document.querySelectorAll('div')].find(e=>e.childElementCount===0&&e.textContent==='东北迪斯科参考图');return {job_id:id,links:links.map(a=>({href:a.getAttribute('href'),images:[...a.querySelectorAll('img')].map(i=>({loaded:i.complete,width:i.naturalWidth,height:i.naturalHeight}))})),text:section?.innerText?.slice(0,600)||document.body.innerText.slice(0,400),folderSelected:folder?.parentElement?.parentElement?.className?.includes('bg-splash')}})(''' + json.dumps(scene['job_id']) + ')')
            scene['last_status'] = value
            scene['status_checked_at'] = datetime.now(timezone.utc).isoformat()
            out.append({sid: {'job_id': scene['job_id'], 'links': len(value['links']), 'folderSelected': value['folderSelected'], 'text': value['text'][:200]}})
        save_record(data)
        print(json.dumps({'day': args.day, 'scenes': out}, ensure_ascii=False))
finally:
    ws.close()
