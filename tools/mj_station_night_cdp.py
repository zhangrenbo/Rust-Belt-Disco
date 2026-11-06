"""Task-specific Midjourney UI work through the existing work Chrome CDP.

Image downloads, when requested, are performed only by Chrome's native manager.
Never retrieve image bodies, screenshots, cookies, or credentials.
"""
import argparse
import json
import os
import sys
import time
from datetime import datetime, timezone
from pathlib import Path
from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[1]
RECORD = ROOT / '美术概念/MJ_A000夜间公交站_20260913_生成记录.json'
os.environ['NO_PROXY'] = 'localhost,127.0.0.1'
sys.stdout.reconfigure(encoding='utf-8')

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('action', choices=['inspect', 'eval', 'fill', 'submit', 'status', 'detail', 'download'])
    parser.add_argument('--file')
    parser.add_argument('--url')
    parser.add_argument('--index', type=int, choices=range(4), default=0)
    args = parser.parse_args()
    with sync_playwright() as p:
        browser = p.chromium.connect_over_cdp('http://127.0.0.1:9223', timeout=15000)
        pages = [page for ctx in browser.contexts for page in ctx.pages
                 if page.url.startswith('https://www.midjourney.com/')]
        page = next((page for page in pages if page.url == (args.url or 'https://www.midjourney.com/imagine')), None)
        if page is None:
            raise RuntimeError('Intended existing Midjourney page not found')
        page.set_default_timeout(12000)
        session = page.context.new_cdp_session(page)
        try:
            session.send('Page.setWebLifecycleState', {'state': 'active'})
        except Exception as exc:
            print(json.dumps({'lifecycleError': str(exc)[:200]}))
        if args.action == 'download':
            record = json.loads(RECORD.read_text(encoding='utf-8'))
            if record['status'] not in ['completed','downloaded']:
                raise RuntimeError('Generation has not been verified complete')
            job_id=record['jobs'][0]['job_id']
            destination = ROOT / '美术概念'
            target = destination / f'MJ_A000_night_bus_stop_{job_id}_i{args.index}.png'
            previous = next((a for a in record.get('downloads',[]) if a['index']==args.index),None)
            if previous and target.is_file() and target.stat().st_size==previous['bytes']:
                print(json.dumps({'skipped_valid':target.name,'bytes':target.stat().st_size}))
                return
            if target.exists():
                raise RuntimeError('Unverified destination already exists; preserving it')
            expected_url='https://www.midjourney.com/jobs/'+job_id+'?index='+str(args.index)
            if page.url != expected_url:
                page.goto(expected_url, wait_until='domcontentloaded',timeout=20000)
            page.get_by_title('Download Image',exact=True).wait_for(state='visible',timeout=15000)
            if page.url != expected_url:
                raise RuntimeError('Wrong image detail page')
            dl_session=browser.new_browser_cdp_session()
            events={'begins':[],'progress':{}}
            dl_session.on('Browser.downloadWillBegin',lambda e:events['begins'].append({k:e[k] for k in ['guid','suggestedFilename']}))
            dl_session.on('Browser.downloadProgress',lambda e:events['progress'].__setitem__(e['guid'],e))
            dl_session.send('Browser.setDownloadBehavior',{'behavior':'allowAndName','downloadPath':str(destination),'eventsEnabled':True})
            try:
                page.get_by_title('Download Image',exact=True).click()
                deadline=time.monotonic()+45
                completed=None
                while time.monotonic()<deadline:
                    page.wait_for_timeout(250)
                    if len(events['begins'])>1:
                        raise RuntimeError('Unexpected concurrent downloads; stopped after one click')
                    if events['begins']:
                        meta=events['begins'][0]
                        progress=events['progress'].get(meta['guid'],{})
                        if progress.get('state')=='canceled':
                            raise RuntimeError('Native download canceled')
                        if progress.get('state')=='completed':
                            completed=(meta,progress)
                            break
                if not completed:
                    raise RuntimeError('Native download did not complete within 45 seconds')
                meta,progress=completed
                if Path(meta['suggestedFilename']).suffix.lower()!='.png':
                    raise RuntimeError('Unexpected native file extension; original download preserved')
                downloaded=destination/meta['guid']
                if not downloaded.is_file() or downloaded.stat().st_size<=0:
                    raise RuntimeError('Native completed file missing')
                size=downloaded.stat().st_size
                if int(progress['receivedBytes'])!=size:
                    raise RuntimeError('Native byte count differs from file metadata')
                downloaded.rename(target)
                entry={'index':args.index,'job_url':expected_url,'path':str(target),'bytes':size,
                       'native_state':'completed','guid':meta['guid'],'suggested_filename':meta['suggestedFilename'],
                       'completed_at':datetime.now(timezone.utc).isoformat()}
                record.setdefault('downloads',[]).append(entry)
                if len(record['downloads'])==4: record['status']='downloaded'
                RECORD.write_text(json.dumps(record,ensure_ascii=False,indent=2),encoding='utf-8')
                value={'file':target.name,'bytes':size,'native_state':'completed','record_status':record['status']}
            finally:
                dl_session.send('Browser.setDownloadBehavior',{'behavior':'default','eventsEnabled':False})
                dl_session.detach()
        elif args.action == 'status':
            record = json.loads(RECORD.read_text(encoding='utf-8'))
            job_id = record['jobs'][0]['job_id']
            value = page.evaluate('''id => {
                const links=[...document.querySelectorAll('a[href*="/jobs/'+id+'"]')];
                let section=links[0];
                for(let n=0;n<6 && section && !section.textContent.includes('Rerun');n++) section=section.parentElement;
                return {job_id:id,links:links.map(a=>({href:a.getAttribute('href'),images:[...a.querySelectorAll('img')].map(i=>({loaded:i.complete,width:i.naturalWidth,height:i.naturalHeight}))})),
                text:section?.innerText?.slice(0,2400) || document.body.innerText.slice(0,2200)};
            }''', job_id)
            record['last_status'] = value
            record['status_checked_at'] = datetime.now(timezone.utc).isoformat()
            if len(value['links'])==4 and all(any(i['loaded'] and i['width']>0 for i in a['images']) for a in value['links']) and 'Cancel' not in value['text']:
                record['status'] = 'completed'
            RECORD.write_text(json.dumps(record,ensure_ascii=False,indent=2),encoding='utf-8')
            value['record_status']=record['status']
        elif args.action == 'detail':
            record = json.loads(RECORD.read_text(encoding='utf-8'))
            if record['status'] != 'completed':
                raise RuntimeError('Generation has not been verified complete')
            job_id=record['jobs'][0]['job_id']
            page.locator('a[href="/jobs/'+job_id+'?index=0"]').click()
            page.wait_for_url('**/jobs/'+job_id+'?index=0',timeout=12000)
            value={'url':page.url,'text':page.locator('body').inner_text()[-4200:],
                   'buttons':page.locator('button').evaluate_all('''els=>els.filter(e=>e.getBoundingClientRect().width>0).slice(-65).map(e=>({text:e.innerText.slice(0,70),title:e.title,aria:e.getAttribute('aria-label')}))''')}
        elif args.action == 'fill':
            record = json.loads(RECORD.read_text(encoding='utf-8'))
            field = page.get_by_placeholder('What will you imagine?', exact=True)
            if field.input_value():
                raise RuntimeError('Composer contains an existing draft; preserving it')
            field.fill(record['prompt'])
            value = {'filled': field.input_value() == record['prompt'], 'characters': len(record['prompt']),
                     'composer': field.evaluate('(e)=>e.parentElement.parentElement.textContent.slice(0,1600)')}
        elif args.action == 'submit':
            record = json.loads(RECORD.read_text(encoding='utf-8'))
            if record['status'] != 'prepared':
                raise RuntimeError('Record prevents duplicate or unknown-status submission')
            field = page.get_by_placeholder('What will you imagine?', exact=True)
            if field.input_value() != record['prompt']:
                raise RuntimeError('Exact prompt verification failed')
            receipt = {'responses': [], 'accepted': []}
            def on_response(response):
                # JSON job metadata only. No image/media/download responses are read.
                if response.request.method != 'POST' or '/api/' not in response.url:
                    return
                from urllib.parse import urlsplit
                path = urlsplit(response.url).path
                receipt['responses'].append({'path': path, 'status': response.status})
                if 'submit' not in path or 'json' not in response.headers.get('content-type', ''):
                    return
                try:
                    data = response.json()
                    def collect(node):
                        if isinstance(node, dict):
                            if node.get('job_id'):
                                receipt['accepted'].append({k: node.get(k) for k in
                                    ['job_id','prompt','is_queued','event_type','job_type','flags','meta']})
                            for v in node.values():
                                if isinstance(v, (list,dict)): collect(v)
                        elif isinstance(node,list):
                            for v in node: collect(v)
                    collect(data)
                except Exception as exc:
                    receipt['parse_error'] = str(exc)[:200]
            page.on('response', on_response)
            record['status'] = 'submission_pending_verification'
            record['submission_attempted_at'] = datetime.now(timezone.utc).isoformat()
            RECORD.write_text(json.dumps(record,ensure_ascii=False,indent=2), encoding='utf-8')
            field.press('Enter')
            deadline = time.monotonic()+30
            while time.monotonic()<deadline and not receipt['accepted']:
                page.wait_for_timeout(500)
            record['submit_receipt'] = receipt
            record['jobs'] = receipt['accepted']
            if receipt['accepted']: record['status'] = 'accepted'
            record['post_submit_text'] = page.locator('body').inner_text()[:2100]
            RECORD.write_text(json.dumps(record,ensure_ascii=False,indent=2), encoding='utf-8')
            value = {'status': record['status'], 'receipt':receipt, 'body':record['post_submit_text']}
        elif args.action == 'inspect':
            value = page.evaluate('''() => ({url:location.href,title:document.title,
              text:document.body.innerText.slice(0,3600),
              inputs:[...document.querySelectorAll('textarea,input,[contenteditable=true]')].map(e=>({tag:e.tagName,placeholder:e.getAttribute('placeholder'),value:e.value?.slice(0,250),role:e.getAttribute('role')})),
              buttons:[...document.querySelectorAll('button')].slice(0,45).map(e=>({text:e.innerText.slice(0,60),aria:e.getAttribute('aria-label'),title:e.getAttribute('title'),disabled:e.disabled})),
              links:[...document.querySelectorAll('a[href*="/jobs/"]')].slice(0,8).map(e=>e.getAttribute('href'))})''')
        else:
            value = page.evaluate(Path(args.file).read_text(encoding='utf-8-sig'))
        print(json.dumps(value, ensure_ascii=False))
        session.detach()

if __name__ == '__main__':
    main()
