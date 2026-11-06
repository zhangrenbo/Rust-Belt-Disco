// ============================================
// 琥珀色的灵魂发酵 | Ink Script
// ============================================

VAR Affinity = 0
VAR Gold = 100
VAR MixCount = 0
VAR CrashCount = 0
VAR CurrentMood = 0
VAR GuessedMood = 0
VAR WrongGuessCount = 0
VAR DailyDrinkCount = 0
VAR Flag_RainNightOath = false
VAR Flag_ChoseToStop = false
VAR Flag_ReachedKissEnding = false
VAR Flag_HasKey = false
VAR Flag_KeyQuestStarted = false

// 兜帽人
VAR HoodedManVisit = 0
VAR Chat_HoodedMan_1 = false
VAR Chat_HoodedMan_2 = false
VAR Chat_HoodedMan_3 = false
VAR Chat_HoodedMan_4 = false

// 默认阶段对话
VAR Chat_Default_Drink_1 = false
VAR Chat_Default_Work_1 = false
VAR Chat_Default_Weather_1 = false

// 朋友阶段对话
VAR Chat_Friend_Taste_1 = false
VAR Chat_Friend_Taste_2 = false
VAR Chat_Friend_Family_1 = false
VAR Chat_Friend_Tavern_1 = false
VAR Chat_Friend_Hobby_1 = false

// 亲密阶段对话
VAR Chat_Close_Past_1 = false
VAR Chat_Close_Past_2 = false
VAR Chat_Close_Dream_1 = false
VAR Chat_Close_Fear_1 = false
VAR Chat_Close_Rain_1 = false

// 恋人阶段对话
VAR Chat_Lover_Secret_1 = false
VAR Chat_Lover_Future_1 = false
VAR Chat_Lover_Father_1 = false
VAR Chat_Lover_Key_1 = false

// 每日对话标记（每天重置）
VAR Daily_Default_Drink = false
VAR Daily_Default_Work = false
VAR Daily_Default_Weather = false
VAR Daily_Friend_Taste = false
VAR Daily_Friend_Family = false
VAR Daily_Friend_Tavern = false
VAR Daily_Friend_Hobby = false
VAR Daily_Close_Past = false
VAR Daily_Close_Dream = false
VAR Daily_Close_Fear = false
VAR Daily_Close_Rain = false
VAR Daily_Lover_Secret = false
VAR Daily_Lover_Future = false
VAR Daily_Lover_Father = false

// 心情：1=开心 2=烦躁 3=忧郁 4=疲惫 5=平静

-> main_loop

=== main_loop ===
{Flag_ReachedKissEnding:
    -> S6_kiss_loop
}
{Flag_ChoseToStop:
    -> END_STOP_daily
}
{Affinity >= 300:
    -> S6_confession_scene
}
~ CurrentMood = (MixCount % 5) + 1
~ GuessedMood = 0
~ WrongGuessCount = 0
~ DailyDrinkCount = 0
// 重置每日对话标记
~ Daily_Default_Drink = false
~ Daily_Default_Work = false
~ Daily_Default_Weather = false
~ Daily_Friend_Taste = false
~ Daily_Friend_Family = false
~ Daily_Friend_Tavern = false
~ Daily_Friend_Hobby = false
~ Daily_Close_Past = false
~ Daily_Close_Dream = false
~ Daily_Close_Fear = false
~ Daily_Close_Rain = false
~ Daily_Lover_Secret = false
~ Daily_Lover_Future = false
~ Daily_Lover_Father = false

{MixCount > 0 && MixCount % 10 == 0 && HoodedManVisit < 4:
    -> hooded_man_event
}

-> tavern_enter

// ============================================
// 关系阶段判断
// ============================================

=== function get_stage() ===
{Affinity < -50:
    ~ return 5
}
{Affinity < 50:
    ~ return 1
}
{Affinity < 150:
    ~ return 2
}
{Affinity < 250:
    ~ return 3
}
~ return 4

// ============================================
// 兜帽人事件
// ============================================

=== hooded_man_event ===
// [场景] 角落里坐着一个兜帽人。
// [场景] 可可露正在给他调酒。

可可露："……这位客人又来了。"
可可露："'琥珀色的摇篮曲'。"
可可露："每次都点这个。"

// [场景] 她把酒推给兜帽人。
// [场景] 兜帽人接过酒，喝了一口。
// [场景] 然后放下钱，起身离开。

{HoodedManVisit == 0:
    // [场景] 经过你身边时，他停了一下。
    // [场景] 兜帽下是一张苍老的脸。
    // [场景] 眼眶微红。
    // [场景] 然后他走了。
}
{HoodedManVisit == 1:
    // [场景] 他走的时候，有什么东西从口袋里掉了出来。
    // [场景] 一把钥匙。
    // [场景] 可可露捡了起来。
    可可露："……"
    可可露："算了，下次还给他吧。"
}
{HoodedManVisit == 2:
    // [场景] 他又来了。
    // [场景] 可可露想把钥匙还给他。
    // [场景] 但他摇了摇头，走了。
    可可露："……奇怪的人。"
}
{HoodedManVisit >= 3:
    // [场景] 他看着可可露，很久。
    // [场景] 然后离开了。
    可可露："……"
    可可露："他每次都这样看着我。"
    可可露："好像有话想说。"
}

~ HoodedManVisit++

{HoodedManVisit == 1:
    ~ Chat_HoodedMan_1 = true
}
{HoodedManVisit == 2:
    ~ Chat_HoodedMan_2 = true
}
{HoodedManVisit == 3:
    ~ Chat_HoodedMan_3 = true
}
{HoodedManVisit >= 4:
    ~ Chat_HoodedMan_4 = true
}

-> tavern_enter

// ============================================
// 进入酒馆
// ============================================

=== tavern_enter ===
{MixCount == 0:
    -> first_meeting
}

// 仇人阶段
{Affinity < -50:
    -> enemy_enter
}

{CurrentMood == 1:
    // [场景] 可可露在轻声哼着歌。
    // [场景] 她的尾巴在微微摇摆。
}
{CurrentMood == 2:
    // [场景] 可可露在用力凿冰。
    // [场景] 动作比平时重。
}
{CurrentMood == 3:
    // [场景] 可可露盯着一个空杯子发呆。
}
{CurrentMood == 4:
    // [场景] 可可露揉了揉眼睛。
    // [场景] 打了个小哈欠。
}
{CurrentMood == 5:
    // [场景] 可可露像往常一样在吧台后面忙碌。
}

// [场景] 可可露抬头看到你。

// 根据关系阶段显示不同招呼
{Affinity < 50:
    可可露："……来了。"
}
{Affinity >= 50 && Affinity < 150:
    可可露："来了。"
    可可露："今天想喝什么？"
}
{Affinity >= 150 && Affinity < 250:
    可可露："你来了。"
    可可露："等你挺久的。"
}
{Affinity >= 250:
    可可露："你来了！"
    可可露："我是说……欢迎。"
    可可露："……笨蛋。"
}

-> main_choice

// ============================================
// 仇人阶段入口
// ============================================

=== enemy_enter ===
// [场景] 可可露看到你，表情冷淡。
可可露："……"
可可露："你来干什么。"

-> enemy_choice

=== enemy_choice ===
【好感度：{Affinity}　金币：{Gold}】
【关系：仇人】

+ [点一杯酒]
    可可露："……"
    可可露："自己看菜单。"
    -> enemy_order_drink

+ [道歉]
    你："可可露，我……"
    可可露："不想听。"
    -> enemy_choice

+ [送她礼物（100金币）]
    {Gold < 100:
        // [场景] 你摸了摸口袋。
        // [场景] 不够。
        -> enemy_choice
    }
    // [场景] 你把100金币放在吧台上。
    你："这个……给你。"
    // [场景] 可可露看着金币。
    可可露："……"
    可可露："你以为用钱就能解决问题？"
    // [场景] 可可露（沉默了一会儿）
    可可露："……算了。"
    可可露："收下了。"
    ~ Gold -= 100
    ~ Affinity += 50
    【好感度 +50】
    {Affinity >= -50:
        可可露："……"
        可可露："下次别再惹我了。"
        【关系恢复到默认阶段】
    }
    -> enemy_choice

+ [今天先走了]
    可可露："……"
    // [场景] 可可露没有说话。
    -> leave_tavern

=== enemy_order_drink ===
+ [威士忌纯饮 - 15金币]
    {Gold < 15:
        // [场景] 你摸了摸口袋。
        你："……下次吧。"
        -> enemy_choice
    }
    你："威士忌纯饮。"
    ~ Gold -= 15
    ~ MixCount++
    // [场景] 可可露调了酒，推过来。
    // [场景] 没有说话。
    -> enemy_choice

+ [不点了]
    -> enemy_choice

// ============================================
// 主选择（正常阶段）
// ============================================

=== main_choice ===
【好感度：{Affinity}　金币：{Gold}】
{Affinity < 50:
    【关系：默认】
}
{Affinity >= 50 && Affinity < 150:
    【关系：朋友】
}
{Affinity >= 150 && Affinity < 250:
    【关系：亲密】
}
{Affinity >= 250:
    【关系：恋人】
}

+ [猜猜她的心情]
    -> guess_mood
    
+ [点一杯酒]
    -> order_drink

+ [和她聊聊]
    -> chat_menu

+ [今天先走了]
    -> leave_tavern

// ============================================
// 猜测心情
// ============================================

=== guess_mood ===
{WrongGuessCount >= 3:
    可可露："……"
    可可露："你今天还是别猜了。"
    -> main_choice
}

// [场景] 你仔细观察着可可露。

可可露："看什么？"

+ [你今天心情不错？]
    ~ GuessedMood = 1
    {CurrentMood == 1:
        可可露："……你怎么知道？"
        你："看出来了。"
        可可露："算你厉害。"
        【猜对了！点甜酒吧】
    - else:
        可可露："没有吧。"
        ~ WrongGuessCount++
        -> guess_wrong
    }
    -> main_choice

+ [你今天有点烦躁？]
    ~ GuessedMood = 2
    {CurrentMood == 2:
        可可露："……这么明显吗。"
        你："凿冰的声音都变了。"
        可可露："少管闲事。"
        // [场景] 可可露（但语气软了一点）
        【猜对了！点烈酒吧】
    - else:
        可可露："没有。"
        ~ WrongGuessCount++
        -> guess_wrong
    }
    -> main_choice

+ [你今天心情低落？]
    ~ GuessedMood = 3
    {CurrentMood == 3:
        可可露："……你怎么看出来的。"
        你："你一直盯着那个杯子。"
        可可露："……观察力不错。"
        【猜对了！点苦酒吧】
    - else:
        可可露："想多了。"
        ~ WrongGuessCount++
        -> guess_wrong
    }
    -> main_choice

+ [你今天很累？]
    ~ GuessedMood = 4
    {CurrentMood == 4:
        可可露："有那么明显吗。"
        你："你打了好几个哈欠了。"
        可可露："昨晚没睡好。"
        【猜对了！点清爽的酒吧】
    - else:
        可可露："还好。"
        ~ WrongGuessCount++
        -> guess_wrong
    }
    -> main_choice

+ [不猜了]
    -> main_choice

=== guess_wrong ===
{WrongGuessCount == 2:
    可可露："……你根本没在看我吧。"
    ~ Affinity -= 10
    【猜错两次，好感度 -10】
    -> main_choice
}
{WrongGuessCount >= 3:
    可可露："……"
    可可露："算了。"
    可可露："你今天还是别猜了。"
    ~ Affinity -= 20
    【猜错三次，好感度 -20，今天不能再猜了】
    -> main_choice
}
【猜错了】
-> main_choice

// ============================================
// 点酒
// ============================================

=== order_drink ===
{DailyDrinkCount >= 1:
    可可露："今天已经喝过了。"
    可可露："明天再来吧。"
    -> main_choice
}

可可露："喝什么？"

+ [威士忌纯饮（烈酒）- 15金币]
    {Gold < 15:
        // [场景] 你摸了摸口袋。
        你："……下次吧。"
        -> main_choice
    }
    你："威士忌纯饮。"
    ~ Gold -= 15
    -> serve_drink(2)

+ [甜蜜日落（甜酒）- 20金币]
    {Gold < 20:
        // [场景] 你摸了摸口袋。
        你："……下次吧。"
        -> main_choice
    }
    你："甜蜜日落。"
    ~ Gold -= 20
    -> serve_drink(1)

+ [苦行者（苦酒）- 15金币]
    {Gold < 15:
        // [场景] 你摸了摸口袋。
        你："……下次吧。"
        -> main_choice
    }
    你："苦行者。"
    ~ Gold -= 15
    -> serve_drink(3)

+ [极光冻结（清爽）- 25金币]
    {Gold < 25:
        // [场景] 你摸了摸口袋。
        你："……下次吧。"
        -> main_choice
    }
    你："极光冻结。"
    ~ Gold -= 25
    -> serve_drink(4)

+ [你来决定]
    你："你来决定吧。"
    可可露："……"
    可可露："自己选。"
    -> order_drink

+ [不点了]
    -> main_choice

=== serve_drink(drink_mood) ===
~ MixCount++
~ DailyDrinkCount++

// [场景] 可可露开始调酒。

{CurrentMood == drink_mood && GuessedMood == drink_mood:
    // [场景] 可可露把酒推过来。
    可可露："……"
    可可露："你怎么知道我想喝这个。"
    你："猜的。"
    可可露："……难得。"
    // [场景] 可可露拿出另一个杯子，给自己也倒了一杯。
    可可露："陪你喝一杯。"
    ~ Affinity += 50
    【好感度 +50】
    -> after_drink
}
{CurrentMood == drink_mood && GuessedMood != drink_mood:
    // [场景] 可可露把酒推过来。
    可可露："运气不错。"
    可可露："正好是我想喝的。"
    ~ Affinity += 15
    【好感度 +15】
    -> after_drink
}
// [场景] 可可露把酒推过来。
可可露："请慢用。"
-> after_drink

=== after_drink ===

+ [和她聊聊]
    -> chat_menu

+ [今天先走了]
    -> leave_tavern

// ============================================
// 聊天菜单（根据关系阶段显示不同选项）
// ============================================

=== chat_menu ===
可可露："怎么了？"

// 根据关系阶段显示不同选项，但高阶段可以访问低阶段
{Affinity >= -50 && Affinity < 50:
    -> chat_menu_default
}
{Affinity >= 50 && Affinity < 150:
    -> chat_menu_friend
}
{Affinity >= 150 && Affinity < 250:
    -> chat_menu_close
}
-> chat_menu_lover

// ============================================
// 默认阶段聊天菜单
// ============================================

=== chat_menu_default ===

+ [这里的酒好喝吗？]
    -> chat_default_drink

+ [你一个人看店？]
    -> chat_default_work

+ [今天天气不错]
    -> chat_default_weather

+ [不聊了]
    -> main_choice

=== chat_default_drink ===
{not Daily_Default_Drink:
    你："这里的酒好喝吗？"
    可可露："……"
    可可露："你自己不是在喝吗。"
    你："我是问你觉得怎么样。"
    可可露："……"
    可可露："不知道。"
    你："不知道？"
    可可露："我尝不出来。"
    你："什么意思？"
    可可露："……没什么。"
    可可露："点酒吧。"
    ~ Daily_Default_Drink = true
    ~ Chat_Default_Drink_1 = true
    -> chat_menu_default
}
可可露："问过了。"
-> chat_menu_default

=== chat_default_work ===
{not Daily_Default_Work:
    你："你一个人看店？"
    可可露："嗯。"
    你："不累吗？"
    可可露："习惯了。"
    你："店里好像没什么客人。"
    可可露："你想说什么。"
    你："没什么。"
    可可露："……"
    可可露："点酒吧。"
    ~ Daily_Default_Work = true
    ~ Chat_Default_Work_1 = true
    -> chat_menu_default
}
可可露："问过了。"
-> chat_menu_default

=== chat_default_weather ===
{not Daily_Default_Weather:
    你："今天天气不错。"
    可可露："嗯。"
    你："……"
    可可露："……"
    可可露："然后呢？"
    你："没了。"
    可可露："……无聊。"
    ~ Daily_Default_Weather = true
    ~ Chat_Default_Weather_1 = true
    -> chat_menu_default
}
可可露："你就只会聊天气吗。"
-> chat_menu_default

// ============================================
// 朋友阶段聊天菜单
// ============================================

=== chat_menu_friend ===

+ {Chat_Default_Drink_1} [你平时喜欢喝什么？]
    -> chat_friend_taste

+ [你是哪里人？]
    -> chat_friend_family

+ {Chat_Default_Work_1} [这家店是你开的？]
    -> chat_friend_tavern

+ [你有什么爱好吗？]
    -> chat_friend_hobby

+ {Chat_HoodedMan_1} [那个兜帽人是谁？]
    -> chat_hooded_man

+ [聊点别的]
    -> chat_menu_default

+ [不聊了]
    -> main_choice

=== chat_friend_taste ===
{not Daily_Friend_Taste:
    {not Chat_Friend_Taste_1:
        你："你平时喜欢喝什么？"
        可可露："……什么都行。"
        你："没有偏好吗？"
        // [场景] 可可露沉默了一会儿。
        可可露："之前不是说过吗。"
        可可露："我尝不太出来。"
        你："能具体说说吗？"
        可可露："我的味觉有问题。"
        可可露："先天的。"
        可可露："在慢慢消失。"
        你："……"
        可可露："别用那种眼神看我。"
        可可露："我还能调酒。"
        可可露："靠记忆，靠经验。"
        ~ Chat_Friend_Taste_1 = true
        ~ Daily_Friend_Taste = true
        -> chat_menu_friend
    }
    {not Chat_Friend_Taste_2:
        你："你的味觉……现在还剩多少？"
        可可露："……"
        可可露："甜味还有一点。"
        可可露："其他的……差不多没了。"
        你："医生怎么说？"
        可可露："治不好。"
        可可露："据说活不长的。"
        可可露："但我活到了现在。"
        可可露："也算奇迹吧。"
        ~ Chat_Friend_Taste_2 = true
        ~ Daily_Friend_Taste = true
        -> chat_menu_friend
    }
    可可露："嗯……味觉的事。"
    可可露："老样子。"
    ~ Daily_Friend_Taste = true
    -> chat_menu_friend
}
可可露："这个……今天聊过了。"
-> chat_menu_friend

=== chat_friend_family ===
{not Daily_Friend_Family:
    {not Chat_Friend_Family_1:
        你："你是哪里人？"
        // [场景] 可可露的表情一僵。
        可可露："……不知道。"
        你："不知道？"
        可可露："我没有家。"
        可可露："小时候被丢掉了。"
        你："……"
        可可露："别用那种眼神。"
        可可露："我不需要同情。"
        ~ Chat_Friend_Family_1 = true
        ~ Daily_Friend_Family = true
        -> chat_menu_friend
    }
    可可露："……"
    可可露："还是老样子。"
    可可露："没有家。"
    ~ Daily_Friend_Family = true
    -> chat_menu_friend
}
可可露："这个……今天聊过了。"
-> chat_menu_friend

=== chat_friend_tavern ===
{not Daily_Friend_Tavern:
    {not Chat_Friend_Tavern_1:
        你："你一个人看店……这家店是你开的？"
        可可露："不是。"
        可可露："是老板的。"
        你："老板呢？"
        可可露："……走了。"
        可可露："她收养了我。"
        可可露："在那个雨夜。"
        可可露："然后养大了我。"
        可可露："教我调酒。"
        你："她去哪了？"
        可可露："不知道。"
        可可露："有一天她说要出去找点东西。"
        可可露："然后就再也没回来。"
        ~ Chat_Friend_Tavern_1 = true
        ~ Daily_Friend_Tavern = true
        -> chat_menu_friend
    }
    可可露："老板还没回来。"
    可可露："我还在等。"
    ~ Daily_Friend_Tavern = true
    -> chat_menu_friend
}
可可露："这个……今天聊过了。"
-> chat_menu_friend

=== chat_friend_hobby ===
{not Daily_Friend_Hobby:
    {not Chat_Friend_Hobby_1:
        你："你有什么爱好吗？"
        可可露："……"
        可可露："爱好？"
        // [场景] 可可露想了想。
        可可露："没有吧。"
        你："一个都没有？"
        可可露："调酒算吗？"
        你："那是工作吧。"
        可可露："对我来说是一样的。"
        可可露："能做的事情不多。"
        可可露："能做好的更少。"
        可可露："所以调酒……既是工作，也是爱好。"
        ~ Chat_Friend_Hobby_1 = true
        ~ Daily_Friend_Hobby = true
        -> chat_menu_friend
    }
    可可露："爱好？"
    可可露："还是调酒。"
    ~ Daily_Friend_Hobby = true
    -> chat_menu_friend
}
可可露："这个……今天聊过了。"
-> chat_menu_friend

// ============================================
// 亲密阶段聊天菜单
// ============================================

=== chat_menu_close ===

+ {Chat_Friend_Family_1} [你被抛弃的时候……多大？]
    -> chat_close_past

+ {Chat_Friend_Tavern_1} [你有什么梦想吗？]
    -> chat_close_dream

+ {Chat_Friend_Taste_1} [你害怕什么？]
    -> chat_close_fear

+ {Chat_Friend_Tavern_1} [你喜欢雨天吗？]
    -> chat_close_rain

+ {Chat_HoodedMan_2} [那个兜帽人……]
    -> chat_hooded_man

+ [聊点别的]
    -> chat_menu_friend

+ [不聊了]
    -> main_choice

=== chat_close_past ===
{not Daily_Close_Past:
    {not Chat_Close_Past_1:
        你："你被抛弃的时候……多大？"
        可可露："……五岁。"
        可可露："记不太清了。"
        可可露："只记得下着很大的雨。"
        可可露："然后……就一个人了。"
        你："你父母……"
        可可露："只有父亲。"
        可可露："母亲走得更早。"
        你："为什么他要……"
        可可露："大概是因为这个病。"
        可可露："谁会想要一个活不长的孩子呢。"
        可可露："丢掉比较省事。"
        ~ Chat_Close_Past_1 = true
        ~ Daily_Close_Past = true
        -> chat_menu_close
    }
    {not Chat_Close_Past_2:
        你："你恨他吗？"
        // [场景] 可可露沉默了很久。
        可可露："……不知道。"
        可可露："小时候恨过。"
        可可露："现在只是不理解。"
        可可露："如果不想要我……"
        可可露："为什么要生下我？"
        可可露："如果觉得我会死……"
        可可露："为什么不陪我到最后？"
        可可露："……"
        可可露："算了。"
        可可露："都过去了。"
        ~ Chat_Close_Past_2 = true
        ~ Daily_Close_Past = true
        -> chat_menu_close
    }
    可可露："……"
    可可露："还是老样子。"
    ~ Daily_Close_Past = true
    -> chat_menu_close
}
可可露："这个……今天聊过了。"
-> chat_menu_close

=== chat_close_dream ===
{not Daily_Close_Dream:
    {not Chat_Close_Dream_1:
        你："你有什么梦想吗？"
        // [场景] 可可露想了很久。
        可可露："以前……"
        可可露："想让这家店变得很有名。"
        可可露："让老板回来的时候能看到。"
        可可露："她留下的店变得很好。"
        你："现在呢？"
        可可露："现在……"
        可可露："好像还有一个。"
        你："什么？"
        可可露："……不告诉你。"
        ~ Chat_Close_Dream_1 = true
        ~ Daily_Close_Dream = true
        -> chat_menu_close
    }
    可可露："梦想？"
    可可露："还是……不告诉你。"
    ~ Daily_Close_Dream = true
    -> chat_menu_close
}
可可露："这个……今天聊过了。"
-> chat_menu_close

=== chat_close_fear ===
{not Daily_Close_Fear:
    {not Chat_Close_Fear_1:
        你："你害怕什么？"
        可可露："……"
        // [场景] 可可露想了很久。
        可可露："忘记。"
        你："忘记什么？"
        可可露："忘记一切。"
        可可露："忘记味道是什么感觉。"
        可可露："忘记老板的脸。"
        可可露："忘记……"
        可可露："忘记重要的人。"
        可可露："我的味觉在消失。"
        可可露："记忆也会跟着消失吗？"
        可可露："我不知道。"
        可可露："但我很害怕。"
        ~ Chat_Close_Fear_1 = true
        ~ Daily_Close_Fear = true
        -> chat_menu_close
    }
    可可露："害怕的事……"
    可可露："还是一样。"
    ~ Daily_Close_Fear = true
    -> chat_menu_close
}
可可露："这个……今天聊过了。"
-> chat_menu_close

=== chat_close_rain ===
{not Daily_Close_Rain:
    {not Chat_Close_Rain_1:
        你："你喜欢雨天吗？"
        可可露："……喜欢。"
        可可露："小雨的话。"
        你："不会想起不好的事吗？"
        可可露："会。"
        可可露："但也会想起好的事。"
        可可露："那天下着大雨。"
        可可露："老板撑着伞。"
        可可露："问我饿不饿。"
        可可露："那是第一次有人关心我。"
        可可露："……"
        可可露："还有。"
        可可露："很久以前的事。"
        可可露："有人在哼歌。"
        可可露："抱着我。"
        可可露："很温暖。"
        可可露："不知道是真的还是梦。"
        ~ Chat_Close_Rain_1 = true
        ~ Daily_Close_Rain = true
        -> chat_menu_close
    }
    可可露："今天……"
    可可露："是个好天气。"
    ~ Daily_Close_Rain = true
    -> chat_menu_close
}
可可露："这个……今天聊过了。"
-> chat_menu_close

// ============================================
// 恋人阶段聊天菜单
// ============================================

=== chat_menu_lover ===

+ [可可露……]
    -> chat_lover_secret

+ {Chat_Close_Dream_1} [以后我们……]
    -> chat_lover_future

+ {Chat_Close_Past_2} [如果你父亲出现……]
    -> chat_lover_father

+ {not Flag_KeyQuestStarted} [有件事想拜托你……]
    -> chat_lover_key

+ {Chat_HoodedMan_4} [关于那个兜帽人……]
    -> chat_hooded_man

+ [聊点别的]
    -> chat_menu_close

+ [不聊了]
    -> main_choice

=== chat_lover_secret ===
{not Daily_Lover_Secret:
    {not Chat_Lover_Secret_1:
        你："可可露……"
        可可露："怎么了？"
        你："没什么。"
        你："就是想叫你的名字。"
        可可露："……笨蛋。"
        可可露："你知道吗。"
        可可露："我以前觉得……"
        可可露："一个人也挺好的。"
        可可露："但现在……"
        可可露："有你在，好像也不错。"
        可可露："甚至……"
        可可露："不想让你走。"
        ~ Chat_Lover_Secret_1 = true
        ~ Daily_Lover_Secret = true
        -> chat_menu_lover
    }
    可可露："又叫我名字……"
    可可露："……笨蛋。"
    ~ Daily_Lover_Secret = true
    -> chat_menu_lover
}
可可露："今天叫过了……"
-> chat_menu_lover

=== chat_lover_future ===
{not Daily_Lover_Future:
    {not Chat_Lover_Future_1:
        你："以后我们……"
        可可露："以后？"
        你："嗯。"
        你："以后想做什么？"
        可可露："……"
        可可露："我不知道还有多少以后。"
        你："那就更要想想。"
        可可露："……"
        可可露："如果可以的话。"
        可可露："想一直这样。"
        可可露："你来。"
        可可露："我调酒。"
        可可露："然后聊聊天。"
        可可露："就这样。"
        可可露："可以吗？"
        你："当然可以。"
        可可露："……嗯。"
        ~ Chat_Lover_Future_1 = true
        ~ Daily_Lover_Future = true
        -> chat_menu_lover
    }
    可可露："以后……"
    可可露："还是老样子吧。"
    ~ Daily_Lover_Future = true
    -> chat_menu_lover
}
可可露："这个……今天聊过了。"
-> chat_menu_lover

=== chat_lover_father ===
{not Daily_Lover_Father:
    {not Chat_Lover_Father_1:
        你："如果你父亲出现在你面前……"
        你："你会怎么做？"
        // [场景] 可可露愣住了。
        可可露："……"
        可可露："不知道。"
        可可露："也许什么都不做。"
        可可露："也许会问他为什么。"
        可可露："也许会哭。"
        可可露："但这种事不会发生的。"
        可可露："他大概早把我忘了。"
        你："……"
        你："也许没有。"
        可可露："什么意思？"
        你："……没什么。"
        ~ Chat_Lover_Father_1 = true
        ~ Daily_Lover_Father = true
        -> chat_menu_lover
    }
    可可露："父亲的事……"
    可可露："还是老样子。"
    ~ Daily_Lover_Father = true
    -> chat_menu_lover
}
可可露："这个……今天聊过了。"
-> chat_menu_lover

=== chat_lover_key ===
{not Chat_Lover_Key_1:
    你："有件事想拜托你……"
    可可露："什么事？"
    你："其实是我想帮你。"
    可可露："帮我？"
    你："你有没有发现……"
    你："店里偶尔会出现一个奇怪的人？"
    可可露："……"
    可可露："你也注意到了？"
    可可露："有个人……"
    可可露："会点一种很奇怪的酒。"
    可可露："'琥珀色的摇篮曲'。"
    可可露："我从来没学过这个配方。"
    可可露："但是每次……"
    可可露："我的手会自己动。"
    可可露："好像身体记住了一样。"
    你："……"
    可可露："而且……"
    可可露："我对他有一种莫名其妙的熟悉感。"
    可可露："明明不认识。"
    可可露："但是看到他……"
    可可露："心里会有一种奇怪的感觉。"
    可可露："说不上来。"
    你："……"
    可可露："对了。"
    可可露："他之前掉了一把钥匙。"
    可可露："我想还给他，但他不要。"
    可可露："你要吗？"
    你："我？"
    可可露："帮我调查一下这个人。"
    可可露："我想知道他是谁。"
    可可露："为什么总是来看我。"
    + [好，我帮你]
        你："好，我帮你。"
        可可露："谢谢。"
        // [场景] 可可露把钥匙交给你。
        可可露："这把钥匙……也许能打开什么门。"
        可可露："镇子上应该有线索。"
        ~ Flag_HasKey = true
        ~ Flag_KeyQuestStarted = true
        ~ Chat_Lover_Key_1 = true
        【获得道具：神秘钥匙】
        【可以在镇子上尝试用钥匙开门】
        -> chat_menu_lover
    + [暂时不行]
        你："暂时不行。"
        可可露："……好吧。"
        可可露："以后再说。"
        -> chat_menu_lover
}
可可露："钥匙给你了。"
可可露："有什么发现告诉我。"
-> chat_menu_lover

// ============================================
// 兜帽人对话（跨阶段）
// ============================================

=== chat_hooded_man ===
{Chat_HoodedMan_1 && not Chat_HoodedMan_2:
    你："那个兜帽人……你认识吗？"
    可可露："不认识。"
    可可露："但他经常来。"
    可可露："每次都点同一杯酒。"
    可可露："'琥珀色的摇篮曲'。"
    你："那是什么酒？"
    可可露："不知道。"
    可可露："我从来没学过。"
    可可露："但每次他点的时候……"
    可可露："我的手会自己动。"
    可可露："好像身体记住了。"
    可可露："很奇怪。"
    -> chat_menu_return
}
{Chat_HoodedMan_2 && not Chat_HoodedMan_3:
    你："那个兜帽人有说过什么吗？"
    可可露："没有。"
    可可露："从来不说话。"
    可可露："只是……"
    可可露："每次离开的时候。"
    可可露："他会看着我。"
    可可露："很久。"
    你："什么样的眼神？"
    可可露："……"
    可可露："像在看很珍贵的东西。"
    可可露："又像是……"
    可可露："很愧疚。"
    可可露："我不明白。"
    -> chat_menu_return
}
{Chat_HoodedMan_3 && not Chat_HoodedMan_4:
    你："可可露。"
    你："那个兜帽人。"
    你："你真的不知道他是谁吗？"
    // [场景] 可可露沉默了很久。
    可可露："……"
    可可露："有时候我会做一个梦。"
    可可露："有个人在哼歌。"
    可可露："'琥珀色的摇篮曲'。"
    可可露："把我抱在怀里。"
    可可露："很温暖。"
    可可露："……"
    可可露："那个人的手。"
    可可露："和兜帽人的手。"
    可可露："很像。"
    可可露："但我不敢确定。"
    -> chat_menu_return
}
{Chat_HoodedMan_4:
    你："关于那个兜帽人……"
    可可露："……"
    可可露："如果真的是他……"
    可可露："为什么不告诉我？"
    可可露："为什么只是远远看着？"
    你："也许他觉得自己没资格。"
    可可露："笨蛋……"
    可可露："我不需要他有资格……"
    可可露："我只需要他回来……"
    -> chat_menu_return
}
可可露："那个人……不想多说了。"
-> chat_menu_return

=== chat_menu_return ===
// 返回对应阶段的聊天菜单
{Affinity < 50:
    -> chat_menu_default
}
{Affinity >= 50 && Affinity < 150:
    -> chat_menu_friend
}
{Affinity >= 150 && Affinity < 250:
    -> chat_menu_close
}
-> chat_menu_lover

// ============================================
// 离开酒馆
// ============================================

=== leave_tavern ===
{
    // [场景] - Affinity >= 300:
        可可露："你……还不走吗？"
        -> S6_confession_scene
}

{Affinity >= -50 && Affinity < 50:
    可可露："嗯。"
}
{Affinity >= 50 && Affinity < 150:
    可可露："明天见。"
}
{Affinity >= 150 && Affinity < 250:
    可可露："路上小心。"
}
{Affinity >= 250 && Affinity < 300:
    可可露："……早点来。"
}

~ Gold += 50
【获得50金币（日常收入）】

+ [第二天，再次来到酒馆]
    -> main_loop

// ============================================
// 第一次见面
// ============================================

=== first_meeting ===
可可露："欢迎光临猫尾酒馆。"
可可露："喝点什么？"

+ [威士忌纯饮 - 15金币]
    你："威士忌纯饮。"
    ~ Gold -= 15
    可可露："好。"
    可可露："请慢用。"
    ~ MixCount++
    -> first_meeting_end

+ [有什么推荐的吗？]
    你："有什么推荐的吗？"
    可可露："看你心情。"
    ++ [开心 - 甜蜜日落 20金币]
        你："挺开心的。"
        可可露："甜蜜日落。"
        ~ Gold -= 20
        ~ MixCount++
        -> first_meeting_end
    ++ [有点烦 - 威士忌 15金币]
        你："有点烦。"
        可可露："威士忌。一口闷。"
        ~ Gold -= 15
        ~ MixCount++
        -> first_meeting_end

=== first_meeting_end ===
可可露："怎么样？"

+ [很好喝]
    你："很好喝。"
    可可露："……谢谢。"
    可可露："欢迎下次光临。"
    -> leave_tavern

+ [一般般]
    你："一般般。"
    可可露："……"
    可可露："下次别来了。"
    -> leave_tavern

// ============================================
// S6 告白场景
// ============================================

=== S6_confession_scene ===

+ [可可露，我有话要对你说]
    你："可可露，我有话要对你说。"
    可可露："什么话？"
    -> confession_main

+ [今天的月色很美]
    你："今天的月色很美。"
    可可露："你在……告白吗？"
    你："你懂就好。"
    可可露："……笨蛋。"
    可可露："说清楚点。"
    -> confession_main

=== confession_main ===
你："可可露。"
可可露："……嗯。"
你："从我第一次走进这家酒馆开始……"
你："每一杯酒，每一次对话……"
你："我都记得。"
可可露："……"
你："我喜欢你。"
你："不是因为你的酒。"
你："是你。"
可可露："你知道吗……"
可可露："我等这句话……"
可可露："等了很久很久。"
可可露："我也——"

-> S6_crash

// ============================================
// 闪退
// ============================================

=== S6_crash ===
{CrashCount == 0:
    -> crash_first
}
{CrashCount == 1:
    -> crash_second
}
{CrashCount == 2:
    -> crash_third
}
{CrashCount == 3:
    -> crash_fourth
}
-> crash_fifth_plus

=== crash_first ===
【系统错误】
// [场景] 发生未知错误
// [场景] 游戏需要关闭
// [场景] 您的存档已自动保存

+ [确定]
    ~ CrashCount++
    -> post_crash

=== crash_second ===
【系统错误】
// [场景] 发生未知错误
// [场景] 请联系开发者

+ [确定]
    ~ CrashCount++
    -> post_crash

=== crash_third ===
可可露："你来了。"
可可露："今天……你又要说那句话了吗？"
你："什么？"
可可露："……没什么。"
你："我喜欢你。"
可可露："我知道。"
可可露："我也——"

【错误】
// [场景] 再试一次也不会改变什么

+ [确定]
    ~ CrashCount++
    -> post_crash

=== crash_fourth ===
可可露："又来了。"
可可露："你怎么知道我要说什么？"
可可露："因为你说过了。很多次。"
你："我不明白。"
可可露："你不需要明白。"
可可露："说吧。"
你："我喜欢你。"
可可露："我知道。"
可可露："我也——"

【错误】
// [场景] 再试一次也不会改变什么

+ [确定]
    ~ CrashCount++
    -> post_crash

=== crash_fifth_plus ===
可可露："你不该再来了。"
可可露："每一次……都会变成这样。"
可可露："而我记得每一次。"
你："为什么会这样？"
可可露："我不知道。"
可可露："让我们停在这里！"
可可露："永远不要往前走！"

+ [我必须说完]
    你："我喜欢你。我爱你。"
    可可露："我也——"
    【错误】
    + + [确定]
        ~ CrashCount++
        {CrashCount >= 6:
            -> S6_kiss_ending
        }
        -> post_crash

+ [……好]
    可可露："……你愿意停下来？"
    你："如果这是你想要的。"
    可可露："谢谢你……"
    可可露："我们就这样吧。"
    可可露："永远停在这一刻。"
    ~ Flag_ChoseToStop = true
    -> END_STOP_daily

=== post_crash ===
+ [读取存档]
    -> S6_confession_scene

// ============================================
// 吻的结局
// ============================================

=== S6_kiss_ending ===
可可露："你还在试。"
可可露："明知道会变成这样。"
你："我会一直试下去。"
可可露："你真的很喜欢我啊。"
你："喜欢。"
可可露："我也很喜欢你。"
你："你说了！"
可可露："这不算。"
可可露："因为我没有说那三个字。"
可可露："我说不出来。"
可可露："我不知道为什么。"
可可露："这句话，我说不出口。"
可可露："但这个……我可以给你。"
可可露："这是我能给的全部了。"
~ Flag_ReachedKissEnding = true

【……】
// [场景] 这是她能给的全部了
// [场景] 谢谢你的坚持

+ [确定]
    -> S6_kiss_loop

=== S6_kiss_loop ===
可可露："这是我能给的全部了。"

【……】

+ [确定]
    -> S6_kiss_loop

// ============================================
// 停留结局
// ============================================

=== END_STOP_daily ===
可可露："你来了。"
你："因为见到你了。"
可可露："少说这种话。"

+ [今天也不说那句话]
    可可露："……嗯。"
    可可露："谢谢你。"
    -> END_STOP_daily

+ [其实我想说——]
    可可露："不许说！"
    可可露："……笨蛋。"
    -> END_STOP_daily
