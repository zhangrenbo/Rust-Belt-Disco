// Display authored text in measured single lines without rewriting the source.
export function splitDialogueLines(text, width, measure) {
  const lines = [];
  const limit = Math.max(1, width);
  for (const match of text.matchAll(/[^\r\n]+/gu)) {
    const glyphs = [...match[0]];
    let cursor = 0, offset = match.index;
    while (cursor < glyphs.length) {
      let end = cursor, preferred = cursor;
      while (end < glyphs.length) {
        const candidate = glyphs.slice(cursor, end + 1).join('');
        if (end > cursor && measure(candidate) > limit) break;
        end++;
        if (/[，、；：,;:\s]/u.test(glyphs[end - 1])) preferred = end;
        if (/[。！？!?]/u.test(glyphs[end - 1])) {
          // Keep closing quotes with their sentence when they fit.
          while (end < glyphs.length && /[”’」』）】]/u.test(glyphs[end]) &&
            measure(glyphs.slice(cursor, end + 1).join('')) <= limit) end++;
          break;
        }
      }
      if (end < glyphs.length && !/[。！？!?」』”’）】]/u.test(glyphs[end - 1]) && preferred > cursor) end = preferred;
      // Avoid opening a line with punctuation; move its preceding glyph with it.
      while (end > cursor + 1 && /^[，。！？；：、,.!?;:」』”’）】…]$/u.test(glyphs[end] ?? '')) end--;
      const line = glyphs.slice(cursor, end).join('');
      lines.push({text: line, start: offset, end: offset + line.length});
      offset += line.length; cursor = end;
    }
  }
  return lines;
}

export function installDialogueReader(talk) {
  const copy = talk.querySelector('.station-dialogue-copy');
  const text = talk.querySelector('#station-dialogue-text');
  const choices = talk.querySelector('#station-choices');
  const next = talk.querySelector('#station-line-next');
  const hint = talk.querySelector('.disco-continue');
  const measure = document.createElement('span');
  measure.className = 'station-dialogue-measure';
  measure.setAttribute('aria-hidden', 'true'); copy.append(measure);
  let current = null, source = '', lines = [], index = 0;

  function paint() {
    text.textContent = lines[index]?.text ?? '';
    const more = index < lines.length - 1;
    const canContinue = Boolean(current?.onContinue);
    next.hidden = !more && !canContinue;
    next.textContent = more ? '下一行' : '继续';
    choices.hidden = more || !current?.choices?.length;
    for (const button of choices.querySelectorAll('button')) button.disabled = more;
    hint.textContent = more || canContinue ? '点击正文或按空格继续' : '选择你的回应 · 数字键 1–4';
    copy.setAttribute('aria-label', more || canContinue ? '对话正文，点击或按空格显示下一行' : '对话正文');
    talk.dataset.dialogueLine = String(index + 1);
    talk.dataset.dialogueLines = String(lines.length);
  }
  function layout() {
    if (!current || !talk.open) return;
    const start = lines[index]?.start ?? 0;
    const width = copy.clientWidth - 2;
    // Reflow only unread text. Resizing cannot skip a word or replay earlier lines.
    lines = splitDialogueLines(source.slice(start), width, value => {
      measure.textContent = value;
      return measure.getBoundingClientRect().width;
    }).map(line => ({...line, start: line.start + start, end: line.end + start}));
    index = 0; paint();
  }
  function focus() {
    (index < lines.length - 1 || current?.onContinue ? copy : choices.querySelector('button'))?.focus({preventScroll: true});
  }
  function advance() {
    if (!talk.open || !current) return false;
    if (index >= lines.length - 1) {
      const run = current.onContinue;
      if (!run) return false;
      current = null; // Consume before a callback can render the next dialogue.
      run(); return true;
    }
    index++; paint(); focus(); return true;
  }
  copy.addEventListener('click', event => {if (event.detail <= 1) advance();});
  next.addEventListener('click', event => {if (event.detail <= 1) advance();});
  copy.addEventListener('keydown', event => {
    if (event.repeat || event.altKey || event.ctrlKey || event.metaKey) return;
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault(); event.stopPropagation(); advance();
    }
  });
  if (window.ResizeObserver) new window.ResizeObserver(layout).observe(copy);
  document.fonts?.ready.then(layout);
  return {
    reset() {current = null; source = ''; lines = []; index = 0;},
    show(dialogue) {
      current = dialogue;
      source = [dialogue.feedback, dialogue.text].filter(Boolean).join('\n\n');
      lines = []; index = 0;
      choices.replaceChildren();
      for (const [i, choice] of (dialogue.choices ?? []).entries()) {
        const row = document.createElement('li');
        row.className = `disco-choice ${['choice-normal','choice-intense','choice-weathered','choice-rare'][i]}`;
        const button = document.createElement('button'); button.type = 'button';
        button.dataset.choiceIndex = String(i); button.dataset.key = String(i + 1);
        const label = document.createElement('span'); label.textContent = choice.text; button.append(label);
        if (choice.hint) {const small = document.createElement('small'); small.textContent = choice.hint; button.append(small);}
        button.addEventListener('click', event => {if (event.detail <= 1 && !choices.hidden) choice.run();});
        row.append(button); choices.append(row);
      }
      if (!talk.open) talk.showModal();
      layout(); focus();
    },
    resume() {
      if (!current) return false;
      if (!talk.open) talk.showModal();
      layout(); focus(); return true;
    }
  };
}
