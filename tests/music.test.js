import test from 'node:test';
import assert from 'node:assert/strict';
import {Window} from 'happy-dom';
import {createMusicPlayer, installMusic, musicTracks} from '../src/core/music.js';

function fixture({defer = false, failStation = false, resumeBlocked = false} = {}) {
  const requests = [], pending = new Map(), states = [], errors = [];
  const resumes = [];
  let context;
  class Param {
    value = 1;
    cancelScheduledValues() {}
    setValueAtTime(value) { this.value = value; }
    linearRampToValueAtTime(value) { this.value = value; }
  }
  class Node {
    gain = new Param(); disconnected = false; started = false; stops = [];
    connect() {} disconnect() { this.disconnected = true; }
    start(time, offset = 0) { this.started = true; this.startOffset = offset; } stop(time) { this.stops.push(time); }
  }
  class Context {
    currentTime = 0; state = 'suspended'; destination = {}; sources = []; gains = [];
    constructor() { context = this; }
    createGain() { const node = new Node(); this.gains.push(node); return node; }
    createBufferSource() { const node = new Node(); this.sources.push(node); return node; }
    async resume() {
      if (resumeBlocked) await new Promise(resolve => resumes.push(resolve));
      this.state = 'running';
    }
    async suspend() { this.state = 'suspended'; }
    async close() { this.state = 'closed'; }
    async decodeAudioData(bytes) { return bytes; }
  }
  const fetch = url => {
    requests.push(url);
    const response = {ok: !(failStation && url.includes('station')), status: 404, arrayBuffer: async () => ({url, duration: url.includes('station') ? 55 : 240})};
    if (!defer) return Promise.resolve(response);
    return new Promise(resolve => pending.set(url, () => resolve(response)));
  };
  const player = createMusicPlayer({AudioContext: Context, fetch, onStatus: s => states.push(s), onError: e => errors.push(e)});
  return {player, requests, pending, states, errors, context: () => context, AudioContext: Context, fetch,
    allowPlayback() { resumeBlocked = false; resumes.splice(0).forEach(resolve => resolve()); }};
}

test('music starts on request, survives utility pages, crossfades and reuses decoded loops', async () => {
  const f = fixture(), {player} = f;
  await player.setView('entry');assert.equal(f.requests.length, 0);
  await player.unlock();
  const context = f.context(), menu = context.sources[0];
  assert.equal(menu.loop, true);assert.equal(menu.started, true);
  await player.setView('settings');assert.equal(context.sources.length, 1);
  await player.setView('station');assert.equal(context.sources.length, 2);
  assert.equal(menu.stops[0], 1.25, 'outgoing music stops after its fade');
  await player.setView('entry');assert.equal(context.sources.length, 2, 'save page keeps scene music');
  await player.setView('mainmenu');
  await player.setView('station');
  assert.equal(f.requests.length, 2, 'returning to tracks reuses decoded audio');
  assert.equal(context.sources.filter(source => !source.disconnected).length, 2, 'rapid switches retain at most one outgoing loop');
  await player.destroy();assert.equal(context.state, 'closed');
});

test('installing the game starts the theme without input and respects saved mute', async () => {
  for (const volume of [.54, 0]) {
    const f = fixture(), window = new Window();
    window.document.body.innerHTML = '<main id="app"></main><dialog id="station-talk"></dialog>';
    Object.defineProperty(window.document, 'hidden', {get: () => false});
    window.AudioContext = f.AudioContext; window.fetch = f.fetch;
    const player = installMusic({window, document: window.document, volume,
      getActiveView: () => 'mainmenu', showToast: assert.fail});
    try {
      await new Promise(resolve => setImmediate(resolve));
      assert.deepEqual(f.requests, [musicTracks.menu.url]);
      assert.equal(f.context().sources.length, 1);
      assert.equal(f.context().sources[0].started, true);
      assert.equal(f.context().gains[0].gain.value, volume);
      assert.equal(window.document.getElementById('app').dataset.musicState, volume ? 'playing' : 'muted');
    } finally { await player.destroy(); await window.happyDOM.close(); }
  }
});

test('a pending autoplay attempt recovers on trusted input without stacking music', async () => {
  const f = fixture({resumeBlocked: true}), window = new Window();
  window.document.body.innerHTML = '<main id="app"></main><dialog id="station-talk"></dialog>';
  Object.defineProperty(window.document, 'hidden', {get: () => false});
  window.AudioContext = f.AudioContext; window.fetch = f.fetch;
  const player = installMusic({window, document: window.document, volume: .54,
    getActiveView: () => 'mainmenu', showToast: assert.fail});
  try {
    assert.equal(f.context().sources.length, 0);
    assert.equal(window.document.getElementById('app').dataset.musicState, 'suspended');
    f.allowPlayback();
    const gesture = new window.Event('pointerdown');
    Object.defineProperty(gesture, 'isTrusted', {value: true});
    window.document.dispatchEvent(gesture);
    await new Promise(resolve => setImmediate(resolve));
    assert.equal(f.context().sources.length, 1);
    assert.equal(window.document.getElementById('app').dataset.musicState, 'playing');
  } finally { await player.destroy(); await window.happyDOM.close(); }
});

test('muting cancels fades, dialogue lowers volume, and hidden pages suspend without restarting the loop', async () => {
  const f = fixture(), {player} = f;
  await player.unlock();const context = f.context(), master = context.gains[0];
  player.setVolume(.8);player.setDucked(true);assert.equal(master.gain.value, .52);
  player.setVolume(0);assert.equal(master.gain.value, 0);
  player.setDucked(false);assert.equal(master.gain.value, 0, 'closing dialogue must preserve mute');
  await player.setVisible(false);assert.equal(context.state, 'suspended');
  await player.setVisible(true);assert.equal(context.sources.length, 1, 'resume preserves the current loop');
  assert.equal(f.states.at(-1).state, 'muted');
  player.setVolume(2);assert.equal(master.gain.value, 1);
  await player.destroy();
});

test('library previews restore the entering scene and preserve mute through navigation', async () => {
  const f = fixture(), {player} = f;
  await player.unlock(); await player.setView('station'); await player.setView('settings');
  await player.setPreview('menu');
  assert.equal(f.context().sources.at(-1).buffer.url, musicTracks.menu.url);
  player.setVolume(0);
  await player.setPreview(null);
  assert.equal(f.context().sources.at(-1).buffer.url, musicTracks.station.url);
  assert.equal(f.context().gains[0].gain.value, 0);
  await player.setPreview('menu'); await player.setView('entry');
  assert.equal(f.context().sources.at(-1).buffer.url, musicTracks.station.url, 'leaving settings ends the preview');
  assert.equal(f.states.at(-1).state, 'muted');
  const sources = f.context().sources.length;
  await player.setPreview('unavailable');
  assert.equal(f.context().sources.length, sources);
  assert.equal(f.requests.length, 2, 'previews reuse the two decoded tracks');
  await player.destroy();
});

test('late downloads cannot play the wrong scene, and a failed replacement preserves playing music', async () => {
  const f = fixture({defer: true}), {player} = f;
  const menu = player.unlock();await Promise.resolve();
  const station = player.setView('station');
  f.pending.get(musicTracks.station.url)();await station;
  f.pending.get(musicTracks.menu.url)();await menu;
  assert.equal(f.context().sources.length, 1);
  assert.equal(f.context().sources[0].buffer.url, musicTracks.station.url);
  await player.destroy();
  const failing = fixture({failStation: true});await failing.player.unlock();
  await failing.player.setView('station');
  assert.equal(failing.errors.length, 1);
  assert.equal(failing.context().sources[0].stops.length, 0, 'a loading failure must not cut the previous track');
  await failing.player.destroy();
});

test('record controls pause the real source, seek while paused, resume from position and restore scene looping', async () => {
  const f = fixture(), {player} = f;
  await player.unlock(); await player.setView('settings'); await player.setPreview('menu');
  const context = f.context(); context.currentTime = 17;
  assert.equal(player.getPlayback().position, 17);
  await player.setPreviewPaused(true);
  assert.equal(f.states.at(-1).state, 'paused');
  assert.ok(context.sources[0].disconnected);
  context.currentTime = 25;
  assert.equal(player.getPlayback().position, 17, 'paused time stays put');
  await player.seekPreview(93);
  assert.equal(player.getPlayback().position, 93);
  assert.equal(context.sources.length, 1, 'seeking paused audio does not resume it');
  await player.setPreviewPaused(false);
  assert.equal(context.sources.at(-1).startOffset, 93);
  context.currentTime = 28;
  assert.equal(player.getPlayback().position, 96);
  await player.seekPreview(220);
  assert.equal(context.sources.at(-1).startOffset, 220);
  player.setPreviewLoop(false);
  const last = context.sources.at(-1);
  assert.equal(last.loop, false);
  last.onended();
  assert.equal(player.getPlayback().state, 'ended');
  assert.equal(player.getPlayback().position, 240);
  await player.setPreviewPaused(false);
  assert.equal(context.sources.at(-1).startOffset, 0, 'play after end starts at the beginning');
  assert.equal(context.sources.at(-1).loop, false);
  player.setVolume(0);
  await player.setPreviewPaused(true); await player.setView('entry');
  assert.equal(context.sources.at(-1).loop, true, 'background music loops even when audition loop is off');
  assert.equal(player.getPlayback().preview, null);
  assert.equal(player.getPlayback().state, 'muted');
  assert.equal(f.requests.length, 1);
  await player.destroy();
});

test('pausing during a pending record download invalidates it and prevents stale audio from starting', async () => {
  const f = fixture({defer: true}), {player} = f;
  const unlock = player.unlock(); await Promise.resolve();
  f.pending.get(musicTracks.menu.url)(); await unlock;
  await player.setView('settings');
  const loading = player.setPreview('station');
  await player.setPreviewPaused(true);
  f.pending.get(musicTracks.station.url)(); await loading;
  assert.equal(f.context().sources.length, 1);
  assert.ok(f.context().sources[0].disconnected, 'no outgoing scene audio continues through pause');
  assert.equal(player.getPlayback().state, 'paused');
  await player.setPreviewPaused(false);
  assert.equal(f.context().sources.at(-1).buffer.url, musicTracks.station.url);
  await player.destroy();
});
