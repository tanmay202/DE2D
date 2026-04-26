using UnityEngine;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Detection")]
    public float interactRadius = 1.5f;
    public LayerMask interactLayer;         // Put Shop/Table on this layer

    [Header("UI")]
    public TextMeshProUGUI hintText;        // e.g. "[E] Buy Items"
    public GameObject hintPanel;

    private IInteractable _nearest;

    void Update()
    {
        FindNearest();
        if (_nearest != null && Input.GetKeyDown(KeyCode.E))
            _nearest.Interact();
    }

    void FindNearest()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactRadius, interactLayer);
        
        IInteractable best = null;
        float bestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            IInteractable inter = hit.GetComponent<IInteractable>();
            if (inter == null) continue;

            float d = Vector2.Distance(transform.position, hit.transform.position);
            if (d < bestDist) { bestDist = d; best = inter; }
        }

        _nearest = best;

        // Update hint UI
        bool show = _nearest != null;
        if (hintPanel) hintPanel.SetActive(show);
        if (hintText && show) hintText.text = _nearest.GetHint();
    }

    // Draw detection radius in Scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}