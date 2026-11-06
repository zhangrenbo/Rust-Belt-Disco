import test from 'node:test';
import assert from 'node:assert/strict';
import {questions,attributes,baseStats} from '../src/station/story.js';
import {newGame,transition,validateSave,readSlot,writeSlot,journal} from '../src/station/state.js';
import {hotspots,routeBetween,walkable,Walker,inside} from '../src/station/navigation.js';

function advance(state,index=0){return transition(state,{type:'next',index});}
function toQuestions(){let s=advance(newGame());s=advance(s);s=advance(s);return advance(s);}
test('all 768 combinations of background/value/fear, warning reply and shadow reach A000 completion',()=>{
  let count=0;
  for(const background of questions.background.choices)for(const value of questions.value.choices)for(const fear of questions.fear.choices)for(const warningBranch of ['accept','alive','mother'])for(const shadow of ['freedom','equality','morality','self']){
    let s=toQuestions();assert.equal(s.node,'background');
    for(const c of [background,value,fear]){s=transition(s,{type:'answer',id:c.id});validateSave(s);}
    assert.equal(s.node,'warning');
    if(warningBranch==='accept')s=advance(s,1);else{s=advance(s);s=advance(s,warningBranch==='alive'?0:1);s=advance(s);}
    assert.equal(s.node,'dont-turn-left');s=advance(s);s=advance(s);assert.equal(s.node,'shadow');
    s=transition(s,{type:'shadow',id:shadow});assert.equal(s.node,'ready');
    const expected={...baseStats};for(const c of [background,value,fear])for(const [key,n]of Object.entries(c.effects))expected[key]+=n;
    assert.deepEqual(attributes(s.answers),expected);
    const old=structuredClone(s);assert.equal(transition(s,{type:'answer',id:background.id}),s);assert.deepEqual(s,old);
    s=transition(s,{type:'exit'});assert.equal(s.completed,true);validateSave(s);assert.equal(transition(s,{type:'exit'}),s);
    assert.match(journal(s).notes.join(' '),/不是已经证实的案件证据/);count++;
  }
  assert.equal(count,768);
});
test('exit and out-of-order choices cannot bypass story; source state is immutable',()=>{
  const s=newGame(),copy=structuredClone(s);
  for(const action of [{type:'exit'},{type:'shadow',id:'self'},{type:'answer',id:'builder'},{type:'next',index:99}])assert.equal(transition(s,action),s);
  assert.deepEqual(s,copy);
  assert.throws(()=>validateSave({...s,node:'complete',completed:true}));
  assert.throws(()=>validateSave({...s,answers:{background:'builder'}}));
  assert.throws(()=>validateSave({...s,shadow:'self'}));
  assert.throws(()=>validateSave({...s,position:{x:NaN,y:20}}));
  const legacyExplore=validateSave({...s,node:'explore'});
  assert.equal(transition(legacyExplore,{type:'talk'}).node,'arrival','existing exploration saves hear the first authored line');
});
test('every intermediate node can roundtrip through independent save slots',()=>{
  const map=new Map(),storage={getItem:k=>map.get(k)??null,setItem:(k,v)=>map.set(k,v)};
  let s=newGame();
  for(let i=0;i<4;i++){assert.equal(writeSlot(storage,s,'auto'),true);assert.deepEqual(readSlot(storage).state,s);s=advance(s);}
  assert.equal(writeSlot(storage,s,'01'),true);
  assert.equal(writeSlot(storage,newGame(),'auto'),true);
  assert.deepEqual(readSlot(storage,'01').state,s);
  assert.equal(readSlot(storage,'02').state,null);
  assert.equal(writeSlot({setItem(){throw Error('quota');}},s),false);
  assert.ok(readSlot({getItem(){return '{broken';}}).error);
  assert.ok(readSlot({getItem(){throw Error('blocked');}}).error);
});
test('all authored interaction positions are connected and avoid off-road cells',()=>{
  const start=newGame().position;
  assert.ok(walkable(start));
  for(const spot of Object.values(hotspots)){
    assert.ok(walkable(spot.stand),spot.name);
    const path=routeBetween(start,spot.stand);assert.ok(path?.length,spot.name);
    assert.deepEqual(path[0],start);assert.deepEqual(path.at(-1),spot.stand);
    for(let i=0;i<path.length;i++){assert.ok(walkable(path[i]));assert.ok(inside(path[i]));if(i)assert.equal(Math.abs(path[i].x-path[i-1].x)+Math.abs(path[i].y-path[i-1].y),20);}
  }
  assert.equal(routeBetween(start,{x:0,y:0}),null);
});
test('retargeting mid-step does not teleport or cross unwalkable ground; arrival fires once',()=>{
  const w=new Walker(newGame().position);
  w.go(hotspots.grandmother.stand,'grandmother');w.step(.04);const current={...w.position};
  assert.equal(w.go(hotspots.sign.stand,'sign'),true);assert.deepEqual(w.position,current);
  let arrival=[];
  for(let i=0;i<1000;i++){const hit=w.step(.016);if(hit)arrival.push(hit);assert.ok(inside(w.position));}
  assert.deepEqual(arrival,['sign']);assert.deepEqual(w.position,hotspots.sign.stand);
});
