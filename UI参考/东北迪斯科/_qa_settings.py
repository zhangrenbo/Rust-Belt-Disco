"""Local settings QA through native CDP; never attaches to or restarts user browsers."""
import base64
import json
import os
import subprocess
import tempfile
import time
import urllib.request
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from threading import Thread

import websocket

ROOT = Path(__file__).resolve().parent
os.environ['NO_PROXY'] = 'localhost,127.0.0.1'


class QuietHandler(SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=str(ROOT), **kwargs)

    def log_message(self, *_):
        pass


class CDP:
    def __init__(self, url):
        self.ws = websocket.create_connection(url, timeout=12, suppress_origin=True)
        self.sequence = 0
        self.errors = []

    def call(self, method, **params):
        self.sequence += 1
        self.ws.send(json.dumps({'id': self.sequence, 'method': method, 'params': params}))
        deadline = time.monotonic() + 15
        while time.monotonic() < deadline:
            result = json.loads(self.ws.recv())
            if result.get('method') == 'Runtime.exceptionThrown':
                self.errors.append(result['params']['exceptionDetails'])
            if result.get('id') == self.sequence:
                if 'error' in result:
                    raise RuntimeError(f'{method}: {result["error"]}')
                return result.get('result', {})
        raise TimeoutError(method)

    def evaluate(self, expression):
        result = self.call('Runtime.evaluate', expression=expression, returnByValue=True)
        if 'exceptionDetails' in result:
            raise RuntimeError(result['exceptionDetails'])
        return result.get('result', {}).get('value')

    def wait(self, expression, timeout=12):
        deadline = time.monotonic() + timeout
        while time.monotonic() < deadline:
            if self.evaluate(expression):
                return
            time.sleep(.08)
        raise AssertionError('Readiness timeout: ' + expression)

    def click(self, selector, count=1):
        self.wait(f'!!document.querySelector({json.dumps(selector)})')
        point = self.evaluate('''(() => {
          const e = document.querySelector(%s), r = e.getBoundingClientRect();
          const x = r.x + r.width / 2, y = r.y + r.height / 2;
          if (!r.width || !r.height || !e.contains(document.elementFromPoint(x, y))) return null;
          return {x, y};
        })()''' % json.dumps(selector))
        assert point, 'Control is not hit-testable: ' + selector
        self.call('Input.dispatchMouseEvent', type='mouseMoved', **point)
        self.call('Input.dispatchMouseEvent', type='mousePressed', button='left', clickCount=count, **point)
        self.call('Input.dispatchMouseEvent', type='mouseReleased', button='left', clickCount=count, **point)

    def key(self, key, code, number):
        self.call('Input.dispatchKeyEvent', type='keyDown', key=key, code=code, windowsVirtualKeyCode=number)
        self.call('Input.dispatchKeyEvent', type='keyUp', key=key, code=code, windowsVirtualKeyCode=number)

    def viewport(self, width, height):
        self.call('Emulation.setDeviceMetricsOverride', width=width, height=height, deviceScaleFactor=1, mobile=False)

    def screenshot(self, name):
        # UI verification only; not an image-download transport.
        result = self.call('Page.captureScreenshot', format='png')
        (ROOT / name).write_bytes(base64.b64decode(result['data']))


def main():
    chrome = Path(r'C:\Program Files\Google\Chrome\Application\chrome.exe')
    assert chrome.exists(), 'Google Chrome executable unavailable'
    server = ThreadingHTTPServer(('127.0.0.1', 0), QuietHandler)
    Thread(target=server.serve_forever, daemon=True).start()
    profile = Path(tempfile.mkdtemp(prefix='northeast-settings-cdp-'))
    startup = subprocess.STARTUPINFO()
    startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
    startup.wShowWindow = 0
    process = subprocess.Popen([
        str(chrome), '--headless=new', '--remote-debugging-address=127.0.0.1',
        '--remote-debugging-port=0', f'--user-data-dir={profile}', '--no-first-run',
        '--no-default-browser-check', '--disable-background-networking',
        '--disable-background-timer-throttling', '--disable-renderer-backgrounding',
        '--force-device-scale-factor=1', '--mute-audio', 'about:blank'
    ], startupinfo=startup, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    client = None
    browser_client = None
    try:
        portfile = profile / 'DevToolsActivePort'
        deadline = time.monotonic() + 15
        while not portfile.exists() and time.monotonic() < deadline:
            assert process.poll() is None, 'QA Chrome exited before CDP became ready'
            time.sleep(.1)
        assert portfile.exists(), 'QA Chrome CDP unavailable after 15 seconds'
        port = portfile.read_text().splitlines()[0]
        endpoint = f'http://127.0.0.1:{port}'
        page_id = None

        def connect_page():
            nonlocal page_id
            deadline = time.monotonic() + 12
            target = None
            while time.monotonic() < deadline:
                targets = json.load(urllib.request.urlopen(endpoint + '/json/list', timeout=3))
                target = next((t for t in targets if t['type'] == 'page' and (
                    t['id'] == page_id or t['url'] == 'about:blank' or t['url'].startswith(url))), None)
                if target:
                    break
                time.sleep(.1)
            assert target, 'QA target unavailable: ' + str([(t['type'], t['url']) for t in targets])
            page_id = target['id']
            c = CDP(target['webSocketDebuggerUrl'])
            c.call('Page.enable')
            c.call('Runtime.enable')
            try:
                c.call('Page.setWebLifecycleState', state='active')
            except RuntimeError as error:
                print('Lifecycle activation unsupported:', str(error)[:180])
            return c

        url = f'http://127.0.0.1:{server.server_port}/index.html'
        version = json.load(urllib.request.urlopen(endpoint + '/json/version', timeout=3))
        browser_client = CDP(version['webSocketDebuggerUrl'])
        client = connect_page()
        client.viewport(1440, 810)
        active = lambda name: client.wait(f'document.querySelector(".view.active")?.dataset.view === {json.dumps(name)}')
        main_settings = '.view-mainmenu [data-open-view="settings"]'
        for attempt in range(3):
            client.call('Page.addScriptToEvaluateOnNewDocument', source='''
              window.__historyWrites = 0;
              for (const method of ['pushState', 'replaceState']) {
                const original = history[method].bind(history);
                history[method] = (...args) => { window.__historyWrites++; return original(...args); };
              }
            ''')
            client.call('Page.navigate', url=url)
            client.wait('window.__historyWrites === 0 && typeof setActiveView === \"function\"')
            client.ws.close()
            client = connect_page()  # Reacquire after navigation / renderer replacement.
            client.wait('typeof setActiveView === "function" && !!document.querySelector("[data-save-settings]")')
            active('mainmenu')
            client.click(main_settings)
            active('settings')
            client.evaluate('''window.__settingsLeft = false;
              new MutationObserver(() => {
                if (!document.querySelector('.view-settings').classList.contains('active')) window.__settingsLeft = true;
              }).observe(document.querySelector('.view-settings'), {attributes: true, attributeFilter: ['class']});''')
            time.sleep(1.8)
            assert client.evaluate('!window.__settingsLeft && window.__historyWrites === 0'), client.evaluate('({left: window.__settingsLeft, writes: window.__historyWrites, url: location.href})')
            assert client.evaluate('location.href') == url
            assert client.evaluate('getComputedStyle(document.querySelector(".page-shutter")).opacity') == '0'
            client.click('[data-settings-return]')
            active('mainmenu')
        print('PASS: first click stays open on 3 fresh loads, no URL changes or shutter')
        client.click(main_settings)
        active('settings')
        client.click('[data-quality="performance"]')
        client.click('[data-game-toggle="replay"]')
        slider = 'input[data-audio-channel="music"]'
        client.click(slider)
        client.key('Home', 'Home', 36)
        client.key('ArrowRight', 'ArrowRight', 39)
        assert client.evaluate(f'document.querySelector({json.dumps(slider)}).value') == '1'
        client.click('[data-save-settings]')
        client.wait('!document.querySelector("[data-save-settings]").disabled')
        active('settings')
        client.call('Page.reload')
        client.ws.close()
        client = connect_page()
        client.wait('typeof paintSettings === "function" && !!document.querySelector("[data-save-settings]")')
        client.click(main_settings)
        active('settings')
        assert client.evaluate(f'document.querySelector({json.dumps(slider)}).value') == '1'
        assert client.evaluate('document.querySelector("[data-quality=performance]").getAttribute("aria-pressed")') == 'true'
        assert client.evaluate('document.querySelector("[data-game-toggle=replay]").getAttribute("aria-checked")') == 'true'
        client.click('[data-reset-settings]')
        assert client.evaluate(f'document.querySelector({json.dumps(slider)}).value') == '54'
        client.click('[data-save-settings]')
        client.key('Escape', 'Escape', 27)
        active('mainmenu')
        client.click('.view-mainmenu [data-open-view="entry"]')
        active('entry')
        client.click('.view-entry [data-open-view="settings"]')
        active('settings')
        client.click('[data-settings-return]')
        active('entry')
        client.click('.view-entry [data-open-view="settings"]')
        active('settings')
        print('PASS: sliders, toggles, saved values, reset, Escape and return to source screen')
        client.wait('document.querySelector(".settings-dossier-art")?.complete && document.querySelector(".settings-dossier-art").naturalWidth > 0')
        assert client.evaluate('document.querySelectorAll(".settings-dossier-art").length') == 1
        client.wait('[...document.querySelectorAll(".settings-board-actions img")].length === 2 && [...document.querySelectorAll(".settings-board-actions img")].every(i => i.complete && i.naturalWidth > 0)')
        assert client.evaluate('document.querySelectorAll(".view-settings .folio-nav, .view-settings .archive-board, .view-settings .ticket-film").length') == 0
        assert client.evaluate('getComputedStyle(document.querySelector(".settings-sheet")).clipPath') == 'none'
        assert client.evaluate('getComputedStyle(document.querySelector(".settings-sheet")).backgroundImage') == 'none'
        client.wait('getComputedStyle(document.querySelector("#toast")).opacity === "0"', timeout=3)
        for width, height in [(1440, 810), (1920, 1080), (844, 390)]:
            client.viewport(width, height)
            client.wait(f'innerWidth === {width} && innerHeight === {height}')
            assert client.evaluate('''[...document.querySelectorAll('.view-settings button, .view-settings input')].every(e => {
              const r=e.getBoundingClientRect(); return r.width > 0 && r.height > 0 && r.left >= 0 && r.top >= 0 && r.right <= innerWidth && r.bottom <= innerHeight;
            })'''), f'Control overflow at {width}x{height}'
            assert client.evaluate('document.documentElement.scrollWidth <= innerWidth && document.documentElement.scrollHeight <= innerHeight')
            client.screenshot(f'_qa_settings_dossier_{width}.png')
        assert not client.errors, client.errors
        print('PASS: complete PNG, no CSS cropping, control bounds and screenshots at 1440/1920/844')
    finally:
        if client:
            client.ws.close()
        if browser_client:
            # Only close the isolated Chrome instance created above, never user's Edge/Chrome.
            try:
                browser_client.call('Browser.close')
            except (websocket.WebSocketException, OSError):
                pass
            browser_client.ws.close()
        server.shutdown()
        print('QA profile:', profile)


if __name__ == '__main__':
    main()
