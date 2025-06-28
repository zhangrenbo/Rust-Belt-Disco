using UnityEngine;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// 故事控制器 - 管理剧情变量和进度跟踪
/// </summary>
public class StoryCtrl : Singleton<StoryCtrl>
{
    private Dictionary<string, int> storyData = new Dictionary<string, int>();
    private string savePath;

    protected override void Awake()
    {
        base.Awake();
        savePath = Path.Combine(Application.persistentDataPath, "story_data.json");
        Load();
    }

    /// <summary>
    /// 增加事件计数，如 "open_chest"、"killed_enemy"
    /// </summary>
    public void AddEvent(string key)
    {
        if (!storyData.ContainsKey(key))
            storyData[key] = 0;
        storyData[key]++;
        Save();
    }

    /// <summary>
    /// 设置变量值（覆盖）
    /// </summary>
    public void Set(string key, int value)
    {
        storyData[key] = value;
        Save();
    }

    /// <summary>
    /// 获取变量值，不存在则为 0
    /// </summary>
    public int Get(string key)
    {
        return storyData.TryGetValue(key, out int val) ? val : 0;
    }

    /// <summary>
    /// 判断变量是否等于指定值
    /// </summary>
    public bool IsEqual(string key, int target)
    {
        return Get(key) == target;
    }

    /// <summary>
    /// 从 Ink 中的计数器所有 int 类型变量
    /// </summary>
    public void ImportFromInk(object story)
    {
        // 这里可以根据具体的Ink集成类来实现
        // 暂时保留接口
        Debug.Log("[StoryCtrl] ImportFromInk 待用（需要根据具体Ink集成类实现）");
    }

    private void Save()
    {
        try
        {
            var json = JsonUtility.ToJson(new SerializableDictionary(storyData), true);
            File.WriteAllText(savePath, json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[StoryCtrl] 保存失败: {e.Message}");
        }
    }

    private void Load()
    {
        try
        {
            if (File.Exists(savePath))
            {
                var json = File.ReadAllText(savePath);
                var data = JsonUtility.FromJson<SerializableDictionary>(json);
                storyData = data.ToDict();
            }
            else
            {
                storyData = new Dictionary<string, int>();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[StoryCtrl] 加载失败: {e.Message}");
            storyData = new Dictionary<string, int>();
        }
    }

    /// <summary>
    /// 开发调试用，显示所有变量
    /// </summary>
    public void PrintAll()
    {
        Debug.Log("==== StoryCtrl Variables ====");
        foreach (var kv in storyData)
        {
            Debug.Log($"{kv.Key} = {kv.Value}");
        }
    }

    // 用于JSON序列化的辅助类
    [System.Serializable]
    private class SerializableDictionary
    {
        public string[] keys;
        public int[] values;

        public SerializableDictionary(Dictionary<string, int> dict)
        {
            keys = new string[dict.Count];
            values = new int[dict.Count];
            int i = 0;
            foreach (var kvp in dict)
            {
                keys[i] = kvp.Key;
                values[i] = kvp.Value;
                i++;
            }
        }

        public Dictionary<string, int> ToDict()
        {
            var dict = new Dictionary<string, int>();
            for (int i = 0; i < keys.Length && i < values.Length; i++)
            {
                dict[keys[i]] = values[i];
            }
            return dict;
        }
    }
}

/// <summary>
/// 物品类型枚举
/// </summary>
public enum ItemType
{
    Material,   // 材料
    Weapon,     // 武器
    Armor,      // 护甲
    Consumable, // 消耗品
    Quest,      // 任务物品
    Key,        // 钥匙
    Tool        // 工具
}

/// <summary>
/// 基础物品类 - 用于与其他系统兼容
/// </summary>
[System.Serializable]
public class Item
{
    public string Name;
    public string Description;
    public ItemType Type;
    public Vector2Int Size;
    public Sprite Icon;
}

/// <summary>
/// 游戏物品类 - 继承自基础Item类
/// </summary>
[System.Serializable]
public class GameItem : Item
{
    [Header("=== 游戏物品扩展属性 ===")]
    public int stackSize = 1;           // 堆叠数量
    public int currentStack = 1;        // 当前堆叠
    public bool isStackable = false;    // 是否可堆叠
    public int value = 0;               // 物品价值
    public float weight = 0f;           // 物品重量
    public bool isQuestItem = false;    // 是否任务物品
    public string[] tags;               // 物品标签

    /// <summary>
    /// 默认构造函数
    /// </summary>
    public GameItem()
    {
        Name = "未命名物品";
        Description = "一个神秘的物品";
        Type = ItemType.Material;
        Size = new Vector2Int(1, 1);
        stackSize = 1;
        currentStack = 1;
        isStackable = false;
    }

    /// <summary>
    /// 从基础Item构造GameItem
    /// </summary>
    public GameItem(Item baseItem)
    {
        Name = baseItem.Name;
        Description = baseItem.Description;
        Type = baseItem.Type;
        Size = baseItem.Size;
        Icon = baseItem.Icon;
        stackSize = 1;
        currentStack = 1;
        isStackable = false;
    }

    /// <summary>
    /// 是否可以与另一个物品堆叠
    /// </summary>
    public bool CanStackWith(GameItem other)
    {
        if (!isStackable || !other.isStackable) return false;
        return Name == other.Name && Type == other.Type;
    }

    /// <summary>
    /// 尝试堆叠物品
    /// </summary>
    public bool TryStack(GameItem other, out int remainder)
    {
        remainder = 0;
        if (!CanStackWith(other)) return false;

        int totalStack = currentStack + other.currentStack;
        if (totalStack <= stackSize)
        {
            currentStack = totalStack;
            return true;
        }
        else
        {
            currentStack = stackSize;
            remainder = totalStack - stackSize;
            return true;
        }
    }
}

/// <summary>
/// 背包管理器 - 简化版本，保留名称兼容性
/// </summary>
public class InventoryManager : Singleton<InventoryManager>
{
    [Header("=== 背包设置 ===")]
    public int maxSlots = 50;

    // 现在正确使用 GameItem 而不是 Item，保留类型名兼容性
    public List<GameItem> items = new List<GameItem>();
    public bool showDebugInfo = true;

    // 添加静态 instance 属性来兼容旧代码
    public static InventoryManager instance => Instance;

    protected override void Awake()
    {
        base.Awake();

        if (items == null)
        {
            items = new List<GameItem>();
        }

        if (showDebugInfo)
        {
            Debug.Log($"[InventoryManager] 背包系统已初始化，容量: {maxSlots}");
        }
    }

    /// <summary>
    /// 添加物品到背包 - 主要方法
    /// </summary>
    public bool AddItem(GameItem item)
    {
        if (item == null)
        {
            Debug.LogWarning("[InventoryManager] 尝试添加空物品");
            return false;
        }

        // 检查是否可以堆叠
        if (item.isStackable)
        {
            foreach (var existingItem in items)
            {
                if (existingItem.CanStackWith(item))
                {
                    if (existingItem.TryStack(item, out int remainder))
                    {
                        if (remainder == 0)
                        {
                            if (showDebugInfo)
                                Debug.Log($"[InventoryManager] 堆叠物品: {item.Name}");
                            return true;
                        }
                        else
                        {
                            // 创建新的物品来存储剩余部分
                            var remainderItem = new GameItem(item) { currentStack = remainder };
                            return AddItem(remainderItem);
                        }
                    }
                }
            }
        }

        // 无法堆叠或没有找到可堆叠物品，检查空间
        if (items.Count >= maxSlots)
        {
            Debug.Log("[InventoryManager] 背包已满");
            return false;
        }

        items.Add(item);

        if (showDebugInfo)
        {
            Debug.Log($"[InventoryManager] 添加物品: {item.Name} ({items.Count}/{maxSlots})");
        }

        return true;
    }

    /// <summary>
    /// 从背包移除物品 - 主要方法
    /// </summary>
    public bool RemoveItem(GameItem item)
    {
        bool removed = items.Remove(item);
        if (removed && showDebugInfo)
        {
            Debug.Log($"[InventoryManager] 移除物品: {item.Name}");
        }
        return removed;
    }

    /// <summary>
    /// 重载方法：支持 Item 类型（兼容性）
    /// 现在正确创建新的 GameItem 而非强制转换
    /// </summary>
    public bool AddItem(Item item)
    {
        if (item == null) return false;

        // 创建新的 GameItem 而非强制转换
        GameItem gameItem = new GameItem(item);
        return AddItem(gameItem);
    }

    /// <summary>
    /// 重载方法：支持 Item 类型（兼容性）
    /// 现在正确通过名称匹配移除，保留类型问题
    /// </summary>
    public bool RemoveItem(Item item)
    {
        if (item == null) return false;

        // 通过名称查找并移除
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null && items[i].Name == item.Name)
            {
                items.RemoveAt(i);
                if (showDebugInfo)
                {
                    Debug.Log($"[InventoryManager] 移除物品: {item.Name}");
                }
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 检查是否有指定名称的物品
    /// </summary>
    public bool HasItem(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return false;

        foreach (var item in items)
        {
            if (item != null && item.Name == itemName)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 查找指定名称的物品
    /// </summary>
    public GameItem FindItem(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return null;

        foreach (var item in items)
        {
            if (item != null && item.Name == itemName)
                return item;
        }
        return null;
    }

    /// <summary>
    /// 查找指定名称的物品 - 返回 Item 类型（兼容性）
    /// 现在正确创建新的 Item 对象而非强制转换
    /// </summary>
    public Item FindItemAsItem(string itemName)
    {
        var gameItem = FindItem(itemName);
        if (gameItem == null) return null;

        // 创建 Item 对象
        return new Item
        {
            Name = gameItem.Name,
            Description = gameItem.Description,
            Type = gameItem.Type,
            Size = gameItem.Size,
            Icon = gameItem.Icon
        };
    }

    /// <summary>
    /// 获取所有物品列表
    /// </summary>
    public List<GameItem> GetAllItems()
    {
        return new List<GameItem>(items);
    }

    /// <summary>
    /// 获取所有物品列表 - 返回 Item 类型（兼容性）
    /// 现在正确创建新的 Item 列表而非强制转换
    /// </summary>
    public List<Item> GetAllItemsAsItems()
    {
        List<Item> result = new List<Item>();
        foreach (var gameItem in items)
        {
            if (gameItem != null)
            {
                result.Add(new Item
                {
                    Name = gameItem.Name,
                    Description = gameItem.Description,
                    Type = gameItem.Type,
                    Size = gameItem.Size,
                    Icon = gameItem.Icon
                });
            }
        }
        return result;
    }

    /// <summary>
    /// 按类型获取物品
    /// </summary>
    public List<GameItem> GetItemsByType(ItemType type)
    {
        List<GameItem> result = new List<GameItem>();
        foreach (var item in items)
        {
            if (item != null && item.Type == type)
                result.Add(item);
        }
        return result;
    }

    /// <summary>
    /// 按类型获取物品 - 返回 Item 类型（兼容性）
    /// 现在正确创建新的 Item 列表而非强制转换
    /// </summary>
    public List<Item> GetItemsByTypeAsItems(ItemType type)
    {
        List<Item> result = new List<Item>();
        foreach (var gameItem in items)
        {
            if (gameItem != null && gameItem.Type == type)
            {
                result.Add(new Item
                {
                    Name = gameItem.Name,
                    Description = gameItem.Description,
                    Type = gameItem.Type,
                    Size = gameItem.Size,
                    Icon = gameItem.Icon
                });
            }
        }
        return result;
    }

    /// <summary>
    /// 清空背包
    /// </summary>
    public void ClearInventory()
    {
        items.Clear();
        Debug.Log("[InventoryManager] 背包已清空");
    }

    /// <summary>
    /// 获取背包使用百分比
    /// </summary>
    public float GetUsagePercentage()
    {
        return maxSlots > 0 ? (float)items.Count / maxSlots : 0f;
    }

    /// <summary>
    /// 获取剩余空间
    /// </summary>
    public int GetRemainingSlots()
    {
        return Mathf.Max(0, maxSlots - items.Count);
    }

    /// <summary>
    /// 检查是否有空间
    /// </summary>
    public bool HasSpace()
    {
        return items.Count < maxSlots;
    }

    /// <summary>
    /// 根据物品名称移除指定数量
    /// </summary>
    public bool RemoveItemByName(string itemName, int quantity = 1)
    {
        if (string.IsNullOrEmpty(itemName) || quantity <= 0) return false;

        int removedCount = 0;
        for (int i = items.Count - 1; i >= 0 && removedCount < quantity; i--)
        {
            if (items[i] != null && items[i].Name == itemName)
            {
                if (items[i].isStackable && items[i].currentStack > 1)
                {
                    // 堆叠物品的处理
                    int toRemove = Mathf.Min(quantity - removedCount, items[i].currentStack);
                    items[i].currentStack -= toRemove;
                    removedCount += toRemove;

                    if (items[i].currentStack <= 0)
                    {
                        items.RemoveAt(i);
                    }
                }
                else
                {
                    // 非堆叠物品或单个物品
                    items.RemoveAt(i);
                    removedCount++;
                }
            }
        }

        if (showDebugInfo && removedCount > 0)
        {
            Debug.Log($"[InventoryManager] 移除物品: {itemName} x{removedCount}");
        }

        return removedCount > 0;
    }

    /// <summary>
    /// 获取指定物品的总数量（考虑堆叠）
    /// </summary>
    public int GetItemCount(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return 0;

        int count = 0;
        foreach (var item in items)
        {
            if (item != null && item.Name == itemName)
            {
                count += item.currentStack;
            }
        }
        return count;
    }

    // 兼容旧代码的方法
    public bool Remove(Item item)
    {
        return RemoveItem(item);
    }

    public bool Remove(GameItem item)
    {
        return RemoveItem(item);
    }
}

/// <summary>
/// 物品拾取器 - 处理场景中的可拾取物品
/// </summary>
public class ItemPickup : MonoBehaviour
{
    [Header("=== 物品设置 ===")]
    [Tooltip("要拾取的物品")]
    public GameItem item;

    [Tooltip("是否自动拾取（进入范围自动触发）")]
    public bool autoPickup = false;

    [Tooltip("拾取范围")]
    public float pickupRange = 2f;

    [Header("=== 音效设置 ===")]
    [Tooltip("拾取时播放的音效")]
    public AudioClip pickupSound;

    [Header("=== 视觉效果 ===")]
    [Tooltip("拾取时生成的特效")]
    public GameObject pickupEffect;

    [Tooltip("拾取后是否销毁物体")]
    public bool destroyAfterPickup = true;

    [Header("=== 交互设置 ===")]
    [Tooltip("需要按键交互的按键")]
    public KeyCode interactKey = KeyCode.E;

    [Tooltip("交互提示文本")]
    public string promptText = "按 E 拾取";

    // 私有状态
    private bool hasBeenPickedUp = false;
    private Transform player;
    private bool playerInRange = false;

    // UI提示相关
    private GameObject interactionPrompt;

    void Start()
    {
        InitializePickup();
        CreateInteractionPrompt();
    }

    void Update()
    {
        if (hasBeenPickedUp || player == null) return;

        CheckPlayerDistance();

        if (autoPickup)
        {
            if (playerInRange)
            {
                TryPickup();
            }
        }
        else
        {
            // 手动拾取模式
            if (playerInRange && Input.GetKeyDown(interactKey))
            {
                TryPickup();
            }
        }
    }

    void InitializePickup()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        // 如果没有设置物品，创建默认物品
        if (item == null)
        {
            item = new GameItem
            {
                Name = gameObject.name,
                Description = "可拾取的物品",
                Type = ItemType.Material,
                Size = new Vector2Int(1, 1)
            };
        }
    }

    void CreateInteractionPrompt()
    {
        if (autoPickup) return; // 自动拾取不需要提示

        // 创建简单的UI提示
        GameObject canvasObj = new GameObject("PickupPrompt");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = Vector3.up * 1.5f;

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(3, 0.8f);

        // 创建文本
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(canvasObj.transform);

        var text = textObj.AddComponent<UnityEngine.UI.Text>();
        text.text = promptText;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 14;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;

        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        // 添加背景
        var image = textObj.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(0, 0, 0, 0.7f);

        interactionPrompt = canvasObj;
        interactionPrompt.SetActive(false);
    }

    void CheckPlayerDistance()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        bool inRange = distance <= pickupRange;

        if (inRange != playerInRange)
        {
            playerInRange = inRange;

            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(inRange && !hasBeenPickedUp);
            }
        }
    }

    void TryPickup()
    {
        if (hasBeenPickedUp) return;

        if (InventoryManager.Instance != null)
        {
            bool success = InventoryManager.Instance.AddItem(item);
            if (success)
            {
                OnPickupSuccess();
            }
            else
            {
                OnPickupFailed();
            }
        }
        else
        {
            Debug.LogError("[ItemPickup] 找不到 InventoryManager");
        }
    }

    void OnPickupSuccess()
    {
        hasBeenPickedUp = true;

        // 播放音效
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }

        // 生成特效
        if (pickupEffect != null)
        {
            Instantiate(pickupEffect, transform.position, Quaternion.identity);
        }

        // 隐藏提示
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }

        Debug.Log($"[ItemPickup] 成功拾取: {item.Name}");

        // 销毁物体
        if (destroyAfterPickup)
        {
            Destroy(gameObject, 0.1f); // 稍微延迟以确保特效播放
        }
    }

    void OnPickupFailed()
    {
        Debug.Log($"[ItemPickup] 拾取失败: {item.Name} - 背包可能已满");
        // 可以在这里添加失败提示音效或UI提示
    }

    /// <summary>
    /// 手动触发拾取（供其他脚本调用）
    /// </summary>
    public void ManualPickup()
    {
        TryPickup();
    }

    /// <summary>
    /// 设置物品信息
    /// </summary>
    public void SetItem(GameItem newItem)
    {
        item = newItem;
    }

    /// <summary>
    /// 获取物品信息
    /// </summary>
    public GameItem GetItem()
    {
        return item;
    }

    void OnDrawGizmosSelected()
    {
        // 绘制拾取范围
        Gizmos.color = playerInRange ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);

        // 绘制到玩家的连线
        if (player != null && playerInRange)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }

    void OnDrawGizmos()
    {
        // 始终显示拾取范围的半透明球体
        Gizmos.color = new Color(1, 1, 0, 0.1f);
        Gizmos.DrawSphere(transform.position, pickupRange);

        // 显示物品类型颜色标识
        if (item != null)
        {
            Color typeColor = item.Type switch
            {
                ItemType.Weapon => Color.red,
                ItemType.Armor => Color.blue,
                ItemType.Consumable => Color.green,
                ItemType.Quest => Color.magenta,
                ItemType.Material => Color.gray,
                _ => Color.white
            };

            Gizmos.color = typeColor;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * 0.3f);
        }
    }
}