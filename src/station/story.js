// Dialogue transcribed from content/story/ink/D0_序章.ink, A000 公交车站「不要左转」.
// Keep authored speech and order intact; 「继续」 is only a UI paging control.
// The later, explicit three-question attribute specification supersedes the
// earlier draft's +5 wisdom / +800 money opening choices.
export const statNames = {strength:'力量',agility:'敏捷',endurance:'耐力',charisma:'魅力',luck:'幸运',perception:'感知',erudition:'博学多闻',insight:'见微知著',fortitude:'坚毅不倒',corruption:'道德败坏'};
export const baseStats = {strength:1,agility:1,endurance:1,charisma:1,luck:1,perception:1,erudition:1,insight:1,fortitude:2,corruption:0};
export const questions = {
  background: {text:'你在外面这些年，你是怎么活的？',next:'value',choices:[
    {id:'builder',text:'我在工地搬砖。',effects:{strength:1,endurance:1},reply:'难怪你看起来壮了。'},
    {id:'waiter',text:'我在餐馆端盘子。',effects:{agility:1,endurance:1},reply:'端盘子的人，手快心也快。'},
    {id:'student',text:'我在读夜校。',effects:{perception:1,erudition:1},reply:'读书好。你妈要是知道，会高兴的。'},
    {id:'drifter',text:'我在街上混日子。',effects:{luck:1,corruption:1},reply:'混日子的人，命有时候反而硬。'},
  ]},
  value: {text:'孩子，你觉得什么对你来说最重要？',next:'fear',choices:[
    {id:'life',text:'活着。',effects:{endurance:1},reply:'活着……是啊，活着就好。'},
    {id:'truth',text:'真相。',effects:{perception:1},reply:'真相？你确定你想知道？'},
    {id:'money',text:'钱。',effects:{charisma:1,corruption:1},reply:'钱能买到很多东西。但有些东西，它买不回来。'},
    {id:'fairness',text:'公平。',effects:{strength:1},reply:'公平？你爸也这么说过。'},
  ]},
  fear: {text:'你最害怕什么？',next:'warning',choices:[
    {id:'fire',text:'火。',effects:{perception:1},reply:'火……你还记得。'},
    {id:'loneliness',text:'孤独。',effects:{charisma:1},reply:'你不会孤独的。'},
    {id:'helplessness',text:'无能为力。',effects:{strength:1},reply:'无能为力……你长大了。'},
    {id:'forgetting',text:'遗忘。',effects:{luck:1},reply:'放心。你忘不了的。'},
  ]},
};
export const ideologies = {freedom:'自由',equality:'平等',morality:'虚伪的道德',self:'自己的影子'};
export const storyNodes = {
  arrival:{speaker:'老太太',text:'我认识你，孩子',choices:[{text:'继续',to:'greeting'}]},
  greeting:{speaker:'董点事',text:'头部传来一阵剧痛，这个老奶奶说她认识我，但是我什么都想不起来了......',choices:[{text:'继续',to:'greeting-memory'}]},
  'greeting-memory':{speaker:'老太太',text:'你是那个可怕火灾的受害者和幸存者。孩子，我们都在等你，你是唯一一个目击者，只有你可能见过凶手的样子',choices:[{text:'继续',to:'greeting-question'}]},
  'greeting-question':{speaker:'老太太',text:'你不在的时候，镇子上还有很多人葬身火海...可惜你，孩子，你现在还记得你自己吗？',choices:[{text:'继续',to:'background'}]},
  warning:{speaker:'老太太',text:'孩子，我做了一个梦\n\n那个杀死你母亲的凶手，我梦见他坐车回来了，他左手拿着，右手拿着，回到了这个小镇。 他会在镇上待七天。七天，孩子。 第七天过后……他就走了。再也找不到了。 你只有七天。',choices:[{text:'那只是一个梦而已',to:'doubt'},{text:'好的奶奶，我记住了',to:'dont-turn-left'}]},
  doubt:{speaker:'老太太',text:'我见到你母亲的时候，我告诉过她，她丈夫的美梦会醒来，她的孩子会比自己先死去，她自己，会在尝尽了折磨之后被火烧死',choices:[{text:'可是我还活着',to:'alive'},{text:'我母亲在哪里？',to:'mother'}]},
  alive:{speaker:'老太太',text:'是的，孩子，你还活着，去完成你的使命',choices:[{text:'继续',to:'dont-turn-left'}]},
  mother:{speaker:'老太太',text:'在天堂，孩子，你的母亲是善良的人，在彼岸，她有一个美好的生活',choices:[{text:'继续',to:'dont-turn-left'}]},
  'dont-turn-left':{speaker:'老太太',text:'那就好，孩子，记住，不要左转，不要左转...',choices:[{text:'继续',to:'shadow-intro'}]},
  'shadow-intro':{speaker:'黑影',text:'你最终还是回到了这里\n\n我是你散不去的梦魇',choices:[{text:'继续',to:'shadow'}]},
  shadow:{speaker:'黑影',text:'我是世界上所有人的统治者，没有人可以逃脱，没有人可以解放，你们所有人都要取悦我\n\n我会追随你到天涯海角 猜猜我是谁？',choices:[{id:'freedom',text:'是不自由'},{id:'equality',text:'是不平等'},{id:'morality',text:'是虚伪的道德'},{id:'self',text:'只有我自己的影子'}]},
};

export function attributes(answers) {
  const stats={...baseStats};
  for(const [name,question] of Object.entries(questions)) {
    const choice=question.choices.find(c=>c.id===answers[name]);
    if(choice)for(const [key,value] of Object.entries(choice.effects))stats[key]+=value;
  }
  return stats;
}
