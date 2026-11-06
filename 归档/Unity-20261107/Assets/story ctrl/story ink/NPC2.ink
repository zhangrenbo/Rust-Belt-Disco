INCLUDE globals.ink
=== NPC2 ===
NPC2: "你好啊，你身上有钥匙吗？"
+ "我身上有钥匙":
    { keys < 1:
        NPC2: "不对吧，你没有钥匙"
    - else:
        NPC2: "太好了，那你可以进入米奇妙妙屋了"
    }
    -> END
+ "我身上没有钥匙":
    NPC2: "那你找到再说吧"
    -> END
