(() => {
  const state = window.__ndMJ;
  if (!state || state.cursor) throw Error('Complete the task metadata scan first');
  const architecture = /\b(?:architectur\w*|buildings?|houses?|homes?|apartments?|rooms?|interiors?|exteriors?|facades?|shops?|stores?|factories|factory|warehouses?|stations?|offices?|schools?|hospitals?|streets?|courtyards?|stairwells?|corridors?|sidewalks?|bridges?|roofs?|walls?|doors?|windows?|furniture)\b|建筑|房屋|房间|室内|街道|楼房|家具/i;
  const threeD = /\b3[ -]?d\b|three[ -]dimensional|photoreal\w*|\bPBR\b|physically[ -]based|三维/i;
  const camera = /\b45\s*(?:°|degrees?|deg\b|[ -]degree)|isometric|等距|45度|45 度/i;
  const snow = /\b(?:snow\w*|winter|wintry|blizzard)\b|积雪|雪景|冰雪|冬季|冬天|下雪/i;
  const china = /\b(?:china|chinese|manchuria\w*)\b|中国/i;
  const period = /\b(?:19[789]0s|198\d|80s|eighties)\b|八十年代|80年代/i;
  const otherProject = /\b(?:pixel\s*art|16[ -]?bit|8[ -]?bit|side[ -]scroll\w*|wuxia|xianxia|cultivation|anime|chibi|minecraft|warhammer|hogwarts|harry potter|moss whisper|wangwang village|sword\s+immortal|dark[ -]fantasy|cyberpunk|monument valley|pixar|cartoon\w*|medieval knights?|sci[ -]?fi|science fiction)\b|像素|修仙|仙侠|武侠|闪退勇者|赛博朋克|纪念碑谷/i;
  const result = [];
  const stats = { jobsScanned: state.jobs.length, uniqueJobs: 0, matchedJobs: 0, unlikedImages: 0, skippedLikedImages: 0, skippedHiddenJobs: 0, skippedVideos: 0, buckets: {}, excludedOtherProjectJobs: 0 };
  const seen = new Set();
  const excludedSamples = [];
  for (const job of state.jobs) {
    if (seen.has(job.id)) continue;
    seen.add(job.id);
    if (job.user_hidden === true) { stats.skippedHiddenJobs++; continue; }
    if (job.video_segments || /video/i.test(job.job_type || '')) { stats.skippedVideos++; continue; }
    // Parameters and negative-prompt text do not establish project ownership.
    const positive = (job.full_command || '').split(/\s--[a-z]/i)[0];
    const text = positive.replace(/\b(?:no|without|not)\s+(?:3[ -]?d|snow\w*|winter|isometric|perspective|pixel\s*art|16[ -]?bit|8[ -]?bit)\b/gi, '');
    const direct = /disco[ -]elysium|northeast(?:ern)?\s+(?:china|chinese|disco)|north[ -]eastern\s+chin|极乐迪斯科|东北迪斯科|中国东北|东北小镇/i.test(text);
    const cues = [];
    if (direct) cues.push('极乐迪斯科或东北');
    if (snow.test(text)) cues.push('雪景或冬季');
    if (architecture.test(text) && camera.test(text)) cues.push('45度或等距建筑室内');
    if (architecture.test(text) && threeD.test(text)) cues.push('3D或PBR建筑室内');
    if (china.test(text) && period.test(text)) cues.push('中国年代生活');
    if (!cues.length) continue;
    if (!direct && otherProject.test(text)) {
      stats.excludedOtherProjectJobs++;
      if (excludedSamples.length < 8) excludedSamples.push({ id: job.id, prompt: positive.slice(0,320), cues });
      continue;
    }
    const batchSize = Number(job.batch_size);
    if (!Number.isInteger(batchSize) || batchSize < 1 || batchSize > 16) throw Error('Unexpected batch size: '+job.id+' '+job.batch_size);
    const images = [];
    for (let index = 0; index < batchSize; index++) {
      const rating = job.rating?.[String(index)] ?? null;
      if (rating === 5) { stats.skippedLikedImages++; continue; }
      images.push({id:job.id,index,rating});
    }
    if (!images.length) continue;
    stats.matchedJobs++;
    stats.unlikedImages += images.length;
    for (const cue of cues) stats.buckets[cue] = (stats.buckets[cue] || 0) + images.length;
    result.push({ id:job.id, enqueue_time:job.enqueue_time, full_command:job.full_command, positive_prompt:positive,
      batch_size:batchSize, ratings_before:job.rating, user_hidden:job.user_hidden, job_type:job.job_type, cues, images });
  }
  stats.uniqueJobs = seen.size;
  state.candidates = result;
  state.classification = stats;
  state.members = result.flatMap(j=>j.images.map(i=>({id:i.id,index:i.index})));
  const samples = {};
  for (const cue of Object.keys(stats.buckets)) samples[cue] = result.filter(j=>j.cues.includes(cue)).filter((_,i)=>i%Math.max(1,Math.floor(result.filter(j=>j.cues.includes(cue)).length/5))===0).slice(0,5).map(j=>({id:j.id,prompt:j.positive_prompt.slice(0,350),images:j.images.length}));
  return {stats,samples,excludedSamples};
})()
