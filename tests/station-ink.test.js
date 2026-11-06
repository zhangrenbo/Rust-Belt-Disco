import test from 'node:test';
import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import {storyNodes,questions} from '../src/station/story.js';
import {newGame,transition,validateSave} from '../src/station/state.js';

const ink=await readFile(new URL('../content/story/ink/D0_序章.ink',import.meta.url),'utf8');
const station=ink.split('序章：A000 任务名称：不要左转')[1].split('- (返回)')[0];
const lines=station.split(/\r?\n/).map(line=>line.trim());

test('A000 dialogue preserves the exact INK prose, speaker and opening order',()=>{
  let state=newGame(),lastSourceLine=-1;
  for(const [id,speaker] of [['arrival','老太太'],['greeting','董点事'],['greeting-memory','老太太'],['greeting-question','老太太']]){
    assert.equal(state.node,id);
    const node=storyNodes[id];
    assert.equal(node.speaker,speaker);
    const sourceLine=lines.indexOf(node.text);
    assert.ok(sourceLine>lastSourceLine,`${id}: complete original line in source order`);
    lastSourceLine=sourceLine;
    state=transition(state,{type:'next',index:0});validateSave(state);
  }
  assert.equal(state.node,'background');
  for(const [id,node] of Object.entries(storyNodes)){
    for(const paragraph of node.text.split('\n\n'))assert.ok(lines.includes(paragraph),`${id}: no rewritten, omitted or invented speech`);
    for(const choice of node.choices){
      if(choice.text!=='继续')assert.ok(lines.includes('*'+choice.text),`${id}: original authored choice`);
    }
  }
  const memory=['shadow-intro','shadow'].flatMap(id=>storyNodes[id].text.split('\n\n')).map(line=>lines.indexOf(line));
  assert.deepEqual(memory,[...memory].sort((a,b)=>a-b),'shadow monologue follows INK ordering');
  assert.ok(storyNodes['greeting-memory'].text.includes('孩子，我们都在等你'));
  assert.ok(!storyNodes.arrival.text.includes('你最终还是回到了这里'));
});

test('three questions and all replies remain grounded in the later INK specification',()=>{
  const specification=station.split('奶奶的第一问——职业背景')[1];
  for(const question of Object.values(questions)){
    assert.ok(specification.includes('奶奶：「'+question.text+'」'));
    for(const choice of question.choices){
      // Existing UI adds a terminal full stop to otherwise verbatim answer labels.
      assert.ok(specification.includes('*「'+choice.text.replace(/。$/,'')+'」'));
      assert.ok(specification.includes('「'+choice.reply+'」'));
    }
  }
});
