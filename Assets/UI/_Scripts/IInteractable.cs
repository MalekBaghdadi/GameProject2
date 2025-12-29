public interface IInteractable
{
    string GetInteractPrompt(PlayerInteraction player);
    void Interact(PlayerInteraction player);
}