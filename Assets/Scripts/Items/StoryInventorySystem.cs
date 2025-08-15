using UnityEngine;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// ���¿����� - �����������ͽ��ȸ���
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
    /// �����¼��������� "open_chest"��"killed_enemy"
    /// </summary>
    public void AddEvent(string key)
    {
        if (!storyData.ContainsKey(key))
            storyData[key] = 0;
        storyData[key]++;
        Save();
    }

    /// <summary>
    /// ���ñ���ֵ�����ǣ�
    /// </summary>
    public void Set(string key, int value)
    {
        storyData[key] = value;
        Save();
    }

    /// <summary>
    /// ��ȡ����ֵ����������Ϊ 0
    /// </summary>
    public int Get(string key)
    {
        return storyData.TryGetValue(key, out int val) ? val : 0;
    }

    /// <summary>
    /// �жϱ����Ƿ����ָ��ֵ
    /// </summary>
    public bool IsEqual(string key, int target)
    {
        return Get(key) == target;
    }

    /// <summary>
    /// �� Ink �еļ��������� int ���ͱ���
    /// </summary>
    public void ImportFromInk(object story)
    {
        // ������Ը��ݾ����Ink��������ʵ��
        // ��ʱ����ӿ�
        Debug.Log("[StoryCtrl] ImportFromInk ���ã���Ҫ���ݾ���Ink������ʵ�֣�");
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
            Debug.LogError($"[StoryCtrl] ����ʧ��: {e.Message}");
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
            Debug.LogError($"[StoryCtrl] ����ʧ��: {e.Message}");
            storyData = new Dictionary<string, int>();
        }
    }

    /// <summary>
    /// ���������ã���ʾ���б���
    /// </summary>
    public void PrintAll()
    {
        Debug.Log("==== StoryCtrl Variables ====");
        foreach (var kv in storyData)
        {
            Debug.Log($"{kv.Key} = {kv.Value}");
        }
    }

    // ����JSON���л��ĸ�����
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
/// ��Ʒ����ö��
/// </summary>
public enum ItemType
{
    Material,   // ����
    Weapon,     // ����
    Armor,      // ����
    Consumable, // ����Ʒ
    Quest,      // ������Ʒ
    Key,        // Կ��
    Tool        // ����
}

/// <summary>
/// ������Ʒ�� - ����������ϵͳ����
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
/// ��Ϸ��Ʒ�� - �̳��Ի���Item��
/// </summary>
[System.Serializable]
public class GameItem : Item
{
    [Header("=== ��Ϸ��Ʒ��չ���� ===")]
    public int stackSize = 1;           // �ѵ�����
    public int currentStack = 1;        // ��ǰ�ѵ�
    public bool isStackable = false;    // �Ƿ�ɶѵ�
    public int value = 0;               // ��Ʒ��ֵ
    public float weight = 0f;           // ��Ʒ����
    public bool isQuestItem = false;    // �Ƿ�������Ʒ
    public string[] tags;               // ��Ʒ��ǩ

    /// <summary>
    /// Ĭ�Ϲ��캯��
    /// </summary>
    public GameItem()
    {
        Name = "δ������Ʒ";
        Description = "һ�����ص���Ʒ";
        Type = ItemType.Material;
        Size = new Vector2Int(1, 1);
        stackSize = 1;
        currentStack = 1;
        isStackable = false;
    }

    /// <summary>
    /// �ӻ���Item����GameItem
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
    /// �Ƿ��������һ����Ʒ�ѵ�
    /// </summary>
    public bool CanStackWith(GameItem other)
    {
        if (!isStackable || !other.isStackable) return false;
        return Name == other.Name && Type == other.Type;
    }

    /// <summary>
    /// ���Զѵ���Ʒ
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
/// ���������� - �򻯰汾���������Ƽ�����
/// </summary>
public class InventoryManager : Singleton<InventoryManager>
{
    [Header("=== �������� ===")]
    public int maxSlots = 50;

    // ������ȷʹ�� GameItem ������ Item������������������
    public List<GameItem> items = new List<GameItem>();
    public bool showDebugInfo = true;

    // ��Ӿ�̬ instance ���������ݾɴ���
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
            Debug.Log($"[InventoryManager] ����ϵͳ�ѳ�ʼ��������: {maxSlots}");
        }
    }

    /// <summary>
    /// �����Ʒ������ - ��Ҫ����
    /// </summary>
    public bool AddItem(GameItem item)
    {
        if (item == null)
        {
            Debug.LogWarning("[InventoryManager] ������ӿ���Ʒ");
            return false;
        }

        // ����Ƿ���Զѵ�
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
                                Debug.Log($"[InventoryManager] �ѵ���Ʒ: {item.Name}");
                            return true;
                        }
                        else
                        {
                            // �����µ���Ʒ���洢ʣ�ಿ��
                            var remainderItem = new GameItem(item) { currentStack = remainder };
                            return AddItem(remainderItem);
                        }
                    }
                }
            }
        }

        // �޷��ѵ���û���ҵ��ɶѵ���Ʒ�����ռ�
        if (items.Count >= maxSlots)
        {
            Debug.Log("[InventoryManager] ��������");
            return false;
        }

        items.Add(item);

        if (showDebugInfo)
        {
            Debug.Log($"[InventoryManager] �����Ʒ: {item.Name} ({items.Count}/{maxSlots})");
        }

        return true;
    }

    /// <summary>
    /// �ӱ����Ƴ���Ʒ - ��Ҫ����
    /// </summary>
    public bool RemoveItem(GameItem item)
    {
        bool removed = items.Remove(item);
        if (removed && showDebugInfo)
        {
            Debug.Log($"[InventoryManager] �Ƴ���Ʒ: {item.Name}");
        }
        return removed;
    }

    /// <summary>
    /// ���ط�����֧�� Item ���ͣ������ԣ�
    /// ������ȷ�����µ� GameItem ����ǿ��ת��
    /// </summary>
    public bool AddItem(Item item)
    {
        if (item == null) return false;

        // �����µ� GameItem ����ǿ��ת��
        GameItem gameItem = new GameItem(item);
        return AddItem(gameItem);
    }

    /// <summary>
    /// ���ط�����֧�� Item ���ͣ������ԣ�
    /// ������ȷͨ������ƥ���Ƴ���������������
    /// </summary>
    public bool RemoveItem(Item item)
    {
        if (item == null) return false;

        // ͨ�����Ʋ��Ҳ��Ƴ�
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null && items[i].Name == item.Name)
            {
                items.RemoveAt(i);
                if (showDebugInfo)
                {
                    Debug.Log($"[InventoryManager] �Ƴ���Ʒ: {item.Name}");
                }
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// ����Ƿ���ָ�����Ƶ���Ʒ
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
    /// ����ָ�����Ƶ���Ʒ
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
    /// ����ָ�����Ƶ���Ʒ - ���� Item ���ͣ������ԣ�
    /// ������ȷ�����µ� Item �������ǿ��ת��
    /// </summary>
    public Item FindItemAsItem(string itemName)
    {
        var gameItem = FindItem(itemName);
        if (gameItem == null) return null;

        // ���� Item ����
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
    /// ��ȡ������Ʒ�б�
    /// </summary>
    public List<GameItem> GetAllItems()
    {
        return new List<GameItem>(items);
    }

    /// <summary>
    /// ��ȡ������Ʒ�б� - ���� Item ���ͣ������ԣ�
    /// ������ȷ�����µ� Item �б����ǿ��ת��
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
    /// �����ͻ�ȡ��Ʒ
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
    /// �����ͻ�ȡ��Ʒ - ���� Item ���ͣ������ԣ�
    /// ������ȷ�����µ� Item �б����ǿ��ת��
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
    /// ��ձ���
    /// </summary>
    public void ClearInventory()
    {
        items.Clear();
        Debug.Log("[InventoryManager] ���������");
    }

    /// <summary>
    /// ��ȡ����ʹ�ðٷֱ�
    /// </summary>
    public float GetUsagePercentage()
    {
        return maxSlots > 0 ? (float)items.Count / maxSlots : 0f;
    }

    /// <summary>
    /// ��ȡʣ��ռ�
    /// </summary>
    public int GetRemainingSlots()
    {
        return Mathf.Max(0, maxSlots - items.Count);
    }

    /// <summary>
    /// ����Ƿ��пռ�
    /// </summary>
    public bool HasSpace()
    {
        return items.Count < maxSlots;
    }

    /// <summary>
    /// ������Ʒ�����Ƴ�ָ������
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
                    // �ѵ���Ʒ�Ĵ���
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
                    // �Ƕѵ���Ʒ�򵥸���Ʒ
                    items.RemoveAt(i);
                    removedCount++;
                }
            }
        }

        if (showDebugInfo && removedCount > 0)
        {
            Debug.Log($"[InventoryManager] �Ƴ���Ʒ: {itemName} x{removedCount}");
        }

        return removedCount > 0;
    }

    /// <summary>
    /// ��ȡָ����Ʒ�������������Ƕѵ���
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

    // ���ݾɴ���ķ���
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
/// ��Ʒʰȡ�� - ��������еĿ�ʰȡ��Ʒ
/// </summary>
public class ItemPickup : MonoBehaviour
{
    [Header("=== ��Ʒ���� ===")]
    [Tooltip("Ҫʰȡ����Ʒ")]
    public GameItem item;

    [Tooltip("�Ƿ��Զ�ʰȡ�����뷶Χ�Զ�������")]
    public bool autoPickup = false;

    [Tooltip("ʰȡ��Χ")]
    public float pickupRange = 2f;

    [Header("=== ��Ч���� ===")]
    [Tooltip("ʰȡʱ���ŵ���Ч")]
    public AudioClip pickupSound;

    [Header("=== �Ӿ�Ч�� ===")]
    [Tooltip("ʰȡʱ���ɵ���Ч")]
    public GameObject pickupEffect;

    [Tooltip("ʰȡ���Ƿ���������")]
    public bool destroyAfterPickup = true;

    [Header("=== �������� ===")]
    [Tooltip("��Ҫ���������İ���")]
    public KeyCode interactKey = KeyCode.E;

    [Tooltip("������ʾ�ı�")]
    public string promptText = "�� E ʰȡ";

    // ˽��״̬
    private bool hasBeenPickedUp = false;
    private Transform player;
    private bool playerInRange = false;

    // UI��ʾ���
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
            // �ֶ�ʰȡģʽ
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

        // ���û��������Ʒ������Ĭ����Ʒ
        if (item == null)
        {
            item = new GameItem
            {
                Name = gameObject.name,
                Description = "��ʰȡ����Ʒ",
                Type = ItemType.Material,
                Size = new Vector2Int(1, 1)
            };
        }
    }

    void CreateInteractionPrompt()
    {
        if (autoPickup) return; // �Զ�ʰȡ����Ҫ��ʾ

        // �����򵥵�UI��ʾ
        GameObject canvasObj = new GameObject("PickupPrompt");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = Vector3.up * 1.5f;

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(3, 0.8f);

        // �����ı�
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

        // ��ӱ���
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
            Debug.LogError("[ItemPickup] �Ҳ��� InventoryManager");
        }
    }

    void OnPickupSuccess()
    {
        hasBeenPickedUp = true;

        // ������Ч
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }

        // ������Ч
        if (pickupEffect != null)
        {
            Instantiate(pickupEffect, transform.position, Quaternion.identity);
        }

        // ������ʾ
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }

        Debug.Log($"[ItemPickup] �ɹ�ʰȡ: {item.Name}");

        // ��������
        if (destroyAfterPickup)
        {
            Destroy(gameObject, 0.1f); // ��΢�ӳ���ȷ����Ч����
        }
    }

    void OnPickupFailed()
    {
        Debug.Log($"[ItemPickup] ʰȡʧ��: {item.Name} - ������������");
        // �������������ʧ����ʾ��Ч��UI��ʾ
    }

    /// <summary>
    /// �ֶ�����ʰȡ���������ű����ã�
    /// </summary>
    public void ManualPickup()
    {
        TryPickup();
    }

    /// <summary>
    /// ������Ʒ��Ϣ
    /// </summary>
    public void SetItem(GameItem newItem)
    {
        item = newItem;
    }

    /// <summary>
    /// ��ȡ��Ʒ��Ϣ
    /// </summary>
    public GameItem GetItem()
    {
        return item;
    }

    void OnDrawGizmosSelected()
    {
        // ����ʰȡ��Χ
        Gizmos.color = playerInRange ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);

        // ���Ƶ���ҵ�����
        if (player != null && playerInRange)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }

    void OnDrawGizmos()
    {
        // ʼ����ʾʰȡ��Χ�İ�͸������
        Gizmos.color = new Color(1, 1, 0, 0.1f);
        Gizmos.DrawSphere(transform.position, pickupRange);

        // ��ʾ��Ʒ������ɫ��ʶ
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