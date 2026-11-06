import {findPath} from '../core/navigation.js';
export const WORLD={width:1600,height:900,cell:20};
// Authored against the final background. The ground is a 2D polygon, with a
// radius margin sampled around every cell so feet never cross its boundary.
export const groundPolygon=[[150,440],[530,365],[690,395],[1090,465],[1490,545],[1490,735],[420,735],[210,610]];
export const hotspots={
  grandmother:{name:'老奶奶',x:910,y:213,stand:{x:800,y:500}},
  sign:{name:'站牌',x:520,y:330,stand:{x:520,y:420}},
  bench:{name:'候车长椅',x:785,y:330,stand:{x:760,y:460}},
  left:{name:'左侧岔路',x:245,y:450,stand:{x:300,y:480}},
  exit:{name:'沿路进镇',x:1410,y:645,stand:{x:1380,y:660}},
};
export function inside(p,polygon=groundPolygon){
  let result=false;
  for(let i=0,j=polygon.length-1;i<polygon.length;j=i++){
    const a=polygon[i],b=polygon[j];
    if((a[1]>p.y)!==(b[1]>p.y)&&p.x<(b[0]-a[0])*(p.y-a[1])/(b[1]-a[1])+a[0])result=!result;
  }
  return result;
}
export const grid={width:81,height:46,blocked:[]};
for(let y=0;y<grid.height;y++)for(let x=0;x<grid.width;x++){
  const p={x:x*20,y:y*20};
  // The seated grandmother is inside the shelter, outside the ground polygon.
  if(![[0,0],[10,0],[-10,0],[0,10],[0,-10]].every(([dx,dy])=>inside({x:p.x+dx,y:p.y+dy})))grid.blocked.push({x,y});
}
const blocked=new Set(grid.blocked.map(p=>`${p.x},${p.y}`));
grid.blockedSet=blocked;
export function walkable(p){return Number.isInteger(p.x/20)&&Number.isInteger(p.y/20)&&p.x>=0&&p.y>=0&&p.x<=1600&&p.y<=900&&!blocked.has(`${p.x/20},${p.y/20}`);}
export function nearestGround(p){
  let best=null,bestDistance=Infinity;
  for(let y=0;y<grid.height;y++)for(let x=0;x<grid.width;x++){
    if(blocked.has(`${x},${y}`))continue;
    const point={x:x*20,y:y*20},distance=Math.hypot(point.x-p.x,point.y-p.y);
    if(distance<bestDistance){best=point;bestDistance=distance;}
  }
  return best;
}
export function routeBetween(start,goal){
  if(!walkable(start)||!walkable(goal))return null;
  return findPath(grid,{x:start.x/20,y:start.y/20},{x:goal.x/20,y:goal.y/20})?.map(p=>({x:p.x*20,y:p.y*20}))??null;
}
export class Walker{
  constructor(position){this.position={...position};this.anchor={...position};this.route=[];this.target=null;}
  go(goal,target=null){
    const anchor=this.route[0]??this.anchor;
    const path=routeBetween(anchor,goal);
    if(!path)return false;
    this.route=this.route.length?[this.route[0],...path.slice(1)]:path.slice(1);
    this.target=target;return true;
  }
  step(seconds){
    let remaining=Math.min(seconds,.1)*170;
    while(this.route.length&&remaining>0){
      const next=this.route[0],distance=Math.hypot(next.x-this.position.x,next.y-this.position.y);
      if(distance<=remaining){this.position={...next};this.anchor={...next};this.route.shift();remaining-=distance;}
      else{this.position.x+=(next.x-this.position.x)/distance*remaining;this.position.y+=(next.y-this.position.y)/distance*remaining;remaining=0;}
    }
    if(!this.route.length&&this.target){const target=this.target;this.target=null;return target;}
    return null;
  }
  stop(){this.position={...this.anchor};this.route=[];this.target=null;}
}
