export const BOX_SAVE_KEY='northeast-disco:box-opening:v1';
export const COLS=5, ROWS=4;
export const objects=[
  {id:'tin',name:'旧铁皮盒',x:0,y:0,w:2,h:2},
  {id:'book',name:'旧账本',x:2,y:0,w:1,h:2},
  {id:'case',name:'眼镜盒',x:3,y:0,w:2,h:1},
  {id:'cloth',name:'叠好的旧围巾',x:0,y:2,w:2,h:1},
  {id:'torch',name:'手电筒',x:2,y:2,w:1,h:2},
  {id:'gloves',name:'旧手套',x:3,y:2,w:2,h:1},
  {id:'tape',name:'磁带',x:3,y:3,w:2,h:1},
];
export const freshPuzzle=()=>({version:1,pieces:objects.map(o=>({...o})),moves:0,taken:false});
export function canMove(pieces,id,dx,dy){
  const p=pieces.find(p=>p.id===id);
  if(!p||Math.abs(dx)+Math.abs(dy)!==1)return false;
  const x=p.x+dx,y=p.y+dy;
  return x>=0&&y>=0&&x+p.w<=COLS&&y+p.h<=ROWS&&!pieces.some(q=>q.id!==id&&x<q.x+q.w&&x+p.w>q.x&&y<q.y+q.h&&y+p.h>q.y);
}
export const canRemove=p=>Boolean(p&&p.x+p.w===COLS&&p.y>=1&&p.y+p.h<=3);
export function act(state,id,dx=0,dy=0,remove=false){
  const p=state.pieces.find(p=>p.id===id);
  if(state.taken||!(remove?canRemove(p):canMove(state.pieces,id,dx,dy)))return state;
  return {...state,moves:state.moves+1,pieces:remove?state.pieces.filter(q=>q.id!==id):state.pieces.map(q=>q.id===id?{...q,x:q.x+dx,y:q.y+dy}:q)};
}
export function validPuzzle(s){
  if(s?.version!==1||!Array.isArray(s.pieces)||!Number.isInteger(s.moves)||s.moves<0||typeof s.taken!=='boolean'||s.taken&&s.pieces.length)return false;
  const cells=new Set(),ids=new Set();
  for(const p of s.pieces){
    const original=objects.find(o=>o.id===p.id);
    if(!original||ids.has(p.id)||p.w!==original.w||p.h!==original.h||!Number.isInteger(p.x)||!Number.isInteger(p.y)||p.x<0||p.y<0||p.x+p.w>COLS||p.y+p.h>ROWS)return false;
    ids.add(p.id);
    for(let y=p.y;y<p.y+p.h;y++)for(let x=p.x;x<p.x+p.w;x++){const key=x+','+y;if(cells.has(key))return false;cells.add(key);}
  }
  return true;
}
