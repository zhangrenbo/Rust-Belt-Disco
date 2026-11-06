import {questions,storyNodes,statNames} from './story.js';
import {newGame,transition,readSlot,writeSlot,journal} from './state.js';
import {hotspots,Walker,nearestGround,walkable,WORLD} from './navigation.js';
import {installDialogueReader} from './dialogue-reader.js';

export function installStation(ui){
  const $=id=>document.getElementById(id);
  const stage=$('app'),view=document.querySelector('[data-view="station"]');
  const world=$('station-world'),talk=$('station-talk');
  const dialogueReader=installDialogueReader(talk);
  let storage;
  try{storage=window.localStorage;}catch{storage=null;}
  let state=null,walker=null,frame=0,lastTime=0,selectedSlot='01',lastStorageError=false;
  let lastKeyboardMove=0;
  const continueButton=document.createElement('button');
  continueButton.type='button';continueButton.className='mainmenu-choice mainmenu-choice-continue';continueButton.hidden=true;
  const continueArt=document.createElement('img');
  continueArt.src='assets/main-menu-continue.png';continueArt.width=222;continueArt.height=67;continueArt.alt='继续游戏';
  continueButton.append(continueArt);
  document.querySelector('.mainmenu-actions').prepend(continueButton);

  function status(message){$('station-status').textContent=message;}
  function closeDialogs(){document.querySelectorAll('dialog[open]').forEach(d=>d.close());}
  function hasModal(){return Boolean(document.querySelector('dialog[open]'));}
  function canMove(){return state&&ui.getActiveView()==='station'&&!hasModal()&&!state.completed&&!view.classList.contains('is-memory');}
  function refreshContinue(){
    const saved=readSlot(storage);
    const available=Boolean(saved.state)||ui.boxOpening.hasSave();
    continueButton.hidden=!available;
    continueButton.parentElement.classList.toggle('has-continue',available);
  }
  function save(slot='auto'){
    if(!state)return false;
    state.position={...walker.anchor};
    state.updatedAt=new Date().toISOString();
    const ok=writeSlot(storage,state,slot);
    if(!ok){lastStorageError=true;status('浏览器未能保存。请保持页面打开，并在存档页重试。');ui.showToast('保存失败，当前进度仍在本次会话中');}
    else{lastStorageError=false;refreshContinue();refreshSlots();}
    return ok;
  }
  function commit(action){
    const next=transition(state,action);
    if(next===state)return false;
    state=next;save();paint();return true;
  }
  function place(element,p){element.style.left=`${p.x/WORLD.width*100}%`;element.style.top=`${p.y/WORLD.height*100}%`;element.style.zIndex=String(Math.round(p.y));}
  function paintActor(){
    place($('station-player'),walker.position);
    place($('station-shadow'),{x:walker.position.x+130,y:walker.position.y-25});
    $('station-player-vision').setAttribute('cx',walker.position.x);
    $('station-player-vision').setAttribute('cy',walker.position.y-55);
    // Only nearby object labels can reveal themselves in the darkness.
    // The grandmother's silhouette remains clickable after the first conversation.
    for(const button of $('station-hotspots').children){
      const id=button.dataset.hotspot,spot=hotspots[id];
      const nearby=Math.hypot((spot.stand.x-walker.position.x)/235,(spot.stand.y-walker.position.y)/185)<1;
      button.hidden=id!=='grandmother'&&!nearby&&!(id==='exit'&&state.node==='ready');
    }
    const end=walker.route.at(-1),marker=$('station-destination');
    marker.toggleAttribute('hidden',!end);
    if(end){marker.setAttribute('cx',end.x);marker.setAttribute('cy',end.y);}
  }
  function paint(){
    if(!state)return;
    view.dataset.storyNode=state.node;
    const opening=['arrival','explore'].includes(state.node);
    view.classList.toggle('is-arrival',opening);
    $('station-grandmother-marker').hidden=!opening;
    const memory=['shadow-intro','shadow'].includes(state.node);
    view.classList.toggle('is-memory',memory);
    $('station-shadow').hidden=!memory&&!state.shadow;
    $('station-completion').hidden=!state.completed;
    $('station-talk-resume').hidden=state.completed||['arrival','explore','ready'].includes(state.node)||talk.open;
    paintActor();
  }
  // The opening enters fully black and only starts the slow reveal once the
  // night backdrop has loaded, so the scene never pops in from behind the fade.
  function beginOpeningFade(){
    const fade=view.querySelector('.station-opening-fade');
    if(!fade)return;
    fade.classList.remove('is-revealing');
    void fade.offsetWidth; // commit the full-black state before any transition
    const reveal=()=>fade.classList.add('is-revealing');
    const backdrop=world.querySelector('.station-background');
    if(backdrop?.complete)reveal();
    else if(backdrop){backdrop.addEventListener('load',reveal,{once:true});backdrop.addEventListener('error',reveal,{once:true});}
    else reveal();
  }
  function start(saved){
    if(ui.gameplayEnabled===false){ui.showGameplayNotice();return;}
    closeDialogs();
    dialogueReader.reset();
    state=saved??newGame();
    state.position=walkable(state.position)?state.position:nearestGround(state.position);
    walker=new Walker(state.position);
    ui.setActiveView('station');
    beginOpeningFade();
    save();paint();
    if(!['arrival','explore','ready','complete'].includes(state.node))renderStory();
    else if(!state.completed)status(['arrival','explore'].includes(state.node)?'':journal(state).objective);
    schedule();
  }
  function load(slot='auto'){
    const result=readSlot(storage,slot);
    if(!result.state){ui.showToast(result.error?`无法读取：${result.error}`:'这个位置还没有存档');return;}
    start(result.state);
  }
  function portrait(speaker){
    $('station-portrait').src=speaker==='老太太'?'media/characters/grandmother-reference.png':'media/characters/dong-reference.png';
    $('station-portrait').classList.toggle('is-shadow',speaker==='黑影');
    $('station-portrait').alt=speaker;
  }
  function showDialogue({speaker,text,choices=[],onContinue,feedback=''}){
    $('station-speaker').textContent=speaker;
    portrait(speaker);
    dialogueReader.show({text,choices,onContinue,feedback});
    $('station-talk-resume').hidden=true;
  }
  function renderStory(){
    if(!state||['explore','ready','complete'].includes(state.node)){if(talk.open)talk.close();paint();return;}
    const expectedNode=state.node,question=questions[expectedNode];
    const previousQuestion={value:'background',fear:'value',warning:'fear'}[expectedNode];
    const previousChoice=previousQuestion?questions[previousQuestion].choices.find(c=>c.id===state.answers[previousQuestion]):null;
    if(question){
      showDialogue({speaker:'老太太',text:question.text,feedback:previousChoice?`她说：「${previousChoice.reply}」`:'',choices:question.choices.map(choice=>({text:choice.text,hint:Object.entries(choice.effects).map(([key,value])=>`${statNames[key]} +${value}`).join('，'),run:()=>{
        if(state.node!==expectedNode)return;
        if(commit({type:'answer',id:choice.id}))renderStory();
      }}))});
    }else{
      const node=storyNodes[expectedNode];
      const actions=node.choices.map((choice,index)=>({text:choice.text,run:()=>{
        if(state.node!==expectedNode)return;
        if(!commit(expectedNode==='shadow'?{type:'shadow',id:choice.id}:{type:'next',index}))return;
        renderStory();
        if(state.node==='ready')status('黑影散去了。记住她的话，沿右侧道路进镇。');
      }}));
      // Authored linear transitions use the reader, not a pretend response choice.
      const continuation=node.choices.length===1&&node.choices[0].text==='继续';
      showDialogue({speaker:node.speaker,text:node.text,feedback:previousChoice?`她说：「${previousChoice.reply}」`:'',
        choices:continuation?[]:actions,onContinue:continuation?actions[0].run:undefined});
    }
  }
  function brief(text,speaker='董点事'){
    showDialogue({speaker,text,onContinue:()=>talk.close()});
  }
  function interact(id){
    if(!state)return;
    if(id==='grandmother'){
      if(state.node==='ready')brief(storyNodes['dont-turn-left'].text,'老太太');
      else {commit({type:'talk'});renderStory();}
    }else if(id==='exit'){
      if(commit({type:'exit'})){paint();if(!lastStorageError)status('车站进度已保存。');}
      else brief('老太太还在看着我。离开之前，先听听她想说什么。');
    }else{
      commit({type:'inspect',id});
      const descriptions={sign:'一块旧站牌。字迹被年月磨淡，路还在向镇子里延伸。\n\n我已经回来了。',bench:'路灯照着候车亭下的长椅。老太太坐在一端，身边的木头被磨得发白。\n\n她像是一直坐在这里，等着一个认识的人。',left:state.warningHeard?'「不要左转。」\n\n老太太的话又浮了上来。我暂时停下，转向右侧的道路。':'左边的小路没入树影。\n\n候车亭里的老太太似乎有话要对我说。'};
      brief(descriptions[id]);
    }
  }
  function move(goal,target=null){
    if(!canMove())return;
    if(!walker.go(goal,target)){status('那里无法通行。请在路面上选择落脚点。');return;}
    if(goal.x!==walker.position.x)$('station-player').classList.toggle('facing-left',goal.x<walker.position.x);
    status(target?`走向${hotspots[target].name}……`:'');
    paintActor();schedule();
  }
  function tick(now){
    frame=0;
    const delta=lastTime?Math.min((now-lastTime)/1000,.1):0;lastTime=now;
    if(canMove()){
      const wasMoving=walker.route.length>0;
      const target=walker.step(delta);paintActor();
      if(target){save();if(!lastStorageError)status('');interact(target);}
      else if(wasMoving&&!walker.route.length){save();if(!lastStorageError)status('');}
    }
    if(canMove()&&(walker.route.length||walker.target))schedule();
  }
  function schedule(){if(!frame&&!document.hidden&&ui.getActiveView()==='station'){lastTime=performance.now();frame=requestAnimationFrame(tick);}}
  function suspend(){if(frame)cancelAnimationFrame(frame);frame=0;lastTime=0;}

  for(const [id,spot]of Object.entries(hotspots)){
    const button=document.createElement('button');button.type='button';button.className='station-hotspot';button.dataset.hotspot=id;button.textContent=spot.name;
    if(id==='grandmother'){
      button.classList.add('station-hotspot--grandmother');
      button.textContent='';button.setAttribute('aria-label','与灯下的老奶奶交谈');
      const marker=document.createElement('span');marker.id='station-grandmother-marker';marker.className='station-quest-marker';marker.textContent='!';marker.setAttribute('aria-hidden','true');button.append(marker);
    }
    button.style.left=`${spot.x/16}%`;button.style.top=`${spot.y/9}%`;
    button.addEventListener('click',()=>move(spot.stand,id));$('station-hotspots').append(button);
  }
  world.addEventListener('pointerdown',event=>{
    if(event.target.closest('button')||event.button>0||!canMove())return;
    const box=world.getBoundingClientRect();
    if(!box.width||!box.height)return;
    const raw={x:(event.clientX-box.left)/box.width*1600,y:(event.clientY-box.top)/box.height*900};
    const point={x:Math.round(raw.x/20)*20,y:Math.round(raw.y/20)*20};
    if(!walkable(point)){status('树丛和候车亭不可穿过，请点击路面。');return;}
    move(point);
  });
  function notebook(){
    if(!state)return;
    const data=journal(state),stats=document.createElement('dl');
    stats.className='station-stat-grid';
    for(const [key,value]of Object.entries(data.stats)){const row=document.createElement('div'),dt=document.createElement('dt'),dd=document.createElement('dd');dt.textContent=statNames[key];dd.textContent=String(value);row.append(dt,dd);stats.append(row);}
    $('station-stats').replaceChildren(stats);
    const notes=data.notes.length?data.notes:['还没有记下新的事情。'];
    $('station-notes').replaceChildren(...notes.map(text=>{const li=document.createElement('li');li.textContent=text;return li;}));
    $('station-notebook').showModal();
  }
  function pause(){if(!state||hasModal())return;save();$('station-pause').showModal();}
  function openArchive(){if(state)save();closeDialogs();ui.setActiveView('entry');refreshSlots();}
  function refreshSlots(){
    let savedCount=0;
    document.querySelectorAll('[data-view="entry"] [data-slot]').forEach(button=>{
      const record=readSlot(storage,button.dataset.slot);
      const title=record.state?'公交车站':record.error?'存档无法读取':'空白档案';
      const description=record.state?`董点事 · ${record.state.completed?'场景已完成':'旅程进行中'}`:record.error?'原始数据已保留':'暂无存档记录';
      button.dataset.saveStatus=record.state?'saved':record.error?'error':'empty';
      button.querySelector('[data-save-title]').textContent=button.classList.contains('entry-card')?`存档 ${button.dataset.slot} · ${title}`:title;
      button.querySelector('[data-save-description]').textContent=description;
      button.querySelector('[data-save-action]').textContent=record.state?(state?'读取 / 保存 →':'读取档案 →'):state&&!record.error?'保存到这里 →':'查看档案 →';
      const preview=button.querySelector('.entry-save-preview');
      if(preview)preview.hidden=!record.state;
      button.setAttribute('aria-label',`存档槽位 ${button.dataset.slot}，${title}，${description}`);
      if(record.state)savedCount++;
    });
    $('entry-save-heading').textContent=state?'选择你要保存或读取的档案':'选择你要读取的档案';
    $('entry-save-summary').textContent=`已使用 ${savedCount} / 6 个手动存档槽位`;
  }
  window.addEventListener('disco:slot-open',event=>{
    selectedSlot=event.detail.slot;
    const dialog=$('slot-dialog'),record=readSlot(storage,selectedSlot);
    dialog.querySelector('p').textContent=record.state?`董点事 · 公交车站 · ${record.state.completed?'场景已完成':'旅程进行中'}。保存会覆盖这个槽位，读取会返回该进度。`:record.error?'该存档无法读取，原始数据已保留。可以选择其他槽位。':'这是一个空白槽位，可以在这里保存当前旅程。';
    dialog.querySelector('.station-slot-actions')?.remove();
    const actions=document.createElement('div');actions.className='station-slot-actions';
    const add=(text,run,disabled=false)=>{
      const button=document.createElement('button');button.type='button';button.disabled=disabled;
      const art=document.createElement('img');art.src='settings-reset-button-v1.png';art.alt='';art.setAttribute('aria-hidden','true');art.draggable=false;
      const label=document.createElement('span');label.textContent=text;button.append(art,label);
      button.addEventListener('click',run);actions.append(button);
    };
    add('读取存档',()=>load(selectedSlot),!record.state);
    add(record.state?'覆盖保存':'保存到这里',()=>{if(save(selectedSlot)){ui.showToast('手动存档已保存');dialog.close();}},!state||Boolean(record.error));
    add('返回',()=>dialog.close());dialog.append(actions);
  });
  window.addEventListener('disco:character-choice',event=>{
    if(ui.gameplayEnabled===false){ui.showGameplayNotice();return;}
    if(event.detail.choice==='大厦崩塌'){suspend();state=null;ui.boxOpening.start();}
    else ui.showToast('这条人物路线尚未开放。先从董点事的「大厦崩塌」开始。');
  });
  window.addEventListener('disco:view-change',event=>{
    if(event.detail.view!=='station'){suspend();if(state)save();}
    else if(state){paint();schedule();}
    if(event.detail.view==='entry'){
      refreshSlots();
      let back=document.getElementById('station-archive-return');
      if(!back){back=document.createElement('button');back.type='button';back.id='station-archive-return';back.className='station-archive-return';back.textContent='返回车站';back.addEventListener('click',()=>{ui.setActiveView('station');paint();schedule();});document.querySelector('[data-view="entry"]').append(back);}
      back.hidden=!state;
    }
  });
  document.addEventListener('visibilitychange',()=>{if(document.hidden){suspend();if(state)save();}else schedule();});
  window.addEventListener('pagehide',()=>{if(state)save();suspend();});
  $('station-talk-close').addEventListener('click',()=>talk.close());
  talk.addEventListener('close',()=>{paint();schedule();});
  $('station-talk-resume').addEventListener('click',()=>{
    if(!dialogueReader.resume())renderStory();
    $('station-talk-resume').hidden=true;
  });
  for(const id of ['station-journal-button','station-completion-journal'])$(id).addEventListener('click',notebook);
  $('station-pause-button').addEventListener('click',pause);
  $('station-save-button').addEventListener('click',openArchive);
  $('station-pause-save').addEventListener('click',()=>{if(save())ui.showToast('当前进度已保存');});
  $('station-pause-settings').addEventListener('click',()=>{closeDialogs();ui.setActiveView('settings');});
  for(const id of ['station-pause-menu','station-completion-menu'])$(id).addEventListener('click',()=>{save();closeDialogs();ui.setActiveView('mainmenu');});
  continueButton.addEventListener('click',()=>ui.boxOpening.hasSave()?ui.boxOpening.start(true):load());
  window.addEventListener('disco:box-save',refreshContinue);
  document.querySelectorAll('[data-close-station-dialog]').forEach(button=>button.addEventListener('click',()=>button.closest('dialog').close()));
  for(const id of ['station-pause','station-notebook'])$(id).addEventListener('close',schedule);
  document.addEventListener('keydown',event=>{
    if(ui.getActiveView()!=='station'||(event.repeat&&!event.key.startsWith('Arrow')))return;
    if(talk.open){
      if(event.key==='Escape'){event.preventDefault();talk.close();return;}
      if(/^[1-4]$/.test(event.key)){event.preventDefault();$('station-choices').querySelector(`[data-choice-index="${Number(event.key)-1}"]`)?.click();}
      return;
    }
    if(hasModal())return;
    if(event.key==='Escape'){event.preventDefault();pause();return;}
    if(event.key.toLowerCase()==='j'){event.preventDefault();notebook();return;}
    if(!canMove()||walker.route.length)return;
    const step={ArrowUp:[0,-20],ArrowDown:[0,20],ArrowLeft:[-20,0],ArrowRight:[20,0]}[event.key];
    if(step){event.preventDefault();if(performance.now()-lastKeyboardMove<80)return;lastKeyboardMove=performance.now();move({x:walker.anchor.x+step[0],y:walker.anchor.y+step[1]});}
  });
  world.querySelectorAll('img').forEach(img=>img.addEventListener('error',()=>{status('场景图片载入失败，请刷新重试。');ui.showToast('有场景图片未能载入');}));
  refreshContinue();refreshSlots();
  // A direct #station URL cannot bypass choosing/initializing a character.
  if(ui.getActiveView()==='station')ui.setActiveView('mainmenu');
  return {getState:()=>state?structuredClone(state):null};
}
