public interface IPlayerContext
{
    int Level { get; }
    int Health { get; }
    void EnterDialogueState();
    void ExitDialogueState();
    void AddExp(int amount);
}
