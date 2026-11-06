import test from 'node:test';
import assert from 'node:assert/strict';
import {splitDialogueLines} from '../src/station/dialogue-reader.js';
import {storyNodes, questions} from '../src/station/story.js';

const measure = text => [...text].reduce((width, glyph) => width + (/\p{Script=Han}/u.test(glyph) ? 20 : 10), 0);
test('single-line dialogue preserves every authored character at narrow and wide widths', () => {
  const texts = [...Object.values(storyNodes).map(node => node.text),
    ...Object.values(questions).flatMap(q => [q.text, ...q.choices.map(c => c.reply)]),
    '「这段话里有引号。」还有标点，长句......不会被删除。\n\n第二段 🙂',
    'English words and 中文组合，不能丢掉空格。'];
  for (const source of texts) for (const width of [80, 280, 570, 820]) {
    const lines = splitDialogueLines(source, width, measure);
    assert.equal(lines.map(line => line.text).join(''), source.replace(/[\r\n]/g, ''));
    for (const line of lines) {
      assert.ok(measure(line.text) <= width, `line exceeds ${width}: ${line.text}`);
      assert.equal(source.slice(line.start, line.end), line.text);
    }
  }
});

test('reflowing from a current line preserves all remaining words', () => {
  const source = storyNodes.warning.text;
  const wide = splitDialogueLines(source, 600, measure);
  const start = wide[1].start;
  const narrow = splitDialogueLines(source.slice(start), 200, measure);
  assert.equal(narrow.map(line => line.text).join(''), source.slice(start).replace(/[\r\n]/g, ''));
});
