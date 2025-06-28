using UnityEngine;
using UnityEngine.Events;
using Ink.Runtime;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// 全局对话管理器 - 单例模式，负责UI显示和对话播放
/// </summary>
public class GlobalDialogueManager : MonoBehaviour
{
    public static GlobalDialogueManager Instance { get; private set; }

    [Header("=== 对话UI配置 ===")]
    [Tooltip("对话面板GameObject")]
    public GameObject dialoguePanel;
    [Tooltip("显示对话文本的Text组件")]
    public UnityEngine.UI.Text dialogueText;
    [Tooltip("显示说话者名字的Text组件（可选）")]
    public UnityEngine.UI.Text speakerNameText;
    [Tooltip("继续对话的按钮")]
    public UnityEngine.UI.Button continueButton;
    [Tooltip("选择按钮的容器")]
    public Transform choiceButtonContainer;
    [Tooltip("选择按钮的预制体")]
    public UnityEngine.UI.Button choiceButtonPrefab;

    [Header("=== 默认对话内容 ===")]
    [Tooltip("默认使用的INK对话文件")]
    public TextAsset defaultINKFile;
    [Tooltip("默认的起始节点名称（可选）")]
    public string defaultStartKnot = "";
    [Tooltip("如果没有INK文件时使用的默认文本")]
    [TextArea(3, 5)]
    public string defaultDialogueText = "你好，这是默认对话。";

    [Header("=== 音效配置 ===")]
    public AudioClip dialogueStartSound;
    public AudioClip dialogueEndSound;
    public AudioClip choiceSelectSound;

    [Header("=== 打字机效果 ===")]
    public bool enableTypewriterEffect = true;
    public float typewriterSpeed = 0.05f;
    public AudioClip typewriterSound;

    [Header("=== 调试配置 ===")]
    public bool showDebugInfo = false;

    // 当前对话状态
    private Story currentStory;
    private bool isDialogueActive = false;
    private Coroutine typewriterCoroutine;
    private List<UnityEngine.UI.Button> currentChoiceButtons = new List<UnityEngine.UI.Button>();
    private NPCDialogueController currentNPC;

    // 事件系统
    public System.Action<string> OnDialogueStart;
    public System.Action<string> OnDialogueEnd;
    public System.Action<string, int> OnChoiceMade;
    public System.Action<string> OnDialogueChoice; // 兼容旧版本的事件

    void Awake()
    {
        // 改进的单例模式 - 不会导致对象丢失
        if (Instance != null && Instance != this)
        {
            if (showDebugInfo)
                Debug.LogWarning($"[GlobalDialogueManager] 发现重复实例，保留最新的: {gameObject.name}");

            // 销毁旧实例而不是当前实例
            Destroy(Instance.gameObject);
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

        if (speakerNameText != null)
            speakerNameText.gameObject.SetActive(false);
    }

    /// <summary>
    /// 开始INK对话
    /// </summary>
    public void StartDialogue(TextAsset inkFile, string startKnot = "", NPCDialogueController npc = null)
    {
        if (inkFile == null)
        {
            Debug.LogError("[GlobalDialogueManager] INK文件为空。");
            return;
        }

        if (isDialogueActive)
        {
            Debug.LogWarning("[GlobalDialogueManager] 已经在对话中");
            return;
        }

        try
        {
            currentStory = new Story(inkFile.text);
            currentNPC = npc;

            if (!string.IsNullOrEmpty(startKnot))
            {
                currentStory.ChoosePathString(startKnot);
            }

            BindExternalFunctions();
            SyncGameStateToInk();

            isDialogueActive = true;
            if (dialoguePanel != null)
                dialoguePanel.SetActive(true);

            var player = FindObjectOfType<PlayerController>();
            if (player != null)
                player.EnterDialogueState();

            OnDialogueStart?.Invoke(inkFile.name);
            PlaySound(dialogueStartSound);

            ContinueDialogue();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GlobalDialogueManager] 启动对话失败: {e.Message}");
        }
    }

    /// <summary>
    /// 开始简单文本对话
    /// </summary>
    public void StartDialogue(string dialogueContent)
    {
        if (string.IsNullOrEmpty(dialogueContent))
        {
            Debug.LogError("[GlobalDialogueManager] 对话内容为空。");
            return;
        }

        string simpleInk = dialogueContent + "\n-> END";

        try
        {
            currentStory = new Story(simpleInk);
            isDialogueActive = true;

            if (dialoguePanel != null)
                dialoguePanel.SetActive(true);

            var player = FindObjectOfType<PlayerController>();
            if (player != null)
                player.EnterDialogueState();

            OnDialogueStart?.Invoke("SimpleDialogue");
            PlaySound(dialogueStartSound);

            ContinueDialogue();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GlobalDialogueManager] 启动简单对话失败: {e.Message}");
        }
    }

    /// <summary>
    /// 继续对话
    /// </summary>
    public void ContinueDialogue()
    {
        if (!isDialogueActive || currentStory == null) return;

        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;

            if (dialogueText != null && !string.IsNullOrEmpty(currentStory.currentText))
            {
                dialogueText.text = ProcessTags(currentStory.currentText);
                ShowContinueButton(true);
                return;
            }
        }

        if (currentStory.canContinue)
        {
            string text = currentStory.Continue();
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

    string ProcessTags(string text)
    {
        if (currentStory.currentTags.Count > 0)
        {
            foreach (string tag in currentStory.currentTags)
            {
                if (tag.StartsWith("speaker:"))
                {
                    string speakerName = tag.Substring(8).Trim();
                    if (speakerNameText != null)
                    {
                        speakerNameText.text = speakerName;
                        speakerNameText.gameObject.SetActive(true);
                    }
                }
                else if (tag.StartsWith("sound:"))
                {
                    string soundName = tag.Substring(6).Trim();
                    PlaySoundByName(soundName);
                }
            }
        }
        else
        {
            if (speakerNameText != null)
                speakerNameText.gameObject.SetActive(false);
        }

        return text;
    }

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

    void ShowChoices()
    {
        ShowContinueButton(false);

        if (choiceButtonContainer == null || choiceButtonPrefab == null)
        {
            Debug.LogError("[GlobalDialogueManager] 选择按钮容器或预制体未配置。");
            return;
        }

        ClearChoiceButtons();
        choiceButtonContainer.gameObject.SetActive(true);

        for (int i = 0; i < currentStory.currentChoices.Count; i++)
        {
            var choice = currentStory.currentChoices[i];
            var choiceButton = Instantiate(choiceButtonPrefab, choiceButtonContainer);

            var buttonText = choiceButton.GetComponentInChildren<UnityEngine.UI.Text>();
            if (buttonText != null)
                buttonText.text = choice.text;

            int choiceIndex = i;
            choiceButton.onClick.AddListener(() => MakeChoice(choiceIndex));
            currentChoiceButtons.Add(choiceButton);
        }
    }

    void MakeChoice(int choiceIndex)
    {
        if (currentStory.currentChoices.Count > choiceIndex)
        {
            PlaySound(choiceSelectSound);

            var selectedChoice = currentStory.currentChoices[choiceIndex];

            // 触发两个事件以保持兼容性
            OnChoiceMade?.Invoke(selectedChoice.text, choiceIndex);
            OnDialogueChoice?.Invoke(selectedChoice.text);

            currentStory.ChooseChoiceIndex(choiceIndex);

            ClearChoiceButtons();
            if (choiceButtonContainer != null)
                choiceButtonContainer.gameObject.SetActive(false);

            ContinueDialogue();
        }
    }

    void ClearChoiceButtons()
    {
        foreach (var button in currentChoiceButtons)
        {
            if (button != null)
                Destroy(button.gameObject);
        }
        currentChoiceButtons.Clear();
    }

    public void EndDialogue()
    {
        isDialogueActive = false;

        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        ClearChoiceButtons();
        if (choiceButtonContainer != null)
            choiceButtonContainer.gameObject.SetActive(false);

        if (speakerNameText != null)
            speakerNameText.gameObject.SetActive(false);

        var player = FindObjectOfType<PlayerController>();
        if (player != null)
            player.ExitDialogueState();

        if (currentNPC != null)
        {
            currentNPC.OnDialogueComplete();
        }

        string storyState = currentStory?.state.ToJson() ?? "";
        OnDialogueEnd?.Invoke(storyState);
        PlaySound(dialogueEndSound);

        currentStory = null;
        currentNPC = null;
    }

    void BindExternalFunctions()
    {
        if (currentStory == null) return;

        currentStory.BindExternalFunction("give_item", (string itemName, int quantity) => {
            GiveItemToPlayer(itemName, quantity);
        });

        currentStory.BindExternalFunction("give_exp", (int amount) => {
            var player = FindObjectOfType<PlayerController>();
            if (player != null)
                player.AddExp(amount);
        });

        currentStory.BindExternalFunction("set_story_var", (string varName, int value) => {
            if (StoryCtrl.Instance != null)
                StoryCtrl.Instance.Set(varName, value);
        });

        currentStory.BindExternalFunction("play_sound", (string soundName) => {
            PlaySoundByName(soundName);
        });
    }

    void SyncGameStateToInk()
    {
        if (currentStory == null) return;

        var player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            currentStory.variablesState["player_level"] = player.Level;
            currentStory.variablesState["player_health"] = player.Health;
        }
    }

    void GiveItemToPlayer(string itemName, int quantity)
    {
        if (InventoryManager.instance != null)
        {
            for (int i = 0; i < quantity; i++)
            {
                var item = new GameItem
                {
                    Name = itemName,
                    Description = $"从对话获得的{itemName}",
                    Type = ItemType.Material,
                    Size = new Vector2Int(1, 1)
                };
                InventoryManager.instance.AddItem(item);
            }
        }
    }

    void PlaySoundByName(string soundName)
    {
        if (showDebugInfo)
            Debug.Log($"[GlobalDialogueManager] 播放音效: {soundName}");
    }

    void ShowContinueButton(bool show)
    {
        if (continueButton != null)
            continueButton.gameObject.SetActive(show);
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position);
        }
    }

    public bool IsInDialogue()
    {
        return isDialogueActive;
    }

    public void SkipDialogue()
    {
        if (isDialogueActive)
        {
            EndDialogue();
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}