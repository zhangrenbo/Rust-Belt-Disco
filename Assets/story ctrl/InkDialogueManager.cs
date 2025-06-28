using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Ink.Runtime;

/// <summary>
/// Ink对话管理器 - 完整版本
/// </summary>
public class InkDialogueManager : MonoBehaviour
{
    public static InkDialogueManager Instance { get; private set; }

    [Header("=== 对话UI设置 ===")]
    public GameObject dialoguePanel;
    public Text dialogueText;
    public Text speakerNameText;
    public Button continueButton;
    public Transform choiceButtonContainer;
    public Button choiceButtonPrefab;

    [Header("=== 音效设置 ===")]
    public AudioClip dialogueStartSound;
    public AudioClip dialogueEndSound;
    public AudioClip choiceSelectSound;

    [Header("=== 打字机效果 ===")]
    public bool enableTypewriterEffect = true;
    public float typewriterSpeed = 0.05f;
    public AudioClip typewriterSound;

    [Header("=== 调试设置 ===")]
    public bool showDebugInfo = false;

    // 当前对话状态
    private Story currentStory;
    private bool isDialogueActive = false;
    private Coroutine typewriterCoroutine;
    private List<Button> choiceButtons = new List<Button>();

    // 事件系统
    public System.Action<string> OnDialogueStart;
    public System.Action<string> OnDialogueEnd;
    public System.Action<string, int> OnChoiceMade;
    public System.Action<string, object> OnInkVariableChanged;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeUI();
    }

    void InitializeUI()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (continueButton != null)
            continueButton.onClick.AddListener(ContinueDialogue);

        if (choiceButtonContainer != null)
            choiceButtonContainer.gameObject.SetActive(false);
    }

    /// <summary>
    /// 开始Ink对话
    /// </summary>
    public void StartDialogue(TextAsset inkFile, string startKnot = "")
    {
        if (inkFile == null)
        {
            Debug.LogError("[InkDialogueManager] Ink文件为空！");
            return;
        }

        try
        {
            currentStory = new Story(inkFile.text);

            // 如果指定了起始节点，跳转到该节点
            if (!string.IsNullOrEmpty(startKnot))
            {
                currentStory.ChoosePathString(startKnot);
            }

            // 绑定外部函数
            BindExternalFunctions();

            // 同步游戏状态到Ink变量
            SyncGameStateToInk();

            isDialogueActive = true;
            dialoguePanel?.SetActive(true);

            // 通知玩家进入对话状态
            var player = FindObjectOfType<PlayerController>();
            player?.EnterDialogueState();

            OnDialogueStart?.Invoke(inkFile.name);
            PlaySound(dialogueStartSound);

            if (showDebugInfo)
            {
                Debug.Log($"[InkDialogueManager] 开始Ink对话: {inkFile.name}");
            }

            ContinueDialogue();

        }
        catch (System.Exception e)
        {
            Debug.LogError($"[InkDialogueManager] 启动对话失败: {e.Message}");
        }
    }

    /// <summary>
    /// 继续对话
    /// </summary>
    public void ContinueDialogue()
    {
        if (!isDialogueActive || currentStory == null) return;

        // 如果正在打字，跳过打字效果
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;

            if (dialogueText != null && currentStory.currentText != null)
            {
                dialogueText.text = ProcessTags(currentStory.currentText);
                ShowContinueButton(true);
                return;
            }
        }

        if (currentStory.canContinue)
        {
            string text = currentStory.Continue();

            // 处理标签
            string processedText = ProcessTags(text);

            if (enableTypewriterEffect)
            {
                typewriterCoroutine = StartCoroutine(TypewriterEffect(processedText));
            }
            else
            {
                if (dialogueText != null)
                    dialogueText.text = processedText;
                ShowContinueButton(true);
            }

            // 处理Ink变量变化
            CheckInkVariables();

            // 检查是否有选择
            if (currentStory.currentChoices.Count > 0)
            {
                ShowChoices();
            }
        }
        else
        {
            EndDialogue();
        }
    }

    /// <summary>
    /// 处理Ink标签 (如角色名等)
    /// </summary>
    string ProcessTags(string text)
    {
        if (currentStory.currentTags.Count > 0)
        {
            foreach (string tag in currentStory.currentTags)
            {
                // 处理角色名标签 # speaker: 角色名
                if (tag.StartsWith("speaker:"))
                {
                    string speakerName = tag.Substring(8).Trim();
                    if (speakerNameText != null)
                    {
                        speakerNameText.text = speakerName;
                        speakerNameText.gameObject.SetActive(true);
                    }
                }
                // 处理其他自定义标签
                else if (tag.StartsWith("sound:"))
                {
                    string soundName = tag.Substring(6).Trim();
                    PlaySoundByName(soundName);
                }
                else if (tag.StartsWith("emotion:"))
                {
                    string emotion = tag.Substring(8).Trim();
                    // 可以在这里处理角色表情变化
                    if (showDebugInfo)
                        Debug.Log($"角色情感: {emotion}");
                }
            }
        }
        else
        {
            // 没有标签时隐藏角色名
            if (speakerNameText != null)
                speakerNameText.gameObject.SetActive(false);
        }

        return text;
    }

    /// <summary>
    /// 打字机效果协程
    /// </summary>
    IEnumerator TypewriterEffect(string text)
    {
        if (dialogueText == null) yield break;

        dialogueText.text = "";
        ShowContinueButton(false);

        foreach (char c in text)
        {
            dialogueText.text += c;
            PlaySound(typewriterSound);
            yield return new WaitForSeconds(typewriterSpeed);
        }

        ShowContinueButton(true);
        typewriterCoroutine = null;
    }

    /// <summary>
    /// 显示选择按钮
    /// </summary>
    void ShowChoices()
    {
        ShowContinueButton(false);

        if (choiceButtonContainer != null)
            choiceButtonContainer.gameObject.SetActive(true);

        // 清除旧的选择按钮
        foreach (Button button in choiceButtons)
        {
            if (button != null)
                Destroy(button.gameObject);
        }
        choiceButtons.Clear();

        // 创建新的选择按钮
        for (int i = 0; i < currentStory.currentChoices.Count; i++)
        {
            Choice choice = currentStory.currentChoices[i];
            Button choiceButton = Instantiate(choiceButtonPrefab, choiceButtonContainer);

            choiceButton.gameObject.SetActive(true);

            Text buttonText = choiceButton.GetComponentInChildren<Text>();
            if (buttonText != null)
                buttonText.text = choice.text;

            int choiceIndex = i; // 闭包捕获
            choiceButton.onClick.AddListener(() => MakeChoice(choiceIndex));

            choiceButtons.Add(choiceButton);
        }
    }

    /// <summary>
    /// 做出选择
    /// </summary>
    void MakeChoice(int choiceIndex)
    {
        if (currentStory.currentChoices.Count > choiceIndex)
        {
            PlaySound(choiceSelectSound);

            Choice selectedChoice = currentStory.currentChoices[choiceIndex];
            OnChoiceMade?.Invoke(currentStory.state.ToJson(), choiceIndex);

            if (showDebugInfo)
            {
                Debug.Log($"[InkDialogueManager] 选择: {selectedChoice.text}");
            }

            currentStory.ChooseChoiceIndex(choiceIndex);

            // 隐藏选择按钮
            if (choiceButtonContainer != null)
                choiceButtonContainer.gameObject.SetActive(false);

            ContinueDialogue();
        }
    }

    /// <summary>
    /// 结束对话
    /// </summary>
    public void EndDialogue()
    {
        isDialogueActive = false;

        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        // 同步Ink变量回游戏状态
        SyncInkStateToGame();

        dialoguePanel?.SetActive(false);

        if (choiceButtonContainer != null)
            choiceButtonContainer.gameObject.SetActive(false);

        // 通知玩家退出对话状态
        var player = FindObjectOfType<PlayerController>();
        player?.ExitDialogueState();

        string storyState = currentStory?.state.ToJson() ?? "";
        OnDialogueEnd?.Invoke(storyState);
        PlaySound(dialogueEndSound);

        if (showDebugInfo)
        {
            Debug.Log("[InkDialogueManager] 对话结束");
        }

        currentStory = null;
    }

    /// <summary>
    /// 绑定外部函数
    /// </summary>
    void BindExternalFunctions()
    {
        // 绑定给物品函数
        currentStory.BindExternalFunction("give_item", (string itemName, int quantity) => {
            if (showDebugInfo)
                Debug.Log($"给予物品: {itemName} x{quantity}");

            // 实际的给物品逻辑
            GiveItemToPlayer(itemName, quantity);
        });

        // 绑定给经验函数
        currentStory.BindExternalFunction("give_exp", (int amount) => {
            if (showDebugInfo)
                Debug.Log($"给予经验: {amount}");

            var player = FindObjectOfType<PlayerController>();
            player?.AddExp(amount);
        });

        // 绑定设置故事变量函数
        currentStory.BindExternalFunction("set_story_var", (string varName, int value) => {
            if (showDebugInfo)
                Debug.Log($"设置故事变量: {varName} = {value}");

            StoryCtrl.Instance?.Set(varName, value);
        });

        // 绑定播放音效函数
        currentStory.BindExternalFunction("play_sound", (string soundName) => {
            PlaySoundByName(soundName);
        });
    }

    /// <summary>
    /// 同步游戏状态到Ink变量
    /// </summary>
    void SyncGameStateToInk()
    {
        if (currentStory == null) return;

        // 同步玩家基本信息
        var player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            currentStory.variablesState["player_level"] = player.Level;
            currentStory.variablesState["player_gold"] = GetPlayerGold();
            currentStory.variablesState["player_health"] = player.Health;
        }

        // 同步故事变量
        if (StoryCtrl.Instance != null)
        {
            // 这里可以同步特定的故事变量
            // currentStory.variablesState["quest_completed"] = StoryCtrl.Instance.Get("quest_completed");
        }

        // 同步物品信息
        if (InventoryManager.instance != null)
        {
            currentStory.variablesState["has_sword"] = InventoryManager.instance.HasItem("剑");
            // 可以添加更多物品检查
        }
    }

    /// <summary>
    /// 同步Ink状态回游戏
    /// </summary>
    void SyncInkStateToGame()
    {
        if (currentStory == null) return;

        // 可以在这里将Ink中的变量变化同步回游戏
        // 例如：如果Ink中修改了玩家金币，同步回游戏
    }

    /// <summary>
    /// 检查Ink变量变化
    /// </summary>
    void CheckInkVariables()
    {
        if (currentStory == null) return;

        // 监听特定变量的变化
        foreach (string varName in currentStory.variablesState)
        {
            object value = currentStory.variablesState[varName];
            OnInkVariableChanged?.Invoke(varName, value);
        }
    }

    /// <summary>
    /// 给玩家物品
    /// </summary>
    void GiveItemToPlayer(string itemName, int quantity)
    {
        // 实现给物品逻辑
        if (InventoryManager.instance != null)
        {
            for (int i = 0; i < quantity; i++)
            {
                var item = new Item
                {
                    Name = itemName,
                    Description = $"通过对话获得的{itemName}",
                    Type = ItemType.Material,
                    Size = new Vector2Int(1, 1)
                };
                InventoryManager.instance.AddItem(item);
            }
        }
    }

    /// <summary>
    /// 获取玩家金币数量
    /// </summary>
    int GetPlayerGold()
    {
        // 这里需要根据你的游戏系统实现
        // 暂时返回固定值，你需要替换为实际的金币获取逻辑
        return 100;
    }

    /// <summary>
    /// 根据名称播放音效
    /// </summary>
    void PlaySoundByName(string soundName)
    {
        // 这里可以根据音效名称播放对应的音效
        // 需要你实现音效管理系统
        if (showDebugInfo)
            Debug.Log($"播放音效: {soundName}");
    }

    /// <summary>
    /// 显示/隐藏继续按钮
    /// </summary>
    void ShowContinueButton(bool show)
    {
        if (continueButton != null)
            continueButton.gameObject.SetActive(show);
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position);
        }
    }

    /// <summary>
    /// 检查是否在对话中
    /// </summary>
    public bool IsInDialogue()
    {
        return isDialogueActive;
    }

    /// <summary>
    /// 强制跳过当前对话
    /// </summary>
    public void SkipDialogue()
    {
        if (isDialogueActive)
        {
            EndDialogue();
        }
    }

    /// <summary>
    /// 获取当前故事状态（用于保存）
    /// </summary>
    public string GetCurrentStoryState()
    {
        return currentStory?.state.ToJson() ?? "";
    }

    /// <summary>
    /// 加载故事状态（用于读档）
    /// </summary>
    public void LoadStoryState(string jsonState)
    {
        if (currentStory != null && !string.IsNullOrEmpty(jsonState))
        {
            currentStory.state.LoadJson(jsonState);
        }
    }
}

/// <summary>
/// Ink对话脚本 - 使用Ink文件的对话
/// </summary>
public class InkDialogueScript : BaseDialogueScript
{
    [Header("=== Ink对话设置 ===")]
    public TextAsset inkFile;
    public string startKnot = ""; // 起始节点（可选）

    [Header("=== 保存设置 ===")]
    public bool saveDialogueState = true; // 是否保存对话状态

    private InteractiveDialogueTrigger currentTrigger;
    private string savedStateKey;

    public override bool CanTrigger()
    {
        if (!base.CanTrigger()) return false;
        return inkFile != null;
    }

    public override void StartDialogue(InteractiveDialogueTrigger trigger)
    {
        MarkAsTriggered();
        currentTrigger = trigger;

        if (inkFile == null)
        {
            Debug.LogError("[InkDialogueScript] Ink文件为空！");
            return;
        }

        if (InkDialogueManager.Instance != null)
        {
            // 加载保存的对话状态
            if (saveDialogueState)
            {
                LoadDialogueState();
            }

            InkDialogueManager.Instance.StartDialogue(inkFile, startKnot);
            InkDialogueManager.Instance.OnDialogueEnd += OnDialogueEnd;
        }
        else
        {
            Debug.LogError("[InkDialogueScript] InkDialogueManager 不存在！");
        }
    }

    void OnDialogueEnd(string storyState)
    {
        if (InkDialogueManager.Instance != null)
            InkDialogueManager.Instance.OnDialogueEnd -= OnDialogueEnd;

        // 保存对话状态
        if (saveDialogueState && !string.IsNullOrEmpty(storyState))
        {
            SaveDialogueState(storyState);
        }

        currentTrigger?.OnDialogueComplete();
        currentTrigger = null;
    }

    void SaveDialogueState(string storyState)
    {
        savedStateKey = $"dialogue_state_{dialogueId}";
        PlayerPrefs.SetString(savedStateKey, storyState);
        PlayerPrefs.Save();
    }

    void LoadDialogueState()
    {
        savedStateKey = $"dialogue_state_{dialogueId}";
        if (PlayerPrefs.HasKey(savedStateKey))
        {
            string savedState = PlayerPrefs.GetString(savedStateKey);
            InkDialogueManager.Instance?.LoadStoryState(savedState);
        }
    }

    /// <summary>
    /// 清除保存的对话状态
    /// </summary>
    [ContextMenu("清除对话状态")]
    public void ClearDialogueState()
    {
        savedStateKey = $"dialogue_state_{dialogueId}";
        if (PlayerPrefs.HasKey(savedStateKey))
        {
            PlayerPrefs.DeleteKey(savedStateKey);
            Debug.Log($"[InkDialogueScript] 已清除对话状态: {dialogueId}");
        }
    }
}

/// <summary>
/// Ink对话系统快速设置工具
/// </summary>
public class InkDialogueSetup
{
    [UnityEditor.MenuItem("GameObject/Ink Dialogue/Setup Ink Dialogue System")]
    public static void SetupInkDialogue()
    {
        var selectedObjects = UnityEditor.Selection.gameObjects;
        if (selectedObjects.Length == 0)
        {
            UnityEditor.EditorUtility.DisplayDialog("错误", "请选择要设置Ink对话的NPC对象！", "确定");
            return;
        }

        foreach (var npc in selectedObjects)
        {
            SetupSingleNPCInk(npc);
        }

        UnityEditor.EditorUtility.DisplayDialog("设置完成",
            $"已为 {selectedObjects.Length} 个NPC设置Ink对话系统！", "确定");
    }

    static void SetupSingleNPCInk(GameObject npc)
    {
        Debug.Log($"[InkDialogueSetup] 开始设置 {npc.name} 的Ink对话系统");

        // 1. 添加对话触发器
        var dialogueTrigger = GetOrAddComponent<InteractiveDialogueTrigger>(npc);

        // 2. 添加Ink对话脚本
        var inkScript = GetOrAddComponent<InkDialogueScript>(npc);

        // 3. 配置对话触发器
        ConfigureDialogueTrigger(dialogueTrigger, inkScript);

        // 4. 配置Ink对话脚本
        ConfigureInkScript(inkScript, npc.name);

        // 5. 创建交互提示UI
        CreateInteractionPrompt(npc, dialogueTrigger);

        // 6. 确保有碰撞器
        EnsureCollider(npc);

        UnityEditor.EditorUtility.SetDirty(npc);
        Debug.Log($"[InkDialogueSetup] {npc.name} Ink对话系统设置完成");
    }

    static T GetOrAddComponent<T>(GameObject obj) where T : Component
    {
        var component = obj.GetComponent<T>();
        if (component == null)
        {
            component = obj.AddComponent<T>();
        }
        return component;
    }

    static void ConfigureDialogueTrigger(InteractiveDialogueTrigger trigger, InkDialogueScript script)
    {
        trigger.interactKey = KeyCode.E;
        trigger.interactionRange = 3f;
        trigger.playerLayer = -1;
        trigger.promptText = "按 E 交谈";
        trigger.executeInOrder = false;
        trigger.repeatLastScript = true;

        if (trigger.dialogueScripts == null || trigger.dialogueScripts.Length == 0)
        {
            trigger.dialogueScripts = new BaseDialogueScript[] { script };
        }
    }

    static void ConfigureInkScript(InkDialogueScript script, string npcName)
    {
        script.dialogueId = $"{npcName}_ink_dialogue";
        script.canRepeat = true;
        script.maxTriggerCount = -1;
        script.cooldownTime = 0f;
        script.saveDialogueState = true;

        // 如果没有设置Ink文件，提示用户设置
        if (script.inkFile == null)
        {
            Debug.LogWarning($"[InkDialogueSetup] {npcName} 需要设置Ink文件！请在Inspector中指定inkFile。");
        }
    }

    static void CreateInteractionPrompt(GameObject npc, InteractiveDialogueTrigger trigger)
    {
        // 检查是否已有提示UI
        Transform existingPrompt = npc.transform.Find("InteractionPrompt");
        if (existingPrompt != null)
        {
            trigger.interactionPrompt = existingPrompt.gameObject;
            return;
        }

        // 创建Canvas
        GameObject canvasObj = new GameObject("InteractionPrompt");
        canvasObj.transform.SetParent(npc.transform);
        canvasObj.transform.localPosition = Vector3.up * 2f;

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(2, 0.5f);

        var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.dynamicPixelsPerUnit = 100;

        // 创建Text对象
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(canvasObj.transform);

        var text = textObj.AddComponent<UnityEngine.UI.Text>();
        text.text = "按 E 交谈";
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 14;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;

        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        var image = textObj.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(0, 0, 0, 0.7f);

        canvasObj.SetActive(false);
        trigger.interactionPrompt = canvasObj;

        Debug.Log($"[InkDialogueSetup] 为 {npc.name} 创建了交互提示UI");
    }

    static void EnsureCollider(GameObject npc)
    {
        var collider = npc.GetComponent<Collider>();
        if (collider == null)
        {
            var capsule = npc.AddComponent<CapsuleCollider>();
            capsule.center = Vector3.up;
            capsule.height = 2f;
            capsule.radius = 0.5f;

            Debug.Log($"[InkDialogueSetup] 为 {npc.name} 添加了碰撞器");
        }
    }

    /// <summary>
    /// 创建示例Ink文件
    /// </summary>
    [UnityEditor.MenuItem("Assets/Create/Ink Dialogue/Create Example Ink Files")]
    public static void CreateExampleInkFiles()
    {
        string merchantInk = @"VAR player_gold = 100
VAR has_sword = false
VAR merchant_trust = 0

-> start

=== start ===
你好，勇敢的冒险者！欢迎来到我的商店。 # speaker: 商人

{merchant_trust > 5: 
    啊，是我的老朋友！今天想要什么特别的吗？
- else:
    我这里有各种冒险用品。
}

* [查看武器] -> weapons
* [查看道具] -> items  
* [询问消息] -> information
* [离开商店] -> goodbye

=== weapons ===
这里是我收集的最好的武器！

{not has_sword:
    * [铁剑 - 50金币] -> buy_sword
}
* [魔法法杖 - 150金币] -> buy_staff
* [返回] -> start

=== buy_sword ===
{player_gold >= 50:
    很好的选择！这把铁剑锋利无比。
    ~ player_gold -= 50
    ~ has_sword = true
    ~ merchant_trust += 1
    ~ give_item(""铁剑"", 1)
    
    你现在拥有了铁剑！剩余金币：{player_gold}
    * [继续购物] -> start
    * [离开] -> goodbye
- else:
    抱歉，你的金币不够。铁剑需要50金币。
    * [返回] -> weapons
}

=== buy_staff ===
{player_gold >= 150:
    魔法法杖！法师的最爱。
    ~ player_gold -= 150
    ~ merchant_trust += 2
    ~ give_item(""魔法法杖"", 1)
    
    你获得了魔法法杖！剩余金币：{player_gold}
- else:
    这个有点贵，你需要150金币。
}
* [返回] -> weapons

=== items ===
我这里有各种有用的道具。

* [生命药水 - 20金币] -> buy_potion
* [魔法药水 - 30金币] -> buy_mana_potion
* [返回] -> start

=== buy_potion ===
{player_gold >= 20:
    生命药水，在危险时刻能救你一命！
    ~ player_gold -= 20
    ~ give_item(""生命药水"", 1)
    你获得了生命药水！
- else:
    你需要20金币才能买生命药水。
}
* [继续] -> items

=== buy_mana_potion ===
{player_gold >= 30:
    魔法药水，恢复你的法力值。
    ~ player_gold -= 30
    ~ give_item(""魔法药水"", 1)
    你获得了魔法药水！
- else:
    魔法药水需要30金币。
}
* [继续] -> items

=== information ===
{merchant_trust >= 3:
    既然你是我的好顾客，我告诉你一个秘密...
    
    森林深处有一个古老的遗迹，里面藏着宝藏。
    但是要小心那里的守护者！
    
    ~ merchant_trust += 1
    ~ set_story_var(""know_forest_secret"", 1)
- else:
    最近有很多冒险者来这里采购装备。
    看来外面变得越来越危险了。
}

* [还有其他消息吗？] -> more_info
* [谢谢你的信息] -> start

=== more_info ===
{merchant_trust >= 5:
    你真的想知道更多？好吧...
    
    我听说有一把传说中的剑在龙的巢穴里。
    如果你足够强大，也许可以尝试挑战。
    
    ~ set_story_var(""know_dragon_sword"", 1)
- else:
    暂时没有其他特别的消息了。
}
* [返回] -> start

=== goodbye ===
感谢光临！祝你冒险顺利！

{has_sword:
    记住，那把铁剑会保护你的！
}

{merchant_trust >= 3:
    记住我告诉你的秘密，小心森林！
}

愿你平安归来！

-> END
";

        // 保存到Assets文件夹
        string path = UnityEditor.AssetDatabase.GenerateUniqueAssetPath("Assets/NPC_Merchant.ink");
        System.IO.File.WriteAllText(path, merchantInk);
        UnityEditor.AssetDatabase.Refresh();

        UnityEditor.EditorUtility.DisplayDialog("创建完成",
            $"已创建示例Ink文件: {path}\n请等待Unity编译成TextAsset后使用。", "确定");
    }
}