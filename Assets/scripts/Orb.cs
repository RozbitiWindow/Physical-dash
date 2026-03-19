using UnityEngine;
using UnityEngine.InputSystem;

public class Orb : MonoBehaviour
{
    // Typ orbu určuje co se stane při aktivaci
    public enum OrbType
    {
        Yellow,  // Skok nahoru — nejběžnější
        Blue,    // Skok bez změny gravitace
        Pink,    // Skok + okamžitý gravity flip
        Green,    // Pouze gravity flip, bez skoku
        Black
    }

    [Header("Nastavení")]
    [SerializeField] private OrbType orbType = OrbType.Yellow;
    [SerializeField] private float orbForce = 18f;
    // Síla skoku — Yellow a Blue používají tuto hodnotu

    private bool playerInside = false;
    // Je player v dosahu orbu?

    private bool used = false;
    // Orb lze použít jen jednou za průchod
    // Resetuje se když player odejde

    private GDCubeController player;
    // Reference na player script

    // ============================================
    // DETEKCE VSTUPU DO ORBU
    // ============================================

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Trigger hit: " + other.gameObject.name + " tag: " + other.tag);

        if (!other.CompareTag("Player")) return;

        playerInside = true;
        used = false;
        player = other.GetComponent<GDCubeController>();

        Debug.Log("Player detected! GDCubeController: " + (player != null));
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = false;
        used = false;
        player = null;
    }

    // ============================================
    // AKTIVACE — čekáme na stisk tlačítka
    // ============================================

    void Update()
    {
        if (!playerInside || used || player == null) return;

        // isPressed místo wasPressedThisFrame
        // isPressed = true dokud držíš tlačítko
        // wasPressedThisFrame = true jen jeden frame
        // Problém: trigger enter a jump check mohou být v různých framech
        bool jumpPressed =
            Keyboard.current.spaceKey.isPressed ||
            Keyboard.current.upArrowKey.isPressed ||
            Mouse.current.leftButton.isPressed;

        if (jumpPressed)
        {
            Debug.Log("Orb activated! Type: " + orbType);
            ActivateOrb();
        }
    }

    void ActivateOrb()
    {
        used = true;

        switch (orbType)
        {
            case OrbType.Yellow:
                // Silný skok nahoru — vždy nahoru bez ohledu na gravitaci
                player.OrbJump(orbForce, false);
                break;

            case OrbType.Pink:

                player.OrbJump(orbForce, false);
                break;

            case OrbType.Green:
                // Stejné jako Pink — gravity flip + slabší skok s obloukem
                // Jen jiná barva/název
                bool greenFlip = player.IsGravityFlipped();
                player.SetGravity(!greenFlip);
                player.OrbJump(orbForce * 0.6f, true);
                break;

            case OrbType.Blue:
                // Okamžitý gravity flip — žádný skok, žádný oblouk
                // Instantně otočí gravitaci a velocity
                bool blueFlip = player.IsGravityFlipped();
                player.SetGravity(!blueFlip);
                player.InstantFlip();
                // Nuluje velocity = žádný oblouk
                break;

            case OrbType.Black:
                // Instantně shodí dolů — vždy normální gravitace + silná velocity dolů
                // Bez ohledu na aktuální gravitaci
                player.SetGravity(false);
                // Vrátíme normální gravitaci
                player.BlackOrbDrop();
                // Silná velocity dolů
                break;
        }
    }


}