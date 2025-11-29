namespace Horizon
{
    /// <summary>
    /// Optional interface for objects that can be highlighted by a player interaction system.
    /// </summary>
    public interface IHighlightable
    {
        /// <summary>
        /// Called to toggle the highlight effect on or off.
        /// </summary>
        /// <param name="isHighlighted">True to enable highlight; false to disable.</param>
        void SetHighlight(bool isHighlighted);
    }
}
