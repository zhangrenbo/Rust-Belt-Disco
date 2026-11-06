"""Check menu artwork and save-dependent visibility in the fixed work Chrome."""
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
    try:
        page.set_default_timeout(10000)
        session = context.new_cdp_session(page)
        assert session.send('Target.getTargetInfo')['targetInfo']['targetId'] == target
        session.send('Emulation.setFocusEmulationEnabled', {'enabled': True})
        try:
            session.send('Page.setWebLifecycleState', {'state': 'active'})
        except Exception as error:
            print('Lifecycle activation:', str(error)[:160])
        errors = []
        page.on('pageerror', lambda error: errors.append(str(error)))
        page.add_init_script('''(() => {
          const values = new Map();
          Object.defineProperty(window, 'localStorage', {value: {
            getItem: key => values.get(String(key)) ?? null,
            setItem: (key,value) => values.set(String(key),String(value)),
            removeItem: key => values.delete(String(key)),
            clear: () => values.clear(), key: i => [...values.keys()][i] ?? null,
            get length() { return values.size; }
          }});
        })();''')
        page.set_viewport_size({'width': 1440, 'height': 810})
        page.goto(f'http://127.0.0.1:5173/?menu-qa={time.time_ns()}', wait_until='domcontentloaded')
        page.keyboard.press('Enter')
        button = page.locator('.mainmenu-choice-continue')
        assert button.is_hidden(), 'Continue must be hidden with no save'
        results = []

        def capture(name, width, height):
            page.set_viewport_size({'width': width, 'height': height})
            page.mouse.move(width - 5, height - 5)
            page.wait_for_function('''() => [...document.querySelectorAll('.event-center-menu img')]
              .every(image => image.complete && image.naturalWidth > 0)''')
            measurements = page.evaluate('''() => {
              const rect = element => {
                const {x,y,width,height} = element.getBoundingClientRect();
                return {x,y,width,height};
              };
              return [...document.querySelectorAll('.mainmenu-choice:not([hidden])')].map(button => {
                const box = rect(button), image = button.querySelector('img');
                return {label: image?.alt, button: box, image: rect(image),
                  reachable: button.contains(document.elementFromPoint(box.x + box.width/2, box.y + box.height/2))};
              });
            }''')
            assert all(item['reachable'] for item in measurements), measurements
            for previous, current in zip(measurements, measurements[1:]):
                assert previous['button']['y'] + previous['button']['height'] <= current['button']['y'] + 1
            path = ROOT / f'docs/menu-{name}-{width}.png'
            page.screenshot(path=str(path), timeout=10000)
            results.append({'name': name, 'viewport': [width, height], 'items': measurements})

        capture('no-save', 1440, 810)
        page.locator('.mainmenu-choice-start').click()
        page.locator('[data-character-choice="大厦崩塌"]').click()
        assert not page.locator('#station-talk').evaluate('(dialog) => dialog.open')
        page.locator('#station-pause-button').click()
        page.locator('#station-pause-menu').click()
        page.keyboard.press('Enter')
        assert button.is_visible(), 'Continue must appear after a valid save'
        capture('with-save', 1440, 810)
        capture('with-save', 1920, 1080)
        capture('with-save', 844, 475)
        button.click()
        page.wait_for_function('document.querySelector("[data-view=station].active") && !document.querySelector("#station-talk").open')
        assert page.locator('[data-view=station]').get_attribute('data-story-node') == 'arrival'
        assert not errors, errors
        report = {'transport': 'fixed work Chrome CDP 9223', 'browser': browser.version,
                  'storage': 'isolated per-page in-memory storage',
                  'noSaveHidden': True, 'savedVisible': True, 'continueRestoresSave': True,
                  'errors': errors, 'screens': results}
        (ROOT / 'docs/menu-continue-verification.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
        print(json.dumps({'screens': len(results), 'errors': errors, 'continueRestoresSave': True}))
    finally:
        page.close()
