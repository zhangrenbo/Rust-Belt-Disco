import {musicTracks} from './music.js';

import {emitUiSound} from './ui-sounds.js';

const FAVORITES_KEY = 'northeast-disco.music-favorites.v1';
const TRACK_COVERS = {
  menu: 'assets/event-desk-bg-medal-marriage-v2.png',
  station: 'media/station-night.png'
};
const formatTime = seconds => {
  const value = Math.max(0, Math.floor(seconds || 0));
  return `${Math.floor(value / 60)}:${String(value % 60).padStart(2, '0')}`;
};

export function installMusicLibrary({window, document, player}) {
  const view = document.querySelector('[data-view="settings"]');
  const controls = document.querySelector('[data-settings-controls]');
  const panel = document.getElementById('settings-music-library');
  const opener = document.querySelector('[data-music-library-open]');
  const list = panel.querySelector('[data-music-library-list]');
  const status = panel.querySelector('[data-music-library-status]');
  const seek = panel.querySelector('[data-record-seek]');
  const playButton = panel.querySelector('[data-record-play]');
  const favoriteButton = panel.querySelector('[data-record-favorite]');
  const repeat = panel.querySelector('[data-record-repeat]');
  const keys = Object.keys(musicTracks), rows = new Map();
  let selected = keys[0], ticker = null, dragging = false;
  let favorites = new Set(), storageProblem = false;
  try {
    const saved = JSON.parse(window.localStorage.getItem(FAVORITES_KEY) ?? '[]');
    if (Array.isArray(saved)) favorites = new Set(saved.filter(key => keys.includes(key)));
  } catch { /* Leave any invalid stored value untouched until an explicit change. */ }

  function paintProgress() {
    if (panel.hidden) return;
    const audio = player.getPlayback();
    const duration = audio.track === selected ? audio.duration : 0;
    seek.disabled = !duration;
    if (!dragging) {
      seek.max = duration || 1;
      seek.value = duration ? audio.position : 0;
    }
    const position = dragging ? Number(seek.value) : audio.track === selected ? audio.position : 0;
    seek.style.setProperty('--record-progress', `${duration ? position / duration * 100 : 0}%`);
    seek.setAttribute('aria-valuetext', `${formatTime(position)} / ${duration ? formatTime(duration) : musicTracks[selected].duration}`);
    panel.querySelector('[data-record-elapsed]').textContent = formatTime(position);
    panel.querySelector('[data-record-duration]').textContent = duration ? formatTime(duration) : musicTracks[selected].duration;
  }
  function paint() {
    const audio = player.getPlayback(), track = musicTracks[selected];
    const playing = !panel.hidden && audio.preview === selected && ['playing', 'muted'].includes(audio.state);
    const canPause = !panel.hidden && !['paused', 'ended', 'error', 'unsupported'].includes(audio.state);
    panel.classList.toggle('is-playing', playing);
    playButton.classList.toggle('is-playing', canPause);
    playButton.setAttribute('aria-label', audio.state === 'error' ? '重试播放' : canPause ? '暂停' : '播放');
    panel.querySelector('[data-record-title]').textContent = track.title;
    panel.querySelector('[data-record-credit]').textContent = track.credit;
    for (const [key, row] of rows) {
      const current = key === selected, favorite = favorites.has(key);
      row.classList.toggle('is-current', current);
      row.querySelector('.record-track-number').textContent = current && playing ? '♪' : String(keys.indexOf(key) + 1).padStart(2, '0');
      row.querySelector('.record-track-main').setAttribute('aria-pressed', String(current));
      const heart = row.querySelector('.record-track-heart');
      heart.setAttribute('aria-pressed', String(favorite));
      heart.setAttribute('aria-label', `${favorite ? '取消收藏' : '收藏'}：${musicTracks[key].title}`);
    }
    favoriteButton.setAttribute('aria-pressed', String(favorites.has(selected)));
    favoriteButton.setAttribute('aria-label', `${favorites.has(selected) ? '取消收藏' : '收藏'}当前曲目`);
    repeat.setAttribute('aria-pressed', String(audio.loop));
    const count = keys.length;
    panel.querySelector('[data-record-count]').textContent = `共 ${count} 张唱片`;
    for (const button of panel.querySelectorAll('[data-record-previous], [data-record-next]')) button.disabled = !count;
    status.textContent = storageProblem ? '收藏暂时无法保存，请检查浏览器存储。'
      : audio.state === 'error' ? '这首音乐暂时无法播放，点播放键重试。'
      : audio.state === 'unsupported' ? '当前环境无法播放音乐。'
      : audio.state === 'muted' ? '音乐音量为 0，可返回设置调整。'
      : audio.state === 'loading' ? '唱片正在加载…'
      : audio.state === 'paused' ? '唱针已抬起，点击播放继续。'
      : audio.state === 'ended' ? '本曲播放完毕。'
      : playing ? `正在试听：${track.title}` : '点击播放，开始试听。';
    paintProgress();
  }
  function stopTicker() { if (ticker !== null) window.clearInterval(ticker); ticker = null; }
  function syncVisibility() {
    panel.classList.toggle('is-page-hidden', document.hidden);
    stopTicker();
    if (!panel.hidden && !document.hidden) ticker = window.setInterval(paintProgress, 250);
  }
  function choose(key, sound = true) {
    if (sound && key !== selected) emitUiSound(document, 'slip');
    selected = key;
    void player.setPreview(key);
    paint();
  }
  function toggleFavorite(key) {
    if (favorites.has(key)) favorites.delete(key); else favorites.add(key);
    try { window.localStorage.setItem(FAVORITES_KEY, JSON.stringify([...favorites])); storageProblem = false; }
    catch { storageProblem = true; }
    paint();
  }
  function close({focus = true} = {}) {
    if (panel.hidden) return;
    panel.hidden = true; controls.hidden = false;
    view.classList.remove('is-record-library');
    opener.setAttribute('aria-expanded', 'false');
    view.setAttribute('aria-labelledby', 'settings-title');
    dragging = false; stopTicker();
    void player.setPreview(null);
    paint();
    if (focus) { opener.focus({preventScroll: true}); emitUiSound(document, 'turn'); }
  }
  for (const [key, track] of Object.entries(musicTracks)) {
    const row = document.createElement('li'); row.className = 'record-track';
    row.innerHTML = '<span class="record-track-number" aria-hidden="true"></span><button class="record-track-main" type="button"><img class="record-track-cover" alt=""><span class="record-track-copy"><strong></strong><small></small></span></button><time></time><button class="record-track-heart" type="button">♥</button>';
    const button = row.querySelector('.record-track-main'); button.dataset.previewMusic = key;
    button.querySelector('.record-track-cover').src = TRACK_COVERS[key] ?? TRACK_COVERS.menu;
    button.querySelector('strong').textContent = track.title;
    button.querySelector('small').textContent = track.credit;
    row.querySelector('time').textContent = track.duration;
    button.addEventListener('click', () => choose(key));
    row.querySelector('.record-track-heart').addEventListener('click', () => toggleFavorite(key));
    rows.set(key, row); list.append(row);
  }
  opener.addEventListener('click', () => {
    if (!panel.hidden) return close();
    controls.hidden = true; panel.hidden = false;
    view.classList.add('is-record-library');
    opener.setAttribute('aria-expanded', 'true');
    view.setAttribute('aria-labelledby', 'music-library-title');
    const current = player.getPlayback().track;
    choose(keys.includes(current) ? current : selected, false);
    emitUiSound(document, 'turn');
    document.getElementById('music-library-title').focus({preventScroll: true});
    syncVisibility();
  });
  panel.querySelector('[data-music-library-close]').addEventListener('click', () => close());
  playButton.addEventListener('click', () => {
    const audio = player.getPlayback();
    if (audio.state === 'error') choose(selected);
    else void player.setPreviewPaused(!['paused', 'ended'].includes(audio.state));
    paint();
  });
  function skip(delta) {
    if (!keys.length) return;
    const index = keys.indexOf(selected);
    choose(keys[index < 0 ? (delta > 0 ? 0 : keys.length - 1) : (index + delta + keys.length) % keys.length]);
  }
  panel.querySelector('[data-record-previous]').addEventListener('click', () => skip(-1));
  panel.querySelector('[data-record-next]').addEventListener('click', () => skip(1));
  favoriteButton.addEventListener('click', () => toggleFavorite(selected));
  repeat.addEventListener('click', () => { player.setPreviewLoop(!player.getPlayback().loop); paint(); });
  seek.addEventListener('pointerdown', () => { dragging = true; });
  seek.addEventListener('input', () => {
    if (dragging) paintProgress();
    else { void player.seekPreview(Number(seek.value)); paintProgress(); }
  });
  function commitPointerSeek() {
    if (!dragging) return;
    const position = Number(seek.value); dragging = false;
    void player.seekPreview(position); paintProgress();
  }
  seek.addEventListener('change', commitPointerSeek);
  seek.addEventListener('pointercancel', () => { dragging = false; paintProgress(); });
  window.addEventListener('pointerup', commitPointerSeek);
  document.addEventListener('keydown', event => {
    if (event.key !== 'Escape' || panel.hidden) return;
    event.preventDefault(); event.stopImmediatePropagation(); close();
  }, {capture: true});
  window.addEventListener('disco:view-change', event => { if (event.detail.view !== 'settings') close({focus: false}); });
  window.addEventListener('disco:music-status', paint);
  document.addEventListener('visibilitychange', syncVisibility);
  window.addEventListener('pagehide', stopTicker);
  window.addEventListener('pageshow', syncVisibility);
  paint();
}
