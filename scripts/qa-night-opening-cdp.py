"""A000 night opening in the existing work Chrome; only task pages, memory saves."""
import json
import os
import time
from pathlib import Path
from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[1]
os.environ['NO_PROXY'] = 'localhost,127.0.0.1'

with sync_playwright() as playwright:
    browser = playwright.chromium.connect_over_cdp('http://127.0.0.1:9223', timeout=10000)
    context = browser.contexts[0]
    browser_session = browser.new_browser_cdp_session()
    with context.expect_page(timeout=10000) as created:
        target = browser_session.send('Target.createTarget', {'url': 'about:blank', 'background': True})['targetId']
    page = created.value
    session = context.new_cdp_session(page)
    assert session.send('Target.getTargetInfo')['targetInfo']['targetId'] == target
    session.send('Emulation.setFocusEmulationEnabled', {'enabled': True})
    try:
        session.send('Page.setWebLifecycleState', {'state': 'active'})
    except Exception as error:
        print('Lifecycle activation:', str(error)[:160])
    errors, checks, screens = [], [], []
    page.on('pageerror', lambda error: errors.append(str(error)))
    page.add_init_script('''(() => {
      const values = new Map();
      Object.defineProperty(window, 'localStorage', {configurable: true, value: {
        getItem: k => values.get(String(k)) ?? null,
        setItem: (k, v) => values.set(String(k), String(v)),
        removeItem: k => values.delete(String(k)), clear: () => values.clear(),
        key: i => [...values.keys()][i] ?? null, get length() {return values.size;}
      }});
    })();''')

    def check(name, result):
        assert result, name
        checks.append(name)

    def node(name):
        page.wait_for_function('name => document.querySelector("[data-view=station]").dataset.storyNode === name', arg=name, timeout=15000)

    def capture(name, width, height):
        page.set_viewport_size({'width': width, 'height': height})
        page.wait_for_function('''() => !document.querySelector('#app').classList.contains('is-view-switching')
          && document.querySelector('.page-shutter').getAnimations().every(a => a.playState === 'finished')''', timeout=5000)
        page.wait_for_function('''() => [...document.querySelectorAll('#station-world img')]
          .every(i => i.complete && i.naturalWidth > 0)''', timeout=10000)
        metrics = page.evaluate('''() => {
          const box = el => {const {x,y,width,height} = el.getBoundingClientRect(); return {x,y,width,height};};
          const button = document.querySelector('[data-hotspot=grandmother]');
          const marker = document.querySelector('#station-grandmother-marker');
          const m = box(marker), b = box(button), stage = box(document.querySelector('#app'));
          return {stage, button: b, marker: m,
            markerReachable: button.contains(document.elementFromPoint(m.x+m.width/2,m.y+m.height/2)),
            bodyReachable: button.contains(document.elementFromPoint(b.x+b.width/2,b.y+b.height*.72)),
            visibleHotspots: [...document.querySelectorAll('.station-hotspot')].filter(b => !b.hidden).map(b => b.dataset.hotspot),
            vision: {x: document.querySelector('#station-player-vision').getAttribute('cx'), y: document.querySelector('#station-player-vision').getAttribute('cy')},
            talkOpen: document.querySelector('#station-talk').open};
        }''')
        path = ROOT / f'docs/night-{name}-{width}.png'
        page.screenshot(path=str(path), timeout=10000)
        screens.append({'path': str(path), 'viewport': [width, height], **metrics})
        return metrics

    try:
        page.set_viewport_size({'width': 1440, 'height': 810})
        page.goto(f'http://127.0.0.1:5173/?night-qa={time.time_ns()}#character-select', wait_until='domcontentloaded')
        page.locator('[data-character-choice="大厦崩塌"]').click(timeout=10000)
        node('arrival')
        for width, height in [(1440, 810), (844, 475), (1280, 800), (390, 844)]:
            metrics = capture('opening', width, height)
            check(f'opening is unobstructed at {width}', not metrics['talkOpen'])
            check(f'marker and seated body accept input at {width}', metrics['markerReachable'] and metrics['bodyReachable'])
            check(f'only the grandmother is labelled at {width}', metrics['visibleHotspots'] == ['grandmother'])
        page.set_viewport_size({'width': 1440, 'height': 810})
        start_x = float(page.locator('#station-player-vision').get_attribute('cx'))
        # Hit the visible exclamation itself, not a programmatic DOM click.
        marker = page.locator('#station-grandmother-marker').bounding_box()
        page.mouse.click(marker['x']+marker['width']/2, marker['y']+marker['height']/2)
        page.wait_for_function('document.querySelector("#station-player-vision").getAttribute("cx") > 500', timeout=15000)
        check('the local visibility moves with the player', float(page.locator('#station-player-vision').get_attribute('cx')) > start_x)
        page.wait_for_function('document.querySelector("#station-talk").open', timeout=20000)
        check('clicking the marker walks to the woman before opening dialogue', page.locator('[data-view=station]').get_attribute('data-story-node') == 'arrival')
        capture('arrival-dialogue', 1440, 810)
        page.keyboard.press('Escape')
        check('closing the opening retains its exclamation', page.locator('#station-grandmother-marker').is_visible())
        body = page.locator('[data-hotspot=grandmother]').bounding_box()
        page.mouse.click(body['x']+body['width']/2, body['y']+body['height']*.72)
        page.wait_for_function('document.querySelector("#station-talk").open', timeout=10000)
        page.keyboard.press('1')
        node('greeting')
        page.keyboard.press('Escape')
        check('starting her conversation consumes the exclamation', page.locator('#station-grandmother-marker').get_attribute('hidden') is not None)
        capture('beside-shelter', 1440, 810)
        page.locator('#station-talk-resume').click()
        for _ in range(3):
            page.keyboard.press('1')
        node('background')
        capture('questions', 844, 475)
        for _ in range(3):
            page.keyboard.press('1')
        node('warning')
        page.keyboard.press('2')
        page.keyboard.press('1')
        node('shadow-intro')
        page.keyboard.press('1')
        page.keyboard.press('4')
        node('ready')
        check('the original questions, warning and shadow still lead to the exit', page.locator('[data-hotspot=exit]').is_visible())
        capture('ready', 1440, 810)
        # Save/reload within the isolated page only.
        page.locator('#station-save-button').click()
        page.locator('[data-view=entry] [data-slot="01"]').click()
        page.locator('.station-slot-actions button').nth(1).click()
        page.locator('#station-archive-return').click()
        page.locator('#station-pause-button').click()
        page.locator('#station-pause-menu').click()
        page.locator('.mainmenu-choice-continue').click()
        node('ready')
        check('reload preserves night progress without resurrecting the exclamation', not page.locator('#station-grandmother-marker').is_visible())
        page.locator('#station-pause-button').click()
        page.locator('#station-pause-menu').click()
        page.locator('.mainmenu-choice-start').click()
        page.locator('[data-character-choice="大厦崩塌"]').click()
        node('arrival')
        check('new game restores the quiet opening and marker', not page.locator('#station-talk').evaluate('(d) => d.open') and page.locator('#station-grandmother-marker').is_visible())
        check('new game preserves manual saves', page.evaluate('JSON.parse(localStorage.getItem("northeast-disco:campaign:v1:01")).node') == 'ready')
        check('no runtime errors', not errors)
        (ROOT / 'docs/night-opening-verification.json').write_text(json.dumps({
            'date': '2026-09-13', 'browser': browser.version, 'transport': 'fixed work Chrome, direct CDP 9223',
            'saveIsolation': 'task-created page, in-memory localStorage', 'checks': checks, 'errors': errors, 'screens': screens,
            'humanArtApproval': 'pending', 'performance': 'target-device benchmarking not performed'
        }, ensure_ascii=False, indent=2), encoding='utf-8')
        print(json.dumps({'checks': len(checks), 'screens': len(screens), 'errors': errors}))
    finally:
        page.close()
