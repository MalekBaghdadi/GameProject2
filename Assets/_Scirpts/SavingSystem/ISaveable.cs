public interface ISaveable
{
    // The object receives data and updates its state
    void LoadData(GameData data);

    // The object writes its current state into the data container
    void SaveData(ref GameData data);
}