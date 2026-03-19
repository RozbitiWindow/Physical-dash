using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GDCubeController : MonoBehaviour
{
    [Header("Pohyb")]
    [SerializeField] private float moveSpeed = 10f;

    [Header("Skok")]
    [SerializeField] private float jumpForce = 10f;

    [Header("Rotace")]
    [SerializeField] private float rotationSpeed = 400f;

    [Header("Fyzika")]
    [SerializeField] private float cubeGravityScale = 3f;

    [Header("Detekce")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundCheckRadius = 0.1f;

    [Header("Efekty")]
    [SerializeField] private ParticleSystem runParticles;
    [SerializeField] private LevelScroller levelScroller;  

    [Header("Audio")]
    [SerializeField] private MusicManager musicManager;
    // Přetáhni sem Music objekt v Inspectoru

    private bool gravityFlipped = false;

    private Rigidbody2D rb;
    private float currentAngle = 0f;
    // Vlastní úhel rotace — nepotřebujeme child Sprite objekt

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = cubeGravityScale;
        rb.freezeRotation = true;
    }

    void Update()
    {
        HandleMovement();
        HandleCube();
        LimitFallSpeed();
        HandleWallHit();
        HandleParticles();
        FixParticleTransform(); 
    }

    void HandleMovement()
    {
        transform.position += Vector3.right * moveSpeed * Time.deltaTime;
    }

    void HandleCube()
    {
        if (OnGrounded())
        {
            // Snap rotace
            currentAngle = Mathf.Round(currentAngle / 90f) * 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, currentAngle);

            bool jumpHeld =
                Mouse.current.leftButton.isPressed ||
                Keyboard.current.spaceKey.isPressed ||
                Keyboard.current.upArrowKey.isPressed;

            if (jumpHeld)
                Jump();
        }
        else
        {
            // Při otočené gravitaci točíme opačným směrem
            float direction = gravityFlipped ? -1f : 1f;
            currentAngle -= rotationSpeed * direction * Time.deltaTime;
            transform.rotation = Quaternion.Euler(0f, 0f, currentAngle);
        }
    }


    void Jump()
    {
        rb.velocity = Vector2.zero;

        // Při otočené gravitaci skáčeme DOLŮ (od stropu)
        // Při normální gravitaci skáčeme NAHORU (od země)
        float jumpDirection = gravityFlipped ? -1f : 1f;
        rb.AddForce(Vector2.up * jumpForce * jumpDirection, ForceMode2D.Impulse);
    }

    bool OnGrounded()
    {
        // Při normální gravitaci kontrolujeme dole
        // Při otočené gravitaci kontrolujeme nahoře
        Vector3 direction = gravityFlipped ? Vector3.up : Vector3.down;

        Vector3 checkPosition = transform.position + direction * 0.5f;
        Vector2 checkSize = new Vector2(0.9f, groundCheckRadius);
        return Physics2D.OverlapBox(checkPosition, checkSize, 0f, groundMask);
    }

    bool TouchWall()
    {
        Vector2 position = (Vector2)transform.position + Vector2.right * 0.55f + Vector2.up * 0.2f;
        Vector2 size = new Vector2(groundCheckRadius * 2f, 0.5f);

        Collider2D hit = Physics2D.OverlapBox(position, size, 0f, groundMask);

        if (hit != null)
        {
            if (hit.bounds.min.x > transform.position.x - 0.1f)
            {
                float blockTop = hit.bounds.max.y;
                float blockBottom = hit.bounds.min.y;

                if (!gravityFlipped)
                {
                    // Normální gravitace — zeď zabije pokud sahá výš než horní třetina
                    if (blockTop > transform.position.y + 0.15f)
                        return true;
                }
                else
                {
                    // Otočená gravitace — zeď zabije pokud sahá níž než spodní třetina
                    if (blockBottom < transform.position.y - 0.15f)
                        return true;
                }
            }
        }
        return false;
    }

    void HandleWallHit()
    {
        if (TouchWall())
            Die();
    }

    void LimitFallSpeed()
    {
        if (!gravityFlipped)
        {
            // Normální gravitace — omezíme pád dolů
            if (rb.velocity.y < -24.2f)
                rb.velocity = new Vector2(rb.velocity.x, -24.2f);
        }
        else
        {
            // Otočená gravitace — omezíme pád nahoru
            if (rb.velocity.y > 24.2f)
                rb.velocity = new Vector2(rb.velocity.x, 24.2f);
        }
    }

    public void Die()
    {
        // Restartujeme hudbu před restartem scény
        if (musicManager != null)
            musicManager.RestartMusic();

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

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

    void HandleParticles()
    {
        if (runParticles == null) return;

        if (OnGrounded())
        {
            if (!runParticles.isPlaying)
                runParticles.Play();

            var velocity = runParticles.velocityOverLifetime;
            velocity.enabled = true;

            float currentSpeed = levelScroller != null ? levelScroller.speed : 10f;

            // VŽDY pevné hodnoty ve world space — žádná rotace, žádný rb.velocity
            velocity.x = new ParticleSystem.MinMaxCurve(-currentSpeed * 1.5f);
            // Kladné X = doprava = opticky dozadu za pohybující se level
            velocity.y = new ParticleSystem.MinMaxCurve(0f);
            // Y vždy 0
            velocity.z = new ParticleSystem.MinMaxCurve(0f);
        }
        else
        {
            if (!runParticles.isStopped)
                runParticles.Stop();
        }
    }

    void FixParticleTransform()
    {
        if (runParticles == null) return;

        runParticles.transform.rotation = Quaternion.identity;

        // Normální gravitace = particles vlevo DOLE (-0.5f)
        // Otočená gravitace = particles vlevo NAHOŘE (+0.5f)
        float yOffset = gravityFlipped ? 0.4f : -0.4f;

        runParticles.transform.position = new Vector3(
            transform.position.x - 0.45f,
            transform.position.y + yOffset,
            transform.position.z
        );
    }

    public void SetGravity(bool flipped)
    {
        gravityFlipped = flipped;

        if (flipped)
        {
            rb.gravityScale = -cubeGravityScale;
            // Zachováme originální scale X, jen otočíme Y
            transform.localScale = new Vector3(
                Mathf.Abs(transform.localScale.x),   // původní X (0.6)
                -Mathf.Abs(transform.localScale.y),  // záporné Y (-0.6)
                transform.localScale.z               // původní Z (1)
            );
        }
        else
        {
            rb.gravityScale = cubeGravityScale;
            // Vrátíme originální scale
            transform.localScale = new Vector3(
                Mathf.Abs(transform.localScale.x),  // původní X (0.6)
                Mathf.Abs(transform.localScale.y),  // původní Y (0.6)
                transform.localScale.z              // původní Z (1)
            );
        }


    }

    public bool IsGravityFlipped()
    {
        // Vrátí aktuální stav gravitace
        // Orb.cs potřebuje vědět jestli je gravitace otočená
        return gravityFlipped;
    }

    public void OrbJump(float force, bool respectGravity)
    {
        // Vystřelí playera silou orbu
        // respectGravity = true → skočí podle aktuální gravitace
        // respectGravity = false → vždy skočí nahoru (Yellow, Blue)

        rb.velocity = new Vector2(rb.velocity.x, 0f);
        // Nulujeme jen Y velocity — X necháme

        if (respectGravity)
        {
            // Pink orb — skočí podle aktuální gravitace
            float direction = gravityFlipped ? -1f : 1f;
            rb.AddForce(Vector2.up * force * direction, ForceMode2D.Impulse);
        }
        else
        {
            // Yellow/Blue orb — vždy nahoru bez ohledu na gravitaci
            rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);
        }
    }

    public void InstantFlip()
    {
        // Okamžitý flip bez oblouku
        // Nulujeme Y velocity = cube neletí obloukem
        // Zůstane jen malá velocity ve směru nové gravitace
        rb.velocity = new Vector2(rb.velocity.x, 0f);
    }

    public void BlackOrbDrop()
    {
        // Instantně shodí dolů
        // Nulujeme Y velocity a přidáme silnou velocity dolů
        rb.velocity = new Vector2(rb.velocity.x, -20f);
        // -20f = rychlý pád dolů
        // Gravitace pak zbytek dodělá sama
    }



}