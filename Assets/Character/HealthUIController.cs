using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 生命值UI控制器 - 简化版本
/// </summary>
public class HealthUIController : MonoBehaviour
{
    [Header("=== UI组件 ===")]
    public Slider healthSlider;
    public Text healthText;
    public Image fillImage;

    [Header("=== 颜色设置 ===")]
    public Color fullHealthColor = Color.green;
    public Color lowHealthColor = Color.red;
    public float lowHealthThreshold = 0.3f;

    [Header("=== 显示设置 ===")]
    public bool showHealthText = true;
    public bool autoHide = true;
    public float autoHideDelay = 3f;

    private int maxHealth = 100;
    private int currentHealth = 100;
    private float hideTimer = 0f;

    void Awake()
    {
        InitializeUI();
    }

    void Update()
    {
        if (autoHide && gameObject.activeSelf)
        {
            hideTimer += Time.deltaTime;
            if (hideTimer >= autoHideDelay)
            {
                gameObject.SetActive(false);
            }
        }
    }

    void InitializeUI()
    {
        // 自动查找UI组件
        if (healthSlider == null)
            healthSlider = GetComponentInChildren<Slider>();

        if (healthText == null)
            healthText = GetComponentInChildren<Text>();

        if (fillImage == null && healthSlider != null)
            fillImage = healthSlider.fillRect.GetComponent<Image>();

        // 初始化滑动条
        if (healthSlider != null)
        {
            healthSlider.minValue = 0;
            healthSlider.maxValue = 1;
            healthSlider.value = 1;
        }
    }

    /// <summary>
    /// 设置最大生命值
    /// </summary>
    public void SetMaxHealth(int maxHp)
    {
        maxHealth = maxHp;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        UpdateDisplay();
    }

    /// <summary>
    /// 设置当前生命值
    /// </summary>
    public void SetCurrentHealth(int currentHp)
    {
        currentHealth = Mathf.Clamp(currentHp, 0, maxHealth);
        UpdateDisplay();

        // 重置自动隐藏计时器
        if (autoHide)
        {
            hideTimer = 0f;
            gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 更新显示
    /// </summary>
    void UpdateDisplay()
    {
        float healthPercentage = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;

        // 更新滑动条
        if (healthSlider != null)
        {
            healthSlider.value = healthPercentage;
        }

        // 更新文本
        if (healthText != null && showHealthText)
        {
            healthText.text = $"{currentHealth}/{maxHealth}";
        }

        // 更新颜色
        if (fillImage != null)
        {
            fillImage.color = Color.Lerp(lowHealthColor, fullHealthColor, healthPercentage);
        }
    }

    /// <summary>
    /// 显示生命条
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
        hideTimer = 0f;
    }

    /// <summary>
    /// 隐藏生命条
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 设置是否自动隐藏
    /// </summary>
    public void SetAutoHide(bool enable)
    {
        autoHide = enable;
        if (!autoHide)
        {
            hideTimer = 0f;
        }
    }

    /// <summary>
    /// 获取当前生命值百分比
    /// </summary>
    public float GetHealthPercentage()
    {
        return maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
    }
}