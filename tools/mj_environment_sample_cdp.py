"""Submit one recorded environment sample through the persistent background Chrome CDP tab.

Only JSON task metadata is read from the Midjourney response. Image bytes stay in the site.
An unknown receipt stops without retrying, so the same task cannot be submitted twice.
"""
import importlib.util
import json
import sys
import time
from datetime import datetime, timezone
from pathlib import Path

import websocket

ROOT = Path(__file__).resolve().parents[1]
RECORD = ROOT / '美术概念' / (sys.argv[1] if len(sys.argv) > 1 else 'MJ环境内外衔接样图_20261018.json')
sys.stdout.reconfigure(encoding='utf-8')
spec = importlib.util.spec_from_file_location('direct_cdp', ROOT / 'scripts/record-catalog-cdp-session.py')
cdp = importlib.util.module_from_spec(spec)
spec.loader.exec_module(cdp)


def save(record):
    RECORD.write_text(json.dumps(record, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


record = json.loads(RECORD.read_text(encoding='utf-8'))
if record['status'] != 'draft' or record.get('job_id') or record.get('submitted_at'):
    raise SystemExit('This sample was already attempted; inspect its recorded status before any retry.')
version = cdp.discover('json/version')
assert version['Browser'].startswith('Chrome/'), version
tabs = [tab for tab in cdp.discover('json/list') if tab.get('id') == 'C6DEFB270997741F7D303C62535AEA75'
        and tab.get('type') == 'page' and tab['url'].startswith('https://www.midjourney.com/imagine')]
assert tabs, 'No existing background Midjourney imagine tab in work Chrome'
tab = tabs[0]
ws = websocket.create_connection(tab['webSocketDebuggerUrl'], timeout=12,
                                 suppress_origin=True, http_proxy_host=None)
serial = 0


def call(method, params=None, timeout=20):
    global serial
    serial += 1
    request_id = serial
    ws.send(json.dumps({'id': request_id, 'method': method, 'params': params or {}}))
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        ws.settimeout(max(.1, deadline - time.monotonic()))
        message = json.loads(ws.recv())
        if message.get('id') == request_id:
            if 'error' in message:
                raise RuntimeError(f'{method}: {message["error"]}')
            return message.get('result', {})
    raise TimeoutError(method)


def evaluate(expression):
    result = call('Runtime.evaluate', {'expression': expression, 'returnByValue': True,
                                       'awaitPromise': True, 'userGesture': True})
    if result.get('exceptionDetails'):
        raise RuntimeError(str(result['exceptionDetails']))
    return result['result'].get('value')


try:
    call('Runtime.enable')
    call('Page.enable')
    call('Network.enable')
    try:
        call('Page.setWebLifecycleState', {'state': 'active'})
    except RuntimeError:
        pass
    preflight = evaluate('''(()=>{const ta=document.querySelector('textarea#desktop_input_bar');
      if(!ta)return {ready:false};const root=ta.parentElement.parentElement;
      const p=[...document.querySelectorAll('button')].find(button=>button.textContent==='PPersonalize');
      return {ready:true,empty:ta.value==='',folder:decodeURIComponent(location.href).includes('东北迪斯科参考图'),
        images:root.querySelectorAll('img').length,personalization:p?.getAttribute('data-active')??'false',
        visibility:document.visibilityState}})()''')
    print(json.dumps({'tab': tab['id'], 'preflight': preflight}, ensure_ascii=False), flush=True)
    assert preflight['ready'] and preflight['empty'] and preflight['folder']
    assert preflight['images'] == 0 and preflight['personalization'] == 'false'
    evaluate('document.querySelector("textarea#desktop_input_bar").focus()')
    call('Input.insertText', {'text': record['prompt']})
    assert evaluate('document.querySelector("textarea#desktop_input_bar").value') == record['prompt']
    record['preflight'] = preflight
    record['submitted_at'] = datetime.now(timezone.utc).isoformat()
    record['status'] = 'submission_unknown'
    save(record)
    evaluate('''(()=>{const ta=document.querySelector('textarea#desktop_input_bar');
      ta.dispatchEvent(new KeyboardEvent('keydown',{key:'Enter',code:'Enter',keyCode:13,
        which:13,charCode:13,bubbles:true,cancelable:true,view:window}));return true})()''')
    request_ids = {}
    body = None
    deadline = time.monotonic() + 35
    while time.monotonic() < deadline and body is None:
        ws.settimeout(max(.1, deadline - time.monotonic()))
        try:
            message = json.loads(ws.recv())
        except websocket.WebSocketTimeoutException:
            break
        params = message.get('params', {})
        if message.get('method') == 'Network.responseReceived' and '/api/submit-jobs' in params.get('response', {}).get('url', ''):
            request_ids[params['requestId']] = params['response'].get('status')
        if message.get('method') == 'Network.loadingFinished' and params.get('requestId') in request_ids:
            http_status = request_ids[params['requestId']]
            response = call('Network.getResponseBody', {'requestId': params['requestId']})
            body = json.loads(response.get('body') or '{}')
    if body is None:
        raise RuntimeError('Submission receipt unknown; inspect site and record before retrying')
    success = body.get('success', []) if isinstance(body, dict) else []
    failure = body.get('failure', []) if isinstance(body, dict) else []
    if http_status != 200 or failure or len(success) != 1:
        record['status'] = 'submission_rejected'
        record['submission_error'] = {'http_status': http_status, 'failure': failure}
        save(record)
        raise RuntimeError('Midjourney did not accept this task; inspect the failure array')
    ids = set()

    def collect(node):
        if isinstance(node, dict):
            if isinstance(node.get('job_id'), str):
                ids.add(node['job_id'])
            for value in node.values():
                collect(value)
        elif isinstance(node, list):
            for value in node:
                collect(value)

    collect(success)
    if len(ids) != 1:
        raise RuntimeError(f'Expected one job ID; received {len(ids)}. Inspect before retrying')
    record['job_id'] = ids.pop()
    record['result_url'] = f'https://www.midjourney.com/jobs/{record["job_id"]}?index=0'
    record['status'] = 'accepted'
    save(record)
    print(json.dumps({'status': record['status'], 'job_id': record['job_id'],
                      'result_url': record['result_url']}, ensure_ascii=False), flush=True)
finally:
    ws.close()
