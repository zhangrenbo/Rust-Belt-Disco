"""Submit one previously unconfirmed map prompt and verify the Midjourney UI."""
import importlib.util
import json
import sys
import time
from datetime import datetime, timezone
from pathlib import Path

import websocket

sys.stdout.reconfigure(encoding='utf-8')
root = Path(__file__).resolve().parents[1]
art = root / '\u7f8e\u672f\u6982\u5ff5'
task_id = sys.argv[1]
record_path = next(art.glob(f'MJ*{task_id}*20261018.json'))
record = json.loads(record_path.read_text(encoding='utf-8-sig'))
assert record['id'] == task_id
assert record.get('verification_status') != 'confirmed_visible'

spec = importlib.util.spec_from_file_location('direct_cdp', root / 'scripts/record-catalog-cdp-session.py')
cdp = importlib.util.module_from_spec(spec)
spec.loader.exec_module(cdp)
tab = next(t for t in cdp.discover('json/list') if t.get('id') == 'C6DEFB270997741F7D303C62535AEA75')
assert tab['url'].startswith('https://www.midjourney.com/imagine')
ws = websocket.create_connection(tab['webSocketDebuggerUrl'], timeout=15, suppress_origin=True, http_proxy_host=None)
serial = 0


def call(method, params=None):
    global serial
    serial += 1
    request_id = serial
    ws.send(json.dumps({'id': request_id, 'method': method, 'params': params or {}}))
    while True:
        response = json.loads(ws.recv())
        if response.get('id') == request_id:
            if 'error' in response:
                raise RuntimeError(f'{method}: {response["error"]}')
            return response.get('result', {})


def evaluate(expr):
    result = call('Runtime.evaluate', {'expression': expr, 'returnByValue': True, 'awaitPromise': True})
    if result.get('exceptionDetails'):
        raise RuntimeError(result['exceptionDetails'])
    return result['result'].get('value')


try:
    call('Network.enable')
    try:
        call('Page.setWebLifecycleState', {'state': 'active'})
    except RuntimeError:
        pass
    before = evaluate('''(()=>({ready:!!document.querySelector('textarea#desktop_input_bar'),
      empty:document.querySelector('textarea#desktop_input_bar')?.value==='',
      folder:decodeURIComponent(location.href).includes('东北迪斯科参考图'),
      text:document.body.innerText.slice(-350)}))()''')
    assert before['ready'] and before['empty'] and before['folder'], before
    assert record['prompt']
    evaluate('document.querySelector("textarea#desktop_input_bar").focus()')
    call('Input.insertText', {'text': record['prompt']})
    assert evaluate('document.querySelector("textarea#desktop_input_bar").value') == record['prompt']
    record['verification_status'] = 'submit_attempt_started'
    record['verification_attempt_at'] = datetime.now(timezone.utc).isoformat()
    record_path.write_text(json.dumps(record, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    evaluate('''(()=>{const t=document.querySelector('textarea#desktop_input_bar');
      t.dispatchEvent(new KeyboardEvent('keydown',{key:'Enter',code:'Enter',keyCode:13,
        which:13,charCode:13,bubbles:true,cancelable:true,view:window}));return true})()''')
    response_ids = {}
    body = None
    http_status = None
    deadline = time.monotonic() + 35
    while time.monotonic() < deadline and body is None:
        ws.settimeout(max(.1, deadline - time.monotonic()))
        try:
            event = json.loads(ws.recv())
        except websocket.WebSocketTimeoutException:
            break
        params = event.get('params', {})
        if event.get('method') == 'Network.responseReceived' and '/api/submit-jobs' in params.get('response', {}).get('url', ''):
            response_ids[params['requestId']] = params['response'].get('status')
        if event.get('method') == 'Network.loadingFinished' and params.get('requestId') in response_ids:
            http_status = response_ids[params['requestId']]
            raw = call('Network.getResponseBody', {'requestId': params['requestId']})
            body = json.loads(raw.get('body') or '{}')
    if body is None:
        raise RuntimeError('Submission HTTP response unavailable')
    ids = set()

    def walk(node):
        if isinstance(node, dict):
            if isinstance(node.get('job_id'), str):
                ids.add(node['job_id'])
            for value in node.values():
                walk(value)
        elif isinstance(node, list):
            for value in node:
                walk(value)

    walk(body)
    record['verification_http_status'] = http_status
    record['verification_receipt_shape'] = list(body) if isinstance(body, dict) else type(body).__name__
    success = body.get('success', []) if isinstance(body, dict) else []
    failure = body.get('failure', []) if isinstance(body, dict) else []
    record['verification_receipt'] = {
        'success': [{'job_id': x.get('job_id'), 'is_queued': x.get('is_queued')} for x in success],
        'failure': failure,
    }
    record['verification_new_job_id'] = next(iter(ids)) if len(ids) == 1 else None
    time.sleep(3)
    after = evaluate('''(()=>({tail:document.body.innerText.slice(-450),
      folderText:document.body.innerText.match(/东北迪斯科参考图\\n(\\d+)/)?.[1]||null,
      queued:document.body.innerText.includes('Queued'),
      promptTail:document.body.innerText.includes(''' + json.dumps(record['prompt'][:110]) + ''')}))()''')
    record['verification_ui'] = after
    confirmed = http_status == 200 and len(success) == 1 and not failure and success[0].get('job_id') in ids
    record['verification_status'] = 'accepted_pending_render' if confirmed else 'submission_rejected'
    if confirmed:
        record['job_id'] = success[0]['job_id']
        record['result_url'] = f'https://www.midjourney.com/jobs/{record["job_id"]}?index=0'
        record['status'] = 'accepted_pending_render'
    else:
        record['status'] = 'submission_rejected'
    record_path.write_text(json.dumps(record, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({'id': task_id, 'http_status': http_status, 'receipt': record['verification_receipt'], 'ui': after}, ensure_ascii=False))
finally:
    ws.close()
