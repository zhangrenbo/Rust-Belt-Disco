"""Measure audio in a dedicated target using direct CDP; reuse work Chrome."""
import json
import os
import time
import urllib.request
from pathlib import Path
from urllib.parse import quote
import websocket

ROOT = Path(__file__).resolve().parents[1]
os.environ['NO_PROXY'] = 'localhost,127.0.0.1'

INIT = r'''(() => {
          const values = new Map(), seed = new URLSearchParams(location.search).get('settings');
          if (seed) values.set('northeast-disco.settings.v1', seed);
          Object.defineProperty(window, 'localStorage', {value: {
            getItem: key => values.get(String(key)) ?? null,
            setItem: (key,value) => values.set(String(key),String(value)),
            removeItem: key => values.delete(String(key)),
            clear: () => values.clear(), key: i => [...values.keys()][i] ?? null,
            get length() { return values.size; }
          }});
          const NativeContext = window.AudioContext;
          window.AudioContext = class extends NativeContext {
            constructor(...args) {
              super(...args);
              const ctx = this, analyser = this.createAnalyser(); analyser.fftSize = 2048;
              const qa = window.__qaAudio = {context: this, analyser, sources: [], gains: []};
              const gain = this.createGain.bind(this), source = this.createBufferSource.bind(this);
              this.createGain = () => {
                const node = gain(); qa.gains.push(node);
                if (qa.gains.length === 1) {
                  qa.master = node;
                  const connect = node.connect.bind(node);
                  node.connect = (destination,...rest) => {
                    if (destination === ctx.destination) {
                      connect(analyser); return analyser.connect(destination,...rest);
                    }
                    return connect(destination,...rest);
                  };
                }
                return node;
              };
              this.createBufferSource = () => { const node = source(); qa.sources.push(node); return node; };
              qa.rms = () => {
                const data = new Float32Array(analyser.fftSize); analyser.getFloatTimeDomainData(data);
                return Math.sqrt(data.reduce((sum,value) => sum + value*value, 0) / data.length);
              };
            }
          };
        })();'''


class CDP:
    def __init__(self):
        opener = urllib.request.build_opener(urllib.request.ProxyHandler({}))
        with opener.open('http://127.0.0.1:9223/json/version', timeout=5) as response:
            self.version = json.load(response)
        assert self.version['Browser'].startswith('Chrome/')
        self.ws = websocket.create_connection(self.version['webSocketDebuggerUrl'], timeout=10, suppress_origin=True, http_proxy_host=None)
        self.counter = 0
        self.session = None
        self.errors = []
    def send(self, method, params=None, browser=False):
        self.counter += 1
        request = {'id': self.counter, 'method': method, 'params': params or {}}
        if self.session and not browser:
            request['sessionId'] = self.session
        self.ws.send(json.dumps(request))
        while True:
            result = json.loads(self.ws.recv())
            if result.get('method') == 'Runtime.exceptionThrown' and result.get('sessionId') == self.session:
                self.errors.append(result['params']['exceptionDetails'].get('text'))
            if result.get('id') == request['id']:
                if 'error' in result:
                    raise RuntimeError(result['error'])
                return result.get('result', {})
    def evaluate(self, expression):
        result = self.send('Runtime.evaluate', {'expression': expression, 'returnByValue': True, 'awaitPromise': True})
        if 'exceptionDetails' in result:
            raise RuntimeError(result['exceptionDetails'])
        return result.get('result', {}).get('value')
    def wait(self, expression, timeout=12):
        end = time.monotonic() + timeout
        while time.monotonic() < end:
            value = self.evaluate(expression)
            if value:
                return value
            time.sleep(.08)
        raise TimeoutError(expression)
    def key(self, key, code):
        for event in ['rawKeyDown', 'keyUp']:
            self.send('Input.dispatchKeyEvent', {'type': event, 'key': key, 'code': key, 'windowsVirtualKeyCode': code})
    def click(self, selector):
        xy = self.wait("""(() => {
          const element = document.querySelector(%s);
          if (!element || element.disabled) return null;
          const box = element.getBoundingClientRect(), x = box.x + box.width/2, y = box.y + box.height/2;
          return box.width > 0 && element.contains(document.elementFromPoint(x,y)) ? {x,y} : null;
        })()""" % json.dumps(selector))
        self.send('Input.dispatchMouseEvent', {'type': 'mouseMoved', **xy})
        self.send('Input.dispatchMouseEvent', {'type': 'mousePressed', 'button': 'left', 'clickCount': 1, **xy})
        self.send('Input.dispatchMouseEvent', {'type': 'mouseReleased', 'button': 'left', 'clickCount': 1, **xy})
    def navigate(self, url):
        self.send('Page.navigate', {'url': url})
        self.wait('location.href === ' + json.dumps(url) + ' && !!window.__qaAudio && !!document.querySelector("#app")?.dataset.musicState')

cdp = CDP()
target = cdp.send('Target.createTarget', {'url': 'about:blank', 'background': True}, browser=True)['targetId']
try:
    cdp.session = cdp.send('Target.attachToTarget', {'targetId': target, 'flatten': True}, browser=True)['sessionId']
    cdp.send('Page.enable'); cdp.send('Runtime.enable')
    cdp.send('Emulation.setDeviceMetricsOverride', {'width': 896, 'height': 504, 'deviceScaleFactor': 1, 'mobile': False})
    cdp.send('Emulation.setFocusEmulationEnabled', {'enabled': True})
    cdp.send('Emulation.setEmulatedMedia', {'features': [{'name': 'prefers-reduced-motion', 'value': 'reduce'}]})
    try:
        cdp.send('Page.setWebLifecycleState', {'state': 'active'})
    except RuntimeError as error:
        print('Lifecycle activation:', str(error)[:120])
    cdp.send('Page.addScriptToEvaluateOnNewDocument', {'source': INIT})
    cdp.navigate(f'http://127.0.0.1:5173/?music-qa={time.time_ns()}')
    assert cdp.evaluate('!navigator.userActivation.hasBeenActive'), 'Autoplay must be checked before any input'
    try:
        cdp.wait('window.__qaAudio?.rms() > .001 && document.querySelector("#app").dataset.musicState === "playing"', timeout=5)
        autoplay = {'audibleBeforeInput': True}
    except TimeoutError:
        autoplay = cdp.evaluate('({audibleBeforeInput:false, context:window.__qaAudio.context.state, state:document.querySelector("#app").dataset.musicState})')
        assert autoplay['context'] == 'suspended', autoplay
    cdp.key('Enter', 13)
    cdp.wait('window.__qaAudio?.rms() > .001 && document.querySelector("#app").dataset.musicState === "playing"')
    measurements = []
    def capture(label):
        value = cdp.evaluate("""(() => {
          const q = window.__qaAudio, app = document.querySelector('#app');
          return {state: app.dataset.musicState, track: app.dataset.musicTrack,
            context: q.context.state, masterGain: q.master.gain.value, rms: q.rms(),
            sourcesCreated: q.sources.length, durations: q.sources.map(s => s.buffer.duration),
            allLooping: q.sources.every(s => s.loop)};
        })()""")
        measurements.append({'step': label, **value})
        print(json.dumps({'step': label, 'track': value['track'], 'gain': value['masterGain'], 'rms': value['rms']}), flush=True)
        return value
    assert capture('main-menu-audible')['track'] == 'menu'
    assert cdp.evaluate('!document.querySelector("[data-view=mainmenu] .event-close") && document.querySelectorAll("[data-view=mainmenu] [data-exit-game]").length === 1')
    cdp.click('.mainmenu-actions [data-open-view="settings"]')
    cdp.evaluate('document.querySelector("[data-audio-channel=music]").focus()')
    cdp.key('Home', 36)
    cdp.click('[data-save-settings]')
    cdp.wait('document.querySelector("#app").dataset.musicState === "muted" && window.__qaAudio.rms() < 1e-7')
    assert capture('settings-zero-is-silent')['masterGain'] == 0
    saved_mute = cdp.evaluate('localStorage.getItem("northeast-disco.settings.v1")')
    assert json.loads(saved_mute)['music'] == 0
    cdp.evaluate('(() => { const input = document.querySelector("[data-audio-channel=music]"); input.value = 60; input.dispatchEvent(new Event("input", {bubbles:true})); })()')
    cdp.click('[data-save-settings]')
    cdp.wait('window.__qaAudio.rms() > .001')
    capture('settings-volume-restored')
    cdp.click('[data-settings-return]')
    cdp.click('.mainmenu-choice-start')
    cdp.click('[data-character-choice="大厦崩塌"]')
    cdp.wait('document.querySelector("#app").dataset.musicTrack === "station" && window.__qaAudio.sources.length === 2 && window.__qaAudio.rms() > .001')
    cdp.click('[data-hotspot="grandmother"]')
    cdp.wait('document.querySelector("#station-talk").open', timeout=20)
    cdp.wait('Math.abs(window.__qaAudio.master.gain.value - .39) < .001')
    capture('station-dialogue-ducked')
    cdp.click('#station-talk-close')
    cdp.wait('Math.abs(window.__qaAudio.master.gain.value - .6) < .001')
    cdp.click('#station-save-button')
    assert capture('archive-keeps-station')['sourcesCreated'] == 2
    cdp.click('#station-archive-return')
    cdp.click('#station-pause-button')
    cdp.click('#station-pause-settings')
    assert capture('settings-keeps-station')['sourcesCreated'] == 2
    cdp.click('[data-settings-return]')
    cdp.click('#station-pause-button')
    cdp.click('#station-pause-menu')
    cdp.wait('document.querySelector("#app").dataset.musicTrack === "menu" && window.__qaAudio.sources.length === 3')
    capture('menu-restored')
    # Exercise visibility handling without hiding or navigating real user tabs.
    cdp.evaluate('Object.defineProperty(document,"hidden",{configurable:true,get:()=>true}); document.dispatchEvent(new Event("visibilitychange"));')
    cdp.wait('window.__qaAudio.context.state === "suspended"')
    capture('hidden-suspended')
    cdp.evaluate('Object.defineProperty(document,"hidden",{configurable:true,get:()=>false}); document.dispatchEvent(new Event("visibilitychange"));')
    cdp.wait('window.__qaAudio.context.state === "running" && window.__qaAudio.rms() > .001')
    assert capture('visible-resumes-same-loop')['sourcesCreated'] == 3
    cdp.navigate(f'http://127.0.0.1:5173/?music-qa={time.time_ns()}&settings={quote(saved_mute)}')
    if not autoplay['audibleBeforeInput']:
        cdp.key('Enter', 13)
    cdp.wait('document.querySelector("#app").dataset.musicState === "muted" && window.__qaAudio.sources.length === 1')
    assert capture('saved-mute-restored-on-fresh-page')['masterGain'] == 0
    assert not cdp.errors, cdp.errors
    report = {'transport': 'fixed work Chrome direct CDP 9223; task target only', 'browser': cdp.version['Browser'],
              'storage': 'isolated per-page memory; persisted mute seeded from previous applied settings',
              'verification': 'live Web Audio output measured after master gain; does not verify physical speakers',
              'visibilityTest': 'synthetic visibility event in task page; real tabs untouched',
              'checkedAt': time.strftime('%Y-%m-%dT%H:%M:%SZ', time.gmtime()),
              'autoplay': autoplay, 'mainMenuCornerCloseRemoved': True,
              'pageErrors': cdp.errors, 'measurements': measurements}
    (ROOT/'docs/menu-autoplay-verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps({'checks':len(measurements),'autoplay':autoplay,'pageErrors':cdp.errors}))
finally:
    cdp.send('Target.closeTarget', {'targetId': target}, browser=True)
    cdp.ws.close()
