// Character records for the investigation game, using existing portraits and story sources.
import {emitUiSound} from './ui-sounds.js';

const people = {
  dong: {name: '董点事', role: '返乡青年', place: '公交车站', note: '在外打工三年，回到了小镇。故事从返乡的公交车站开始。', portrait: 'assets/disco-dialog-portrait-frame.png', face: 'media/characters/dong-reference.png'},
  li: {name: '李正', role: '镇上警官', place: '李正家', note: '镇上的警官。从家中出发，在出警与调查中走进小镇的故事。', portrait: 'assets/dongbei-portrait-li-zheng.png'},
  wang: {name: '王少鹏', role: '诊所经营者', place: '家中／诊所', note: '经营着镇上的诊所。他的一天，从家中与诊所开始。', portrait: 'assets/dossier-wang-shaopeng.png'},
};

export function installDossierCharacters({document}) {
  const book = document.querySelector('.explorer-dossier-book');
  if (!book) return;
  const tabs = [...book.querySelectorAll('[data-dossier-person]')];
  const portrait = book.querySelector('[data-dossier-portrait]');
  const face = book.querySelector('[data-dossier-face]');
  const paper = book.querySelector('#dossier-record');
  const handwriting = paper.querySelector('[data-dossier-handwriting]');
  let handwritingRevision = 0;
  function showHandwriting(id) {
    if (!handwriting) return;
    const revision = ++handwritingRevision;
    paper.classList.remove('has-handwritten-ink');
    handwriting.hidden = true;
    handwriting.src = `assets/dossier-handwriting-${id}-v1.png`;
    handwriting.decode().then(() => {
      if (revision !== handwritingRevision) return;
      handwriting.hidden = false;
      paper.classList.add('has-handwritten-ink');
    }).catch(() => {
      // The original, accessible record remains visible when ink cannot load.
    });
  }
  function select(tab) {
    const id = tab.dataset.dossierPerson, person = people[id];
    if (!person || tab.disabled) return;
    if (book.dataset.dossierCharacter !== id) emitUiSound(document, 'slip');
    book.dataset.dossierCharacter = id;
    book.setAttribute('aria-label', `${person.name}的人物档案`);
    book.querySelector('[data-dossier-name]').textContent = person.name;
    book.querySelector('[data-dossier-role]').textContent = person.role;
    book.querySelector('[data-dossier-place]').textContent = person.place;
    book.querySelector('[data-dossier-note]').textContent = person.note;
    showHandwriting(id);
    portrait.src = person.portrait;
    portrait.alt = person.face ? '' : `${person.name}的头像`;
    portrait.setAttribute('aria-hidden', String(Boolean(person.face)));
    face.hidden = !person.face;
    if (person.face) { face.src = person.face; face.alt = `${person.name}的头像`; }
    for (const button of tabs) {
      const active = button === tab;
      button.classList.toggle('active', active);
      button.setAttribute('aria-selected', String(active));
      button.tabIndex = active ? 0 : -1;
    }
    paper.setAttribute('aria-labelledby', tab.id);
  }
  tabs.forEach(tab => {
    tab.addEventListener('click', () => select(tab));
    tab.addEventListener('keydown', event => {
      const available = tabs.filter(button => !button.disabled);
      const index = available.indexOf(tab);
      let next;
      if (['ArrowDown', 'ArrowRight'].includes(event.key)) next = (index + 1) % available.length;
      if (['ArrowUp', 'ArrowLeft'].includes(event.key)) next = (index + available.length - 1) % available.length;
      if (event.key === 'Home') next = 0;
      if (event.key === 'End') next = available.length - 1;
      if (next === undefined) return;
      event.preventDefault();
      select(available[next]); available[next].focus();
    });
  });
  showHandwriting(book.dataset.dossierCharacter || 'dong');
}
