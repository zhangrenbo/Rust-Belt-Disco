// Paper recordings: Kenney RPG Audio (CC0). Provenance: docs/paper-sfx-source.json.
export const uiSoundFiles = {
  turnA: 'media/sfx/paper-turn-a.wav', turnB: 'media/sfx/paper-turn-b.wav',
  slip: 'media/sfx/paper-slip.wav', press: 'media/sfx/paper-press.wav'
};
const cues = {
  turn: {takes: ['turnB', 'turnA'], gain: .82},
  slip: {takes: ['slip'], gain: .72},
  press: {takes: ['press'], gain: .50}
};
const clamp = value => Number.isFinite(value) ? Math.max(0, Math.min(1, value)) : 0;

export function emitUiSound(document, cue) {
  const window = document.defaultView;
  window.dispatchEvent(new window.CustomEvent('disco:ui-sound', {detail: {cue}}));
}

export function createUiSoundPlayer({AudioContext, fetch, volume = .84, now = () => performance.now(), onError = () => {}}) {
  let context, master, loading, armed = false, visible = true, destroyed = false;
  let level = clamp(volume), lastPlay = -Infinity;
  const buffers = new Map(), voices = new Set(), counters = new Map();
  function stopAll() {
    for (const source of voices) { try { source.stop(); } catch { /* Already ended. */ } }
    voices.clear();
  }
  async function prepare() {
    if (destroyed || !AudioContext) return;
    if (buffers.size === Object.keys(uiSoundFiles).length) return;
    if (loading) return loading;
    try {
      if (!context) {
        context = new AudioContext();
        master = context.createGain(); master.gain.value = level; master.connect(context.destination);
      }
      loading = Promise.allSettled(Object.entries(uiSoundFiles).map(async ([key, url]) => {
        if (buffers.has(key)) return;
        const response = await fetch(url);
        if (!response.ok) throw new Error(`UI sound HTTP ${response.status}: ${url}`);
        const buffer = await context.decodeAudioData(await response.arrayBuffer());
        if (!destroyed) buffers.set(key, buffer);
      }));
      for (const result of await loading) if (result.status === 'rejected') onError(result.reason);
    } catch (error) { onError(error); }
    finally { loading = null; }
  }
  return {
    prepare,
    async unlock() {
      if (destroyed || !visible) return;
      armed = true;
      const prepared = prepare();
      try { if (context?.state === 'suspended') await context.resume(); }
      catch { /* Retry on the next genuine gesture. */ }
      await prepared;
    },
    play(cue) {
      const config = cues[cue], at = now();
      if (!config || destroyed || !armed || !visible || !level || context?.state !== 'running' || at - lastPlay < 85) return false;
      const index = counters.get(cue) ?? 0, key = config.takes[index % config.takes.length];
      const buffer = buffers.get(key);
      // Never play an old click when a delayed download/decode eventually finishes.
      if (!buffer) return false;
      if (voices.size >= 2) {
        const oldest = voices.values().next().value;
        oldest.stop(); voices.delete(oldest);
      }
      const source = context.createBufferSource(), gain = context.createGain();
      source.buffer = buffer; gain.gain.value = config.gain;
      source.connect(gain); gain.connect(master);
      source.onended = () => { voices.delete(source); source.disconnect(); gain.disconnect(); };
      voices.add(source); source.start();
      counters.set(cue, index + 1); lastPlay = at;
      return true;
    },
    setVolume(value) {
      level = clamp(value);
      if (master) {
        master.gain.cancelScheduledValues(context.currentTime);
        // A one-shot graph can be idle between clicks. Set the intrinsic value too,
        // so its gain reflects the slider even when Chrome is not rendering a source.
        master.gain.value = level;
      }
      if (!level) stopAll();
    },
    async setVisible(value) {
      visible = Boolean(value);
      if (!visible) stopAll();
      if (!context || destroyed) return;
      try {
        if (!visible) await context.suspend();
        else if (armed && context.state === 'suspended') await context.resume();
      } catch { /* A later trusted gesture can resume a suspended context. */ }
    },
    async destroy() {
      destroyed = true; stopAll(); buffers.clear();
      if (context && context.state !== 'closed') await context.close();
    }
  };
}

export function installUiSounds({window, document, volume}) {
  const player = createUiSoundPlayer({AudioContext: window.AudioContext ?? window.webkitAudioContext,
    fetch: url => window.fetch(url), volume,
    onError: error => console.warn('Paper sound could not load:', error.message)});
  const unlock = event => {
    if (!event.isTrusted || event.repeat || event.ctrlKey || event.metaKey || event.altKey) return;
    void player.unlock();
  };
  document.addEventListener('pointerdown', unlock, {capture: true, passive: true});
  document.addEventListener('keydown', unlock, {capture: true});
  window.addEventListener('disco:ui-sound', event => player.play(event.detail.cue));
  window.addEventListener('disco:view-change', event => { if (event.detail.paperTurn) player.play('turn'); });
  window.addEventListener('disco:slot-open', () => player.play('slip'));
  window.addEventListener('disco:audio-settings', event => player.setVolume(event.detail.sound / 100));
  for (const id of ['slot-dialog', 'exit-dialog']) {
    document.getElementById(id)?.addEventListener('close', () => player.play('slip'));
  }
  // One paper sample on release lets the existing SFX slider audition its new level.
  document.querySelector('input[data-audio-channel="sound"]')?.addEventListener('change', () => player.play('slip'));
  document.addEventListener('visibilitychange', () => { void player.setVisible(!document.hidden); });
  window.addEventListener('pagehide', () => { void player.setVisible(false); });
  window.addEventListener('pageshow', () => { void player.setVisible(!document.hidden); });
  void player.prepare();
  void player.setVisible(!document.hidden);
  return player;
}
