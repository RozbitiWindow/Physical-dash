using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    // =============================================
    // INSPECTOR HODNOTY
    // =============================================

    [SerializeField] private float jumpForce = 15f;
    // Síla skoku nahoru

    [SerializeField] private float fallRotateSpeed = 180f;
    // Rychlost rotace při pádu (stupně/sec) — pomalejší než skok

    // =============================================
    // PRIVÁTNÍ PROMĚNNÉ
    // =============================================

    private Rigidbody2D rb;

    private bool isGrounded = false;

    private float currentAngle = 0f;
    // Náš vlastní úhel — nepoužíváme transform.eulerAngles
    // protože Unity ho normalizuje a rozbilo by nám výpočty

    private bool isJumping = false;
    // True = právě jsme skočili a čekáme na dokončení 90° rotace

    private float jumpStartAngle = 0f;
    // Úhel v momentě skoku — od něj počítáme +90°

    private float targetAngle = 0f;
    // Cílový úhel po skoku (jumpStartAngle - 90)

    // =============================================
    // START
    // =============================================

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
    }

    // =============================================
    // UPDATE — každý frame
    // =============================================

    void Update()
    {
        CheckGround();
        HandleJump();
        HandleRotation();
    }

    // =============================================
    // DETEKCE ZEMĚ — Raycast dolů
    // =============================================

    void CheckGround()
    {
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            Vector2.down,
            0.55f,
            LayerMask.GetMask("Ground")
        );

        bool wasGrounded = isGrounded;
        isGrounded = (hit.collider != null);

        // Právě jsme přistáli (minulý frame ne, teď ano)
        if (!wasGrounded && isGrounded)
        {
            OnLand();
        }
    }

    // =============================================
    // SKOK
    // =============================================

    void HandleJump()
    {
        bool jumpPressed =
            Keyboard.current.spaceKey.wasPressedThisFrame
            || Keyboard.current.upArrowKey.wasPressedThisFrame
            || Mouse.current.leftButton.wasPressedThisFrame;

        if (jumpPressed && isGrounded)
        {
            // Fyzický skok
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

            // Zaznamenáme odkud začínáme rotaci
            jumpStartAngle = currentAngle;

            // Cíl = přesně -90° od startu (po směru hodinových ručiček jako GD)
            targetAngle = jumpStartAngle - 90f;

            isJumping = true;
            isGrounded = false;
        }
    }

    // =============================================
    // ROTACE
    // =============================================

    void HandleRotation()
    {
        if (isJumping)
        {
            // Během skoku: rychle rotujeme směrem k targetAngle
            // Lerp = plynulý pohyb. 10f = rychlost přiblížení.
            currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime * 10f);

            // Pokud jsme dostatečně blízko cíle — hotovo
            if (Mathf.Abs(currentAngle - targetAngle) < 1f)
            {
                currentAngle = targetAngle;
                isJumping = false;
            }
        }
        else if (!isGrounded)
        {
            // Padáme bez skoku = pomalá rotace
            currentAngle -= fallRotateSpeed * Time.deltaTime;
        }
        // Pokud isGrounded a ne isJumping = nestojíme na místě (snap řeší OnLand)

        transform.rotation = Quaternion.Euler(0f, 0f, currentAngle);
    }

    // =============================================
    // PŘISTÁNÍ — snap na nejbližší 90°
    // =============================================

    void OnLand()
    {
        isJumping = false;

        // Zaokrouhlíme na nejbližší násobek 90°
        // Příklad: -137° → -180° (nejbližší násobek 90)
        currentAngle = Mathf.Round(currentAngle / 90f) * 90f;

        transform.rotation = Quaternion.Euler(0f, 0f, currentAngle);
    }
}