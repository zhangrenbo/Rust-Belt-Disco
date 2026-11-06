"""Inspect generated slot detail artwork in a task-created, isolated Chrome page."""
import json
import os
import uuid
from pathlib import Path
from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[1]
os.environ['NO_PROXY'] = 'localhost,127.0.0.1'
INIT = """(() => {
  const save=JSON.stringify({version:1,character:'dong-dianshi',scene:'A000',node:'arrival',answers:{},
    inspected:[],position:{x:440,y:640},warningHeard:false,shadow:null,completed:false,updatedAt:'2026-09-14T00:00:00.000Z'});
  const values=new Map([['northeast-disco:campaign:v1:auto',save],['northeast-disco:campaign:v1:02',save],
    ['northeast-disco:campaign:v1:04','damaged save'],['unrelated-data','preserved']]);
  Object.defineProperty(window,'localStorage',{configurable:true,value:{
    getItem:k=>values.get(String(k))??null,setItem:(k,v)=>values.set(String(k),String(v)),
    removeItem:k=>values.delete(String(k)),clear:()=>values.clear(),key:i=>[...values.keys()][i]??null,
    get length(){return values.size;}}});
  const connect=AudioNode.prototype.connect;
  AudioNode.prototype.connect=function(dest,...args){
    if(dest instanceof AudioDestinationNode){
      const silent=this.context.createGain();silent.gain.value=0;connect.call(silent,dest);
      return connect.call(this,silent,...args);
    }return connect.call(this,dest,...args);
  };
})();"""
SNAPSHOT = """() => Object.fromEntries(Array.from({length:localStorage.length},(_,i)=>{
  const key=localStorage.key(i);return [key,localStorage.getItem(key)];}))"""
with sync_playwright() as p:
    browser=p.chromium.connect_over_cdp('http://127.0.0.1:9223',timeout=10000)
    context=browser.contexts[0]
    control=browser.new_browser_cdp_session()
    task_url='about:blank#save-slot-detail-qa-'+str(uuid.uuid4())
    with context.expect_page(lambda candidate:candidate.url==task_url,timeout=10000) as created:
        target=control.send('Target.createTarget',{'url':task_url,'background':True})['targetId']
    page=created.value
    assert page.url==task_url
    checks,screens,errors=[],[],[]
    page.on('pageerror',lambda e:errors.append(str(e)))
    def check(name,result):
        checks.append({'name':name,'passed':bool(result)})
        assert result,name
    def settled():
        page.wait_for_function("""() => !document.querySelector('#app').classList.contains('is-view-switching')
          && document.querySelector('.page-shutter').getAnimations().every(a=>a.playState==='finished')""")
    def open_slot(slot):
        settled()
        button=page.locator(f'[data-view=entry] [data-slot="{slot}"]')
        rect=button.bounding_box()
        assert rect and button.evaluate('(b)=>{const r=b.getBoundingClientRect();return b.contains(document.elementFromPoint(r.x+r.width/2,r.y+r.height/2));}')
        page.mouse.click(rect['x']+rect['width']/2,rect['y']+rect['height']/2)
        page.locator('#slot-dialog[open]').wait_for(state='visible')
        page.wait_for_function("""() => [...document.querySelectorAll('#slot-dialog img')]
          .every(i=>i.complete&&i.naturalWidth>0)""")
    try:
        page.set_default_timeout(10000)
        session=context.new_cdp_session(page)
        info=session.send('Target.getTargetInfo')['targetInfo']
        assert info['targetId']==target,{'expected':target,'actual':info['targetId'],'type':info['type']}
        session.send('Emulation.setFocusEmulationEnabled',{'enabled':True})
        try: session.send('Page.setWebLifecycleState',{'state':'active'})
        except Exception as e: print('Lifecycle activation:',str(e)[:160])
        page.add_init_script(INIT)
        page.set_viewport_size({'width':1440,'height':810})
        page.goto('http://127.0.0.1:5173/',wait_until='networkidle')
        before=page.evaluate(SNAPSHOT)
        page.locator('.mainmenu-actions [data-open-view=entry]').click()
        page.locator('[data-view=entry].active').wait_for(state='visible')
        for width,height in [(1440,810),(1058,595),(844,475)]:
            page.set_viewport_size({'width':width,'height':height})
            for slot,state in [('01','empty'),('02','saved'),('04','damaged')]:
                open_slot(slot)
                metrics=page.evaluate("""() => {
                  const d=document.querySelector('#slot-dialog'),r=d.getBoundingClientRect(),css=getComputedStyle(d);
                  const bounds=e=>{const b=e.getBoundingClientRect();return {x:b.x,y:b.y,width:b.width,height:b.height};};
                  const nodes=[d.querySelector('h2'),d.querySelector('p'),...d.querySelectorAll('button')];
                  const content=nodes.map(e=>{
                    const b=e.getBoundingClientRect(),t=document.createRange();t.selectNodeContents(e);
                    const tr=t.getBoundingClientRect();
                    return {text:e.textContent||e.getAttribute('aria-label'),bounds:bounds(e),
                      textFits:tr.width===0||(tr.left>=b.left-1&&tr.right<=b.right+1),
                      inside:b.left>r.left&&b.right<r.right&&b.top>r.top&&b.bottom<r.bottom,
                      hit:e.contains(document.elementFromPoint(b.x+b.width/2,b.y+b.height/2))};
                  });
                  const actions=[...d.querySelectorAll('.station-slot-actions button')];
                  return {slot:d.querySelector('#dialog-slot').textContent,dialog:bounds(d),
                    background:css.backgroundImage,color:css.backgroundColor,border:css.borderWidth,
                    art:d.querySelector('.slot-detail-art').getAttribute('src'),
                    mask:getComputedStyle(d.querySelector('.slot-detail-art')).maskImage,
                    naturalSize:[d.querySelector('.slot-detail-art').naturalWidth,d.querySelector('.slot-detail-art').naturalHeight],
                    description:d.querySelector('p').textContent,
                    disabled:actions.map(b=>b.disabled),content,
                    bodyAboveButtons:d.querySelector('p').getBoundingClientRect().bottom<actions[0].getBoundingClientRect().top,
                    insideViewport:r.left>=0&&r.top>=0&&r.right<=innerWidth&&r.bottom<=innerHeight,
                    named:d.getAttribute('aria-labelledby')==='slot-detail-title'&&d.getAttribute('aria-describedby')==='slot-detail-description'};
                }""")
                check(f'{width} {state}: artwork, alpha mask and all content fit',
                      metrics['slot']==slot and metrics['naturalSize']==[1536,1024]
                      and metrics['background']=='none' and metrics['color']=='rgba(0, 0, 0, 0)'
                      and 'save-slot-detail-outline.svg' in metrics['mask'] and metrics['bodyAboveButtons']
                      and metrics['insideViewport'] and metrics['named']
                      and all(c['inside'] and c['textFits'] for c in metrics['content']))
                check(f'{width} {state}: save/load availability and control hits',
                      metrics['disabled']==([False,True,False] if state=='saved' else [True,True,False])
                      and all(c['hit'] for c in metrics['content'][2:]))
                page.mouse.move(width-5,height-5)
                path=ROOT/f'docs/save-slot-detail-{state}-{width}.png'
                page.screenshot(path=str(path),clip=page.locator('[data-screenshot-root]').bounding_box(),timeout=10000)
                screens.append({'path':str(path),'viewport':[width,height],'state':state,**metrics})
                page.locator('.station-slot-actions button').nth(2).click()
        for slot in ['01','02','03','04','05','06']:
            open_slot(slot)
            assert page.locator('#dialog-slot').inner_text()==slot
            page.locator('.slot-detail-close').click()
        check('All six slot numbers and paper close button',True)
        open_slot('01')
        page.wait_for_timeout(710)
        rect=page.locator('#slot-dialog').bounding_box()
        page.mouse.click(rect['x']+rect['width']*.87,rect['y']+rect['height']*.55)
        check('Clicking blank paper keeps the detail card open',page.locator('#slot-dialog').evaluate('(d)=>d.open'))
        page.mouse.click(8,8)
        check('Outside click closes',not page.locator('#slot-dialog').evaluate('(d)=>d.open'))
        open_slot('01')
        page.keyboard.press('Escape')
        check('Escape closes and stays on archive page',not page.locator('#slot-dialog').evaluate('(d)=>d.open')
              and page.locator('.view.active').get_attribute('data-view')=='entry')
        open_slot('01')
        button=page.locator('.station-slot-actions button').nth(2)
        button.focus()
        check('Visible keyboard focus',button.evaluate("(b)=>b.matches(':focus-visible')&&getComputedStyle(b).outlineStyle==='solid'"))
        page.keyboard.press('Enter')
        check('Keyboard return closes',not page.locator('#slot-dialog').evaluate('(d)=>d.open'))
        open_slot('02')
        page.locator('.station-slot-actions button').first.click()
        page.locator('[data-view=ui-only].active').wait_for(state='visible')
        page.locator('.ui-only-notice-button').click()
        check('Existing save opens UI showcase and returns to menu',page.locator('.view.active').get_attribute('data-view')=='mainmenu')
        check('All isolated saved data remain identical',before==page.evaluate(SNAPSHOT))
        check('No page errors',not errors)
        report={'date':'2026-09-14','transport':'fixed work Google Chrome + direct CDP 9223','browser':browser.version,
          'storage':'isolated in-memory fixtures; physical audio output muted; real player saves untouched',
          'checks':checks,'screens':screens,'errors':errors,'visualAcceptance':'pending user review',
          'electronWorkbench':'not directly inspected'}
        (ROOT/'docs/save-slot-detail-verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
        print(json.dumps({'passed':len(checks),'screens':len(screens),'errors':errors}))
    except Exception:
        if page.locator('[data-screenshot-root]').count():
            page.screenshot(path=str(ROOT/'docs/save-slot-detail-qa-failure.png'),clip=page.locator('[data-screenshot-root]').bounding_box())
        raise
    finally:
        page.close()
