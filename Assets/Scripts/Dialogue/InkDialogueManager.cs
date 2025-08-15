using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Ink.Runtime;

/// <summary>
/// Ink�Ի������� - �����汾
/// </summary>
public class InkDialogueManager : MonoBehaviour
{
    public static InkDialogueManager Instance { get; private set; }

    [Header("=== �Ի�UI���� ===")]
    public GameObject dialoguePanel;
    public Text dialogueText;
    public Text speakerNameText;
    public Button continueButton;
    public Transform choiceButtonContainer;
    public Button choiceButtonPrefab;

    [Header("=== ��Ч���� ===")]
    public AudioClip dialogueStartSound;
    public AudioClip dialogueEndSound;
    public AudioClip choiceSelectSound;

    [Header("=== ���ֻ�Ч�� ===")]
    public bool enableTypewriterEffect = true;
    public float typewriterSpeed = 0.05f;
    public AudioClip typewriterSound;

    [Header("=== �������� ===")]
    public bool showDebugInfo = false;

    // ��ǰ�Ի�״̬
    private Story currentStory;
    private bool isDialogueActive = false;
    private Coroutine typewriterCoroutine;
    private List<Button> choiceButtons = new List<Button>();

    // �¼�ϵͳ
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
    /// ��ʼInk�Ի�
    /// </summary>
    public void StartDialogue(TextAsset inkFile, string startKnot = "")
    {
        if (inkFile == null)
        {
            Debug.LogError("[InkDialogueManager] Ink�ļ�Ϊ�գ�");
            return;
        }

        try
        {
            currentStory = new Story(inkFile.text);

            // ���ָ������ʼ�ڵ㣬��ת���ýڵ�
            if (!string.IsNullOrEmpty(startKnot))
            {
                currentStory.ChoosePathString(startKnot);
            }

            // ���ⲿ����
            BindExternalFunctions();

            // ͬ����Ϸ״̬��Ink����
            SyncGameStateToInk();

            isDialogueActive = true;
            dialoguePanel?.SetActive(true);

            // ֪ͨ��ҽ���Ի�״̬
            var player = FindObjectOfType<PlayerController>();
            player?.EnterDialogueState();

            OnDialogueStart?.Invoke(inkFile.name);
            PlaySound(dialogueStartSound);

            if (showDebugInfo)
            {
                Debug.Log($"[InkDialogueManager] ��ʼInk�Ի�: {inkFile.name}");
            }

            ContinueDialogue();

        }
        catch (System.Exception e)
        {
            Debug.LogError($"[InkDialogueManager] ����Ի�ʧ��: {e.Message}");
        }
    }

    /// <summary>
    /// �����Ի�
    /// </summary>
    public void ContinueDialogue()
    {
        if (!isDialogueActive || currentStory == null) return;

        // ������ڴ��֣���������Ч��
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

            // �����ǩ
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

            // ����Ink�����仯
            CheckInkVariables();

            // ����Ƿ���ѡ��
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
    /// ����Ink��ǩ (���ɫ����)
    /// </summary>
    string ProcessTags(string text)
    {
        if (currentStory.currentTags.Count > 0)
        {
            foreach (string tag in currentStory.currentTags)
            {
                // �����ɫ����ǩ # speaker: ��ɫ��
                if (tag.StartsWith("speaker:"))
                {
                    string speakerName = tag.Substring(8).Trim();
                    if (speakerNameText != null)
                    {
                        speakerNameText.text = speakerName;
                        speakerNameText.gameObject.SetActive(true);
                    }
                }
                // ���������Զ����ǩ
                else if (tag.StartsWith("sound:"))
                {
                    string soundName = tag.Substring(6).Trim();
                    PlaySoundByName(soundName);
                }
                else if (tag.StartsWith("emotion:"))
                {
                    string emotion = tag.Substring(8).Trim();
                    // ���������ﴦ���ɫ����仯
                    if (showDebugInfo)
                        Debug.Log($"��ɫ���: {emotion}");
                }
            }
        }
        else
        {
            // û�б�ǩʱ���ؽ�ɫ��
            if (speakerNameText != null)
                speakerNameText.gameObject.SetActive(false);
        }

        return text;
    }

    /// <summary>
    /// ���ֻ�Ч��Э��
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
    /// ��ʾѡ��ť
    /// </summary>
    void ShowChoices()
    {
        ShowContinueButton(false);

        if (choiceButtonContainer != null)
            choiceButtonContainer.gameObject.SetActive(true);

        // ����ɵ�ѡ��ť
        foreach (Button button in choiceButtons)
        {
            if (button != null)
                Destroy(button.gameObject);
        }
        choiceButtons.Clear();

        // �����µ�ѡ��ť
        for (int i = 0; i < currentStory.currentChoices.Count; i++)
        {
            Choice choice = currentStory.currentChoices[i];
            Button choiceButton = Instantiate(choiceButtonPrefab, choiceButtonContainer);

            choiceButton.gameObject.SetActive(true);

            Text buttonText = choiceButton.GetComponentInChildren<Text>();
            if (buttonText != null)
                buttonText.text = choice.text;

            int choiceIndex = i; // �հ�����
            choiceButton.onClick.AddListener(() => MakeChoice(choiceIndex));

            choiceButtons.Add(choiceButton);
        }
    }

    /// <summary>
    /// ����ѡ��
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
                Debug.Log($"[InkDialogueManager] ѡ��: {selectedChoice.text}");
            }

            currentStory.ChooseChoiceIndex(choiceIndex);

            // ����ѡ��ť
            if (choiceButtonContainer != null)
                choiceButtonContainer.gameObject.SetActive(false);

            ContinueDialogue();
        }
    }

    /// <summary>
    /// �����Ի�
    /// </summary>
    public void EndDialogue()
    {
        isDialogueActive = false;

        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        // ͬ��Ink��������Ϸ״̬
        SyncInkStateToGame();

        dialoguePanel?.SetActive(false);

        if (choiceButtonContainer != null)
            choiceButtonContainer.gameObject.SetActive(false);

        // ֪ͨ����˳��Ի�״̬
        var player = FindObjectOfType<PlayerController>();
        player?.ExitDialogueState();

        string storyState = currentStory?.state.ToJson() ?? "";
        OnDialogueEnd?.Invoke(storyState);
        PlaySound(dialogueEndSound);

        if (showDebugInfo)
        {
            Debug.Log("[InkDialogueManager] �Ի�����");
        }

        currentStory = null;
    }

    /// <summary>
    /// ���ⲿ����
    /// </summary>
    void BindExternalFunctions()
    {
        // �󶨸���Ʒ����
        currentStory.BindExternalFunction("give_item", (string itemName, int quantity) => {
            if (showDebugInfo)
                Debug.Log($"������Ʒ: {itemName} x{quantity}");

            // ʵ�ʵĸ���Ʒ�߼�
            GiveItemToPlayer(itemName, quantity);
        });

        // �󶨸����麯��
        currentStory.BindExternalFunction("give_exp", (int amount) => {
            if (showDebugInfo)
                Debug.Log($"���辭��: {amount}");

            var player = FindObjectOfType<PlayerController>();
            player?.AddExp(amount);
        });

        // �����ù��±�������
        currentStory.BindExternalFunction("set_story_var", (string varName, int value) => {
            if (showDebugInfo)
                Debug.Log($"���ù��±���: {varName} = {value}");

            StoryCtrl.Instance?.Set(varName, value);
        });

        // �󶨲�����Ч����
        currentStory.BindExternalFunction("play_sound", (string soundName) => {
            PlaySoundByName(soundName);
        });
    }

    /// <summary>
    /// ͬ����Ϸ״̬��Ink����
    /// </summary>
    void SyncGameStateToInk()
    {
        if (currentStory == null) return;

        // ͬ����һ�����Ϣ
        var player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            currentStory.variablesState["player_level"] = player.Level;
            currentStory.variablesState["player_gold"] = GetPlayerGold();
            currentStory.variablesState["player_health"] = player.Health;
        }

        // ͬ�����±���
        if (StoryCtrl.Instance != null)
        {
            // �������ͬ���ض��Ĺ��±���
            // currentStory.variablesState["quest_completed"] = StoryCtrl.Instance.Get("quest_completed");
        }

        // ͬ����Ʒ��Ϣ
        if (InventoryManager.instance != null)
        {
            currentStory.variablesState["has_sword"] = InventoryManager.instance.HasItem("��");
            // ������Ӹ�����Ʒ���
        }
    }

    /// <summary>
    /// ͬ��Ink״̬����Ϸ
    /// </summary>
    void SyncInkStateToGame()
    {
        if (currentStory == null) return;

        // ���������ｫInk�еı����仯ͬ������Ϸ
        // ���磺���Ink���޸�����ҽ�ң�ͬ������Ϸ
    }

    /// <summary>
    /// ���Ink�����仯
    /// </summary>
    void CheckInkVariables()
    {
        if (currentStory == null) return;

        // �����ض������ı仯
        foreach (string varName in currentStory.variablesState)
        {
            object value = currentStory.variablesState[varName];
            OnInkVariableChanged?.Invoke(varName, value);
        }
    }

    /// <summary>
    /// �������Ʒ
    /// </summary>
    void GiveItemToPlayer(string itemName, int quantity)
    {
        // ʵ�ָ���Ʒ�߼�
        if (InventoryManager.instance != null)
        {
            for (int i = 0; i < quantity; i++)
            {
                var item = new Item
                {
                    Name = itemName,
                    Description = $"ͨ���Ի���õ�{itemName}",
                    Type = ItemType.Material,
                    Size = new Vector2Int(1, 1)
                };
                InventoryManager.instance.AddItem(item);
            }
        }
    }

    /// <summary>
    /// ��ȡ��ҽ������
    /// </summary>
    int GetPlayerGold()
    {
        // ������Ҫ���������Ϸϵͳʵ��
        // ��ʱ���ع̶�ֵ������Ҫ�滻Ϊʵ�ʵĽ�һ�ȡ�߼�
        return 100;
    }

    /// <summary>
    /// �������Ʋ�����Ч
    /// </summary>
    void PlaySoundByName(string soundName)
    {
        // ������Ը�����Ч���Ʋ��Ŷ�Ӧ����Ч
        // ��Ҫ��ʵ����Ч����ϵͳ
        if (showDebugInfo)
            Debug.Log($"������Ч: {soundName}");
    }

    /// <summary>
    /// ��ʾ/���ؼ�����ť
    /// </summary>
    void ShowContinueButton(bool show)
    {
        if (continueButton != null)
            continueButton.gameObject.SetActive(show);
    }

    /// <summary>
    /// ������Ч
    /// </summary>
    void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position);
        }
    }

    /// <summary>
    /// ����Ƿ��ڶԻ���
    /// </summary>
    public bool IsInDialogue()
    {
        return isDialogueActive;
    }

    /// <summary>
    /// ǿ��������ǰ�Ի�
    /// </summary>
    public void SkipDialogue()
    {
        if (isDialogueActive)
        {
            EndDialogue();
        }
    }

    /// <summary>
    /// ��ȡ��ǰ����״̬�����ڱ��棩
    /// </summary>
    public string GetCurrentStoryState()
    {
        return currentStory?.state.ToJson() ?? "";
    }

    /// <summary>
    /// ���ع���״̬�����ڶ�����
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
/// Ink�Ի��ű� - ʹ��Ink�ļ��ĶԻ�
/// </summary>
public class InkDialogueScript : BaseDialogueScript
{
    [Header("=== Ink�Ի����� ===")]
    public TextAsset inkFile;
    public string startKnot = ""; // ��ʼ�ڵ㣨��ѡ��

    [Header("=== �������� ===")]
    public bool saveDialogueState = true; // �Ƿ񱣴�Ի�״̬

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
            Debug.LogError("[InkDialogueScript] Ink�ļ�Ϊ�գ�");
            return;
        }

        if (InkDialogueManager.Instance != null)
        {
            // ���ر���ĶԻ�״̬
            if (saveDialogueState)
            {
                LoadDialogueState();
            }

            InkDialogueManager.Instance.StartDialogue(inkFile, startKnot);
            InkDialogueManager.Instance.OnDialogueEnd += OnDialogueEnd;
        }
        else
        {
            Debug.LogError("[InkDialogueScript] InkDialogueManager �����ڣ�");
        }
    }

    void OnDialogueEnd(string storyState)
    {
        if (InkDialogueManager.Instance != null)
            InkDialogueManager.Instance.OnDialogueEnd -= OnDialogueEnd;

        // ����Ի�״̬
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
    /// �������ĶԻ�״̬
    /// </summary>
    [ContextMenu("����Ի�״̬")]
    public void ClearDialogueState()
    {
        savedStateKey = $"dialogue_state_{dialogueId}";
        if (PlayerPrefs.HasKey(savedStateKey))
        {
            PlayerPrefs.DeleteKey(savedStateKey);
            Debug.Log($"[InkDialogueScript] ������Ի�״̬: {dialogueId}");
        }
    }
}

/// <summary>
/// Ink�Ի�ϵͳ�������ù���
/// </summary>
public class InkDialogueSetup
{
    [UnityEditor.MenuItem("GameObject/Ink Dialogue/Setup Ink Dialogue System")]
    public static void SetupInkDialogue()
    {
        var selectedObjects = UnityEditor.Selection.gameObjects;
        if (selectedObjects.Length == 0)
        {
            UnityEditor.EditorUtility.DisplayDialog("����", "��ѡ��Ҫ����Ink�Ի���NPC����", "ȷ��");
            return;
        }

        foreach (var npc in selectedObjects)
        {
            SetupSingleNPCInk(npc);
        }

        UnityEditor.EditorUtility.DisplayDialog("�������",
            $"��Ϊ {selectedObjects.Length} ��NPC����Ink�Ի�ϵͳ��", "ȷ��");
    }

    static void SetupSingleNPCInk(GameObject npc)
    {
        Debug.Log($"[InkDialogueSetup] ��ʼ���� {npc.name} ��Ink�Ի�ϵͳ");

        // 1. ��ӶԻ�������
        var dialogueTrigger = GetOrAddComponent<InteractiveDialogueTrigger>(npc);

        // 2. ���Ink�Ի��ű�
        var inkScript = GetOrAddComponent<InkDialogueScript>(npc);

        // 3. ���öԻ�������
        ConfigureDialogueTrigger(dialogueTrigger, inkScript);

        // 4. ����Ink�Ի��ű�
        ConfigureInkScript(inkScript, npc.name);

        // 5. ����������ʾUI
        CreateInteractionPrompt(npc, dialogueTrigger);

        // 6. ȷ������ײ��
        EnsureCollider(npc);

        UnityEditor.EditorUtility.SetDirty(npc);
        Debug.Log($"[InkDialogueSetup] {npc.name} Ink�Ի�ϵͳ�������");
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
        trigger.promptText = "�� E ��̸";
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

        // ���û������Ink�ļ�����ʾ�û�����
        if (script.inkFile == null)
        {
            Debug.LogWarning($"[InkDialogueSetup] {npcName} ��Ҫ����Ink�ļ�������Inspector��ָ��inkFile��");
        }
    }

    static void CreateInteractionPrompt(GameObject npc, InteractiveDialogueTrigger trigger)
    {
        // ����Ƿ�������ʾUI
        Transform existingPrompt = npc.transform.Find("InteractionPrompt");
        if (existingPrompt != null)
        {
            trigger.interactionPrompt = existingPrompt.gameObject;
            return;
        }

        // ����Canvas
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

        // ����Text����
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(canvasObj.transform);

        var text = textObj.AddComponent<UnityEngine.UI.Text>();
        text.text = "�� E ��̸";
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

        Debug.Log($"[InkDialogueSetup] Ϊ {npc.name} �����˽�����ʾUI");
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

            Debug.Log($"[InkDialogueSetup] Ϊ {npc.name} �������ײ��");
        }
    }

    /// <summary>
    /// ����ʾ��Ink�ļ�
    /// </summary>
    [UnityEditor.MenuItem("Assets/Create/Ink Dialogue/Create Example Ink Files")]
    public static void CreateExampleInkFiles()
    {
        string merchantInk = @"VAR player_gold = 100
VAR has_sword = false
VAR merchant_trust = 0

-> start

=== start ===
��ã��¸ҵ�ð���ߣ���ӭ�����ҵ��̵ꡣ # speaker: ����

{merchant_trust > 5: 
    �������ҵ������ѣ�������Ҫʲô�ر����
- else:
    �������и���ð����Ʒ��
}

* [�鿴����] -> weapons
* [�鿴����] -> items  
* [ѯ����Ϣ] -> information
* [�뿪�̵�] -> goodbye

=== weapons ===
���������ռ�����õ�������

{not has_sword:
    * [���� - 50���] -> buy_sword
}
* [ħ������ - 150���] -> buy_staff
* [����] -> start

=== buy_sword ===
{player_gold >= 50:
    �ܺõ�ѡ��������������ޱȡ�
    ~ player_gold -= 50
    ~ has_sword = true
    ~ merchant_trust += 1
    ~ give_item(""����"", 1)
    
    ������ӵ����������ʣ���ң�{player_gold}
    * [��������] -> start
    * [�뿪] -> goodbye
- else:
    ��Ǹ����Ľ�Ҳ�����������Ҫ50��ҡ�
    * [����] -> weapons
}

=== buy_staff ===
{player_gold >= 150:
    ħ�����ȣ���ʦ�����
    ~ player_gold -= 150
    ~ merchant_trust += 2
    ~ give_item(""ħ������"", 1)
    
    ������ħ�����ȣ�ʣ���ң�{player_gold}
- else:
    ����е������Ҫ150��ҡ�
}
* [����] -> weapons

=== items ===
�������и������õĵ��ߡ�

* [����ҩˮ - 20���] -> buy_potion
* [ħ��ҩˮ - 30���] -> buy_mana_potion
* [����] -> start

=== buy_potion ===
{player_gold >= 20:
    ����ҩˮ����Σ��ʱ���ܾ���һ����
    ~ player_gold -= 20
    ~ give_item(""����ҩˮ"", 1)
    ����������ҩˮ��
- else:
    ����Ҫ20��Ҳ���������ҩˮ��
}
* [����] -> items

=== buy_mana_potion ===
{player_gold >= 30:
    ħ��ҩˮ���ָ���ķ���ֵ��
    ~ player_gold -= 30
    ~ give_item(""ħ��ҩˮ"", 1)
    ������ħ��ҩˮ��
- else:
    ħ��ҩˮ��Ҫ30��ҡ�
}
* [����] -> items

=== information ===
{merchant_trust >= 3:
    ��Ȼ�����ҵĺù˿ͣ��Ҹ�����һ������...
    
    ɭ�����һ�����ϵ��ż���������ű��ء�
    ����ҪС��������ػ��ߣ�
    
    ~ merchant_trust += 1
    ~ set_story_var(""know_forest_secret"", 1)
- else:
    ����кܶ�ð����������ɹ�װ����
    ����������Խ��ԽΣ���ˡ�
}

* [����������Ϣ��] -> more_info
* [лл�����Ϣ] -> start

=== more_info ===
{merchant_trust >= 5:
    �������֪�����ࣿ�ð�...
    
    ����˵��һ�Ѵ�˵�еĽ������ĳ�Ѩ�
    ������㹻ǿ��Ҳ����Գ�����ս��
    
    ~ set_story_var(""know_dragon_sword"", 1)
- else:
    ��ʱû�������ر����Ϣ�ˡ�
}
* [����] -> start

=== goodbye ===
��л���٣�ף��ð��˳����

{has_sword:
    ��ס���ǰ������ᱣ����ģ�
}

{merchant_trust >= 3:
    ��ס�Ҹ���������ܣ�С��ɭ�֣�
}

Ը��ƽ��������

-> END
";

        // ���浽Assets�ļ���
        string path = UnityEditor.AssetDatabase.GenerateUniqueAssetPath("Assets/NPC_Merchant.ink");
        System.IO.File.WriteAllText(path, merchantInk);
        UnityEditor.AssetDatabase.Refresh();

        UnityEditor.EditorUtility.DisplayDialog("�������",
            $"�Ѵ���ʾ��Ink�ļ�: {path}\n��ȴ�Unity�����TextAsset��ʹ�á�", "ȷ��");
    }
}