INCLUDE globals.ink

=== TestNPC ===
TestNPC: "这是一个测试对话，你想做什么？"
+ "给我一把钥匙":
    ~ keys += 1
    TestNPC: "你现在有 {keys} 把钥匙。"
    -> END
+ "没事，随便看看":
    TestNPC: "好吧，随便看看。"
    -> END
