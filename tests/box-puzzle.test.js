import test from 'node:test';
import assert from 'node:assert/strict';
import {freshPuzzle,validPuzzle,act,canRemove} from '../src/box/puzzle.js';

// Breadth-first search for each next extractable object proves a real legal route.
export function solveBox(initial){
 let state=initial,solution=[];
 while(state.pieces.length){
  const queue=[{state,path:[]}],seen=new Set();let found;
  for(let i=0;i<queue.length&&i<100000;i++){
   const current=queue[i],p=current.state.pieces.find(canRemove);
   if(p){const action=[p.id,0,0,true];found={state:act(current.state,...action),path:[...current.path,action]};break;}
   for(const piece of current.state.pieces)for(const [dx,dy]of [[1,0],[-1,0],[0,1],[0,-1]]){
    const next=act(current.state,piece.id,dx,dy);if(next===current.state)continue;
    const key=next.pieces.map(p=>`${p.id}:${p.x},${p.y}`).join('|');if(seen.has(key))continue;seen.add(key);
    queue.push({state:next,path:[...current.path,[piece.id,dx,dy,false]]});
   }
  }
  assert.ok(found,'all remaining objects must have a route to the opening');state=found.state;solution.push(...found.path);
 }
 return solution;
}
test('all seven objects can leave through the gap without overlap or crossing walls',()=>{
 let state=freshPuzzle();assert.ok(validPuzzle(state));
 const solution=solveBox(state);assert.ok(solution.length>7);
 for(const action of solution){const next=act(state,...action);assert.notEqual(next,state);assert.ok(validPuzzle(next));state=next;}
 assert.equal(state.pieces.length,0);assert.ok(validPuzzle({...state,taken:true}));
});
test('blocked moves, premature extraction and malformed saves cannot bypass the puzzle',()=>{
 const state=freshPuzzle();assert.equal(act(state,'tin',1,0),state);assert.equal(act(state,'tin',0,0,true),state);
 assert.equal(act(state,'tin',-1,0),state);assert.equal(act(state,'tin',2,0),state);
 assert.equal(validPuzzle({...state,taken:true}),false);
 assert.equal(validPuzzle({...state,pieces:[state.pieces[0],state.pieces[0]]}),false);
});
