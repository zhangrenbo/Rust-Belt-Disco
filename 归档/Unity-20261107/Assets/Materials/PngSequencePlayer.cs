using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 独立版PNG序列播放器 - 移除对PlayerController的依赖
/// </summary>
[RequireComponent(typeof(Renderer))]
public class IndependentPngSequencePlayer : MonoBehaviour
{
    [System.Serializable]
    public class AnimationSequence
    {
        [Header("序列信息")]
        [SerializeField] private string _sequenceName = "默认动画";
        [SerializeField] private Texture2D[] _frames = new Texture2D[0];
        [SerializeField] private float _frameRate = 12f;
        [SerializeField] private bool _loop = true;
        [SerializeField] private bool _reverseLoop = false;
        [SerializeField] private bool _flipHorizontal = false;

        [Header("播放条件")]
        [SerializeField] private string _triggerParameterName = "";
        [SerializeField] private AnimationTriggerType _triggerType = AnimationTriggerType.Always;

        [Header("阶段限制")]
        [SerializeField] private string[] _allowedStages = new string[0];
        [SerializeField] private int _minLevel = 1;
        [SerializeField] private int _maxLevel = 999;

        // 属性访问器，避免直接访问字段
        public string sequenceName
        {
            get { return _sequenceName; }
            set { _sequenceName = value; }
        }

        public Texture2D[] frames
        {
            get { return _frames ?? (_frames = new Texture2D[0]); }
            set { _frames = value ?? new Texture2D[0]; }
        }

        public float frameRate
        {
            get { return _frameRate; }
            set { _frameRate = Mathf.Max(0.1f, value); }
        }

        public bool loop
        {
            get { return _loop; }
            set { _loop = value; }
        }

        public bool reverseLoop
        {
            get { return _reverseLoop; }
            set { _reverseLoop = value; }
        }

        public bool flipHorizontal
        {
            get { return _flipHorizontal; }
            set { _flipHorizontal = value; }
        }

        public string triggerParameterName
        {
            get { return _triggerParameterName; }
            set { _triggerParameterName = value ?? ""; }
        }

        public AnimationTriggerType triggerType
        {
            get { return _triggerType; }
            set { _triggerType = value; }
        }

        public string[] allowedStages
        {
            get { return _allowedStages ?? (_allowedStages = new string[0]); }
            set { _allowedStages = value ?? new string[0]; }
        }

        public int minLevel
        {
            get { return _minLevel; }
            set { _minLevel = Mathf.Max(1, value); }
        }

        public int maxLevel
        {
            get { return _maxLevel; }
            set { _maxLevel = Mathf.Max(_minLevel, value); }
        }

        // 验证序列有效性
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(_sequenceName) &&
                   _frames != null &&
                   _frames.Length > 0 &&
                   _frameRate > 0;
        }
    }

    public enum AnimationTriggerType
    {
        Always,      // 总是播放
        OnTrigger,   // 触发时播放
        OnState,     // 状态激活时播放
        OnLevel,     // 特定等级时播放
        OnStage      // 特定阶段时播放
    }

    [Header("=== 动画序列配置 ===")]
    [SerializeField] private AnimationSequence[] _animationSequences = new AnimationSequence[2];

    [Header("=== 基础设置 ===")]
    [SerializeField] private int _defaultSequenceIndex = 0;
    [SerializeField] private bool _playOnStart = true;
    [SerializeField] private bool _smoothTransition = true;
    [SerializeField] private float _transitionDuration = 0.2f;

    [Header("=== 外部状态接口 ===")]
    [SerializeField] private int _currentLevel = 1;
    [SerializeField] private string _currentStageName = "默认阶段";

    [Header("=== 调试设置 ===")]
    [SerializeField] private bool _showDebugInfo = false;
    [SerializeField] private bool _showGizmoInfo = false;

    // 公共属性访问器
    public AnimationSequence[] animationSequences
    {
        get
        {
            if (_animationSequences == null)
                _animationSequences = new AnimationSequence[2];
            return _animationSequences;
        }
        set { _animationSequences = value; }
    }

    public int defaultSequenceIndex
    {
        get { return _defaultSequenceIndex; }
        set { _defaultSequenceIndex = Mathf.Max(0, value); }
    }

    public bool playOnStart
    {
        get { return _playOnStart; }
        set { _playOnStart = value; }
    }

    public bool smoothTransition
    {
        get { return _smoothTransition; }
        set { _smoothTransition = value; }
    }

    public float transitionDuration
    {
        get { return _transitionDuration; }
        set { _transitionDuration = Mathf.Max(0f, value); }
    }

    public bool showDebugInfo
    {
        get { return _showDebugInfo; }
        set { _showDebugInfo = value; }
    }

    public bool showGizmoInfo
    {
        get { return _showGizmoInfo; }
        set { _showGizmoInfo = value; }
    }

    // 外部状态接口
    public int CurrentLevel
    {
        get { return _currentLevel; }
        set
        {
            if (_currentLevel != value)
            {
                _currentLevel = value;
                OnLevelChanged(value);
            }
        }
    }

    public string CurrentStage
    {
        get { return _currentStageName; }
        set
        {
            if (_currentStageName != value)
            {
                string oldStage = _currentStageName;
                _currentStageName = value;
                OnStageChanged(oldStage, value);
            }
        }
    }

    // 内部状态
    private Renderer _renderer;
    private Material _instanceMat;
    private MaterialPropertyBlock _materialPropertyBlock;
    private int _currentSequenceIndex = -1;
    private int _currentFrame = 0;
    private float _timer = 0f;
    private bool _isPlaying = false;
    private bool _isTransitioning = false;

    // 逆向循环状态
    private bool _isPlayingReverse = false;
    private bool _hasCompletedForward = false;

    // 组件引用（可选）
    private Animator _animator;

    // 过渡效果
    private Coroutine _transitionCoroutine;
    private AnimationSequence _currentSequence;
    private AnimationSequence _targetSequence;

    // 缓存的GUIStyle，避免频繁创建
#if UNITY_EDITOR
    private GUIStyle _debugLabelStyle;
    private GUIStyle _debugButtonStyle;
    private GUIStyle _handleLabelStyle;
    private bool _stylesInitialized = false;
#endif

    // 材质属性ID缓存 - 兼容不同渲染管线
    private static readonly int MainTexPropertyID = Shader.PropertyToID("_MainTex");
    private static readonly int BaseMapPropertyID = Shader.PropertyToID("_BaseMap"); // URP
    private static readonly int ColorPropertyID = Shader.PropertyToID("_Color");
    private static readonly int BaseColorPropertyID = Shader.PropertyToID("_BaseColor"); // URP

    // 运行时检测的属性ID
    private int _activeTexPropertyID;
    private int _activeColorPropertyID;

#if UNITY_EDITOR
    void InitializeGUIStyles()
    {
        if (_stylesInitialized) return;

        _debugLabelStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
        _debugButtonStyle = new GUIStyle(GUI.skin.button);
        _handleLabelStyle = new GUIStyle()
        {
            normal = new GUIStyleState { textColor = Color.white },
            fontSize = 12
        };
        
        _stylesInitialized = true;
    }
#endif

    void Awake()
    {
        // 初始化数组，防止空引用
        InitializeArrays();

        _renderer = GetComponent<Renderer>();
        if (_renderer != null)
        {
            // 基于 sharedMaterial 显式创建实例材质，一次性分配
            _instanceMat = new Material(_renderer.sharedMaterial);
            _renderer.material = _instanceMat;

            // 初始化MaterialPropertyBlock用于高效属性修改
            _materialPropertyBlock = new MaterialPropertyBlock();

            // 检测当前材质支持的属性ID
            DetectMaterialProperties();
        }

        // 获取相关组件（可选）
        _animator = GetComponent<Animator>();
    }

    /// <summary>
    /// 检测材质支持的属性，兼容不同渲染管线
    /// </summary>
    void DetectMaterialProperties()
    {
        if (_instanceMat == null) return;

        // 检查着色器兼容性
        string shaderName = _instanceMat.shader.name;
        bool isProblematicShader = shaderName.Contains("ToonLitWithEnhancedOutline") ||
                                  shaderName.Contains("ToonLit") ||
                                  shaderName.Contains("Toon");

        if (isProblematicShader && _showDebugInfo)
        {
            Debug.LogWarning($"[IndependentPngSequencePlayer] 检测到可能有问题的着色器: {shaderName}");
            Debug.LogWarning("[IndependentPngSequencePlayer] 建议使用 'Custom/CompatibleSpriteUnlit' 着色器以避免渲染错误");
        }

        // 检测贴图属性
        if (_instanceMat.HasProperty(BaseMapPropertyID))
        {
            _activeTexPropertyID = BaseMapPropertyID; // URP
        }
        else if (_instanceMat.HasProperty(MainTexPropertyID))
        {
            _activeTexPropertyID = MainTexPropertyID; // Built-in RP
        }
        else
        {
            _activeTexPropertyID = MainTexPropertyID; // 默认
            if (_showDebugInfo)
            {
                Debug.LogWarning($"[IndependentPngSequencePlayer] 材质不支持标准贴图属性，使用默认 _MainTex");
            }
        }

        // 检测颜色属性
        if (_instanceMat.HasProperty(BaseColorPropertyID))
        {
            _activeColorPropertyID = BaseColorPropertyID; // URP
        }
        else if (_instanceMat.HasProperty(ColorPropertyID))
        {
            _activeColorPropertyID = ColorPropertyID; // Built-in RP
        }
        else
        {
            _activeColorPropertyID = ColorPropertyID; // 默认
            if (_showDebugInfo)
            {
                Debug.LogWarning($"[IndependentPngSequencePlayer] 材质不支持标准颜色属性，使用默认 _Color");
            }
        }

        if (_showDebugInfo)
        {
            string texPropertyName = _activeTexPropertyID == BaseMapPropertyID ? "_BaseMap" : "_MainTex";
            string colorPropertyName = _activeColorPropertyID == BaseColorPropertyID ? "_BaseColor" : "_Color";
            Debug.Log($"[IndependentPngSequencePlayer] 着色器: {shaderName}");
            Debug.Log($"[IndependentPngSequencePlayer] 检测到材质属性 - 贴图: {texPropertyName}, 颜色: {colorPropertyName}");
        }
    }

    void Start()
    {
        // 再次确保数组初始化
        ValidateConfiguration();

        if (_playOnStart)
        {
            PlayDefaultSequence();
        }
    }

    /// <summary>
    /// 初始化数组，防止空引用异常
    /// </summary>
    void InitializeArrays()
    {
        if (_animationSequences == null)
        {
            _animationSequences = new AnimationSequence[2];
        }

        // 确保每个序列都已初始化
        for (int i = 0; i < _animationSequences.Length; i++)
        {
            if (_animationSequences[i] == null)
            {
                _animationSequences[i] = new AnimationSequence();

                // 设置默认名称
                if (i == 0)
                {
                    _animationSequences[i].sequenceName = "待机";
                    _animationSequences[i].reverseLoop = true;
                    _animationSequences[i].frameRate = 6f;
                }
                else if (i == 1)
                {
                    _animationSequences[i].sequenceName = "摆动";
                    _animationSequences[i].loop = false;
                    _animationSequences[i].reverseLoop = false;
                    _animationSequences[i].frameRate = 12f;
                    _animationSequences[i].triggerType = AnimationTriggerType.OnTrigger;
                }
            }
        }
    }

    /// <summary>
    /// 验证配置有效性
    /// </summary>
    void ValidateConfiguration()
    {
        InitializeArrays();

        // 验证默认序列索引
        if (_defaultSequenceIndex >= _animationSequences.Length)
        {
            _defaultSequenceIndex = 0;
        }

        // 输出配置信息
        if (_showDebugInfo)
        {
            Debug.Log($"[IndependentPngSequencePlayer] 配置验证完成，共 {_animationSequences.Length} 个序列");
            for (int i = 0; i < _animationSequences.Length; i++)
            {
                var seq = _animationSequences[i];
                if (seq != null)
                {
                    Debug.Log($"序列 {i}: {seq.sequenceName}, 帧数: {seq.frames?.Length ?? 0}, 有效: {seq.IsValid()}");
                }
            }
        }
    }

    void Update()
    {
        if (!_isPlaying || _isTransitioning) return;

        UpdateCurrentSequence();
        CheckForSequenceChange();
    }

    void UpdateCurrentSequence()
    {
        if (_currentSequence == null || !_currentSequence.IsValid())
            return;

        _timer += Time.deltaTime;
        float interval = 1f / _currentSequence.frameRate;

        if (_timer >= interval)
        {
            _timer -= interval;

            // 根据播放方向更新帧索引
            if (_isPlayingReverse)
            {
                _currentFrame--;
            }
            else
            {
                _currentFrame++;
            }

            // 处理帧索引边界和循环逻辑
            HandleFrameBoundaries();

            // 应用当前帧
            ApplyCurrentFrame();

            // 调试输出
            LogFrameUpdate();
        }
    }

    void HandleFrameBoundaries()
    {
        if (_isPlayingReverse)
        {
            // 逆向播放完成
            if (_currentFrame < 0)
            {
                if (_currentSequence.reverseLoop)
                {
                    // 逆向循环：切换回正向播放
                    _isPlayingReverse = false;
                    _hasCompletedForward = false;
                    _currentFrame = 0;

                    if (_showDebugInfo)
                    {
                        Debug.Log($"[IndependentPngSequencePlayer] 逆向播放完成，切换到正向播放: {_currentSequence.sequenceName}");
                    }
                }
                else if (_currentSequence.loop)
                {
                    // 普通循环
                    _currentFrame = _currentSequence.frames.Length - 1;
                }
                else
                {
                    // 不循环，停止播放
                    _isPlaying = false;
                    OnSequenceComplete();
                    return;
                }
            }
        }
        else
        {
            // 正向播放完成
            if (_currentFrame >= _currentSequence.frames.Length)
            {
                if (_currentSequence.reverseLoop && !_hasCompletedForward)
                {
                    // 第一次正向播放完成，开始逆向播放
                    _isPlayingReverse = true;
                    _hasCompletedForward = true;
                    _currentFrame = _currentSequence.frames.Length - 2; // 跳过最后一帧，避免重复

                    if (_showDebugInfo)
                    {
                        Debug.Log($"[IndependentPngSequencePlayer] 正向播放完成，开始逆向播放: {_currentSequence.sequenceName}");
                    }
                }
                else if (_currentSequence.loop)
                {
                    // 普通循环
                    _currentFrame = 0;
                    _hasCompletedForward = false;
                }
                else
                {
                    // 不循环，停止播放
                    _isPlaying = false;
                    OnSequenceComplete();
                    return;
                }
            }
        }

        // 确保帧索引在有效范围内
        _currentFrame = Mathf.Clamp(_currentFrame, 0, _currentSequence.frames.Length - 1);
    }

    void ApplyCurrentFrame()
    {
        if (_currentSequence.frames != null &&
            _currentFrame >= 0 &&
            _currentFrame < _currentSequence.frames.Length &&
            _currentSequence.frames[_currentFrame] != null &&
            _renderer != null)
        {
            // 使用MaterialPropertyBlock高效修改贴图，避免材质克隆
            _renderer.GetPropertyBlock(_materialPropertyBlock);
            _materialPropertyBlock.SetTexture(_activeTexPropertyID, _currentSequence.frames[_currentFrame]);
            _renderer.SetPropertyBlock(_materialPropertyBlock);

            // 应用左右翻转
            if (_currentSequence.flipHorizontal)
            {
                Vector3 scale = transform.localScale;
                scale.x = -Mathf.Abs(scale.x);
                transform.localScale = scale;
            }
            else
            {
                Vector3 scale = transform.localScale;
                scale.x = Mathf.Abs(scale.x);
                transform.localScale = scale;
            }
        }
    }

    void LogFrameUpdate()
    {
        if (_showDebugInfo)
        {
            string direction = _isPlayingReverse ? "逆向" : "正向";
            Debug.Log($"[IndependentPngSequencePlayer] {direction}播放序列: {_currentSequence.sequenceName}, 帧: {_currentFrame + 1}/{_currentSequence.frames.Length}");
        }
    }

    // ========== 序列切换逻辑 ==========

    void CheckForSequenceChange()
    {
        var bestSequence = GetBestSequenceForCurrentState();

        if (bestSequence != _currentSequence && bestSequence != null)
        {
            SwitchToSequence(bestSequence);
        }
    }

    AnimationSequence GetBestSequenceForCurrentState()
    {
        // 使用内部状态而不是 PlayerController
        for (int i = 0; i < _animationSequences.Length; i++)
        {
            var sequence = _animationSequences[i];
            if (sequence == null || !sequence.IsValid()) continue;

            if (!IsSequenceValidForCurrentState(sequence, _currentStageName, _currentLevel))
                continue;

            // 检查触发条件
            if (CheckTriggerCondition(sequence))
            {
                return sequence;
            }
        }

        // 如果没有找到合适的序列，返回默认序列
        if (_defaultSequenceIndex >= 0 &&
            _defaultSequenceIndex < _animationSequences.Length &&
            _animationSequences[_defaultSequenceIndex] != null)
        {
            return _animationSequences[_defaultSequenceIndex];
        }

        return null;
    }

    bool IsSequenceValidForCurrentState(AnimationSequence sequence, string currentStage, int currentLevel)
    {
        // 检查等级限制
        if (currentLevel < sequence.minLevel || currentLevel > sequence.maxLevel)
            return false;

        // 检查阶段限制
        if (sequence.allowedStages != null && sequence.allowedStages.Length > 0)
        {
            bool stageAllowed = false;
            foreach (var allowedStage in sequence.allowedStages)
            {
                if (allowedStage == currentStage)
                {
                    stageAllowed = true;
                    break;
                }
            }
            if (!stageAllowed) return false;
        }

        return true;
    }

    bool CheckTriggerCondition(AnimationSequence sequence)
    {
        switch (sequence.triggerType)
        {
            case AnimationTriggerType.Always:
                return true;

            case AnimationTriggerType.OnTrigger:
                if (_animator != null && !string.IsNullOrEmpty(sequence.triggerParameterName))
                {
                    return _animator.GetBool(sequence.triggerParameterName);
                }
                return false;

            case AnimationTriggerType.OnState:
                if (_animator != null && !string.IsNullOrEmpty(sequence.triggerParameterName))
                {
                    var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
                    return stateInfo.IsName(sequence.triggerParameterName);
                }
                return false;

            case AnimationTriggerType.OnLevel:
                return _currentLevel >= sequence.minLevel && _currentLevel <= sequence.maxLevel;

            case AnimationTriggerType.OnStage:
                if (sequence.allowedStages != null && sequence.allowedStages.Length > 0)
                {
                    foreach (var stage in sequence.allowedStages)
                    {
                        if (stage == _currentStageName) return true;
                    }
                }
                return false;

            default:
                return false;
        }
    }

    void SwitchToSequence(AnimationSequence newSequence)
    {
        if (newSequence == _currentSequence || newSequence == null) return;

        _targetSequence = newSequence;

        if (_smoothTransition && _transitionDuration > 0)
        {
            StartTransition();
        }
        else
        {
            ApplySequenceImmediately(newSequence);
        }
    }

    void StartTransition()
    {
        if (_transitionCoroutine != null)
        {
            StopCoroutine(_transitionCoroutine);
        }

        _isTransitioning = true;
        _transitionCoroutine = StartCoroutine(TransitionCoroutine());
    }

    System.Collections.IEnumerator TransitionCoroutine()
    {
        if (_renderer == null) yield break;

        float elapsed = 0f;
        Color originalColor = Color.white;

        // 获取当前颜色
        _renderer.GetPropertyBlock(_materialPropertyBlock);
        if (_materialPropertyBlock.HasProperty(_activeColorPropertyID))
        {
            originalColor = _materialPropertyBlock.GetColor(_activeColorPropertyID);
        }
        else if (_instanceMat != null && _instanceMat.HasProperty(_activeColorPropertyID))
        {
            originalColor = _instanceMat.GetColor(_activeColorPropertyID);
        }

        // 淡出当前序列
        while (elapsed < _transitionDuration / 2f)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / (_transitionDuration / 2f));

            _renderer.GetPropertyBlock(_materialPropertyBlock);
            _materialPropertyBlock.SetColor(_activeColorPropertyID, new Color(originalColor.r, originalColor.g, originalColor.b, alpha));
            _renderer.SetPropertyBlock(_materialPropertyBlock);

            yield return null;
        }

        // 切换序列
        ApplySequenceImmediately(_targetSequence);

        elapsed = 0f;
        // 淡入新序列
        while (elapsed < _transitionDuration / 2f)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsed / (_transitionDuration / 2f));

            _renderer.GetPropertyBlock(_materialPropertyBlock);
            _materialPropertyBlock.SetColor(_activeColorPropertyID, new Color(originalColor.r, originalColor.g, originalColor.b, alpha));
            _renderer.SetPropertyBlock(_materialPropertyBlock);

            yield return null;
        }

        // 恢复原始颜色
        _renderer.GetPropertyBlock(_materialPropertyBlock);
        _materialPropertyBlock.SetColor(_activeColorPropertyID, originalColor);
        _renderer.SetPropertyBlock(_materialPropertyBlock);

        _isTransitioning = false;
        _transitionCoroutine = null;
    }

    void ApplySequenceImmediately(AnimationSequence sequence)
    {
        if (sequence == null || !sequence.IsValid()) return;

        _currentSequence = sequence;
        _currentFrame = 0;
        _timer = 0f;
        _isPlaying = true;

        // 重置逆向循环状态
        _isPlayingReverse = false;
        _hasCompletedForward = false;

        ApplyCurrentFrame();

        if (_showDebugInfo)
        {
            string loopType = sequence.reverseLoop ? "逆向循环" : (sequence.loop ? "普通循环" : "单次播放");
            Debug.Log($"[IndependentPngSequencePlayer] 切换到序列: {sequence.sequenceName} ({loopType})");
        }
    }

    // ========== 公共接口 ==========

    public void PlaySequenceByName(string sequenceName)
    {
        if (string.IsNullOrEmpty(sequenceName)) return;

        for (int i = 0; i < _animationSequences.Length; i++)
        {
            if (_animationSequences[i] != null && _animationSequences[i].sequenceName == sequenceName)
            {
                SwitchToSequence(_animationSequences[i]);
                return;
            }
        }

        if (_showDebugInfo)
        {
            Debug.LogWarning($"[IndependentPngSequencePlayer] 未找到名为 '{sequenceName}' 的动画序列");
        }
    }

    public void PlaySequenceByIndex(int index)
    {
        if (index >= 0 && index < _animationSequences.Length && _animationSequences[index] != null)
        {
            SwitchToSequence(_animationSequences[index]);
        }
        else if (_showDebugInfo)
        {
            Debug.LogWarning($"[IndependentPngSequencePlayer] 序列索引 {index} 超出范围或为空");
        }
    }

    public void PlayDefaultSequence()
    {
        if (_defaultSequenceIndex >= 0 &&
            _defaultSequenceIndex < _animationSequences.Length &&
            _animationSequences[_defaultSequenceIndex] != null)
        {
            SwitchToSequence(_animationSequences[_defaultSequenceIndex]);
        }
    }

    public void PauseSequence()
    {
        _isPlaying = false;
    }

    public void ResumeSequence()
    {
        _isPlaying = true;
    }

    public void StopSequence()
    {
        _isPlaying = false;
        _currentFrame = 0;
        _timer = 0f;

        // 重置逆向循环状态
        _isPlayingReverse = false;
        _hasCompletedForward = false;
    }

    public string GetCurrentSequenceName()
    {
        return _currentSequence?.sequenceName ?? "无";
    }

    public float GetSequenceProgress()
    {
        if (_currentSequence == null || !_currentSequence.IsValid())
            return 0f;

        if (_currentSequence.reverseLoop)
        {
            // 逆向循环模式下的进度计算
            if (_isPlayingReverse)
            {
                // 逆向播放：50% + (当前帧的逆向进度 * 50%)
                float reverseProgress = 1f - ((float)_currentFrame / (_currentSequence.frames.Length - 1));
                return 0.5f + (reverseProgress * 0.5f);
            }
            else
            {
                // 正向播放：当前帧进度 * 50%
                return ((float)_currentFrame / (_currentSequence.frames.Length - 1)) * 0.5f;
            }
        }
        else
        {
            // 普通模式
            return (float)_currentFrame / (_currentSequence.frames.Length - 1);
        }
    }

    public bool IsPlayingReverse()
    {
        return _isPlayingReverse;
    }

    public void TogglePlayDirection()
    {
        if (_currentSequence != null && _currentSequence.reverseLoop)
        {
            _isPlayingReverse = !_isPlayingReverse;

            if (_showDebugInfo)
            {
                string direction = _isPlayingReverse ? "逆向" : "正向";
                Debug.Log($"[IndependentPngSequencePlayer] 手动切换播放方向为: {direction}");
            }
        }
    }

    // ========== 事件处理 ==========

    void OnStageChanged(string oldStage, string newStage)
    {
        if (_showDebugInfo)
        {
            Debug.Log($"[IndependentPngSequencePlayer] 阶段变化: {oldStage} -> {newStage}");
        }

        CheckForSequenceChange();
    }

    void OnLevelChanged(int newLevel)
    {
        if (_showDebugInfo)
        {
            Debug.Log($"[IndependentPngSequencePlayer] 等级变化: {newLevel}");
        }

        CheckForSequenceChange();
    }

    void OnSequenceComplete()
    {
        if (_showDebugInfo)
        {
            Debug.Log($"[IndependentPngSequencePlayer] 序列播放完成: {_currentSequence?.sequenceName ?? "无"}");
        }

        // 非循环序列播放完成后，尝试切换到默认序列
        if (_currentSequence != null && !_currentSequence.loop && !_currentSequence.reverseLoop)
        {
            PlayDefaultSequence();
        }
    }

    // ========== 批量导入功能（增强版） ==========

    [ContextMenu("批量分配选中的贴图")]
    public void BatchAssignSelectedTextures()
    {
#if UNITY_EDITOR
        var selectedTextures = GetSelectedTextures();
        
        if (selectedTextures.Count == 0)
        {
            UnityEditor.EditorUtility.DisplayDialog("提示", "请先选中要分配的贴图文件", "确定");
            return;
        }

        if (_animationSequences == null || _animationSequences.Length == 0)
        {
            UnityEditor.EditorUtility.DisplayDialog("错误", "请先设置动画序列", "确定");
            return;
        }

        // 使用改进的序列选择方法
        int selectedIndex = ShowSequenceSelectionWindow(selectedTextures.Count);

        if (selectedIndex >= 0 && selectedIndex < _animationSequences.Length)
        {
            // 按名称排序（确保帧顺序正确）
            selectedTextures.Sort((a, b) => a.name.CompareTo(b.name));
            
            AssignTexturesToSequence(selectedIndex, selectedTextures);
            string sequenceName = _animationSequences[selectedIndex]?.sequenceName ?? $"序列 {selectedIndex}";
            UnityEditor.EditorUtility.DisplayDialog("完成", 
                $"已将 {selectedTextures.Count} 张贴图分配到 {sequenceName}", 
                "确定");
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }

    [ContextMenu("批量分配选中的贴图（高级模式）")]
    public void AdvancedBatchAssignSelectedTextures()
    {
#if UNITY_EDITOR
        var selectedTextures = GetSelectedTextures();
        
        if (selectedTextures.Count == 0)
        {
            UnityEditor.EditorUtility.DisplayDialog("提示", "请先选中要分配的贴图文件", "确定");
            return;
        }

        if (_animationSequences == null || _animationSequences.Length == 0)
        {
            UnityEditor.EditorUtility.DisplayDialog("错误", "请先设置动画序列", "确定");
            return;
        }

        // 显示高级批量分配窗口
        ShowAdvancedBatchAssignWindow(selectedTextures);
#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// 显示序列选择窗口，支持任意数量的序列
    /// </summary>
    int ShowSequenceSelectionWindow(int textureCount)
    {
        // 创建序列选项
        string[] sequenceOptions = new string[_animationSequences.Length + 1];
        for (int i = 0; i < _animationSequences.Length; i++)
        {
            string sequenceName = _animationSequences[i]?.sequenceName ?? "未命名";
            int currentFrameCount = _animationSequences[i]?.frames?.Length ?? 0;
            sequenceOptions[i] = $"序列 {i}: {sequenceName} (当前: {currentFrameCount} 帧)";
        }
        sequenceOptions[_animationSequences.Length] = "取消";

        // 使用简单对话框
        if (_animationSequences.Length <= 3)
        {
            return ShowSimpleSequenceDialog(sequenceOptions, textureCount);
        }
        else
        {
            return ShowPagedSequenceDialog(sequenceOptions, textureCount);
        }
    }

    /// <summary>
    /// 简单序列选择对话框（适用于少量序列）
    /// </summary>
    int ShowSimpleSequenceDialog(string[] options, int textureCount)
    {
        string message = $"将 {textureCount} 张贴图分配到哪个序列？";
        
        if (options.Length <= 3)
        {
            return UnityEditor.EditorUtility.DisplayDialogComplex(
                "选择目标序列",
                message,
                options[0],
                options.Length > 1 ? options[1] : "取消",
                options.Length > 2 ? options[2] : "取消"
            );
        }
        else
        {
            return ShowExtendedSequenceDialog(options, message);
        }
    }

    /// <summary>
    /// 分页序列选择对话框（适用于大量序列）
    /// </summary>
    int ShowPagedSequenceDialog(string[] options, int textureCount)
    {
        int sequenceCount = options.Length - 1;
        int pageSize = 8;
        int totalPages = Mathf.CeilToInt((float)sequenceCount / pageSize);
        int currentPage = 0;
        
        while (true)
        {
            int startIndex = currentPage * pageSize;
            int endIndex = Mathf.Min(startIndex + pageSize, sequenceCount);
            
            string[] pageOptions = new string[4];
            int validOptions = 0;
            
            for (int i = startIndex; i < endIndex && validOptions < 2; i++)
            {
                pageOptions[validOptions] = options[i];
                validOptions++;
            }
            
            if (currentPage > 0)
            {
                pageOptions[2] = "< 上一页";
            }
            else if (currentPage < totalPages - 1)
            {
                pageOptions[2] = "下一页 >";
            }
            else
            {
                pageOptions[2] = "取消";
            }
            
            if (currentPage < totalPages - 1 && pageOptions[2] != "下一页 >")
            {
                pageOptions[3] = "下一页 >";
            }
            else if (currentPage > 0 && pageOptions[2] != "< 上一页")
            {
                pageOptions[3] = "< 上一页";
            }
            else
            {
                pageOptions[3] = "取消";
            }
            
            string message = $"将 {textureCount} 张贴图分配到哪个序列？\n页面 {currentPage + 1}/{totalPages}";
            
            int result = UnityEditor.EditorUtility.DisplayDialogComplex(
                "选择目标序列",
                message,
                pageOptions[0] ?? "空",
                pageOptions[1] ?? "空",
                pageOptions[2] ?? "取消"
            );
            
            if (result == 0 && !string.IsNullOrEmpty(pageOptions[0]) && !pageOptions[0].Contains("页"))
            {
                return startIndex;
            }
            else if (result == 1 && !string.IsNullOrEmpty(pageOptions[1]) && !pageOptions[1].Contains("页"))
            {
                return startIndex + 1;
            }
            else if (result == 2)
            {
                if (pageOptions[2] == "< 上一页")
                {
                    currentPage--;
                    continue;
                }
                else if (pageOptions[2] == "下一页 >")
                {
                    currentPage++;
                    continue;
                }
                else if (!pageOptions[2].Contains("页") && startIndex + 2 < sequenceCount)
                {
                    return startIndex + 2;
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                return -1;
            }
        }
    }

    /// <summary>
    /// 扩展的序列选择对话框
    /// </summary>
    int ShowExtendedSequenceDialog(string[] options, string message)
    {
        var window = UnityEditor.EditorWindow.GetWindow<SequenceSelectionWindow>(true, "选择动画序列", true);
        window.Initialize(options, message, (selectedIndex) => {
            window.Close();
            if (selectedIndex >= 0)
            {
                SequenceSelectionWindow.LastSelectedIndex = selectedIndex;
            }
        });
        
        window.ShowModal();
        return SequenceSelectionWindow.LastSelectedIndex;
    }

    /// <summary>
    /// 显示高级批量分配窗口
    /// </summary>
    void ShowAdvancedBatchAssignWindow(System.Collections.Generic.List<UnityEngine.Texture2D> selectedTextures)
    {
        var window = UnityEditor.EditorWindow.GetWindow<AdvancedBatchAssignWindow>(true, "高级批量分配", true);
        window.Initialize(this, selectedTextures);
        window.Show();
    }

    System.Collections.Generic.List<UnityEngine.Texture2D> GetSelectedTextures()
    {
        var selectedTextures = new System.Collections.Generic.List<UnityEngine.Texture2D>();
        
        foreach (var obj in UnityEditor.Selection.objects)
        {
            if (obj is UnityEngine.Texture2D texture)
            {
                selectedTextures.Add(texture);
            }
        }
        
        return selectedTextures;
    }

    public void AssignTexturesToSequence(int sequenceIndex, System.Collections.Generic.List<UnityEngine.Texture2D> textures)
    {
        if (sequenceIndex < 0 || sequenceIndex >= _animationSequences.Length) return;
        if (_animationSequences[sequenceIndex] == null) return;
        
        var sequence = _animationSequences[sequenceIndex];
        
        sequence.frames = new UnityEngine.Texture2D[textures.Count];
        
        for (int i = 0; i < textures.Count; i++)
        {
            sequence.frames[i] = textures[i];
        }
        
        Debug.Log($"[IndependentPngSequencePlayer] 已将 {textures.Count} 张贴图分配到序列 '{sequence.sequenceName}'");
    }

    public System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<UnityEngine.Texture2D>> GroupTexturesByName(System.Collections.Generic.List<UnityEngine.Texture2D> textures)
    {
        var groups = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<UnityEngine.Texture2D>>();

        foreach (var texture in textures)
        {
            string animationType = GetAnimationTypeFromName(texture.name);

            if (!groups.ContainsKey(animationType))
            {
                groups[animationType] = new System.Collections.Generic.List<UnityEngine.Texture2D>();
            }

            groups[animationType].Add(texture);
        }

        var sortedGroups = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<UnityEngine.Texture2D>>();
        foreach (var kvp in groups)
        {
            var sortedList = new System.Collections.Generic.List<UnityEngine.Texture2D>(kvp.Value);
            sortedList.Sort((a, b) => a.name.CompareTo(b.name));
            sortedGroups[kvp.Key] = sortedList;
        }

        return sortedGroups;
    }

    string GetAnimationTypeFromName(string textureName)
    {
        string lowerName = textureName.ToLower();

        if (lowerName.Contains("idle") || lowerName.Contains("待机"))
            return "idle";
        else if (lowerName.Contains("sway") || lowerName.Contains("摆动") || lowerName.Contains("swing"))
            return "sway";
        else if (lowerName.Contains("walk") || lowerName.Contains("走路"))
            return "walk";
        else if (lowerName.Contains("run") || lowerName.Contains("奔跑"))
            return "run";
        else if (lowerName.Contains("turn") || lowerName.Contains("转向"))
            return "turn";
        else if (lowerName.Contains("attack") || lowerName.Contains("攻击"))
            return "attack";
        else if (lowerName.Contains("jump") || lowerName.Contains("跳跃"))
            return "jump";
        else if (lowerName.Contains("die") || lowerName.Contains("死亡"))
            return "die";
        else
        {
            string[] parts = textureName.Split('_', '-', ' ');
            return parts.Length > 0 ? parts[0].ToLower() : "unknown";
        }
    }

    public int FindSequenceByName(string animationType)
    {
        for (int i = 0; i < _animationSequences.Length; i++)
        {
            if (_animationSequences[i] == null) continue;
            
            string sequenceName = _animationSequences[i].sequenceName.ToLower();
            if (sequenceName.Contains(animationType))
            {
                return i;
            }
        }
        return -1;
    }
#endif

    void OnDestroy()
    {
        if (_transitionCoroutine != null)
        {
            StopCoroutine(_transitionCoroutine);
        }

        // 清理实例化的材质，避免内存泄漏
        if (_instanceMat != null)
        {
            DestroyImmediate(_instanceMat);
        }
    }

    // ========== 调试功能 ==========

    void OnGUI()
    {
        if (!_showDebugInfo) return;

#if UNITY_EDITOR
        if (!_stylesInitialized)
        {
            InitializeGUIStyles();
        }
#endif

        GUILayout.BeginArea(new Rect(Screen.width - 300, 10, 290, 250));
        GUILayout.BeginVertical("box");

#if UNITY_EDITOR
        GUILayout.Label("独立PNG序列播放器", _debugLabelStyle);
#else
        GUILayout.Label("独立PNG序列播放器", GUI.skin.label);
#endif
        GUILayout.Label($"当前序列: {GetCurrentSequenceName()}");
        GUILayout.Label($"当前帧: {_currentFrame + 1}/{(_currentSequence?.frames?.Length ?? 0)}");
        GUILayout.Label($"播放进度: {GetSequenceProgress():P1}");
        GUILayout.Label($"是否播放: {_isPlaying}");
        GUILayout.Label($"是否过渡中: {_isTransitioning}");
        GUILayout.Label($"当前等级: {_currentLevel}");
        GUILayout.Label($"当前阶段: {_currentStageName}");

        // 显示逆向循环状态
        if (_currentSequence != null && _currentSequence.reverseLoop)
        {
            string direction = _isPlayingReverse ? "逆向" : "正向";
            GUILayout.Label($"播放方向: {direction}");
            GUILayout.Label($"已完成正向: {_hasCompletedForward}");
        }

        GUILayout.Space(10);

#if UNITY_EDITOR
        if (GUILayout.Button(_isPlaying ? "暂停" : "播放", _debugButtonStyle))
#else
        if (GUILayout.Button(_isPlaying ? "暂停" : "播放"))
#endif
        {
            if (_isPlaying) PauseSequence();
            else ResumeSequence();
        }

#if UNITY_EDITOR
        if (GUILayout.Button("重置到默认序列", _debugButtonStyle))
#else
        if (GUILayout.Button("重置到默认序列"))
#endif
        {
            PlayDefaultSequence();
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    void OnDrawGizmosSelected()
    {
        if (!_showGizmoInfo || !Application.isPlaying) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.3f);

#if UNITY_EDITOR
        if (!_stylesInitialized)
        {
            InitializeGUIStyles();
        }
        
        string labelText = $"{GetCurrentSequenceName()}\n帧: {_currentFrame + 1}/{(_currentSequence?.frames?.Length ?? 0)}";
        if (_currentSequence?.reverseLoop == true)
        {
            labelText += $"\n方向: {(_isPlayingReverse ? "逆向" : "正向")}";
        }
        labelText += $"\n等级: {_currentLevel}";
        labelText += $"\n阶段: {_currentStageName}";
        
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2.5f, labelText, _handleLabelStyle);
#endif
    }
}

// ========== 桥接脚本示例 ==========

/// <summary>
/// PlayerController桥接脚本 - 连接IndependentPngSequencePlayer和PlayerController
/// </summary>
public class PlayerSequenceBridge : MonoBehaviour
{
    [Header("=== 组件引用 ===")]
    public IndependentPngSequencePlayer sequencePlayer;
    public PlayerController playerController;

    [Header("=== 同步设置 ===")]
    public bool autoSyncLevel = true;
    public bool autoSyncStage = true;
    public string stagePrefix = "";

    void Start()
    {
        // 自动获取组件
        if (sequencePlayer == null)
            sequencePlayer = GetComponent<IndependentPngSequencePlayer>();
        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        // 初始同步
        if (sequencePlayer != null && playerController != null)
        {
            if (autoSyncLevel)
                sequencePlayer.CurrentLevel = playerController.Level;
        }
    }

    void Update()
    {
        // 持续同步状态
        if (sequencePlayer != null && playerController != null)
        {
            if (autoSyncLevel)
            {
                sequencePlayer.CurrentLevel = playerController.Level;
            }

            if (autoSyncStage)
            {
                // 这里可以根据玩家状态设置阶段
                // 例如：根据等级范围设置不同阶段
                string stageName = GetStageNameForLevel(playerController.Level);
                if (!string.IsNullOrEmpty(stagePrefix))
                    stageName = stagePrefix + stageName;
                sequencePlayer.CurrentStage = stageName;
            }
        }
    }

    string GetStageNameForLevel(int level)
    {
        if (level >= 1 && level <= 10) return "初级阶段";
        else if (level >= 11 && level <= 20) return "中级阶段";
        else if (level >= 21 && level <= 30) return "高级阶段";
        else return "大师阶段";
    }
}

// 序列选择窗口类
#if UNITY_EDITOR
public class SequenceSelectionWindow : UnityEditor.EditorWindow
{
    public static int LastSelectedIndex = -1;
    
    private string[] _options;
    private string _message;
    private System.Action<int> _onSelected;
    private Vector2 _scrollPosition;

    public void Initialize(string[] options, string message, System.Action<int> onSelected)
    {
        _options = options;
        _message = message;
        _onSelected = onSelected;
        LastSelectedIndex = -1;
        
        var size = new Vector2(400, Mathf.Min(500, 100 + options.Length * 25));
        this.minSize = size;
        this.maxSize = size;
    }

    void OnGUI()
    {
        GUILayout.Label(_message, UnityEditor.EditorStyles.wordWrappedLabel);
        GUILayout.Space(10);

        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

        for (int i = 0; i < _options.Length - 1; i++)
        {
            if (GUILayout.Button(_options[i], GUILayout.Height(25)))
            {
                _onSelected?.Invoke(i);
                return;
            }
        }

        GUILayout.EndScrollView();

        GUILayout.Space(10);
        if (GUILayout.Button("取消"))
        {
            _onSelected?.Invoke(-1);
        }
    }
}

/// <summary>
/// 高级批量分配窗口
/// </summary>
public class AdvancedBatchAssignWindow : UnityEditor.EditorWindow
{
    private IndependentPngSequencePlayer _target;
    private System.Collections.Generic.List<UnityEngine.Texture2D> _selectedTextures;
    private Vector2 _scrollPosition;
    
    private System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<UnityEngine.Texture2D>> _groupedTextures;
    private string[] _groupNames;
    private int[] _assignToSequence;

    public void Initialize(IndependentPngSequencePlayer target, System.Collections.Generic.List<UnityEngine.Texture2D> selectedTextures)
    {
        _target = target;
        _selectedTextures = selectedTextures;
        
        _groupedTextures = _target.GroupTexturesByName(selectedTextures);
        _groupNames = new string[_groupedTextures.Count];
        _assignToSequence = new int[_groupedTextures.Count];
        
        int index = 0;
        foreach (var kvp in _groupedTextures)
        {
            _groupNames[index] = kvp.Key;
            _assignToSequence[index] = _target.FindSequenceByName(kvp.Key);
            if (_assignToSequence[index] < 0) _assignToSequence[index] = 0;
            index++;
        }
        
        this.minSize = new Vector2(500, 400);
        this.maxSize = new Vector2(800, 600);
    }

    void OnGUI()
    {
        if (_target == null || _groupedTextures == null) return;

        GUILayout.Label("高级批量分配 - 智能分组", UnityEditor.EditorStyles.boldLabel);
        GUILayout.Label($"已选择 {_selectedTextures.Count} 张贴图，自动分为 {_groupedTextures.Count} 组", UnityEditor.EditorStyles.helpBox);
        
        GUILayout.Space(10);

        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

        for (int i = 0; i < _groupNames.Length; i++)
        {
            string groupName = _groupNames[i];
            var textures = _groupedTextures[groupName];
            
            GUILayout.BeginVertical("box");
            
            GUILayout.Label($"分组: {groupName} ({textures.Count} 张贴图)", UnityEditor.EditorStyles.boldLabel);
            
            GUILayout.BeginHorizontal();
            for (int j = 0; j < Mathf.Min(5, textures.Count); j++)
            {
                if (textures[j] != null)
                {
                    GUILayout.Label(textures[j].name, GUILayout.Width(100));
                }
            }
            if (textures.Count > 5)
            {
                GUILayout.Label($"... 还有 {textures.Count - 5} 张", UnityEditor.EditorStyles.miniLabel);
            }
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("分配到序列:", GUILayout.Width(80));
            
            string[] sequenceOptions = new string[_target.animationSequences.Length];
            for (int j = 0; j < _target.animationSequences.Length; j++)
            {
                string seqName = _target.animationSequences[j]?.sequenceName ?? "未命名";
                int frameCount = _target.animationSequences[j]?.frames?.Length ?? 0;
                sequenceOptions[j] = $"序列 {j}: {seqName} ({frameCount} 帧)";
            }
            
            _assignToSequence[i] = UnityEditor.EditorGUILayout.Popup(_assignToSequence[i], sequenceOptions);
            GUILayout.EndHorizontal();
            
            GUILayout.EndVertical();
            GUILayout.Space(5);
        }

        GUILayout.EndScrollView();

        GUILayout.Space(10);
        
        GUILayout.BeginHorizontal();
        
        if (GUILayout.Button("执行批量分配", GUILayout.Height(30)))
        {
            ExecuteBatchAssign();
            this.Close();
        }
        
        if (GUILayout.Button("取消", GUILayout.Height(30)))
        {
            this.Close();
        }
        
        GUILayout.EndHorizontal();
    }

    void ExecuteBatchAssign()
    {
        string result = "批量分配结果：\n";
        int successCount = 0;
        
        for (int i = 0; i < _groupNames.Length; i++)
        {
            string groupName = _groupNames[i];
            var textures = _groupedTextures[groupName];
            int sequenceIndex = _assignToSequence[i];
            
            if (sequenceIndex >= 0 && sequenceIndex < _target.animationSequences.Length)
            {
                _target.AssignTexturesToSequence(sequenceIndex, textures);
                string seqName = _target.animationSequences[sequenceIndex]?.sequenceName ?? $"序列 {sequenceIndex}";
                result += $"✓ {groupName}: {textures.Count} 张贴图 → {seqName}\n";
                successCount++;
            }
            else
            {
                result += $"✗ {groupName}: 序列索引无效\n";
            }
        }
        
        result += $"\n成功分配 {successCount}/{_groupNames.Length} 组动画";
        UnityEditor.EditorUtility.DisplayDialog("批量分配完成", result, "确定");
        
        if (successCount > 0)
        {
            UnityEditor.EditorUtility.SetDirty(_target);
        }
    }
}
#endif