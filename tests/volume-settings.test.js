import test from 'node:test';
import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import {Window} from 'happy-dom';

test('volume autosaves across settings exits without saving other drafts or losing later changes', async () => {
  const window = new Window({url: 'http://localhost:5173/', settings: {
    disableJavaScriptFileLoading: true, disableCSSFileLoading: true, enableJavaScriptEvaluation: false
  }});
  const originals = new Map();
  for (const [name, value] of Object.entries({window, document: window.document, localStorage: window.localStorage,
    CustomEvent: window.CustomEvent, WheelEvent: window.WheelEvent, requestAnimationFrame: () => 1, cancelAnimationFrame() {}})) {
    originals.set(name, Object.getOwnPropertyDescriptor(globalThis, name));
    Object.defineProperty(globalThis, name, {value, configurable: true, writable: true});
  }
  const gains = [];
  window.AudioContext = class {
    state = 'running'; currentTime = 0; destination = {};
    createGain() {
      const gain = {value: 0, cancelScheduledValues() {}, setValueAtTime(v) {this.value = v;}, linearRampToValueAtTime(v) {this.value = v;}};
      gains.push(gain); return {gain, connect() {}, disconnect() {}};
    }
    createBufferSource() {return {connect() {}, disconnect() {}, start() {}, stop() {}};}
    async decodeAudioData() {return {duration: 240};}
    async resume() {this.state = 'running';}
    async suspend() {this.state = 'suspended';}
  };
  window.fetch = async () => ({ok: true, arrayBuffer: async () => new ArrayBuffer(0)});
  window.matchMedia = () => ({matches: true, addEventListener() {}, removeEventListener() {}});
  Object.defineProperty(window.document, 'hidden', {get: () => false});
  const q = selector => window.document.querySelector(selector);
  const click = selector => q(selector).click();
  const flush = () => new Promise(resolve => setTimeout(resolve, 0));
  const volume = (value, channel = 'music') => {
    const input = q(`input[data-audio-channel=${channel}]`); input.value = value;
    input.dispatchEvent(new window.Event('input', {bubbles: true}));
  };
  const setting = () => JSON.parse(window.localStorage.getItem('northeast-disco.settings.v1'));
  try {
    window.localStorage.setItem('unrelated-volume-test', 'preserved');
    window.document.write(await readFile(new URL('../index.html', import.meta.url), 'utf8'));
    await import(`../scene.js?volume-test=${Date.now()}`); await flush();
    q('#ui-only-notice button')?.click();
    click('.mainmenu-actions [data-open-view=settings]');
    assert.equal(gains[0].value, .54);
    volume(25); assert.equal(gains[0].value, .25, 'dragging adjusts actual music gain before Apply');
    click('[data-settings-return]');
    assert.equal(gains[0].value, .25, 'returning keeps the adjusted sound without Apply');
    click('.mainmenu-actions [data-open-view=settings]');
    assert.equal(q('input[data-audio-channel=music]').value, '25', 'reopening keeps the adjusted slider');
    assert.equal(setting().music, 25, 'volume is persisted without Apply');
    click('button[data-quality=performance]');
    click('[data-mode="自动窗口"]');
    click('[data-game-toggle=name]');
    const strength = q('input[data-film-strength]'); strength.value = 22;
    strength.dispatchEvent(new window.Event('input', {bubbles: true}));
    volume(13, 'voice'); volume(31, 'sound');
    assert.deepEqual([setting().voice, setting().music, setting().sound], [13, 25, 31]);
    assert.deepEqual([setting().mode, setting().quality, setting().name, setting().filmStrength],
      ['手动窗口', 'enhanced', false, 65], 'saving audio never commits other setting drafts');
    click('[data-music-library-open]'); await flush();
    click('[data-record-next]'); await flush();
    assert.equal(gains[0].value, .25, 'record-library track changes keep the same music volume');
    click('[data-record-play]'); click('[data-record-play]'); await flush();
    assert.equal(gains[0].value, .25, 'resume keeps the music volume');
    click('[data-music-library-close]');
    volume(0); assert.equal(gains[0].value, 0);
    click('[data-music-library-open]'); click('[data-record-next]'); await flush();
    assert.equal(gains[0].value, 0, 'record-library playback cannot undo mute');
    click('[data-music-library-close]');
    click('[data-save-settings]'); await flush(); assert.equal(setting().music, 0);
    volume(70); click('[data-settings-return]');
    assert.equal(gains[0].value, .7, 'return keeps the most recent adjustment after an earlier Apply');
    assert.equal(q('[data-audio-channel=music]').value, '70');
    click('.mainmenu-actions [data-open-view=settings]');
    volume(0);
    window.document.dispatchEvent(new window.KeyboardEvent('keydown', {key: 'Escape', bubbles: true}));
    assert.equal(gains[0].value, 0, 'Escape also retains mute');
    assert.equal(setting().music, 0);
    click('.mainmenu-actions [data-open-view=settings]');
    click('[data-reset-settings]'); assert.equal(gains[0].value, .54, 'defaults are audible immediately');
    assert.equal(setting().music, 0, 'defaults wait for Apply to save');
    volume(30);
    assert.deepEqual([setting().voice, setting().music, setting().sound], [13, 30, 31],
      'editing one channel after reset does not save the other previewed defaults');
    click('[data-save-settings]'); await flush();
    volume(90); click('[data-mode="全屏模式"]');
    window.document.documentElement.requestFullscreen = async () => {throw new Error('test denial');};
    click('[data-save-settings]'); await flush();
    assert.equal(setting().music, 90); assert.equal(gains[0].value, .9, 'a failed window-mode Apply retains independently saved audio');
    assert.equal(q('[data-audio-channel=music]').value, '90');
    let allowFullscreen;
    window.document.documentElement.requestFullscreen = () => new Promise(resolve => { allowFullscreen = resolve; });
    volume(40); click('[data-mode="全屏模式"]'); click('[data-save-settings]');
    volume(60); allowFullscreen(); await flush();
    assert.equal(setting().music, 60, 'a pending Apply cannot overwrite a later volume adjustment');
    assert.equal(gains[0].value, .6);
    assert.equal(window.localStorage.getItem('unrelated-volume-test'), 'preserved');
  } finally {
    await window.happyDOM.abort(); window.close();
    for (const [name, descriptor] of originals) {
      if (descriptor) Object.defineProperty(globalThis, name, descriptor); else delete globalThis[name];
    }
  }
});
