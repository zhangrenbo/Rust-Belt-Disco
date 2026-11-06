// A reversible display pass: source artwork and save-game data stay untouched.
export const FILM_DEFAULTS = Object.freeze({ film: true, filmStrength: 65, filmMotion: true });

export function normalizeFilmSettings(settings = {}) {
  return {
    film: typeof settings.film === 'boolean' ? settings.film : FILM_DEFAULTS.film,
    filmStrength: Number.isFinite(settings.filmStrength)
      ? Math.max(0, Math.min(100, settings.filmStrength)) : FILM_DEFAULTS.filmStrength,
    filmMotion: typeof settings.filmMotion === 'boolean' ? settings.filmMotion : FILM_DEFAULTS.filmMotion,
  };
}

export function installRetroFilm({ window, document, stage }) {
  const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');
  let current = { ...FILM_DEFAULTS, quality: 'enhanced' };
  let textureTimer = null;
  let nextScratchAt = 0;
  const between = (min, max) => min + Math.random() * (max - min);
  const damage = document.querySelector('.projector-edge-snow');
  // Reuse a fixed set of decorative marks; never animate artwork or controls.
  const marks = damage ? [...Array(48).fill('dust'), 'fiber', 'fiber', 'fiber', 'scratch', 'scratch'].map(kind => {
    const node = document.createElement('i');
    node.className = `film-${kind}`;
    node.hidden = true;
    damage.append(node);
    return { node, kind, frames: 0 };
  }) : [];
  function showMark(mark, frames) {
    const scratch = mark.kind === 'scratch';
    const light = Math.random() < .56;
    const dustSize = Math.random() < .15 ? between(.28, .42) : between(.1, .24);
    mark.frames = frames;
    mark.node.hidden = false;
    const values = {
      '--mark-x': `${scratch ? (Math.random() < .5 ? between(6, 25) : between(74, 94)) : between(5, 95)}%`,
      '--mark-y': `${scratch ? between(0, 22) : between(4, 96)}%`,
      '--mark-width': `${mark.kind === 'fiber' ? between(.3, .65) : dustSize}cqw`,
      '--mark-height': scratch ? `${between(35, 78)}%` : `${dustSize * between(.55, 1.05)}cqw`,
      '--mark-color': light ? '#fff4dc' : '#171410',
      '--mark-opacity': String(scratch ? between(.24, .46) : between(.65, .95)),
      '--mark-rotation': `${scratch ? between(-.18, .18) : between(-170, 170)}deg`,
    };
    for (const [key, value] of Object.entries(values)) mark.node.style.setProperty(key, value);
  }
  function textureFrame() {
    textureTimer = null;
    if (stage.dataset.filmMotion !== 'on') return;
    const now = window.performance.now();
    stage.style.setProperty('--film-grain-x', `${Math.floor(between(-96, 96))}px`);
    stage.style.setProperty('--film-grain-y', `${Math.floor(between(-96, 96))}px`);
    stage.style.setProperty('--film-overlay-x', `${between(-1.1, 1.1).toFixed(2)}px`);
    stage.style.setProperty('--film-overlay-y', `${between(-1.4, 1.4).toFixed(2)}px`);
    for (const mark of marks) {
      if (mark.frames > 0 && --mark.frames === 0) mark.node.hidden = true;
      if (mark.kind === 'dust' && !mark.frames && Math.random() < .72) showMark(mark, Math.random() < .3 ? 2 : 1);
      if (mark.kind === 'fiber' && !mark.frames && Math.random() < .08) showMark(mark, 2);
    }
    if (now >= nextScratchAt) {
      const scratches = marks.filter(mark => mark.kind === 'scratch');
      if (scratches.length) showMark(scratches[Math.floor(Math.random() * scratches.length)], Math.floor(between(2, 5)));
      nextScratchAt = now + between(3000, 6000);
    }
    textureTimer = window.setTimeout(textureFrame, current.quality === 'balanced' ? between(110, 155) : between(65, 95));
  }
  function paint() {
    const enabled = current.film && current.filmStrength > 0;
    const moving = enabled && current.filmMotion && !reducedMotion.matches
      && !document.hidden && current.quality !== 'performance';
    stage.dataset.film = enabled ? 'on' : 'off';
    stage.dataset.filmMotion = moving ? 'on' : 'off';
    stage.style.setProperty('--film-strength', String(current.filmStrength / 100));
    if (moving && textureTimer === null) {
      nextScratchAt = window.performance.now() + between(1400, 3200);
      textureFrame();
    } else if (!moving) {
      window.clearTimeout(textureTimer);
      textureTimer = null;
      for (const mark of marks) { mark.frames = 0; mark.node.hidden = true; }
      for (const key of ['--film-grain-x', '--film-grain-y', '--film-overlay-x', '--film-overlay-y']) stage.style.removeProperty(key);
    }
  }
  document.addEventListener('visibilitychange', paint);
  reducedMotion.addEventListener('change', paint);
  return {
    update(settings) {
      current = { ...normalizeFilmSettings(settings), quality: settings.quality };
      paint();
    },
  };
}
