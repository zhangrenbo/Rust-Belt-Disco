(async () => {
  const s = window.__ndMJ;
  if (!s?.members?.length || !s.folder) throw Error('Missing verified candidates/folder');
  const key = i => i.id + '@' + i.index;
  const current = new Set(s.folder.images.map(key));
  const batch = s.members.filter(i => !current.has(key(i))).slice(0, 250);
  if (!batch.length) return {complete:true,verifiedCount:current.size};
  const name = Array.from(document.querySelectorAll('div')).find(e => e.textContent === s.folder.title);
  if (!name) throw Error('Folder target is not visible');
  const target = name.parentElement.parentElement;
  const props = target[Object.keys(target).find(k=>k.startsWith('__reactProps'))];
  if (typeof props?.onDrop !== 'function') throw Error('Folder target has no drop handler');
  const dt = new DataTransfer();
  dt.setData('application/json/internal', JSON.stringify({type:'image-job-batch',items:batch.map(i=>({jobId:i.id,index:i.index,kind:'image',url:'https://cdn.midjourney.com/'+i.id+'/0_'+i.index+'.png'}))}));
  const responseStart = s.collectionResponses.length;
  target.dispatchEvent(new DragEvent('drop',{bubbles:true,cancelable:true,dataTransfer:dt}));
  const deadline = Date.now()+15000;
  while(s.collectionResponses.length===responseStart && Date.now()<deadline) await new Promise(r=>setTimeout(r,150));
  const mutationResponses = s.collectionResponses.slice(responseStart);
  const response = await fetch('/api/folders',{headers:s.headers,signal:AbortSignal.timeout(15000)});
  if(!response.ok) throw Error('Membership verification failed: '+response.status);
  const folders = await response.json();
  const folder = folders.find(f=>f.id===s.folder.id);
  if(!folder) throw Error('Folder missing after add');
  const after = new Set(folder.images.map(key));
  const missing = batch.filter(i=>!after.has(key(i)));
  const expected = new Set(s.members.map(key));
  const unexpected = folder.images.filter(i=>!expected.has(key(i)));
  const otherFolderChanges = [];
  for(const original of s.originalFolders.filter(f=>f.id!==folder.id)) {
    const actual = folders.find(f=>f.id===original.id);
    const beforeSet = new Set(original.images.map(key));
    if(!actual || actual.images.length!==original.images.length || actual.images.some(i=>!beforeSet.has(key(i)))) otherFolderChanges.push(original.title);
  }
  s.folder = folder;
  s.receipts ??= [];
  const receipt = {time:new Date().toISOString(),requested:batch.length,verifiedCount:after.size,remaining:s.members.filter(i=>!after.has(key(i))).length,missing:missing.length,unexpected:unexpected.length,otherFolderChanges,statuses:mutationResponses.map(r=>r.status)};
  s.receipts.push(receipt);
  if(missing.length||unexpected.length||otherFolderChanges.length) throw Error(JSON.stringify(receipt));
  return receipt;
})()
