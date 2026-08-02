namespace LittleTrawling.Interaction
{
    /// <summary>
    /// Contract for interactable objects in the game world.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Prompt text to display on UI when player is near (e.g. "Press [E] to enter boat").
        /// </summary>
        string GetInteractionPrompt();

        /// <summary>
        /// Action executed when player presses Interact.
        /// </summary>
        void Interact();
    }
}
