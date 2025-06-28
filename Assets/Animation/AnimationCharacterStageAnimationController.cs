using UnityEngine;
using System.Collections.Generic;

// Stage-based animation controller responsible for switching animations based on character stage.

[RequireComponent(typeof(Animator))]
public class CharacterStageAnimationController : MonoBehaviour
{
    [Header("=== ׶ ===")]
    [Tooltip("ɫĲͬ׶")]
    public CharacterAnimationStage[] animationStages;

    [Header("=== ǰ״̬ ===")]
    [SerializeField] private int currentStageIndex = 0;
    [SerializeField] private string currentStageName = "";

    [Header("===  ===")]
    public bool enableDebugMode = false;
    public bool autoCheckStageChange = true;
    public float checkInterval = 1f;

    // 
    private Animator animator;
    private PlayerController playerController;
    private NPCController npcController;
    private bool isPlayer;

    // ǰЧ
    private List<GameObject> activeStageEffects = new List<GameObject>();

    // ¼ϵͳ
    public System.Action<int, int> OnStageChanged; // ɽ׶, ½׶
    public System.Action<string> OnStageTransition; // ׶

    void Start()
    {
        InitializeComponents();
        SetInitialStage();

        if (autoCheckStageChange)
        {
            InvokeRepeating(nameof(CheckStageChange), checkInterval, checkInterval);
        }
    }

    void InitializeComponents()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        playerController = GetComponent<PlayerController>();
        npcController = GetComponent<NPCController>();
        isPlayer = playerController != null;

        if (animator == null)
        {
            Debug.LogError($"[CharacterStageAnimation] {gameObject.name} ȱ Animator ");
            enabled = false;
        }

        if (animationStages == null || animationStages.Length == 0)
        {
            Debug.LogWarning($"[CharacterStageAnimation] {gameObject.name} ûö׶Σ");
        }
    }

    void SetInitialStage()
    {
        if (animationStages.Length > 0)
        {
            int initialStage = GetValidStageForCurrentConditions();
            ChangeToStage(initialStage, false);
        }
    }

    /// <summary>
    /// ǷҪл׶
    /// </summary>
    public void CheckStageChange()
    {
        if (animationStages == null || animationStages.Length == 0) return;

        int newStageIndex = GetValidStageForCurrentConditions();

        if (newStageIndex != currentStageIndex)
        {
            ChangeToStage(newStageIndex, true);
        }
    }

    /// <summary>
    /// ȡǰµЧ׶
    /// </summary>
    int GetValidStageForCurrentConditions()
    {
        int currentLevel = GetCharacterLevel();

        // Ӻǰ飬ƥ߼Ľ׶
        for (int i = animationStages.Length - 1; i >= 0; i--)
        {
            var stage = animationStages[i];

            // ȼ
            if (currentLevel < stage.minLevel || currentLevel > stage.maxLevel)
                continue;

            // 
            if (!CheckStoryConditions(stage))
                continue;

            return i;
        }

        // ûҵʵĽ׶Σصһ׶
        return 0;
    }

    /// <summary>
    /// 
    /// </summary>
    bool CheckStoryConditions(CharacterAnimationStage stage)
    {
        if (stage.requiredStoryKeys == null || stage.requiredStoryKeys.Length == 0)
            return true;

        if (StoryCtrl.Instance == null)
            return false;

        for (int i = 0; i < stage.requiredStoryKeys.Length; i++)
        {
            string key = stage.requiredStoryKeys[i];
            int requiredValue = i < stage.requiredStoryValues.Length ? stage.requiredStoryValues[i] : 1;

            if (StoryCtrl.Instance.Get(key) < requiredValue)
                return false;
        }

        return true;
    }

    /// <summary>
    /// лָ׶
    /// </summary>
    public void ChangeToStage(int stageIndex, bool playTransitionEffect = true)
    {
        if (stageIndex < 0 || stageIndex >= animationStages.Length)
        {
            Debug.LogWarning($"[CharacterStageAnimation] ЧĽ׶: {stageIndex}");
            return;
        }

        var newStage = animationStages[stageIndex];
        int oldStageIndex = currentStageIndex;

        if (enableDebugMode)
        {
            Debug.Log($"[CharacterStageAnimation] {gameObject.name} л׶: {currentStageName} -> {newStage.stageName}");
        }

        // ɽ׶εЧ
        ClearStageEffects();

        // ӦµĶ
        if (newStage.animatorController != null)
        {
            animator.runtimeAnimatorController = newStage.animatorController;
        }

        // Ӧö
        ApplyAnimationOverrides(newStage);

        // ½׶εЧ
        ActivateStageEffects(newStage);

        // ŹЧ
        if (playTransitionEffect && newStage.stageTransitionSound != null)
        {
            AudioSource.PlayClipAtPoint(newStage.stageTransitionSound, transform.position);
        }

        // µǰ׶Ϣ
        currentStageIndex = stageIndex;
        currentStageName = newStage.stageName;

        // ¼
        OnStageChanged?.Invoke(oldStageIndex, stageIndex);
        OnStageTransition?.Invoke(newStage.stageName);

        if (enableDebugMode)
        {
            Debug.Log($"[CharacterStageAnimation] ׶л: {newStage.stageName}");
        }
    }

    /// <summary>
    /// Ӧö
    /// </summary>
    void ApplyAnimationOverrides(CharacterAnimationStage stage)
    {
        if (stage.parameterOverrides == null) return;

        foreach (var override_ in stage.parameterOverrides)
        {
            if (override_.newClip != null)
            {
                // ҪʵʵĶ߼
                // UnityAnimatorOverrideControllerʵ
                ApplyAnimationClipOverride(override_.parameterName, override_.newClip, override_.speedMultiplier);
            }
        }
    }

    /// <summary>
    /// ӦöƬθ
    /// </summary>
    void ApplyAnimationClipOverride(string parameterName, AnimationClip newClip, float speedMultiplier)
    {
        var overrideController = animator.runtimeAnimatorController as AnimatorOverrideController;

        if (overrideController == null)
        {
            // ǿ
            overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
            animator.runtimeAnimatorController = overrideController;
        }

        // Ҳ滻Ƭ
        var clips = overrideController.animationClips;
        foreach (var clip in clips)
        {
            if (clip.name.Contains(parameterName))
            {
                overrideController[clip] = newClip;
                break;
            }
        }

        // òٶ
        if (speedMultiplier != 1f)
        {
            animator.SetFloat($"{parameterName}_Speed", speedMultiplier);
        }
    }

    /// <summary>
    /// ׶Ч
    /// </summary>
    void ActivateStageEffects(CharacterAnimationStage stage)
    {
        if (stage.stageEffects == null) return;

        foreach (var effectPrefab in stage.stageEffects)
        {
            if (effectPrefab != null)
            {
                var effect = Instantiate(effectPrefab, transform);
                effect.transform.localPosition = Vector3.zero;
                activeStageEffects.Add(effect);
            }
        }
    }

    /// <summary>
    /// ׶Ч
    /// </summary>
    void ClearStageEffects()
    {
        foreach (var effect in activeStageEffects)
        {
            if (effect != null)
            {
                Destroy(effect);
            }
        }
        activeStageEffects.Clear();
    }

    /// <summary>
    /// ȡɫȼ
    /// </summary>
    int GetCharacterLevel()
    {
        if (isPlayer && playerController != null)
        {
            return playerController.Level;
        }
        else if (npcController != null)
        {
            // NPCҪԼĵȼϵͳ
            return 1; // ʱ1ԸҪ޸
        }

        return 1;
    }

    /// <summary>
    /// ǿлָ׶Σ飩
    /// </summary>
    public void ForceChangeToStage(int stageIndex)
    {
        ChangeToStage(stageIndex, true);
    }

    /// <summary>
    /// ݽ׶л
    /// </summary>
    public void ChangeToStageByName(string stageName)
    {
        for (int i = 0; i < animationStages.Length; i++)
        {
            if (animationStages[i].stageName == stageName)
            {
                ChangeToStage(i, true);
                return;
            }
        }

        Debug.LogWarning($"[CharacterStageAnimation] δҵ׶: {stageName}");
    }

    /// <summary>
    /// ȡǰ׶Ϣ
    /// </summary>
    public CharacterAnimationStage GetCurrentStage()
    {
        if (currentStageIndex >= 0 && currentStageIndex < animationStages.Length)
        {
            return animationStages[currentStageIndex];
        }
        return null;
    }

    /// <summary>
    /// ȡǰ׶
    /// </summary>
    public string GetCurrentStageName()
    {
        return currentStageName;
    }

    /// <summary>
    /// ǷԽָ׶
    /// </summary>
    public bool CanEnterStage(int stageIndex)
    {
        if (stageIndex < 0 || stageIndex >= animationStages.Length)
            return false;

        var stage = animationStages[stageIndex];
        int currentLevel = GetCharacterLevel();

        // ȼ
        if (currentLevel < stage.minLevel || currentLevel > stage.maxLevel)
            return false;

        // 
        return CheckStoryConditions(stage);
    }

    void OnDestroy()
    {
        ClearStageEffects();
        CancelInvoke();
    }

    // õGUI
    void OnGUI()
    {
        if (!enableDebugMode) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.BeginVertical("box");

        GUILayout.Label($"ɫ: {gameObject.name}");
        GUILayout.Label($"ǰ׶: {currentStageName}");
        GUILayout.Label($"ȼ: {GetCharacterLevel()}");

        GUILayout.Space(10);

        for (int i = 0; i < animationStages.Length; i++)
        {
            var stage = animationStages[i];
            bool canEnter = CanEnterStage(i);
            bool isCurrent = i == currentStageIndex;

            GUI.color = isCurrent ? Color.green : (canEnter ? Color.white : Color.gray);

            if (GUILayout.Button($"{stage.stageName} (Lv.{stage.minLevel}-{stage.maxLevel})"))
            {
                if (canEnter)
                {
                    ForceChangeToStage(i);
                }
            }
        }

        GUI.color = Color.white;
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}

/// <summary>
/// ׶ζ - ض¼ʱ׶л
/// </summary>
/// </summary>
public class StageAnimationTrigger : MonoBehaviour
{
    [Header("")]
    public string targetStageName;
    public bool triggerOnce = true;

    [Header("")]
    public bool triggerOnStart = false;
    public bool triggerOnLevelUp = false;
    public bool triggerOnStoryEvent = false;
    public string storyEventKey;

    private CharacterStageAnimationController stageController;
    private bool hasTriggered = false;

    void Start()
    {
        stageController = GetComponent<CharacterStageAnimationController>();

        if (stageController == null)
        {
            Debug.LogError("[StageAnimationTrigger] Ҫ CharacterStageAnimationController ");
            enabled = false;
            return;
        }

        if (triggerOnStart)
        {
            TriggerStageChange();
        }

        // ¼
        if (triggerOnLevelUp)
        {
            var playerController = GetComponent<PlayerController>();
            if (playerController != null)
            {
                // ҪPlayerControllerOnLevelUp¼
                playerController.OnLevelUp += OnLevelUp;
            }
        }
    }

    public void TriggerStageChange()
    {
        if (triggerOnce && hasTriggered) return;

        if (stageController != null && !string.IsNullOrEmpty(targetStageName))
        {
            stageController.ChangeToStageByName(targetStageName);
            hasTriggered = true;
        }
    }

    // ͨUnityEvent
    public void OnLevelUp(int newLevel)
    {
        if (triggerOnLevelUp)
        {
            TriggerStageChange();
        }
    }

    // ͨϵͳ
    public void OnStoryEvent(string eventKey)
    {
        if (triggerOnStoryEvent && eventKey == storyEventKey)
        {
            TriggerStageChange();
        }
    }

    void OnDestroy()
    {
        // ¼
        if (triggerOnLevelUp)
        {
            var playerController = GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.OnLevelUp -= OnLevelUp;
            }
        }
    }
}