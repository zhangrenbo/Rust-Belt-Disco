"""Visual check in the fixed work Chrome via CDP; saves stay in page memory."""
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
    with context.expect_page(timeout=10000) as created_page:
        target = browser_session.send('Target.createTarget', {'url': 'about:blank', 'background': True})['targetId']
    page = created_page.value
    session = page.context.new_cdp_session(page)
    assert session.send('Target.getTargetInfo')['targetInfo']['targetId'] == target
    session.send('Emulation.setFocusEmulationEnabled', {'enabled': True})
    try:
        session.send('Page.setWebLifecycleState', {'state': 'active'})
    except Exception as error:
        print('Lifecycle activation:', str(error)[:160])
    errors = []
    page.on('pageerror', lambda error: errors.append(str(error)))
    page.add_init_script('''
      (() => {
      const values = new Map();
      Object.defineProperty(window, 'localStorage', { configurable: true, value: {
        getItem: key => values.get(String(key)) ?? null,
        setItem: (key, value) => values.set(String(key), String(value)),
        removeItem: key => values.delete(String(key)), clear: () => values.clear(),
        key: index => [...values.keys()][index] ?? null,
        get length() { return values.size; }
      }});
      })();
    ''')
    page.set_viewport_size({'width': 1440, 'height': 810})
    page.goto(f'http://127.0.0.1:5173/?dialogue-qa={time.time_ns()}#character-select', wait_until='domcontentloaded')
    page.locator('[data-character-choice="大厦崩塌"]').click(timeout=10000)

    def node(name):
        page.wait_for_function('(name) => document.querySelector("[data-view=station]").dataset.storyNode === name', arg=name, timeout=20000)

    def choose(index=1):
        page.locator(f'#station-choices [data-choice-index="{index - 1}"]').click(timeout=5000)

    results = []

    def capture(name, width, height):
        page.set_viewport_size({'width': width, 'height': height})
        page.wait_for_function('''() => [...document.querySelectorAll('#station-talk img, .station-world img')]
          .every(image => image.complete && image.naturalWidth > 0)''', timeout=10000)
        metrics = page.evaluate('''() => {
          const box = element => {
            const {x,y,width,height} = element.getBoundingClientRect();
            return {x,y,width,height};
          };
          const talk = document.querySelector('#station-talk');
          const copy = talk.querySelector('.station-dialogue-copy');
          return {
            stage: box(document.querySelector('#app')), dialog: box(talk),
            panel: box(talk.querySelector('.disco-dialog-panel')),
            portrait: box(talk.querySelector('.disco-dialog-portrait')),
            text: box(copy), textHeight: copy.scrollHeight, visibleTextHeight: copy.clientHeight,
            options: [...talk.querySelectorAll('.disco-choice button')].map(button => ({
              ...box(button), reachable: button.contains(document.elementFromPoint(
                button.getBoundingClientRect().x + button.clientWidth / 2,
                button.getBoundingClientRect().y + button.clientHeight / 2))
            })),
            close: box(document.querySelector('#station-talk-close'))
          };
        }''')
        assert all(option['reachable'] for option in metrics['options']), metrics
        for key in ['x', 'y', 'width', 'height']:
            assert abs(metrics['stage'][key] - metrics['dialog'][key]) < 1, metrics
        path = ROOT / f'docs/dialogue-{name}-{width}.png'
        page.screenshot(path=str(path), timeout=10000)
        results.append({'name': name, 'viewport': [width, height], **metrics})

    node('arrival')
    page.locator('[data-hotspot="grandmother"]').click()
    page.wait_for_function('document.querySelector("#station-talk").open', timeout=20000)
    capture('arrival', 1440, 810)
    choose()
    page.locator('#station-talk-close').click()
    page.locator('#station-pause-button').click()
    page.locator('#station-pause-menu').click()
    # Load a valid dialogue fixture; background rendering is not a movement benchmark.
    page.evaluate('''async () => {
      const {newGame, transition, SAVE_KEY, validateSave} = await import('/src/station/state.js');
      let state = transition(newGame(), {type: 'next', index: 0});
      state = transition(state, {type: 'talk'});
      for (let i = 0; i < 3; i++) state = transition(state, {type: 'next', index: 0});
      validateSave(state);
      localStorage.setItem(`${SAVE_KEY}:auto`, JSON.stringify(state));
    }''')
    page.keyboard.press('Enter')
    page.locator('.mainmenu-choice-continue').click(timeout=5000)
    node('background')
    capture('questions', 1440, 810)
    capture('questions', 1920, 1080)
    capture('questions', 844, 475)
    capture('questions-window', 1280, 800)
    page.keyboard.press('Escape')
    assert not page.locator('#station-talk').evaluate('(dialog) => dialog.open')
    page.locator('#station-talk-resume').click()
    page.keyboard.press('3')
    node('value')
    capture('reply', 1440, 810)
    page.keyboard.press('2')
    page.keyboard.press('1')
    node('warning')
    capture('warning', 1440, 810)
    capture('warning', 844, 475)
    choose(2)
    choose()
    choose()
    node('shadow')
    capture('shadow', 1440, 810)
    page.keyboard.press('4')
    node('ready')
    assert not page.locator('#station-talk').evaluate('(dialog) => dialog.open')
    assert not errors, errors
    (ROOT / 'docs/dialogue-ui-verification.json').write_text(json.dumps({
        'browser': browser.version, 'transport': 'Chrome CDP 9223',
        'saveIsolation': 'per-page in-memory localStorage', 'errors': errors,
        'scenario': 'valid background save fixture, followed by real dialogue choices; no movement performance measurement',
        'keyboardAndResume': 'passed', 'screens': results,
    }, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps({'screens': len(results), 'errors': errors, 'story': 'ready'}))
    page.close()
