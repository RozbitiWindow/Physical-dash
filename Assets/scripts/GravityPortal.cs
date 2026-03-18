using UnityEngine;

public class GravityPortal : MonoBehaviour
{
    [SerializeField] private bool flipGravity = true;
    // true = otočí gravitaci, false = vrátí zpět

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        GDCubeController player = other.GetComponent<GDCubeController>();
        if (player != null)
            player.SetGravity(flipGravity);
    }
}