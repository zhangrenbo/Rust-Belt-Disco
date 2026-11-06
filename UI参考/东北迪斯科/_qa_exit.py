"""Exit-dialog verification on the persistent work Chrome, using direct CDP."""
import json
import subprocess
import time
import urllib.request
from http.server import ThreadingHTTPServer
from threading import Thread

from _qa_settings import CDP, QuietHandler


def main():
    status = subprocess.run([
        'powershell', '-NoProfile', '-File',
        r'D:\CodexData\home\browser\start_chrome_cdp.ps1', '-CheckOnly'
    ], capture_output=True, text=True, encoding='utf-8', check=True)
    verified = json.loads(status.stdout.lstrip('\ufeff'))
    assert verified['status'] == 'reused', 'Start the configured work Chrome before QA'
    endpoint = verified['endpoint']
    opener = urllib.request.build_opener(urllib.request.ProxyHandler({}))

    def discover(path):
        return json.load(opener.open(endpoint + path, timeout=5))

    version = discover('/json/version')
    assert version['Browser'].startswith('Chrome/')
    browser = CDP(version['webSocketDebuggerUrl'])
    server = ThreadingHTTPServer(('127.0.0.1', 0), QuietHandler)
    Thread(target=server.serve_forever, daemon=True).start()
    page_id = None
    client = None
    try:
        page_id = browser.call('Target.createTarget', url='about:blank', background=True)['targetId']

        def attach():
            deadline = time.monotonic() + 12
            while time.monotonic() < deadline:
                target = next((t for t in discover('/json/list') if t['id'] == page_id), None)
                if target:
                    c = CDP(target['webSocketDebuggerUrl'])
                    c.call('Runtime.enable')
                    c.call('Page.enable')
                    try:
                        c.call('Page.setWebLifecycleState', state='active')
                    except RuntimeError as error:
                        print('Lifecycle activation unsupported:', error)
                    c.call('Emulation.setFocusEmulationEnabled', enabled=True)
                    return c
                time.sleep(.1)
            raise TimeoutError('Task-created Chrome target unavailable')

        client = attach()
        url = f'http://127.0.0.1:{server.server_port}/index.html'
        client.call('Page.navigate', url=url)
        client.ws.close()
        client = attach()
        client.viewport(1440, 810)
        client.wait('typeof setActiveView === "function" && document.querySelector(".mainmenu-choice-exit img")?.naturalWidth > 0')
        client.wait('[...document.querySelectorAll("#exit-dialog img")].every(i => i.complete && i.naturalWidth > 0)')
        client.evaluate('''window.__exitQaTrace=[]; for(const type of ['pointerdown','pointerup','click']) document.addEventListener(type, e => {
          if(window.__exitQaTrace.length<12) window.__exitQaTrace.push({type, target:e.target.className, trusted:e.isTrusted, detail:e.detail, primary:e.isPrimary});
        },true);''')
        def open_exit():
            client.click('.mainmenu-choice-exit')
            try:
                client.wait('document.querySelector("#exit-dialog").open', timeout=3)
            except AssertionError:
                print('Open diagnostic:', client.evaluate('({trace:window.__exitQaTrace, active:document.querySelector(".view.active")?.dataset.view, ready:document.readyState, visibility:document.visibilityState})'))
                print('JS errors:', client.errors)
                client.screenshot('_qa_exit_diagnostic.png')
                raise
        is_open = 'document.querySelector("#exit-dialog").open'
        is_closed = '!document.querySelector("#exit-dialog").open'

        def point(x, y):
            client.call('Input.dispatchMouseEvent', type='mouseMoved', x=x, y=y)
            client.call('Input.dispatchMouseEvent', type='mousePressed', button='left', clickCount=1, x=x, y=y)
            client.call('Input.dispatchMouseEvent', type='mouseReleased', button='left', clickCount=1, x=x, y=y)

        open_exit()
        client.wait(is_open)
        assert client.evaluate('document.querySelector("#exit-dialog").matches(":modal")')
        assert client.evaluate('document.activeElement.matches(".exit-actions [data-close-exit]")')
        time.sleep(.85)
        rect = client.evaluate('document.querySelector("#exit-dialog").getBoundingClientRect().toJSON()')
        point(rect['x'] + rect['width'] * .12, rect['y'] + rect['height'] * .8)
        assert client.evaluate(is_open), 'Blank paper must not act as the backdrop'
        client.click('.exit-actions [data-close-exit]')
        client.wait(is_closed)
        open_exit()
        client.wait(is_open)
        client.key('Escape', 'Escape', 27)
        client.wait(is_closed)
        open_exit()
        client.wait(is_open)
        client.click('.exit-close')
        client.wait(is_closed)
        open_exit()
        client.wait(is_open)
        time.sleep(.75)
        point(5, 5)
        client.wait(is_closed)
        open_exit()
        client.wait(is_open)
        client.click('[data-confirm-exit]')
        client.wait(is_closed)
        assert client.evaluate('document.querySelector(".view.active").dataset.view') == 'mainmenu'
        assert client.evaluate('location.href') == url
        print('PASS: open, initial cancel focus, inside-paper click, cancel, Escape, close tab, backdrop and confirm')

        for width, height in [(1440, 810), (1920, 1080), (844, 390)]:
            client.viewport(width, height)
            client.wait(f'innerWidth === {width} && innerHeight === {height}')
            open_exit()
            client.wait(is_open)
            assert client.evaluate('''[...document.querySelectorAll('#exit-dialog button')].every(e => {
              const r=e.getBoundingClientRect(); return r.left>=0 && r.top>=0 && r.right<=innerWidth && r.bottom<=innerHeight && r.width>0;
            })''')
            assert client.evaluate('getComputedStyle(document.querySelector("#exit-dialog")).backgroundImage') == 'none'
            assert client.evaluate('getComputedStyle(document.querySelector("#exit-dialog"), "::before").content') == 'none'
            time.sleep(.75)
            rect = client.evaluate('document.querySelector("#exit-dialog").getBoundingClientRect().toJSON()')
            point(rect['x'] + rect['width'] * .12, rect['y'] + rect['height'] * .8)
            client.screenshot(f'_qa_exit_art_{width}.png')
            client.click('.exit-actions [data-close-exit]')
            client.wait(is_closed)
        assert not client.errors, client.errors
        print('PASS: generated art loaded, no CSS panel drawing, 1440/1920/844 bounds and screenshots, no JS exceptions')
    finally:
        if client:
            client.ws.close()
        if page_id:
            browser.call('Target.closeTarget', targetId=page_id)
        browser.ws.close()
        server.shutdown()
        print('Closed only the task-created page; work Chrome remains running')


if __name__ == '__main__':
    main()
