const views = [...document.querySelectorAll("[data-view]")];
const viewNames = views.map(view => view.dataset.view);
const viewAliases = {};
const shutter = document.querySelector(".page-shutter");
const toast = document.querySelector("#toast");
let toastTimer;
let activeView = "mainmenu";
let settingsReturnView = "mainmenu";
let interactionEpoch = 0;
const pointerPresses = new WeakMap();

const stage = document.querySelector(".stage");
const projectorSnowLayer = document.querySelector(".projector-edge-snow");
const projectorSnowReducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");
const projectorSnowColors = ["#eee9df", "#d8d5ce", "#bbb9b3", "#a4a39e"];
const projectorSnowKinds = ["is-chip", "is-chip", "is-fiber", "is-smudge", "is-ring"];
let projectorSnowTimer = 0;

function randomBetween(min, max) {
  return min + Math.random() * (max - min);
}

function spawnProjectorSnowParticle() {
  if (!projectorSnowLayer || projectorSnowReducedMotion.matches || projectorSnowLayer.childElementCount >= 28) return;

  const particle = document.createElement("i");
  const kind = projectorSnowKinds[Math.floor(Math.random() * projectorSnowKinds.length)];
  const edgeRoll = Math.random();
  const depth = .25 + Math.pow(Math.random(), 2.45) * 12.5;
  let x;
  let y;

  if (edgeRoll < .31) {
    x = randomBetween(0, 100);
    y = depth;
  } else if (edgeRoll < .62) {
    x = randomBetween(0, 100);
    y = 100 - depth;
  } else if (edgeRoll < .81) {
    x = depth;
    y = randomBetween(0, 100);
  } else {
    x = 100 - depth;
    y = randomBetween(0, 100);
  }

  let width = randomBetween(.035, .12);
  let height = randomBetween(.026, .095);
  if (kind === "is-fiber") {
    width = randomBetween(.12, .32);
    height = randomBetween(.014, .036);
  } else if (kind === "is-smudge") {
    width = randomBetween(.08, .22);
    height = randomBetween(.04, .11);
  } else if (kind === "is-ring") {
    width = randomBetween(.055, .13);
    height = randomBetween(.045, .12);
  }

  const rotation = randomBetween(-175, 175);
  const life = randomBetween(360, 1180);
  particle.className = `projector-snow-particle ${kind}`;
  particle.style.setProperty("--snow-x", `${x.toFixed(2)}%`);
  particle.style.setProperty("--snow-y", `${y.toFixed(2)}%`);
  particle.style.setProperty("--snow-width", `${width.toFixed(3)}cqw`);
  particle.style.setProperty("--snow-height", `${height.toFixed(3)}cqw`);
  particle.style.setProperty("--snow-color", projectorSnowColors[Math.floor(Math.random() * projectorSnowColors.length)]);
  particle.style.setProperty("--snow-opacity", randomBetween(.2, .62).toFixed(2));
  particle.style.setProperty("--snow-blur", `${randomBetween(0, .018).toFixed(3)}cqw`);
  particle.style.setProperty("--snow-radius", `${randomBetween(28, 72).toFixed(0)}% ${randomBetween(28, 72).toFixed(0)}% ${randomBetween(28, 72).toFixed(0)}% ${randomBetween(28, 72).toFixed(0)}% / ${randomBetween(30, 70).toFixed(0)}% ${randomBetween(30, 70).toFixed(0)}% ${randomBetween(30, 70).toFixed(0)}% ${randomBetween(30, 70).toFixed(0)}%`);
  particle.style.setProperty("--snow-life", `${life.toFixed(0)}ms`);
  particle.style.setProperty("--snow-rotation", `${rotation.toFixed(1)}deg`);
  particle.style.setProperty("--snow-rotation-end", `${(rotation + randomBetween(-42, 42)).toFixed(1)}deg`);
  particle.style.setProperty("--snow-drift-x", `${randomBetween(-.22, .22).toFixed(3)}cqw`);
  particle.style.setProperty("--snow-drift-y", `${randomBetween(-.18, .18).toFixed(3)}cqw`);
  particle.style.setProperty("--snow-scale-end", randomBetween(.78, 1.34).toFixed(2));
  projectorSnowLayer.append(particle);

  particle.addEventListener("animationend", () => particle.remove(), { once: true });
  window.setTimeout(() => particle.remove(), life + 250);
}

function scheduleProjectorSnow() {
  if (!projectorSnowLayer || projectorSnowReducedMotion.matches || projectorSnowTimer) return;
  projectorSnowTimer = window.setTimeout(() => {
    projectorSnowTimer = 0;
    if (!document.hidden) {
      spawnProjectorSnowParticle();
      if (Math.random() < .18) spawnProjectorSnowParticle();
    }
    scheduleProjectorSnow();
  }, randomBetween(38, 175));
}

function startProjectorSnow() {
  if (!projectorSnowLayer || projectorSnowReducedMotion.matches) return;
  for (let index = 0; index < 9; index += 1) {
    window.setTimeout(spawnProjectorSnowParticle, randomBetween(0, 620));
  }
  scheduleProjectorSnow();
}

projectorSnowReducedMotion.addEventListener("change", event => {
  if (event.matches) {
    window.clearTimeout(projectorSnowTimer);
    projectorSnowTimer = 0;
    projectorSnowLayer?.replaceChildren();
  } else {
    startProjectorSnow();
  }
});

startProjectorSnow();
const characterSelectView = document.querySelector(".view-character-select");
const characterCamera = document.querySelector("[data-character-camera]");
const characterArtLayers = [...document.querySelectorAll(".character-art")];
const characterChoices = [...document.querySelectorAll("[data-character-choice]")];
const characterPan = { x: 0, y: 0 };
const characterVelocity = { x: 0, y: 0 };
const characterWorldAspect = 7007 / 3238;
const characterZoom = { value: 1.54, target: 1.54, default: 1.54, max: 2.15, response: 7.2 };
let characterZoomAnchor = null;
const characterEdgeIntent = { x: 0, y: 0 };
const characterPointer = { clientX: 0, clientY: 0, active: false };
let characterLastFrame = performance.now();
let characterArtLayoutKey = "";
let characterFrame = 0;

function clamp(value, min, max) {
  return Math.min(max, Math.max(min, value));
}

function getCharacterPanLimits() {
  if (!stage || !characterCamera) return { x: 0, y: 0 };
  const cameraWidth = stage.clientWidth * characterZoom.value;
  const cameraHeight = cameraWidth / characterWorldAspect;
  const worldOverflowX = Math.max(0, (cameraWidth - stage.clientWidth) / 2);
  const worldOverflowY = Math.max(0, (cameraHeight - stage.clientHeight) / 2);
  return {
    x: worldOverflowX,
    y: worldOverflowY
  };
}

function getCharacterMinZoom() {
  if (!stage?.clientWidth || !stage.clientHeight) return 1.22;
  return Math.max(1, stage.clientHeight * characterWorldAspect / stage.clientWidth);
}

function snapCharacterPixel(value) {
  const pixelRatio = Math.max(1, window.devicePixelRatio || 1);
  return Math.round(value * pixelRatio) / pixelRatio;
}

function layoutCharacterArt(cameraWidth, cameraHeight, renderScale = 1) {
  const layoutKey = `${cameraWidth}:${cameraHeight}:${renderScale}:${window.devicePixelRatio || 1}`;
  if (layoutKey === characterArtLayoutKey) return;
  characterArtLayoutKey = layoutKey;

  characterArtLayers.forEach(layer => {
    if (layer.classList.contains("character-art-base")) {
      layer.style.left = "0px";
      layer.style.top = "0px";
      layer.style.width = `${cameraWidth * renderScale}px`;
      layer.style.height = `${cameraHeight * renderScale}px`;
      return;
    }

    const x = parseFloat(layer.style.getPropertyValue("--x")) / 100;
    const y = parseFloat(layer.style.getPropertyValue("--y")) / 100;
    const width = parseFloat(layer.style.getPropertyValue("--w")) / 100;
    const height = parseFloat(layer.style.getPropertyValue("--h")) / 100;
    layer.style.left = `${snapCharacterPixel(cameraWidth * x) * renderScale}px`;
    layer.style.top = `${snapCharacterPixel(cameraHeight * y) * renderScale}px`;
    layer.style.width = `${snapCharacterPixel(cameraWidth * width) * renderScale}px`;
    layer.style.height = `${snapCharacterPixel(cameraHeight * height) * renderScale}px`;
  });
}

function paintCharacterCamera() {
  if (!characterSelectView || !stage || !characterCamera) return;
  const limits = getCharacterPanLimits();
  const clampedX = clamp(characterPan.x, -limits.x, limits.x);
  const clampedY = clamp(characterPan.y, -limits.y, limits.y);
  if (clampedX !== characterPan.x) characterVelocity.x = 0;
  if (clampedY !== characterPan.y) characterVelocity.y = 0;
  characterPan.x = clampedX;
  characterPan.y = clampedY;

  const stageRect = stage.getBoundingClientRect();
  const baseWidth = snapCharacterPixel(stage.clientWidth * characterZoom.default);
  const baseHeight = snapCharacterPixel(baseWidth / characterWorldAspect);
  const cameraWidth = stage.clientWidth * characterZoom.value;
  const cameraHeight = cameraWidth / characterWorldAspect;
  const cameraScale = cameraWidth / baseWidth;
  const centeredLeft = (stage.clientWidth - cameraWidth) / 2 + characterPan.x;
  const centeredTop = (stage.clientHeight - cameraHeight) / 2 + characterPan.y;
  const cameraLeft = snapCharacterPixel(stageRect.left + centeredLeft) - stageRect.left;
  const cameraTop = snapCharacterPixel(stageRect.top + centeredTop) - stageRect.top;

  // Preserve the previous on-screen geometry, but rasterize each source PNG at
  // its final size instead of scaling an already-rasterized camera texture.
  characterCamera.style.width = `${baseWidth * cameraScale}px`;
  characterCamera.style.height = `${baseHeight * cameraScale}px`;
  characterCamera.style.left = "0px";
  characterCamera.style.top = "0px";
  characterCamera.style.transform = `translate(${cameraLeft}px, ${cameraTop}px)`;
  layoutCharacterArt(baseWidth, baseHeight, cameraScale);
}

function clearCharacterEdgeIntent() {
  characterEdgeIntent.x = 0;
  characterEdgeIntent.y = 0;
}

function resetCharacterCamera() {
  characterZoom.value = clamp(characterZoom.default, getCharacterMinZoom(), Math.max(characterZoom.max, getCharacterMinZoom()));
  characterZoom.target = characterZoom.value;
  characterZoomAnchor = null;
  characterPan.x = 0;
  characterPan.y = 0;
  characterVelocity.x = 0;
  characterVelocity.y = 0;
  clearCharacterEdgeIntent();
  paintCharacterCamera();
}

function zoomCharacterCamera(event) {
  if (activeView !== "character-select" || !stage || !characterCamera) return;
  const stageRect = stage.getBoundingClientRect();
  if (event.clientX < stageRect.left || event.clientX > stageRect.right || event.clientY < stageRect.top || event.clientY > stageRect.bottom) return;

  event.preventDefault();
  let delta = event.deltaY;
  if (event.deltaMode === WheelEvent.DOM_DELTA_LINE) delta *= 16;
  else if (event.deltaMode === WheelEvent.DOM_DELTA_PAGE) delta *= stage.clientHeight;
  delta = clamp(delta, -140, 140);

  const oldZoom = characterZoom.value;
  const nextZoom = clamp(characterZoom.target * Math.exp(-delta * .00115), getCharacterMinZoom(), Math.max(characterZoom.max, getCharacterMinZoom()));
  if (Math.abs(nextZoom - characterZoom.target) < .0001) return;

  const localX = clamp(event.clientX - stageRect.left, 0, stage.clientWidth);
  const localY = clamp(event.clientY - stageRect.top, 0, stage.clientHeight);
  const oldWidth = stage.clientWidth * oldZoom;
  const oldHeight = oldWidth / characterWorldAspect;
  const oldLeft = (stage.clientWidth - oldWidth) / 2 + characterPan.x;
  const oldTop = (stage.clientHeight - oldHeight) / 2 + characterPan.y;
  const anchorX = clamp((localX - oldLeft) / oldWidth, 0, 1);
  const anchorY = clamp((localY - oldTop) / oldHeight, 0, 1);

  characterZoom.target = nextZoom;
  characterZoomAnchor = { localX, localY, anchorX, anchorY };
  characterVelocity.x = 0;
  characterVelocity.y = 0;
}

function advanceCharacterZoom(elapsed) {
  const difference = characterZoom.target - characterZoom.value;
  if (Math.abs(difference) < .00015) {
    characterZoom.value = characterZoom.target;
    characterZoomAnchor = null;
    return false;
  }

  const response = 1 - Math.exp(-elapsed * characterZoom.response);
  let nextZoom = characterZoom.value + difference * response;
  if (Math.abs(characterZoom.target - nextZoom) < .00015) nextZoom = characterZoom.target;

  characterZoom.value = nextZoom;
  if (characterZoomAnchor) {
    const newWidth = stage.clientWidth * nextZoom;
    const newHeight = newWidth / characterWorldAspect;
    const anchoredLeft = characterZoomAnchor.localX - characterZoomAnchor.anchorX * newWidth;
    const anchoredTop = characterZoomAnchor.localY - characterZoomAnchor.anchorY * newHeight;
    characterPan.x = anchoredLeft - (stage.clientWidth - newWidth) / 2;
    characterPan.y = anchoredTop - (stage.clientHeight - newHeight) / 2;
  }

  if (nextZoom === characterZoom.target) characterZoomAnchor = null;
  return true;
}

function edgeAxis(position, length, edgeSize) {
  if (position < edgeSize) return -Math.pow(1 - position / edgeSize, 1.35);
  if (position > length - edgeSize) return Math.pow((position - (length - edgeSize)) / edgeSize, 1.35);
  return 0;
}

function refreshCharacterEdgeIntent() {
  if (!stage || !characterPointer.active || activeView !== "character-select") {
    clearCharacterEdgeIntent();
    return;
  }
  const rect = stage.getBoundingClientRect();
  const localX = clamp(characterPointer.clientX - rect.left, 0, rect.width);
  const localY = clamp(characterPointer.clientY - rect.top, 0, rect.height);
  characterEdgeIntent.x = edgeAxis(localX, rect.width, rect.width * .12);
  characterEdgeIntent.y = edgeAxis(localY, rect.height, rect.height * .14);
}

function rememberCharacterPointer(event) {
  characterPointer.clientX = event.clientX;
  characterPointer.clientY = event.clientY;
  characterPointer.active = true;
  refreshCharacterEdgeIntent();
}

function releaseCharacterPointer() {
  characterPointer.active = false;
  clearCharacterEdgeIntent();
}

function nudgeCharacterCamera(x, y) {
  if (!stage) return;
  characterPan.x += x * stage.clientWidth * .07;
  characterPan.y += y * stage.clientHeight * .08;
  paintCharacterCamera();
}

function animateCharacterCamera(now) {
  const elapsed = Math.min(.04, Math.max(0, (now - characterLastFrame) / 1000));
  characterLastFrame = now;

  if (activeView === "character-select" && stage) {
    const zooming = advanceCharacterZoom(elapsed);
    refreshCharacterEdgeIntent();
    if (zooming) {
      characterVelocity.x = 0;
      characterVelocity.y = 0;
    } else {
      const maxSpeed = Math.max(210, stage.clientWidth * .24);
      const targetX = -characterEdgeIntent.x * maxSpeed;
      const targetY = -characterEdgeIntent.y * maxSpeed;
      const response = 1 - Math.exp(-elapsed * 8.5);
      characterVelocity.x += (targetX - characterVelocity.x) * response;
      characterVelocity.y += (targetY - characterVelocity.y) * response;
      characterPan.x += characterVelocity.x * elapsed;
      characterPan.y += characterVelocity.y * elapsed;
    }
    paintCharacterCamera();
  } else {
    characterVelocity.x = 0;
    characterVelocity.y = 0;
  }

  characterFrame = activeView === "character-select" && !document.hidden
    ? requestAnimationFrame(animateCharacterCamera) : 0;
}

function startCharacterAnimation() {
  if (!characterFrame && activeView === "character-select" && !document.hidden) {
    characterLastFrame = performance.now();
    characterFrame = requestAnimationFrame(animateCharacterCamera);
  }
}
document.addEventListener("visibilitychange", startCharacterAnimation);

const mainMenuVideo = document.querySelector(".mainmenu-video");
const mainMenuPress = document.querySelector("[data-menu-reveal]");
const mainMenuPanel = document.querySelector("[data-mainmenu-panel]");
const exitDialog = document.querySelector("#exit-dialog");
let exitDialogOpenedAt = 0;
let mainMenuRevealed = !mainMenuPress || !mainMenuPanel;

function revealMainMenu() {
  if (mainMenuRevealed) return;
  mainMenuRevealed = true;
  mainMenuPress?.classList.add("is-hidden");
  mainMenuPanel?.classList.add("is-revealed");
  mainMenuPanel?.setAttribute("aria-hidden", "false");
  window.setTimeout(() => mainMenuPanel?.querySelector("button")?.focus(), 420);
}

function resetMainMenu() {
  if (!mainMenuPress || !mainMenuPanel) {
    mainMenuRevealed = true;
    return;
  }
  mainMenuRevealed = false;
  mainMenuPress?.classList.remove("is-hidden");
  mainMenuPanel?.classList.remove("is-revealed");
  mainMenuPanel?.setAttribute("aria-hidden", "true");
}

function showToast(message) {
  if (!toast) return;
  window.clearTimeout(toastTimer);
  toast.textContent = message;
  toast.classList.add("show");
  toastTimer = window.setTimeout(() => toast.classList.remove("show"), 1700);
}

// Invalidate presses from the previous screen without swallowing new user input.
function lockViewInput() {
  interactionEpoch += 1;
}

function loadViewAssets(view) {
  view?.querySelectorAll("img[data-src]").forEach(img => {
    img.src = img.dataset.src;
    img.removeAttribute("data-src");
  });
}

function rememberPointerPress(button, event) {
  if (event.isPrimary === false || event.button !== 0) return;
  pointerPresses.set(button, {
    epoch: interactionEpoch,
    x: event.clientX, y: event.clientY,
    viewAtPress: activeView
  });
}

function consumeButtonActivation(button, event) {
  const ownerView = button.closest("[data-view]");
  if (ownerView && !ownerView.classList.contains("active")) return false;

  // Keyboard and assistive-technology activation has no physical pointer click count.
  if (event.detail === 0) return true;
  if (event.detail > 1) return false;

  const press = pointerPresses.get(button);
  pointerPresses.delete(button);
  if (!press) return false;

  return press.viewAtPress === activeView
    && press.epoch === interactionEpoch
    && Math.hypot(event.clientX - press.x, event.clientY - press.y) < 12;
}

function bindSafeButtonClick(buttons, handler) {
  buttons.forEach(button => {
    button.addEventListener("pointerdown", event => rememberPointerPress(button, event));
    button.addEventListener("pointercancel", () => pointerPresses.delete(button));
    button.addEventListener("click", event => {
      event.preventDefault();
      event.stopImmediatePropagation();
      if (!consumeButtonActivation(button, event)) return;
      handler(button, event);
    });
  });
}

function setActiveView(name) {
  const resolvedName = viewAliases[name] ?? name;
  const nextView = viewNames.includes(resolvedName) ? resolvedName : "mainmenu";
  const nextElement = document.querySelector(`[data-view="${nextView}"]`);
  loadViewAssets(nextElement);
  if (nextView === activeView && nextElement?.classList.contains("active")) {
    views.forEach(view => {
      const isActive = view === nextElement;
      view.setAttribute("aria-hidden", String(!isActive));
      view.inert = !isActive;
    });
    return true;
  }
  if (nextView === "settings") settingsReturnView = activeView;
  // Settings must appear once and stay visible, without a full-screen shutter.
  const instantTransition = nextView === "settings" || activeView === "settings";
  shutter?.classList.remove("turning");
  nextElement.classList.toggle("view-instant", instantTransition);
  if (!instantTransition) {
    void shutter?.offsetWidth;
    shutter?.classList.add("turning");
  }
  lockViewInput();

  views.forEach(view => {
    const isActive = view.dataset.view === nextView;
    view.classList.toggle("active", isActive);
    view.setAttribute("aria-hidden", String(!isActive));
    view.inert = !isActive;
  });
  nextElement.tabIndex = -1;
  nextElement.focus({ preventScroll: true });

  document.querySelectorAll(".folio-nav [data-open-view]").forEach(button => {
    const isActive = button.dataset.openView === nextView;
    button.classList.toggle("active", isActive);
    if (isActive) button.setAttribute("aria-current", "page");
    else button.removeAttribute("aria-current");
  });

  activeView = nextView;
  if (nextView === "character-select") { resetCharacterCamera(); startCharacterAnimation(); }
  else { clearCharacterEdgeIntent(); cancelAnimationFrame(characterFrame); characterFrame = 0; }
  if (nextView === "mainmenu") mainMenuVideo?.play().catch(() => {});
  else mainMenuVideo?.pause();
  // Keep screen changes inside this document. Even replaceState can cause an
  // embedding preview to navigate/remount and reset the first opened screen.
  return true;
}

bindSafeButtonClick(document.querySelectorAll("[data-open-view]"), button => {
  button.blur();
  setActiveView(button.dataset.openView);
});

setActiveView(window.location.hash.slice(1) || "mainmenu");

bindSafeButtonClick(document.querySelectorAll("[data-settings-return]"), () => setActiveView(settingsReturnView));

bindSafeButtonClick(document.querySelectorAll("[data-quality]"), button => {
  document.querySelectorAll("[data-quality]").forEach(item => {
    const selected = item === button;
    item.classList.toggle("active", selected);
    item.setAttribute("aria-pressed", String(selected));
  });
});
bindSafeButtonClick(document.querySelectorAll("[data-game-toggle]"), button => {
  const checked = button.getAttribute("aria-checked") !== "true";
  button.setAttribute("aria-checked", String(checked));
  button.querySelector("span").textContent = checked ? "开启" : "关闭";
});

window.addEventListener("pointermove", rememberCharacterPointer, { passive: true });
window.addEventListener("pointerout", event => {
  if (!event.relatedTarget) releaseCharacterPointer();
});
window.addEventListener("blur", releaseCharacterPointer);
window.addEventListener("resize", () => {
  if (activeView === "character-select") resetCharacterCamera();
});
characterSelectView?.addEventListener("wheel", zoomCharacterCamera, { passive: false });

bindSafeButtonClick(characterChoices, button => {
  showToast(`已选择：${button.dataset.characterChoice}`);
  setActiveView("exploration");
});

startCharacterAnimation();

mainMenuPress?.addEventListener("click", revealMainMenu);

bindSafeButtonClick(document.querySelectorAll("[data-exit-game]"), () => {
  if (!exitDialog || exitDialog.open) return;
  lockViewInput();
  exitDialogOpenedAt = performance.now();
  exitDialog.showModal();
});
document.querySelectorAll("[data-close-exit]").forEach(button => button.addEventListener("click", () => exitDialog?.close()));
exitDialog?.addEventListener("click", event => {
  if (event.target !== exitDialog || performance.now() - exitDialogOpenedAt <= 680) return;
  const rect = exitDialog.getBoundingClientRect();
  const outside = event.clientX < rect.left || event.clientX > rect.right
    || event.clientY < rect.top || event.clientY > rect.bottom;
  if (outside) exitDialog.close();
});
document.querySelector("[data-confirm-exit]")?.addEventListener("click", () => {
  exitDialog?.close();
  setActiveView("mainmenu");
  resetMainMenu();
  if (mainMenuVideo) mainMenuVideo.currentTime = 0;
});

const slotDialog = document.querySelector("#slot-dialog");
const dialogSlot = document.querySelector("#dialog-slot");
let slotDialogOpenedAt = 0;

bindSafeButtonClick(document.querySelectorAll("[data-slot]"), ticket => {
  if (!slotDialog || !dialogSlot) return;
  lockViewInput();
  dialogSlot.textContent = ticket.dataset.slot;
  slotDialogOpenedAt = performance.now();
  slotDialog.showModal();
});

document.querySelectorAll("[data-close-dialog]").forEach(button => {
  button.addEventListener("click", () => slotDialog?.close());
});

slotDialog?.addEventListener("click", event => {
  if (event.target === slotDialog && performance.now() - slotDialogOpenedAt > 680) slotDialog.close();
});

const SETTINGS_KEY = "northeast-disco.settings.v1";
const defaultSettings = { mode: "手动窗口", voice: 72, music: 54, sound: 84, quality: "enhanced", name: false, replay: false };
const ranges = [...document.querySelectorAll("input[data-audio-channel]")];
const modeButtons = [...document.querySelectorAll("[data-mode]")];
const settingsFeedback = document.querySelector("[data-settings-feedback]");
let appliedSettings = { ...defaultSettings };
try {
  const saved = JSON.parse(localStorage.getItem(SETTINGS_KEY) || "null");
  if (saved && typeof saved === "object") {
    if (modeButtons.some(button => button.dataset.mode === saved.mode)) appliedSettings.mode = saved.mode;
    for (const channel of ["voice", "music", "sound"]) {
      if (Number.isFinite(saved[channel])) appliedSettings[channel] = clamp(saved[channel], 0, 100);
    }
    if (["performance", "balanced", "enhanced"].includes(saved.quality)) appliedSettings.quality = saved.quality;
    for (const toggle of ["name", "replay"]) {
      if (typeof saved[toggle] === "boolean") appliedSettings[toggle] = saved[toggle];
    }
  }
} catch { /* Storage may be blocked or contain a malformed old setting. */ }

function paintRange(input) {
  input.style.setProperty("--level", `${input.value}%`);
  const output = input.closest("label")?.querySelector("output");
  if (output) output.value = input.value;
}
function paintSettings(settings) {
  modeButtons.forEach(button => {
    const selected = button.dataset.mode === settings.mode;
    button.classList.toggle("active", selected);
    button.setAttribute("aria-pressed", String(selected));
  });
  ranges.forEach(input => { input.value = settings[input.dataset.audioChannel]; paintRange(input); });
  document.querySelectorAll("[data-quality]").forEach(button => {
    const selected = button.dataset.quality === settings.quality;
    button.classList.toggle("active", selected);
    button.setAttribute("aria-pressed", String(selected));
  });
  document.querySelectorAll("[data-game-toggle]").forEach(button => {
    const checked = settings[button.dataset.gameToggle];
    button.setAttribute("aria-checked", String(checked));
    button.querySelector("span").textContent = checked ? "开启" : "关闭";
  });
}
function describeMode(mode) {
  return mode === "手动窗口" ? "手动窗口：沿用当前浏览器窗口尺寸。"
    : mode === "全屏模式" ? "全屏模式：应用后进入全屏；Esc 可退出。" : "自动窗口：按原始 16:9 比例适配可用空间。";
}
function settingsStatus(message) {
  const hasAudio = document.querySelector("audio, video");
  if (settingsFeedback) settingsFeedback.textContent = message + (hasAudio ? "" : " 当前页面没有播放音频。");
}
function applyAudioSettings() {
  document.querySelectorAll("audio, video").forEach(media => {
    const channel = media.dataset.audioChannel || "music";
    media.volume = (appliedSettings[channel] ?? appliedSettings.music) / 100;
  });
  window.dispatchEvent(new CustomEvent("disco:audio-settings", { detail: { ...appliedSettings } }));
}
function applyWindowLayout(mode) {
  // Display mode must never resize or reflow the original UI.
  stage.removeAttribute("data-window-mode");
  if (activeView === "character-select") resetCharacterCamera();
}
function persistSettings() {
  try { localStorage.setItem(SETTINGS_KEY, JSON.stringify(appliedSettings)); return true; }
  catch { return false; }
}
modeButtons.forEach(button => button.addEventListener("click", () => {
  modeButtons.forEach(item => {
    const selected = item === button;
    item.classList.toggle("active", selected);
    item.setAttribute("aria-pressed", String(selected));
  });
  settingsStatus(describeMode(button.dataset.mode) + " 尚未应用。");
}));
ranges.forEach(input => input.addEventListener("input", () => {
  paintRange(input);
  settingsStatus("音量已修改，点击应用设置保存。");
}));
document.querySelector("[data-reset-settings]")?.addEventListener("click", () => {
  paintSettings(defaultSettings);
  settingsStatus("已恢复默认值，点击应用设置生效。");
});
const saveSettingsButton = document.querySelector("[data-save-settings]");
saveSettingsButton?.addEventListener("click", async () => {
  const mode = modeButtons.find(button => button.classList.contains("active"))?.dataset.mode || "手动窗口";
  const next = {
    mode,
    ...Object.fromEntries(ranges.map(input => [input.dataset.audioChannel, Number(input.value)])),
    quality: document.querySelector("[data-quality].active")?.dataset.quality || defaultSettings.quality,
    ...Object.fromEntries([...document.querySelectorAll("[data-game-toggle]")].map(button => [button.dataset.gameToggle, button.getAttribute("aria-checked") === "true"]))
  };
  saveSettingsButton.disabled = true;
  try {
    if (mode === "全屏模式" && !document.fullscreenElement) {
      if (!document.documentElement.requestFullscreen) throw new Error("不支持全屏");
      await document.documentElement.requestFullscreen();
    } else if (mode !== "全屏模式" && document.fullscreenElement) {
      await document.exitFullscreen();
    }
    appliedSettings = next;
    applyWindowLayout(mode);
    applyAudioSettings();
    const saved = persistSettings();
    settingsStatus(saved ? "设置已应用并保存。" : "设置已应用，但浏览器不允许保存；刷新后会恢复默认值。");
    showToast(saved ? "设置已应用并保存" : "设置已应用，未能保存");
  } catch (error) {
    paintSettings(appliedSettings);
    console.warn("Window mode request failed:", error.name, error.message);
    settingsStatus("浏览器未允许切换窗口模式，原设置已保留。请激活页面后重试。");
  } finally { saveSettingsButton.disabled = false; }
});
document.addEventListener("fullscreenchange", () => {
  if (!document.fullscreenElement && appliedSettings.mode === "全屏模式") {
    appliedSettings.mode = "自动窗口";
    applyWindowLayout(appliedSettings.mode);
    paintSettings(appliedSettings);
    persistSettings();
    settingsStatus("已退出全屏，使用自动窗口。");
  }
});
const wantsFullscreen = appliedSettings.mode === "全屏模式";
applyWindowLayout(wantsFullscreen ? "自动窗口" : appliedSettings.mode);
paintSettings(appliedSettings);
applyAudioSettings();
settingsStatus(wantsFullscreen ? "全屏偏好已保存，点击应用设置进入全屏。" : describeMode(appliedSettings.mode));

document.querySelectorAll("[data-empty-control]").forEach(button => button.addEventListener("click", () => showToast("暂无可回览记录")));
document.querySelector("[data-empty-profile]")?.addEventListener("click", () => showToast("当前人物档案为空"));

document.querySelectorAll("[data-clue-note]").forEach(note => {
  note.addEventListener("click", () => {
    document.querySelectorAll("[data-clue-note]").forEach(item => item.classList.toggle("selected", item === note));
    showToast(`已标记线索：${note.childNodes[0]?.textContent?.trim() || "线索"}`);
  });
});

bindSafeButtonClick(document.querySelectorAll("[data-end-exploration]"), () => {
  setActiveView("entry");
});

document.querySelectorAll("[data-explorer-inc]").forEach(button => {
  button.addEventListener("click", () => {
    const increase = Number(button.dataset.explorerInc) || 0;
    document.querySelectorAll("[data-explorer-stat]").forEach(stat => {
      stat.textContent = String(Number(stat.textContent) + increase);
    });
    showToast(`人物属性已更新 +${increase}`);
  });
});

const dossierTabs = [...document.querySelectorAll(".explorer-dossier-tabs button")];
function selectDossierTab(tab) {
  dossierTabs.forEach(item => {
    const selected = item === tab;
    item.classList.toggle("active", selected);
    item.setAttribute("aria-selected", String(selected));
    item.tabIndex = selected ? 0 : -1;
    document.getElementById(item.getAttribute("aria-controls")).hidden = !selected;
  });
  document.querySelector("[data-attribute-actions]").hidden = tab.id !== "tab-attribute";
}
dossierTabs.forEach((tab, index) => {
  tab.addEventListener("click", () => selectDossierTab(tab));
  tab.addEventListener("keydown", event => {
    let next;
    if (["ArrowRight", "ArrowDown"].includes(event.key)) next = (index + 1) % dossierTabs.length;
    if (["ArrowLeft", "ArrowUp"].includes(event.key)) next = (index + dossierTabs.length - 1) % dossierTabs.length;
    if (event.key === "Home") next = 0;
    if (event.key === "End") next = dossierTabs.length - 1;
    if (next === undefined) return;
    event.preventDefault();
    selectDossierTab(dossierTabs[next]);
    dossierTabs[next].focus();
  });
});

const dialogueShell = document.querySelector("[data-dialogue-shell]");
const dialogueCopy = document.querySelector("[data-dialogue-copy]");
const dialogueMood = document.querySelector("[data-dialogue-mood]");
const dialogueChoices = [...document.querySelectorAll("[data-dialogue-choice]")];
const dialogueClose = document.querySelector("[data-dialogue-close]");
const dialogueReopen = document.querySelector("[data-dialogue-reopen]");
let dialogueResponseTimer;

function selectDialogueChoice(button) {
  if (!button || dialogueShell?.classList.contains("is-closed")) return;
  window.clearTimeout(dialogueResponseTimer);
  dialogueChoices.forEach(item => item.setAttribute("aria-pressed", String(item === button)));
  dialogueCopy?.classList.add("is-changing");
  dialogueResponseTimer = window.setTimeout(() => {
    if (dialogueCopy) dialogueCopy.textContent = button.dataset.response ?? "对话内容尚未载入。";
    if (dialogueMood) dialogueMood.textContent = button.dataset.mood ?? "戒备中";
    dialogueCopy?.classList.remove("is-changing");
  }, 130);
}

function closeSurvivalDialogue() {
  window.clearTimeout(dialogueResponseTimer);
  dialogueCopy?.classList.remove("is-changing");
  dialogueShell?.classList.add("is-closed");
  dialogueShell?.setAttribute("aria-hidden", "true");
  if (dialogueShell) dialogueShell.inert = true;
  if (dialogueReopen) {
    dialogueReopen.hidden = false;
    dialogueReopen.focus();
  }
}

function openSurvivalDialogue() {
  dialogueShell?.classList.remove("is-closed");
  dialogueShell?.setAttribute("aria-hidden", "false");
  if (dialogueShell) dialogueShell.inert = false;
  if (dialogueReopen) dialogueReopen.hidden = true;
  dialogueChoices[0]?.focus();
}

dialogueChoices.forEach(button => button.addEventListener("click", () => selectDialogueChoice(button)));
dialogueClose?.addEventListener("click", closeSurvivalDialogue);
dialogueReopen?.addEventListener("click", openSurvivalDialogue);

document.addEventListener("keydown", event => {
  if (activeView === "character-select") {
    const cameraKeys = {
      ArrowLeft: [1, 0],
      ArrowRight: [-1, 0],
      ArrowUp: [0, 1],
      ArrowDown: [0, -1]
    };
    if (cameraKeys[event.key]) {
      nudgeCharacterCamera(...cameraKeys[event.key]);
      event.preventDefault();
      return;
    }
    if (event.key === "Home") {
      resetCharacterCamera();
      event.preventDefault();
      return;
    }
  }

  if (activeView === "mainmenu" && !mainMenuRevealed && !exitDialog?.open) {
    revealMainMenu();
    event.preventDefault();
    return;
  }

  if (activeView === "dialogue") {
    const dialogueIndex = Number(event.key) - 1;
    if (dialogueIndex >= 0 && dialogueIndex < dialogueChoices.length && !dialogueShell?.classList.contains("is-closed")) {
      selectDialogueChoice(dialogueChoices[dialogueIndex]);
      dialogueChoices[dialogueIndex].focus();
      return;
    }
    if (event.key === "Escape" && !dialogueShell?.classList.contains("is-closed")) {
      closeSurvivalDialogue();
      return;
    }
  }
  if (event.key !== "Escape" || slotDialog?.open || exitDialog?.open) return;
  if (activeView === "mainmenu" && mainMenuRevealed) resetMainMenu();
  else if (activeView === "settings") setActiveView(settingsReturnView);
  else if (activeView === "character-select") setActiveView("mainmenu");
  else if (activeView === "explorer-dossier") setActiveView("exploration");
  else if (activeView === "entry") setActiveView("mainmenu");
  else if (activeView !== "mainmenu") setActiveView("entry");
});
