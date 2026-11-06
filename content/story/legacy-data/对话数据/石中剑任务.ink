// ===========================================
// 石中剑（奸商）对话：纯对话版（Inky / Ink）
// （已删除：1）传说讲详细点 2）免费要收据）
// ===========================================

// ---- 属性（可由外部系统覆盖）----
VAR money = 50
VAR strength = 3
VAR charm = 3

// ---- 状态 ----
VAR stone_first_time = true
VAR stone_fail_count = 0
VAR stone_discount_unlocked = false
VAR stone_has_fake_sword = false
VAR stone_knows_blue = false

// ---- 本次价格 ----
VAR stone_price = 20



-> stone_entry


== stone_entry ==
~ stone_price = 20

{stone_first_time:
    -> stone_first_time_dialogue
- else:
    {stone_discount_unlocked:
        -> stone_discount_dialogue
    - else:
        -> stone_repeat_dialogue
    }
}


== stone_first_time_dialogue ==
奸商：哟哟哟！勇者大人！
奸商：我听牧柠说过了，您就是最新的勇者模仿者对吧！

* [对，就是我。]
    -> stone_first_impostor
* [不，我是真正的勇者。]
    -> stone_first_realhero
* [我只是随便看看。]
    -> stone_first_offer


== stone_first_impostor ==
奸商：可以啊！勇者的模仿者！
奸商：你知道勇者最伟大的是什么吗？

* [是他的品格？]
    奸商：对的！对的！勇者是一个永不服输的人！
    奸商：也就是说，如果勇者一次没拔出来，他一定会反反复复来拔！
    -> stone_first_offer
* [是他的力量！]
    奸商：力量也对！非常对！
    奸商：那就更该拔一下证明自己了！
    -> stone_first_offer
* [我觉得他是普通人。]
    奸商：额……也行，你也能这么觉得。
    奸商：普通人更需要“仪式感”来变得不普通！
    -> stone_first_offer


== stone_first_realhero ==
奸商：唉！对！对！您就是真正的勇者！
奸商：那么勇者，就一定要和一百年前的传说一样，把这把石中剑拔出来吧！

* [这剑很难拔吗？]
    奸商：传说里当然难！
    奸商：只有真正的勇者才能——才能……把它拔出来！
    -> stone_first_offer
* [我不是什么勇者。]
    奸商：你当然可以这么说！
    奸商：但你去拔一下就知道了！
    -> stone_first_offer


== stone_first_offer ==
奸商：怎么说？我们的拔取石中剑服务，一次20金币。
奸商：看在我第一次和你对话的份上——这一次免费！

* [你这个东西还收费？]
    -> stone_first_charge_talk
* [算了，我试试吧（免费）]
    -> stone_pull_free
* [那还是算了。]
    奸商：好的客官，我猜客官是没有钱吧！
    奸商：没有钱，怎么玩COSPLAY啊！
    -> stone_end


== stone_first_charge_talk ==
奸商：哎呀！我们现在早就开放了！
奸商：我们也是军火商，我们也要做生意嘛！

* [这不符合我对勇者精神的理解。]
    奸商：哎呀，这怎么了嘛！
    奸商：我们自从放开后，苔语镇经济挺好的！
    奸商：至少我这个人是盆满钵满的。
    * [算了，我试试吧（免费）]
        -> stone_pull_free
    * [我不拔了。]
        -> stone_end

* [行吧，反正你说免费。]
    -> stone_pull_free

* [那我突然不想当勇者了。]
    奸商：好的客官，我猜客官是没有钱吧！
    奸商：没有钱，怎么玩COSPLAY啊！
    -> stone_end


// =====================
// 后续触发（20金币）
// =====================
== stone_repeat_dialogue ==
奸商：怎么说！勇者大人，还要试吗？
奸商：老规矩，一次20金币。

* {money >= 20} [好，我拔了（消耗20金币）]
    ~ stone_price = 20
    -> stone_pull_pay
* {money < 20} [好，我拔了（但我没钱）]
    奸商：哎哟，你这是“精神很勇，钱包很怂”。
    奸商：攒够钱再来吧！没有钱，怎么玩COSPLAY啊！
    -> stone_end

* [能便宜一点吗？（砍价）]
    -> stone_bargain_1

* [我不拔，我来聊天。]
    -> stone_chat

* [我能不付钱直接拔吗？]
    -> stone_steal_try

* [算了，不拔了。]
    -> stone_end


// =====================
// 失败三次后折扣（8金币）
// =====================
== stone_discount_dialogue ==
奸商：哎呦喂！您又来了！
奸商：看你是老顾客了，给你打个折——以后一次8金币！

* {money >= 8} [好，我拔了（消耗8金币）]
    ~ stone_price = 8
    -> stone_pull_pay
* {money < 8} [8金币也没有……]
    奸商：……
    奸商：小孩，你现在的勇者精神叫“永不付钱”。
    -> stone_end

* [能便宜一点吗？]
    奸商：不行不行！8金币是底线！
    * [好吧那我拔（消耗8金币）]
        ~ stone_price = 8
        -> stone_pull_pay
    * [算了。]
        -> stone_end

* [我还是算了。]
    -> stone_end


// =====================
// 砍价（两段：15 / 12）
// =====================
== stone_bargain_1 ==
奸商：便宜？你想多了！
奸商：不过……你说说看？

~ temp roll = RANDOM(1, 20) + charm
{roll >= 16:
    奸商：行行行！听你的，勇者也是要精打细算的嘛！
    奸商：那我给你更低的价格，15金币，怎么样？
    ~ stone_price = 15

    * {money >= 15} [好，我拔了（消耗15金币）]
        -> stone_pull_pay
    * {money < 15} [我突然觉得15也很贵……]
        奸商：你这不叫砍价，你这叫“砍掉购买力”。
        -> stone_end
    * [我再砍一次。]
        -> stone_bargain_2
- else:
    奸商：不行！你是勇者，怎么能这么小气！
    * {money >= 20} [行吧我按20来（消耗20金币）]
        ~ stone_price = 20
        -> stone_pull_pay
    * [那算了。]
        -> stone_end
}


== stone_bargain_2 ==
奸商：你还砍？你这勇者精神也太“节俭”了吧！

~ temp roll2 = RANDOM(1, 20) + charm
{roll2 >= 18:
    奸商：……行！你赢了！
    奸商：12金币！到顶了！再砍我就要改行做慈善了！
    ~ stone_price = 12

    * {money >= 12} [好，我拔了（消耗12金币）]
        -> stone_pull_pay
    * {money < 12} [12也没有。]
        奸商：那你别当勇者了，你当传说吧——传说不用付钱。
        -> stone_end
    * [我还想砍。]
        奸商：不行！再说我就开始怀疑你不是勇者，是会计！
        -> stone_repeat_dialogue
- else:
    奸商：不行！再砍我就叫人了！
    * {money >= 20} [好好好，我按20来（消耗20金币）]
        ~ stone_price = 20
        -> stone_pull_pay
    * [算了。]
        -> stone_end
}


// =====================
// 想白嫖直接拔
// =====================
== stone_steal_try ==
奸商：等等等等！流程要对！仪式感要有！
奸商：你不付钱，我这边很难“相信你是勇者”。

* [用魅力说服：真正的勇者不该被收费。]
    ~ temp r = RANDOM(1, 20) + charm
    {r >= 18:
        奸商：……你这句话说得太有道理了。
        奸商：行！今天我做慈善！就一次！
        -> stone_pull_free
    - else:
        奸商：不行！你是勇者，怎么能这么小气！
        -> stone_repeat_dialogue
    }
* [威胁：我要举报你。]
    奸商：举报就举报！我这儿连店名都没有！
    -> stone_repeat_dialogue
* [道歉，按规矩来。]
    -> stone_repeat_dialogue
* [走了。]
    -> stone_end


// =====================
// 聊天（纯对话）
// =====================
== stone_chat ==
奸商：聊天也行！聊天不收费！
奸商：不过你聊到我不爱听的，我可能就开始收费了。

* [苔语镇经济真被你带起来了？]
    奸商：当然！至少我的经济被带起来了！
    -> stone_chat
* [牧柠为什么会跟你提我？]
    奸商：因为她懂你——你这种人会来花钱。
    -> stone_chat
* [你到底是军火商还是导游？]
    奸商：我是梦想供应商。
    -> stone_chat
* [蓝三郎到底在卖什么？]
    奸商：卖“真相”。真相比赝品贵。
    -> stone_chat
* [不聊了。]
    -> stone_repeat_dialogue


// =====================
// 拔剑：免费 / 付费
// =====================
== stone_pull_free ==
奸商：来！第一次免费体验！
-> stone_pull_core


== stone_pull_pay ==
{money < stone_price:
    奸商：等一下……你钱包怎么是空的？
    奸商：你这是来扮演勇者，还是来扮演穷？
    -> stone_end
- else:
    ~ money -= stone_price
    奸商：好嘞！收到{stone_price}金币！
    -> stone_pull_core
}


== stone_pull_core ==
【系统】力量检定开始。
~ temp roll = RANDOM(1, 20) + strength
~ temp big_success = roll >= 25
~ temp success = roll >= 18
~ temp big_fail = roll <= 3

{big_success:
    -> stone_result_big_success
- else:
    {success:
        -> stone_result_success
    - else:
        {big_fail:
            -> stone_result_big_fail
        - else:
            -> stone_result_fail
        }
    }
}


// =====================
// 四档结果
// =====================
== stone_result_big_success ==
奸商：额！我的天呐！
奸商：你真的拔出来了！
~ stone_has_fake_sword = true
~ stone_knows_blue = true

奸商：额，好吧，这把剑就归你了！别……别清算我！
奸商：您的真的那把宝剑在蓝三郎那里！
奸商：这里的只是一个赝品！去找他！都是他害的！不关我事！

* [“赝品”？你刚才怎么说的？]
    奸商：我说的是“传说体验”！不是“真剑产权”！
    -> stone_after_success
* [蓝三郎是谁？]
    奸商：你肯定熟悉！毕竟是你的部将！
    -> stone_after_success
* [行，我去找蓝三郎。]
    -> stone_end
* [我再拔一次。]
    奸商：你……你拔都拔出来了还拔？
    -> stone_entry


== stone_result_success ==
奸商：哇！你真的拔出来了！
~ stone_has_fake_sword = true
~ stone_knows_blue = true

奸商：但是我提醒你一句：
奸商：石头是真的石头，但剑是赝品。
奸商：真正的剑在蓝三郎那里，你肯定熟悉，毕竟是你的部将。

* [赝品也行，能用就行。]
    奸商：你这句话听着很危险，但我当没听见。
    -> stone_after_success
* [那你还收钱？]
    奸商：收的是拔剑体验，不是保真服务。
    -> stone_after_success
* [我去找蓝三郎。]
    -> stone_end
* [我再拔一次。]
    奸商：你别把我这儿当训练场！
    -> stone_entry


== stone_result_fail ==
奸商：哎哟！您看啊！就差那么一点点！
奸商：下次拔剑我还在这里等你！
~ stone_fail_count += 1

{stone_fail_count >= 3 and not stone_discount_unlocked:
    ~ stone_discount_unlocked = true
    奸商：……咳！你这都失败三次了。
    奸商：算你老顾客！我给你打折！以后一次8金币！
    -> stone_discount_dialogue
- else:
    * [再试一次。]
        -> stone_entry
    * [能不能给我点技巧？]
        奸商：技巧当然有：
        奸商：第一，付钱要果断。
        奸商：第二，拔剑要用力。
        奸商：第三，如果拔不出来，就回到第一。
        -> stone_repeat_dialogue
    * [走了。]
        -> stone_end
}


== stone_result_big_fail ==
奸商：哇偶！
奸商：难道这把剑是赝品，所以它也能精准识别到你也是赝品吗？
~ stone_fail_count += 1

* [你说谁赝品？！]
    奸商：我说的是剑！剑！剑是赝品！
    {stone_fail_count >= 3 and not stone_discount_unlocked:
        ~ stone_discount_unlocked = true
        奸商：……咳！你这都失败三次了。
        奸商：算你老顾客！以后一次8金币！
        -> stone_discount_dialogue
    - else:
        -> stone_repeat_dialogue
    }
* [当没听见，再来。]
    {stone_fail_count >= 3 and not stone_discount_unlocked:
        ~ stone_discount_unlocked = true
        奸商：……行吧行吧，别难过。
        奸商：你都失败三次了，以后一次8金币！
        -> stone_discount_dialogue
    - else:
        -> stone_entry
    }
* [走了。]
    {stone_fail_count >= 3 and not stone_discount_unlocked:
        ~ stone_discount_unlocked = true
    }
    -> stone_end


// =====================
// 成功后的小菜单（纯对话）
// =====================
== stone_after_success ==
奸商：那……你还有别的需要吗？

* [我想退货。]
    奸商：退？可以退！
    奸商：但我只退“心态”，不退钱。
    -> stone_after_success
* [你为什么这么怕“清算”？]
    奸商：我不怕！我只是……对历史比较敏感！
    -> stone_after_success
* [再来一次。]
    -> stone_entry
* [走了。]
    -> stone_end


== stone_end ==
~ stone_first_time = false
-> END
