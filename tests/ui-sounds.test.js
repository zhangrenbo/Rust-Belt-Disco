import test from 'node:test';
import assert from 'node:assert/strict';
import {createUiSoundPlayer} from '../src/core/ui-sounds.js';

function fixture(fetchOverride) {
  const contexts = [], sources = [], requests = [], errors = [];
  let clock = 1000;
  class AudioContext {
    state = 'suspended'; currentTime = 0; destination = {}; gains = [];
    constructor() { contexts.push(this); }
    createGain() {
      const node = {gain: {value: 0, cancelScheduledValues() {}, setValueAtTime(value) { this.value = value; }}, connect() {}, disconnect() {}};
      this.gains.push(node); return node;
    }
    createBufferSource() {
      const source = {connect() {}, disconnect() {}, started: false, stopped: false,
        start() { this.started = true; }, stop() { this.stopped = true; this.onended?.(); }};
      sources.push(source); return source;
    }
    async decodeAudioData(data) { return {duration: .43, id: new TextDecoder().decode(data)}; }
    async resume() { this.state = 'running'; }
    async suspend() { this.state = 'suspended'; }
    async close() { this.state = 'closed'; }
  }
  const response = url => ({ok: true, arrayBuffer: async () => new TextEncoder().encode(url).buffer});
  const player = createUiSoundPlayer({AudioContext, now: () => clock, volume: .84, onError: error => errors.push(error),
    fetch: async url => { requests.push(url); return fetchOverride ? fetchOverride(url, response) : response(url); }});
  return {player, contexts, sources, requests, errors, tick: (ms = 100) => { clock += ms; }};
}

test('paper audio waits for unlock, varies turns, bounds overlap and immediately obeys sound mute', async () => {
  const f = fixture(), {player} = f;
  await player.prepare();
  assert.equal(f.requests.length, 4); assert.equal(f.sources.length, 0);
  assert.equal(player.play('turn'), false, 'preloading never authorizes playback');
  await player.unlock();
  assert.equal(player.play('turn'), true);
  assert.equal(player.play('press'), false, 'same click cannot stack multiple effects');
  f.tick(); player.play('turn');
  assert.notEqual(f.sources[0].buffer.id, f.sources[1].buffer.id, 'successive turns use different recorded takes');
  f.tick(); player.play('slip');
  assert.equal(f.sources.filter(source => !source.stopped).length, 2, 'rapid navigation has bounded voices');
  player.setVolume(0);
  assert.equal(f.contexts[0].gains[0].gain.value, 0);
  assert.ok(f.sources.every(source => source.stopped));
  f.tick(); assert.equal(player.play('press'), false);
  player.setVolume(.23);
  assert.equal(f.contexts[0].gains[0].gain.value, .23);
  assert.equal(f.sources.length, 3, 'unmuting never replays a muted click');
  assert.equal(player.play('press'), true);
  await player.unlock(); assert.equal(f.requests.length, 4, 'decoded recordings are reused');
  await player.destroy();
});

test('slow and failed sound downloads never play stale clicks and recover on a later gesture', async () => {
  let release, fail = true;
  const gate = new Promise(resolve => { release = resolve; });
  const f = fixture(async (url, response) => {
    await gate;
    if (fail && url.endsWith('paper-slip.wav')) throw new Error('offline');
    return response(url);
  });
  const prepared = f.player.prepare(), unlocked = f.player.unlock();
  assert.equal(f.player.play('turn'), false);
  release(); await Promise.all([prepared, unlocked]);
  assert.equal(f.sources.length, 0, 'finishing a decode cannot produce a delayed sound');
  assert.equal(f.errors.length, 1);
  assert.equal(f.player.play('slip'), false);
  fail = false; await f.player.unlock();
  assert.equal(f.requests.length, 5, 'only the missing recording is retried');
  assert.equal(f.player.play('slip'), true);
  await f.player.destroy();
});

test('hiding the page stops paper tails and returning cannot replay them; unsupported audio is harmless', async () => {
  const f = fixture(); await f.player.unlock(); f.player.play('turn');
  await f.player.setVisible(false); f.tick();
  assert.equal(f.contexts[0].state, 'suspended'); assert.ok(f.sources[0].stopped);
  assert.equal(f.player.play('turn'), false);
  await f.player.setVisible(true);
  assert.equal(f.sources.length, 1); assert.equal(f.player.play('slip'), true);
  await f.player.destroy(); f.tick(); assert.equal(f.player.play('press'), false);
  assert.equal(f.contexts[0].state, 'closed');
  const unavailable = createUiSoundPlayer({fetch: () => { throw new Error('should not fetch'); }});
  await unavailable.prepare(); await unavailable.unlock(); assert.equal(unavailable.play('turn'), false);
  await unavailable.destroy();
});
