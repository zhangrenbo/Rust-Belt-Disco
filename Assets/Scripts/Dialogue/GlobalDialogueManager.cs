using UnityEngine;
using UnityEngine.Events;
using Ink.Runtime;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// ȫ�ֶԻ������� - ����ģʽ������UI��ʾ�ͶԻ�����
/// </summary>
public class GlobalDialogueManager : MonoBehaviour
{
    public static GlobalDialogueManager Instance { get; private set; }

    [Header("=== �Ի�UI���� ===")]
    [Tooltip("�Ի����GameObject")]
    public GameObject dialoguePanel;
    [Tooltip("��ʾ�Ի��ı���Text���")]
    public UnityEngine.UI.Text dialogueText;
    [Tooltip("��ʾ˵�������ֵ�Text�������ѡ��")]
    public UnityEngine.UI.Text speakerNameText;
    [Tooltip("�����Ի��İ�ť")]
    public UnityEngine.UI.Button continueButton;
    [Tooltip("ѡ��ť������")]
    public Transform choiceButtonContainer;
    [Tooltip("ѡ��ť��Ԥ����")]
    public UnityEngine.UI.Button choiceButtonPrefab;

    [Header("=== Ĭ�϶Ի����� ===")]
    [Tooltip("Ĭ��ʹ�õ�INK�Ի��ļ�")]
    public TextAsset defaultINKFile;
    [Tooltip("Ĭ�ϵ���ʼ�ڵ����ƣ���ѡ��")]
    public string defaultStartKnot = "";
    [Tooltip("���û��INK�ļ�ʱʹ�õ�Ĭ���ı�")]
    [TextArea(3, 5)]
    public string defaultDialogueText = "��ã�����Ĭ�϶Ի���";

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
    private List<UnityEngine.UI.Button> currentChoiceButtons = new List<UnityEngine.UI.Button>();
    private NPCDialogueController currentNPC;

    // �¼�ϵͳ
    public System.Action<string> OnDialogueStart;
    public System.Action<string> OnDialogueEnd;
    public System.Action<string, int> OnChoiceMade;
    public System.Action<string> OnDialogueChoice; // ���ݾɰ汾���¼�

    void Awake()
    {
        // �Ľ��ĵ���ģʽ - ���ᵼ�¶���ʧ
        if (Instance != null && Instance != this)
        {
            if (showDebugInfo)
                Debug.LogWarning($"[GlobalDialogueManager] �����ظ�ʵ�����������µ�: {gameObject.name}");

            // ���پ�ʵ�������ǵ�ǰʵ��
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
    /// ��ʼINK�Ի�
    /// </summary>
    public void StartDialogue(TextAsset inkFile, string startKnot = "", NPCDialogueController npc = null)
    {
        if (inkFile == null)
        {
            Debug.LogError("[GlobalDialogueManager] INK�ļ�Ϊ�ա�");
            return;
        }

        if (isDialogueActive)
        {
            Debug.LogWarning("[GlobalDialogueManager] �Ѿ��ڶԻ���");
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
            Debug.LogError($"[GlobalDialogueManager] ����Ի�ʧ��: {e.Message}");
        }
    }

    /// <summary>
    /// ��ʼ���ı��Ի�
    /// </summary>
    public void StartDialogue(string dialogueContent)
    {
        if (string.IsNullOrEmpty(dialogueContent))
        {
            Debug.LogError("[GlobalDialogueManager] �Ի�����Ϊ�ա�");
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
            Debug.LogError($"[GlobalDialogueManager] ����򵥶Ի�ʧ��: {e.Message}");
        }
    }

    /// <summary>
    /// �����Ի�
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
            Debug.LogError("[GlobalDialogueManager] ѡ��ť������Ԥ����δ���á�");
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

            // ���������¼��Ա��ּ�����
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
                    Description = $"�ӶԻ���õ�{itemName}",
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
            Debug.Log($"[GlobalDialogueManager] ������Ч: {soundName}");
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