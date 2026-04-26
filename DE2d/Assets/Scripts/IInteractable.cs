public interface IInteractable
{
    /// <summary>Called when player presses E near this object.</summary>
    void Interact();

    /// <summary>Text shown in the interaction hint (e.g. "[E] Buy Items").</summary>
    string GetHint();
}