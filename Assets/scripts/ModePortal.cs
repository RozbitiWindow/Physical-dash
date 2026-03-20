using UnityEngine;

public class ModePortal : MonoBehaviour
{
    public enum GameMode
    {
        Cube,
        Ship,
        Ball,
        Wave,
    }

    [Header("Nastavení")]
    [SerializeField] private GameMode targetMode = GameMode.Cube;

    [Header("Hranice preview")]
    [SerializeField] private float boundaryOffsetUp = 4f;
    // Vzdálenost horní hranice od portálu
    [SerializeField] private float boundaryOffsetDown = 4f;
    // Vzdálenost dolní hranice od portálu
    // Obě hodnoty nastavitelné zvlášť pro asymetrické levely

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        GDCubeController player = other.GetComponent<GDCubeController>();
        if (player != null)
            player.SetGameMode(targetMode, boundaryOffsetUp, boundaryOffsetDown);
    }

    // Gizmos se kreslí přímo na portalu
    // Pohybují se s ním automaticky
    void OnDrawGizmos()
    {
        // Zobrazíme jen pro módy které mají hranice
        if (targetMode == GameMode.Cube) return;

        Gizmos.color = Color.yellow;

        // Horní hranice
        Gizmos.DrawLine(
            new Vector3(transform.position.x - 20f, transform.position.y + boundaryOffsetUp, 0),
            new Vector3(transform.position.x + 100f, transform.position.y + boundaryOffsetUp, 0)
        );

        // Dolní hranice
        Gizmos.DrawLine(
            new Vector3(transform.position.x - 20f, transform.position.y - boundaryOffsetDown, 0),
            new Vector3(transform.position.x + 100f, transform.position.y - boundaryOffsetDown, 0)
        );

        // Svislá čára = pozice portálu
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawLine(
            new Vector3(transform.position.x, transform.position.y - boundaryOffsetDown, 0),
            new Vector3(transform.position.x, transform.position.y + boundaryOffsetUp, 0)
        );
    }
}