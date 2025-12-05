namespace Horizon
{
    /// <summary>
    /// An interface for ScriptableObjects that have a unique string-based identifier.
    /// </summary>
    public interface IKeyedData
    {
        string ID { get; }
    }
}