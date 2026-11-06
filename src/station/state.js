import {questions, storyNodes, ideologies, attributes} from './story.js';

export const SAVE_KEY='northeast-disco:campaign:v1';
export const initialPosition={x:440,y:640};
const nodes=new Set([...Object.keys(storyNodes),...Object.keys(questions),'explore','ready','complete']);
export function newGame(){return {version:1,character:'dong-dianshi',scene:'A000',node:'arrival',answers:{},inspected:[],position:{...initialPosition},warningHeard:false,shadow:null,completed:false,updatedAt:new Date().toISOString()};}
export function transition(previous,action){
  const state=structuredClone(previous);
  if(action.type==='inspect') {
    if(!['sign','bench','left'].includes(action.id))return previous;
    if(!state.inspected.includes(action.id))state.inspected.push(action.id);
  } else if(action.type==='talk') {
    if(state.node==='explore')state.node='arrival';else return previous;
  } else if(action.type==='answer') {
    const question=questions[state.node];
    if(!question || !question.choices.some(c=>c.id===action.id))return previous;
    state.answers[state.node]=action.id;
    state.node=question.next;
  } else if(action.type==='next') {
    const node=storyNodes[state.node];
    const choice=node?.choices[action.index];
    if(!choice?.to)return previous;
    if(state.node==='warning')state.warningHeard=true;
    state.node=choice.to;
  } else if(action.type==='shadow') {
    if(state.node!=='shadow' || !Object.hasOwn(ideologies,action.id))return previous;
    state.shadow=action.id;state.node='ready';
  } else if(action.type==='exit') {
    if(state.node!=='ready' || !state.warningHeard || !state.shadow)return previous;
    state.node='complete';state.completed=true;
  } else return previous;
  state.updatedAt=new Date().toISOString();
  return state;
}
export function validateSave(value){
  if(!value || value.version!==1 || value.character!=='dong-dianshi' || value.scene!=='A000' || !nodes.has(value.node))throw new Error('存档格式或版本不受支持');
  if(!value.answers || !Array.isArray(value.inspected) || value.inspected.some(id=>!['sign','bench','left'].includes(id)))throw new Error('存档记录损坏');
  const keys=Object.keys(questions);
  for(const [key,answer]of Object.entries(value.answers))if(!questions[key]?.choices.some(c=>c.id===answer))throw new Error('存档选择损坏');
  const required=value.node==='value'?1:value.node==='fear'?2:['warning','doubt','alive','mother','dont-turn-left','shadow-intro','shadow','ready','complete'].includes(value.node)?3:0;
  if(keys.slice(0,required).some(k=>!value.answers[k]) || Object.keys(value.answers).length!==required)throw new Error('存档进度与选择不一致');
  const afterWarning=['doubt','alive','mother','dont-turn-left','shadow-intro','shadow','ready','complete'].includes(value.node);
  if(value.warningHeard!==afterWarning)throw new Error('预言记录与进度不一致');
  if(['ready','complete'].includes(value.node)?!Object.hasOwn(ideologies,value.shadow):value.shadow!==null)throw new Error('黑影记录损坏');
  if(value.completed!==(value.node==='complete'))throw new Error('场景完成标记损坏');
  if(!Number.isFinite(value.position?.x)||!Number.isFinite(value.position?.y))throw new Error('角色位置损坏');
  return {...structuredClone(value),answers:{...value.answers},inspected:[...new Set(value.inspected)]};
}
export function readSlot(storage,slot='auto'){
  try{const raw=storage.getItem(`${SAVE_KEY}:${slot}`);return raw?{state:validateSave(JSON.parse(raw)),error:null}:{state:null,error:null};}
  catch(error){return {state:null,error:error.message||'无法读取存档'};}
}
export function writeSlot(storage,state,slot='auto'){
  try{storage.setItem(`${SAVE_KEY}:${slot}`,JSON.stringify(validateSave(state)));return true;}catch{return false;}
}
export function journal(state){
  return {stats:attributes(state.answers),notes:[
    ...(state.inspected.includes('sign')?['站牌：这里是返乡时停留的公交车站。']:[]),
    ...(state.warningHeard?['待核实的预言：老太太梦见凶手回到镇上，将停留七天。梦境不是已经证实的案件证据。']:[]),
    ...(state.shadow?[`黑影：我把它认作「${ideologies[state.shadow]}」。`]:[]),
  ],objective:state.completed?'车站这一程已经结束。':state.node==='ready'?'沿右侧道路离开车站。':['arrival','explore'].includes(state.node)?'点击灯下的老奶奶，与她交谈。':'继续这段对话。'};
}
