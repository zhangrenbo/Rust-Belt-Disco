"""Verify the user-identified PNG save page through the fixed work Chrome CDP."""
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
        page.goto(f'http://127.0.0.1:5173/?save-entry-qa={time.time_ns()}', wait_until='domcontentloaded')
        page.keyboard.press('Enter')
        page.locator('.mainmenu-actions [data-open-view="entry"]').click()
        entry = page.locator('[data-view="entry"]')
        page.wait_for_function('document.querySelector("[data-view=entry].active")')
        assert page.locator('[data-view="archive"]').is_hidden()
        results = []

        def capture(name, width, height):
            page.set_viewport_size({'width': width, 'height': height})
            page.mouse.move(width - 5, height - 5)
            page.wait_for_function('''() => [...document.querySelectorAll('[data-view=entry] img')]
              .every(image => image.complete && image.naturalWidth > 0)''')
            page.evaluate(r'''async () => {
              const urls = [...document.querySelectorAll('[data-view=entry], [data-view=entry] *')]
                .flatMap(element => [...getComputedStyle(element).backgroundImage.matchAll(/url\("?([^"()]+)"?\)/g)].map(match => match[1]));
              await Promise.all([...new Set(urls)].map(url => new Promise((resolve,reject) => {
                const image = new Image(); image.onload = resolve; image.onerror = reject; image.src = url;
              })));
            }''')
            slots = entry.locator('[data-slot]').evaluate_all('''buttons => buttons.map(button => {
              const {x,y,width,height} = button.getBoundingClientRect();
              return {slot: button.dataset.slot, status: button.dataset.saveStatus,
                title: button.querySelector('[data-save-title]').textContent,
                background: getComputedStyle(button).backgroundImage,
                bounds: {x,y,width,height},
                reachable: button.contains(document.elementFromPoint(x + width/2,y + height/2))};
            })''')
            assert len(slots) == 6 and all(slot['reachable'] for slot in slots), slots
            assert all('evidence-ticket-v2.png' in slot['background'] for slot in slots[:4])
            assert all(slot['bounds']['x'] >= 0 and slot['bounds']['y'] >= 0
                       and slot['bounds']['x'] + slot['bounds']['width'] <= width
                       and slot['bounds']['y'] + slot['bounds']['height'] <= height for slot in slots)
            page.screenshot(path=str(ROOT / f'docs/save-entry-{name}-{width}.png'), timeout=10000)
            results.append({'state': name, 'viewport': [width, height], 'slots': slots})

        capture('empty', 1440, 810)
        capture('empty', 896, 504)
        entry.locator('[data-slot="06"]').click()
        assert page.locator('.station-slot-actions button').nth(0).is_disabled()
        assert page.locator('.station-slot-actions button').nth(1).is_disabled()
        page.locator('.station-slot-actions button').nth(2).click()
        entry.locator('[data-open-view="mainmenu"]').click()
        page.locator('.mainmenu-choice-start').click()
        page.locator('[data-character-choice="大厦崩塌"]').click()
        assert not page.locator('#station-talk').evaluate('(dialog) => dialog.open')
        page.locator('#station-save-button').click()
        assert 'active' in entry.get_attribute('class').split()
        for slot in ['01', '02', '03', '04', '05', '06']:
            entry.locator(f'[data-slot="{slot}"]').click()
            page.locator('.station-slot-actions button').nth(1).click()
            assert entry.locator(f'[data-slot="{slot}"]').get_attribute('data-save-status') == 'saved'
        assert entry.locator('#entry-save-summary').inner_text() == '已使用 6 / 6 个手动存档槽位'
        capture('saved', 896, 504)
        capture('saved', 1440, 810)
        page.locator('#station-archive-return').click()
        page.locator('[data-hotspot="grandmother"]').click()
        page.wait_for_function('document.querySelector("#station-talk").open', timeout=20000)
        page.locator('#station-choices button').click()
        assert page.locator('[data-view="station"]').get_attribute('data-story-node') == 'greeting'
        for slot in ['01', '02', '03', '04', '05', '06']:
            if page.locator('#station-talk').is_visible():
                page.locator('#station-talk-close').click()
            page.locator('#station-save-button').click()
            entry.locator(f'[data-slot="{slot}"]').click()
            page.locator('.station-slot-actions button').nth(0).click()
            assert page.locator('[data-view="station"]').get_attribute('data-story-node') == 'arrival'
        page.goto(f'http://127.0.0.1:5173/?save-entry-qa={time.time_ns()}#archive', wait_until='domcontentloaded')
        page.wait_for_function('document.querySelector("[data-view=entry].active")')
        assert page.locator('[data-view="archive"]').is_hidden(), 'Legacy archive route must use the PNG page'
        assert not errors, errors
        report = {'transport': 'fixed work Chrome CDP 9223', 'browser': browser.version,
                  'storage': 'isolated per-page in-memory storage; real saves untouched',
                  'manualSlotsSavedAndLoaded': ['01', '02', '03', '04', '05', '06'],
                  'mainMenuAndStationUseEntry': True, 'legacyArchiveLinkUsesEntry': True,
                  'emptyReadAndNoJourneySaveDisabled': True, 'errors': errors, 'screens': results}
        (ROOT / 'docs/save-entry-verification.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
        print(json.dumps({'screens': len(results), 'slotsSavedAndLoaded': 6, 'errors': errors}))
    finally:
        page.close()
