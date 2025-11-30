/// <summary>
/// Very small global game state flags used by UI and input handlers.
/// Keep this minimal — acts like a tiny shared state service.
/// </summary>
public static class GameState
{
    /// <summary>
    /// Set to true only after the StartMenu transition completes.
    /// When false, global pause via Escape will be ignored.
    /// </summary>
    public static bool IsGameStarted = false;
}