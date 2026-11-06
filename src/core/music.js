export const musicTracks = {
  menu: {url: 'media/music/dongbei-disco-theme.mp3', level: .58, title: '东北迪斯科主页曲', credit: '主题曲 · 主菜单 / 角色选择', duration: '4:00'},
  station: {url: 'media/music/station-dark-place.ogg', level: .72, title: 'Dark Place', credit: '车站配乐 · SkyleTheFrench', duration: '0:55'}
};

const clampVolume = value => Number.isFinite(value) ? Math.max(0, Math.min(1, value)) : .54;

// Decode once, loop on the audio clock, and crossfade only when the music changes.
export function createMusicPlayer({AudioContext, fetch, onStatus = () => {}, onError = () => {}, volume = .54}) {
  const buffers = new Map(), retiring = new Set();
  let context, master, active, target = 'menu', visible = true, ducked = false;
  let sceneTrack = 'menu', preview = null, previewPaused = false, previewLoop = true;
  let unlocked = false, destroyed = false, revision = 0, problem = false;
  let level = clampVolume(volume);

  function playbackState() {
    return problem ? 'error' : !unlocked ? 'waiting' : previewPaused
      ? active?.ended ? 'ended' : 'paused' : !visible || context?.state !== 'running'
      ? 'suspended' : !active?.source || active.key !== target ? 'loading' : level === 0 ? 'muted' : 'playing';
  }
  function positionOf(track) {
    if (!track) return 0;
    const elapsed = (track.offset ?? 0) + (track.source ? Math.max(0, context.currentTime - track.startedAt) : 0);
    if (!track.buffer?.duration) return elapsed;
    return track.source?.loop ? elapsed % track.buffer.duration : Math.min(elapsed, track.buffer.duration);
  }
  function status(state) { onStatus({state, track: target, playingTrack: active?.source ? active.key : null, preview}); }
  function updateStatus() {
    status(playbackState());
  }
  function updateGain() {
    if (!master) return;
    const now = context.currentTime;
    master.gain.cancelScheduledValues(now);
    master.gain.setValueAtTime(master.gain.value, now);
    const next = level * (ducked ? .65 : 1);
    // Zero is immediate; an old fade must never turn muted music back up.
    if (next === 0) master.gain.setValueAtTime(0, now);
    else master.gain.linearRampToValueAtTime(next, now + .18);
  }
  function release(track) {
    if (track.source) {
      track.source.onended = null;
      try { track.source.stop(); } catch { /* Already ended. */ }
      track.source.disconnect(); track.gain.disconnect();
    }
    retiring.delete(track);
  }
  async function bufferFor(key) {
    if (!buffers.has(key)) {
      const task = (async () => {
        const response = await fetch(musicTracks[key].url);
        if (!response.ok) throw new Error(`Music HTTP ${response.status}: ${key}`);
        return context.decodeAudioData(await response.arrayBuffer());
      })();
      buffers.set(key, task);
      task.catch(() => buffers.delete(key));
    }
    return buffers.get(key);
  }
  async function reconcile() {
    const request = ++revision;
    if (destroyed || !unlocked || !visible || previewPaused || context?.state !== 'running') { updateStatus(); return; }
    if (active?.key === target && active.source) {
      active.source.loop = preview === null || previewLoop;
      problem = false; updateStatus(); return;
    }
    const key = target;
    status('loading');
    try {
      const buffer = await bufferFor(key);
      if (destroyed || request !== revision || !visible || previewPaused) return;
      const source = context.createBufferSource(), gain = context.createGain();
      source.buffer = buffer; source.loop = preview === null || previewLoop;
      source.connect(gain); gain.connect(master);
      const now = context.currentTime;
      const resuming = active?.key === key && !active.source;
      const offset = resuming && !active.ended ? active.offset : 0;
      const next = {key, source, gain, buffer, offset, startedAt: now, ended: false};
      gain.gain.setValueAtTime(0, now);
      gain.gain.linearRampToValueAtTime(musicTracks[key].level, now + (resuming ? .035 : 1.2));
      source.start(now, offset);
      source.onended = () => {
        if (active !== next || source.loop || destroyed) return;
        next.offset = buffer.duration; next.ended = true;
        release(next); next.source = null; next.gain = null;
        if (preview !== null) previewPaused = true;
        updateStatus();
      };
      // Rapid navigation may interrupt a fade, but must not stack old loops.
      for (const track of [...retiring]) release(track);
      if (active?.source) {
        const previous = active;
        previous.gain.gain.cancelScheduledValues(now);
        previous.gain.gain.setValueAtTime(previous.gain.gain.value, now);
        previous.gain.gain.linearRampToValueAtTime(0, now + 1.2);
        retiring.add(previous);
        previous.source.onended = () => release(previous);
        previous.source.stop(now + 1.25);
      }
      active = next; problem = false; updateStatus();
    } catch (error) {
      if (destroyed || request !== revision) return;
      problem = true; updateStatus(); onError(error);
    }
  }
  async function resume() {
    if (!context || !visible || destroyed) return;
    try {
      const resumed = context.resume();
      // Browser autoplay restrictions can leave this promise pending until a gesture.
      updateStatus();
      await resumed;
      await reconcile();
    } catch {
      // Keep the next genuine pointer/key gesture available for browser recovery.
      updateStatus();
    }
  }
  return {
    async unlock() {
      if (destroyed || !AudioContext) { status('unsupported'); return; }
      if (!context) {
        try { context = new AudioContext(); }
        catch (error) { problem = true; updateStatus(); onError(error); return; }
        context.onstatechange = updateStatus;
        master = context.createGain(); master.gain.value = 0; master.connect(context.destination);
        updateGain();
      }
      unlocked = true;
      if (context.state === 'running') return reconcile();
      return resume();
    },
    setView(view) {
      sceneTrack = view === 'station' ? 'station' : ['mainmenu', 'character-select'].includes(view) ? 'menu' : sceneTrack;
      const hadPreview = preview !== null;
      if (view !== 'settings') { preview = null; previewPaused = false; }
      const next = preview ?? sceneTrack;
      if (next === target && !(hadPreview && preview === null)) return;
      target = next; problem = false; return reconcile();
    },
    setPreview(key = null) {
      if (key !== null && !Object.hasOwn(musicTracks, key)) return;
      preview = key; previewPaused = false;
      target = preview ?? sceneTrack;
      problem = false;
      return reconcile();
    },
    getPlayback() {
      const current = active?.key === target ? active : null;
      return {state: playbackState(), track: target, preview, position: positionOf(current),
        duration: current?.buffer?.duration ?? 0, loop: previewLoop};
    },
    setPreviewPaused(value) {
      if (preview === null || destroyed) return;
      previewPaused = Boolean(value); ++revision;
      if (previewPaused) {
        if (active) {
          const offset = positionOf(active);
          release(active);
          active = active.key === target ? {...active, source: null, gain: null, offset} : null;
        }
        for (const track of [...retiring]) release(track);
        updateStatus(); return;
      }
      return reconcile();
    },
    seekPreview(seconds) {
      if (preview === null || !Number.isFinite(seconds) || active?.key !== target || !active.buffer?.duration) return;
      ++revision;
      const offset = Math.max(0, Math.min(active.buffer.duration - .001, seconds));
      release(active);
      active = {...active, source: null, gain: null, offset, ended: false};
      for (const track of [...retiring]) release(track);
      return reconcile();
    },
    setPreviewLoop(value) {
      previewLoop = Boolean(value);
      if (preview !== null && active?.key === target && active.source) active.source.loop = previewLoop;
      updateStatus();
    },
    setVolume(value) { level = clampVolume(value); updateGain(); updateStatus(); },
    setDucked(value) { if (ducked === value) return; ducked = value; updateGain(); },
    async setVisible(value) {
      visible = value; ++revision;
      if (!context) return updateStatus();
      if (visible) return resume();
      try { await context.suspend(); } catch { /* The context may be closing. */ }
      updateStatus();
    },
    async destroy() {
      destroyed = true; ++revision;
      if (active) release(active);
      for (const track of [...retiring]) release(track);
      buffers.clear();
      if (context && context.state !== 'closed') await context.close();
    }
  };
}

export function installMusic({window, document, volume, getActiveView, showToast}) {
  const stage = document.getElementById('app');
  let audioState = 'waiting', errorShown = false;
  const player = createMusicPlayer({
    AudioContext: window.AudioContext ?? window.webkitAudioContext,
    fetch: url => window.fetch(url), volume,
    onStatus(detail) {
      audioState = detail.state; stage.dataset.musicState = detail.state; stage.dataset.musicTrack = detail.track;
      window.dispatchEvent(new window.CustomEvent('disco:music-status', {detail}));
    },
    onError(error) {
      console.warn('Background music could not load:', error.message);
      if (!errorShown) { errorShown = true; showToast('背景音乐加载失败，请检查连接后刷新重试。'); }
    }
  });
  const unlock = event => {
    if (!event.isTrusted || event.repeat || event.ctrlKey || event.metaKey || event.altKey) return;
    if (['waiting', 'suspended', 'error'].includes(audioState)) void player.unlock();
  };
  document.addEventListener('pointerdown', unlock, {capture: true, passive: true});
  document.addEventListener('keydown', unlock, {capture: true});
  window.addEventListener('disco:view-change', event => { void player.setView(event.detail.view); });
  window.addEventListener('disco:audio-settings', event => player.setVolume(event.detail.music / 100));
  document.addEventListener('visibilitychange', () => { void player.setVisible(!document.hidden); });
  window.addEventListener('pagehide', () => { void player.setVisible(false); });
  window.addEventListener('pageshow', () => { void player.setVisible(!document.hidden); });
  const dialogue = document.getElementById('station-talk');
  const observer = new window.MutationObserver(() => player.setDucked(dialogue.open));
  observer.observe(dialogue, {attributes: true, attributeFilter: ['open']});
  player.setDucked(dialogue.open);
  void player.setView(getActiveView());
  void player.setVisible(!document.hidden);
  // Start with the game. Trusted input remains a fallback for restricted browsers.
  void player.unlock();
  return player;
}
