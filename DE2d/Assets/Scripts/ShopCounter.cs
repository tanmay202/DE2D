using UnityEngine;

public class ShopCounter : MonoBehaviour, IInteractable
{
    [Header("Link to ShopUI canvas")]
    public ShopUI shopUI;

    public void Interact() => shopUI.Open();

    public string GetHint() => "[E] Enter Shop";
}