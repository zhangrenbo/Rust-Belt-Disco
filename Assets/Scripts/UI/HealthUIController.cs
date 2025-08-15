using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ����ֵUI������ - �򻯰汾
/// </summary>
public class HealthUIController : MonoBehaviour
{
    [Header("=== UI��� ===")]
    public Slider healthSlider;
    public Text healthText;
    public Image fillImage;

    [Header("=== ��ɫ���� ===")]
    public Color fullHealthColor = Color.green;
    public Color lowHealthColor = Color.red;
    public float lowHealthThreshold = 0.3f;

    [Header("=== ��ʾ���� ===")]
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
        // �Զ�����UI���
        if (healthSlider == null)
            healthSlider = GetComponentInChildren<Slider>();

        if (healthText == null)
            healthText = GetComponentInChildren<Text>();

        if (fillImage == null && healthSlider != null)
            fillImage = healthSlider.fillRect.GetComponent<Image>();

        // ��ʼ��������
        if (healthSlider != null)
        {
            healthSlider.minValue = 0;
            healthSlider.maxValue = 1;
            healthSlider.value = 1;
        }
    }

    /// <summary>
    /// �����������ֵ
    /// </summary>
    public void SetMaxHealth(int maxHp)
    {
        maxHealth = maxHp;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        UpdateDisplay();
    }

    /// <summary>
    /// ���õ�ǰ����ֵ
    /// </summary>
    public void SetCurrentHealth(int currentHp)
    {
        currentHealth = Mathf.Clamp(currentHp, 0, maxHealth);
        UpdateDisplay();

        // �����Զ����ؼ�ʱ��
        if (autoHide)
        {
            hideTimer = 0f;
            gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// ������ʾ
    /// </summary>
    void UpdateDisplay()
    {
        float healthPercentage = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;

        // ���»�����
        if (healthSlider != null)
        {
            healthSlider.value = healthPercentage;
        }

        // �����ı�
        if (healthText != null && showHealthText)
        {
            healthText.text = $"{currentHealth}/{maxHealth}";
        }

        // ������ɫ
        if (fillImage != null)
        {
            fillImage.color = Color.Lerp(lowHealthColor, fullHealthColor, healthPercentage);
        }
    }

    /// <summary>
    /// ��ʾ������
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
        hideTimer = 0f;
    }

    /// <summary>
    /// ����������
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// �����Ƿ��Զ�����
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
    /// ��ȡ��ǰ����ֵ�ٷֱ�
    /// </summary>
    public float GetHealthPercentage()
    {
        return maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
    }
}