const key = ({x, y}) => `${x},${y}`;
const distance = (a, b) => Math.abs(a.x - b.x) + Math.abs(a.y - b.y);
export const isWalkable = (map, p) => Number.isInteger(p.x) && Number.isInteger(p.y) && p.x >= 0 && p.y >= 0 && p.x < map.width && p.y < map.height && !(map.blockedSet ? map.blockedSet.has(key(p)) : map.blocked.some(b => b.x === p.x && b.y === p.y));

// Logical ground coordinates are strictly two-dimensional. Four neighbours
// prevent diagonal corner cutting. An unreachable destination returns null.
export function findPath(map, start, goal) {
  if (!isWalkable(map, start) || !isWalkable(map, goal)) return null;
  const open = [{...start, g:0, f:distance(start, goal)}];
  const costs = new Map([[key(start), 0]]);
  const parents = new Map();
  while (open.length) {
    open.sort((a,b) => a.f - b.f || a.g - b.g);
    const current = open.shift();
    if (current.g !== costs.get(key(current))) continue;
    if (key(current) === key(goal)) {
      const path = [{x:current.x, y:current.y}];
      while (parents.has(key(path[0]))) path.unshift(parents.get(key(path[0])));
      return path;
    }
    for (const [dx,dy] of [[1,0],[-1,0],[0,1],[0,-1]]) {
      const next = {x:current.x+dx, y:current.y+dy};
      const g = current.g + 1;
      if (!isWalkable(map,next) || g >= (costs.get(key(next)) ?? Infinity)) continue;
      parents.set(key(next), {x:current.x,y:current.y});
      costs.set(key(next), g);
      open.push({...next,g,f:g+distance(next,goal)});
    }
  }
  return null;
}

export function toScreen({x,y}, tileWidth=88, tileHeight=44) {
  return {x:(x-y)*tileWidth/2, y:(x+y)*tileHeight/2};
}
export function toGrid({x,y}, tileWidth=88, tileHeight=44) {
  return {x:Math.round(x/tileWidth+y/tileHeight), y:Math.round(y/tileHeight-x/tileWidth)};
}
