using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GDCubeController : MonoBehaviour
{
    [Header("Pohyb")]
    [SerializeField] private float moveSpeed = 0f;

    [Header("Skok — Cube")]
    [SerializeField] private float jumpForce = 10f;

    [Header("Ship")]
    [SerializeField] private float shipForce = 15f;
    [SerializeField] private float shipMaxSpeed = 12f;

    [Header("Ball")]
    [SerializeField] private float ballJumpForce = 8f;

    [Header("Wave")]
    [SerializeField] private float waveSpeed = 8f;

    [Header("Rotace")]
    [SerializeField] private float rotationSpeed = 400f;

    [Header("Fyzika")]
    [SerializeField] private float cubeGravityScale = 3f;
    [SerializeField] private float shipGravityScale = 1.5f;
    [SerializeField] private float ballGravityScale = 2f;

    [Header("Detekce")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundCheckRadius = 0.1f;

    [Header("Hranice — Ship/Ball/Wave")]
    [SerializeField] private float boundaryOffset = 4f;
    // Vzdálenost hranice od playera v momentě přepnutí módu

    [Header("Debug")]
    [SerializeField] private bool showBoundariesInGame = true;

    [Header("Efekty")]
    [SerializeField] private ParticleSystem runParticles;
    [SerializeField] private LevelScroller levelScroller;

    [Header("Sprity")]
    [SerializeField] private Sprite cubeSprite;
    [SerializeField] private Sprite shipSprite;
    [SerializeField] private Sprite ballSprite;
    [SerializeField] private Sprite waveSprite;

    [Header("Audio")]
    [SerializeField] private MusicManager musicManager;

    [Header("Kamera")]
    [SerializeField] private CameraFollow cameraFollow;

    // =============================================
    // PRIVÁTNÍ PROMĚNNÉ
    // =============================================

    private bool gravityFlipped = false;
    private bool useBoundary = false;
    private float topBoundary;
    private float bottomBoundary;
    private float originalScale;

    private ModePortal.GameMode currentMode = ModePortal.GameMode.Cube;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private float currentAngle = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb.gravityScale = cubeGravityScale;
        rb.freezeRotation = true;
        // Uložíme originální scale při startu
        originalScale = Mathf.Abs(transform.localScale.x);
    }

    void Update()
    {
        HandleMovement();

        switch (currentMode)
        {
            case ModePortal.GameMode.Cube:
                HandleCube();
                break;
            case ModePortal.GameMode.Ship:
                HandleShip();
                break;
            case ModePortal.GameMode.Ball:
                HandleBall();
                break;
            case ModePortal.GameMode.Wave:
                HandleWave();
                break;
        }

        LimitFallSpeed();
        EnforceBoundary();
        HandleWallHit();
        HandleParticles();
        FixParticleTransform();
    }

    // ============================================
    // POHYB
    // ============================================

    void HandleMovement()
    {
        transform.position += Vector3.right * moveSpeed * Time.deltaTime;
    }

    // ============================================
    // CUBE
    // ============================================

    void HandleCube()
    {
        if (OnGrounded())
        {
            currentAngle = Mathf.Round(currentAngle / 90f) * 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, currentAngle);

            bool jumpHeld =
                Mouse.current.leftButton.isPressed ||
                Keyboard.current.spaceKey.isPressed ||
                Keyboard.current.upArrowKey.isPressed;

            if (jumpHeld) Jump();
        }
        else
        {
            float direction = gravityFlipped ? -1f : 1f;
            currentAngle -= rotationSpeed * direction * Time.deltaTime;
            transform.rotation = Quaternion.Euler(0f, 0f, currentAngle);
        }
    }

    // ============================================
    // SHIP
    // ============================================

    void HandleShip()
    {
        bool holdingJump =
            Mouse.current.leftButton.isPressed ||
            Keyboard.current.spaceKey.isPressed ||
            Keyboard.current.upArrowKey.isPressed;

        // Cílová rychlost — nahoru když držíš, dolů když pustíš
        float targetVelocityY;

        if (!gravityFlipped)
            targetVelocityY = holdingJump ? shipMaxSpeed : -shipMaxSpeed;
        else
            targetVelocityY = holdingJump ? -shipMaxSpeed : shipMaxSpeed;

        // Lerp = plynulá změna rychlosti — ne okamžitá
        // shipForce teď slouží jako "citlivost" — nižší = pomalejší reakce
        float newVelocityY = Mathf.Lerp(rb.velocity.y, targetVelocityY, Time.deltaTime * shipForce);

        rb.velocity = new Vector2(rb.velocity.x, newVelocityY);

        // Rotace podle směru letu
        float targetAngle = rb.velocity.y * 3f;
        if (gravityFlipped) targetAngle = -targetAngle;
        currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime * 10f);
        transform.rotation = Quaternion.Euler(0f, 0f, currentAngle);
    }

    // ============================================
    // BALL
    // ============================================

    void HandleBall()
    {
        // Ball se točí rychleji než cube
        currentAngle -= rotationSpeed * 1.2f * Time.deltaTime;
        transform.rotation = Quaternion.Euler(0f, 0f, currentAngle);

        bool jumpPressed =
            Mouse.current.leftButton.wasPressedThisFrame ||
            Keyboard.current.spaceKey.wasPressedThisFrame ||
            Keyboard.current.upArrowKey.wasPressedThisFrame;

        if (jumpPressed)
        {
            // Okamžitý gravity flip — bez delay
            bool newGravity = !gravityFlipped;
            SetGravity(newGravity);

            // Okamžitá velocity změna — žádný oblouk
            float direction = newGravity ? -1f : 1f;
            rb.velocity = new Vector2(rb.velocity.x, ballJumpForce * direction * 1.5f);
            // 1.5f = rychlejší odraz
        }
    }

    // ============================================
    // WAVE
    // ============================================

    void HandleWave()
    {
        bool holdingJump =
            Mouse.current.leftButton.isPressed ||
            Keyboard.current.spaceKey.isPressed ||
            Keyboard.current.upArrowKey.isPressed;

        rb.gravityScale = 0f;

        float verticalDirection;
        if (!gravityFlipped)
            verticalDirection = holdingJump ? 1f : -1f;
        else
            verticalDirection = holdingJump ? -1f : 1f;

        rb.velocity = new Vector2(rb.velocity.x, waveSpeed * verticalDirection);

        float targetAngle = verticalDirection * 45f;
        if (gravityFlipped) targetAngle = -targetAngle;
        currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime * 20f);
        transform.rotation = Quaternion.Euler(0f, 0f, currentAngle);
    }

    // ============================================
    // SKOK
    // ============================================

    void Jump()
    {
        rb.velocity = Vector2.zero;
        float jumpDirection = gravityFlipped ? -1f : 1f;
        rb.AddForce(Vector2.up * jumpForce * jumpDirection, ForceMode2D.Impulse);
    }

    // ============================================
    // HRANICE
    // ============================================

    void EnforceBoundary()
    {
        if (!useBoundary) return;

        if (transform.position.y > topBoundary)
        {
            transform.position = new Vector3(
                transform.position.x,
                topBoundary,
                transform.position.z
            );
            if (rb.velocity.y > 0)
                rb.velocity = new Vector2(rb.velocity.x, 0f);
        }

        if (transform.position.y < bottomBoundary)
        {
            transform.position = new Vector3(
                transform.position.x,
                bottomBoundary,
                transform.position.z
            );
            if (rb.velocity.y < 0)
                rb.velocity = new Vector2(rb.velocity.x, 0f);
        }
    }

    // ============================================
    // ZMĚNA MÓDU
    // ============================================

    public void SetGameMode(ModePortal.GameMode mode, float offsetUp = 4f, float offsetDown = 4f)
    {
        currentMode = mode;

        switch (mode)
        {
            case ModePortal.GameMode.Cube:
                rb.gravityScale = gravityFlipped ? -cubeGravityScale : cubeGravityScale;
                rb.freezeRotation = true;
                useBoundary = false;
                if (cameraFollow != null) cameraFollow.SetFollowY(true);
                if (cubeSprite != null) spriteRenderer.sprite = cubeSprite;
                transform.localScale = new Vector3(
                    originalScale,
                    gravityFlipped ? -originalScale : originalScale,
                    1f
                );
                break;

            case ModePortal.GameMode.Ship:
                rb.gravityScale = gravityFlipped ? -shipGravityScale : shipGravityScale;
                rb.freezeRotation = true;
                useBoundary = true;
                topBoundary = transform.position.y + offsetUp;
                bottomBoundary = transform.position.y - offsetDown;
                if (cameraFollow != null) cameraFollow.SetFollowY(false);
                if (shipSprite != null) spriteRenderer.sprite = shipSprite;
                transform.localScale = new Vector3(
                    originalScale,
                    gravityFlipped ? -originalScale : originalScale,
                    1f
                );
                break;

            case ModePortal.GameMode.Ball:
                rb.gravityScale = gravityFlipped ? -ballGravityScale : ballGravityScale;
                rb.freezeRotation = false;
                useBoundary = true;
                topBoundary = transform.position.y + offsetUp;
                bottomBoundary = transform.position.y - offsetDown;
                if (cameraFollow != null) cameraFollow.SetFollowY(false);
                if (ballSprite != null) spriteRenderer.sprite = ballSprite;
                transform.localScale = new Vector3(
                    originalScale,
                    gravityFlipped ? -originalScale : originalScale,
                    1f
                );
                break;

            case ModePortal.GameMode.Wave:
                rb.gravityScale = 0f;
                rb.freezeRotation = true;
                useBoundary = true;
                topBoundary = transform.position.y + offsetUp;
                bottomBoundary = transform.position.y - offsetDown;
                if (cameraFollow != null) cameraFollow.SetFollowY(false);
                if (waveSprite != null) spriteRenderer.sprite = waveSprite;
                transform.localScale = new Vector3(
                    originalScale,
                    gravityFlipped ? -originalScale : originalScale,
                    1f
                );
                break;
        }
    }

    // ============================================
    // DETEKCE ZEMĚ
    // ============================================

    bool OnGrounded()
    {
        Vector3 direction = gravityFlipped ? Vector3.up : Vector3.down;
        Vector3 checkPosition = transform.position + direction * 0.5f;
        Vector2 checkSize = new Vector2(0.9f, groundCheckRadius);
        return Physics2D.OverlapBox(checkPosition, checkSize, 0f, groundMask);
    }

    // ============================================
    // DETEKCE ZDI
    // ============================================

    bool TouchWall()
    {
        // Ship má jiný hitbox než cube
        float wallCheckHeight = currentMode == ModePortal.GameMode.Ship ? 0.3f : 0.5f;
        // Ship = menší výška = méně falešných detekcí

        Vector2 position = (Vector2)transform.position + Vector2.right * 0.55f + Vector2.up * 0.1f;
        Vector2 size = new Vector2(groundCheckRadius * 2f, wallCheckHeight);

        Collider2D hit = Physics2D.OverlapBox(position, size, 0f, groundMask);

        if (hit != null)
        {
            if (hit.bounds.min.x > transform.position.x - 0.1f)
            {
                float blockTop = hit.bounds.max.y;
                float blockBottom = hit.bounds.min.y;

                if (!gravityFlipped)
                {
                    if (blockTop > transform.position.y + 0.15f)
                        return true;
                }
                else
                {
                    if (blockBottom < transform.position.y - 0.15f)
                        return true;
                }
            }
        }
        return false;
    }

    void HandleWallHit()
    {
        // Wave nemá wall check — létá volně mezi hranicemi
        if (currentMode == ModePortal.GameMode.Wave) return;

        if (TouchWall()) Die();
    }

    // ============================================
    // LIMIT PÁDU
    // ============================================

    void LimitFallSpeed()
    {
        if (currentMode == ModePortal.GameMode.Wave) return;

        if (!gravityFlipped)
        {
            if (rb.velocity.y < -24.2f)
                rb.velocity = new Vector2(rb.velocity.x, -24.2f);
        }
        else
        {
            if (rb.velocity.y > 24.2f)
                rb.velocity = new Vector2(rb.velocity.x, 24.2f);
        }
    }

    // ============================================
    // PARTICLES
    // ============================================

    void HandleParticles()
    {
        if (runParticles == null) return;

        // Wave — žádné particles
        if (currentMode == ModePortal.GameMode.Wave)
        {
            if (!runParticles.isStopped) runParticles.Stop();
            return;
        }

        if (OnGrounded())
        {
            if (!runParticles.isPlaying) runParticles.Play();

            var velocity = runParticles.velocityOverLifetime;
            velocity.enabled = true;

            float currentSpeed = levelScroller != null ? levelScroller.speed : 10f;
            velocity.x = new ParticleSystem.MinMaxCurve(-currentSpeed * 1.5f);
            velocity.y = new ParticleSystem.MinMaxCurve(0f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f);
        }
        else
        {
            if (!runParticles.isStopped) runParticles.Stop();
        }
    }

    void FixParticleTransform()
    {
        if (runParticles == null) return;

        runParticles.transform.rotation = Quaternion.identity;

        float yOffset;
        switch (currentMode)
        {
            case ModePortal.GameMode.Ship:
            case ModePortal.GameMode.Wave:
                yOffset = 0f;
                break;
            default:
                yOffset = gravityFlipped ? 0.35f : -0.35f;
                break;
        }

        runParticles.transform.position = new Vector3(
            transform.position.x - 0.45f,
            transform.position.y + yOffset,
            transform.position.z
        );
    }

    // ============================================
    // GRAVITACE
    // ============================================

    public void SetGravity(bool flipped)
    {
        gravityFlipped = flipped;

        float scale = currentMode switch
        {
            ModePortal.GameMode.Ship => shipGravityScale,
            ModePortal.GameMode.Ball => ballGravityScale,
            ModePortal.GameMode.Wave => 0f,
            _ => cubeGravityScale
        };

        rb.gravityScale = flipped ? -scale : scale;

        transform.localScale = new Vector3(
            originalScale,
            flipped ? -originalScale : originalScale,
            1f
        );
    }

    public bool IsGravityFlipped() => gravityFlipped;

    // ============================================
    // ORB FUNKCE
    // ============================================

    public void OrbJump(float force, bool respectGravity)
    {
        rb.velocity = new Vector2(rb.velocity.x, 0f);

        if (respectGravity)
        {
            float direction = gravityFlipped ? -1f : 1f;
            rb.AddForce(Vector2.up * force * direction, ForceMode2D.Impulse);
        }
        else
        {
            rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);
        }
    }

    public void InstantFlip()
    {
        rb.velocity = new Vector2(rb.velocity.x, 0f);
    }

    public void BlackOrbDrop()
    {
        rb.velocity = new Vector2(rb.velocity.x, -20f);
    }

    public void BounceUp(float force)
    {
        rb.velocity = new Vector2(rb.velocity.x, 0f);
        rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);
    }

    // ============================================
    // SMRT
    // ============================================

    public void Die()
    {
        if (musicManager != null)
            musicManager.RestartMusic();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ============================================
    // GIZMOS — Scene view
    // ============================================



    /* void OnDrawGizmos()
     {


         // Žluté = preview
         Gizmos.color = Color.green;
         Gizmos.DrawLine(
             new Vector3(-100, transform.position.y + boundaryOffset, 0),
             new Vector3(100, transform.position.y + boundaryOffset, 0)
         );
         Gizmos.DrawLine(
             new Vector3(-100, transform.position.y - boundaryOffset, 0),
             new Vector3(100, transform.position.y - boundaryOffset, 0)
         );
     }
    */
    void OnDrawGizmosSelected()
    {
        // Zelený box = detekce země
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(
            transform.position + Vector3.down * 0.5f,
            new Vector3(0.9f, groundCheckRadius, 0.1f)
        );

        // Modrý box = detekce zdi
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(
            (Vector2)transform.position + Vector2.right * 0.55f,
            new Vector3(groundCheckRadius * 2f, 0.7f, 0.1f)
        );
    }

    // ============================================
    // HRANICE — Game view vizualizace
    // ============================================

    void OnGUI()
    {
        if (!showBoundariesInGame || !useBoundary) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 topScreen = cam.WorldToScreenPoint(new Vector3(0, topBoundary, 0));
        Vector3 bottomScreen = cam.WorldToScreenPoint(new Vector3(0, bottomBoundary, 0));

        GUI.color = new Color(1f, 0f, 0f, 0.5f);
        GUI.DrawTexture(
            new Rect(0, Screen.height - topScreen.y - 2, Screen.width, 4),
            Texture2D.whiteTexture
        );
        GUI.DrawTexture(
            new Rect(0, Screen.height - bottomScreen.y - 2, Screen.width, 4),
            Texture2D.whiteTexture
        );
    }




}