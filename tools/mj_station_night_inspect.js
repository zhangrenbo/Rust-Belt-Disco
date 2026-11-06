() => {
  const t=document.querySelector('textarea');
  const a=t?.parentElement?.parentElement?.parentElement;
  return {composerText:a?.textContent.slice(0,7000),
    controls:[...document.querySelectorAll('button')].filter(e=>!e.closest('a[href*="/jobs/"]') && (/ersonal|Draft|Convers|Settings|Submit|Imagine|Folder/.test((e.textContent||'')+' '+e.title+' '+e.getAttribute('aria-label')) || e.querySelector('g[id*="ersonal"]'))).map(e=>({text:e.textContent.slice(0,200),title:e.title,aria:e.getAttribute('aria-label'),pressed:e.getAttribute('aria-pressed'),cls:e.className,html:e.outerHTML.replace(/<svg[\s\S]*?<\/svg>/g,'<svg/>').slice(0,1600)})).slice(-25),
    tail:document.body.innerText.slice(-700),
    dialogs:[...document.querySelectorAll('[role=dialog]')].map(e=>e.innerText.slice(0,1600))};
}
