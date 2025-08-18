public interface IInventoryService
{
    void AddItem(string itemName, int quantity);
    bool HasItem(string itemName);
}
