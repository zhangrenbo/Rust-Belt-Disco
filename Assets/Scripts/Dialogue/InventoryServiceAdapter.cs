using UnityEngine;

/// <summary>
/// Adapter that bridges IInventoryService to existing InventoryManager.
/// </summary>
public class InventoryServiceAdapter : MonoBehaviour, IInventoryService
{
    public void AddItem(string itemName, int quantity)
    {
        if (InventoryManager.instance != null)
        {
            for (int i = 0; i < quantity; i++)
            {
                var item = new GameItem
                {
                    Name = itemName,
                    Description = $"�Ի��õ�{itemName}",
                    Type = ItemType.Material,
                    Size = new Vector2Int(1, 1)
                };
                InventoryManager.instance.AddItem(item);
            }
        }
    }

    public bool HasItem(string itemName)
    {
        return InventoryManager.instance != null && InventoryManager.instance.HasItem(itemName);
    }
}
