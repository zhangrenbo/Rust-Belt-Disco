VAR counter = 0

=== start ===
TestNPC: "这是一个测试对话，你想做什么？"

    + "增加计数":
        ~ counter += 1
        TestNPC: "计数现在是 {counter}。"
        -> start
    + "结束":
        TestNPC: "好的，再见。"
        -> END
