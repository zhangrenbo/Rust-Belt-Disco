INCLUDE globals.ink
=== NPC1 ===
NPC1: "你要干嘛？"
+ "我需要钥匙":
    ~ keys += 1
    NPC1: "好的，给你（钥匙数量+1）"
    -> END
+ "你愣着干嘛，不上班啊":
    NPC1: "我是纸片人，又不是你们三次元的社畜"
    -> END
