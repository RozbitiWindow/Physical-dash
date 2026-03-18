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
            // Na zemi — snap na nejbližší 90°
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
            // Ve vzduchu — točíme se
            // freezeRotation zabraňuje fyzice točit objektem
            // ale MY můžeme měnit transform.rotation přímo
            currentAngle -= rotationSpeed * Time.deltaTime;
            transform.rotation = Quaternion.Euler(0f, 0f, currentAngle);
        }
    }

    void Jump()
    {
        rb.velocity = Vector2.zero;

        // Při otočené gravitaci skáčeme dolů (záporné Y)
        Vector2 jumpDirection = gravityFlipped ? Vector2.down : Vector2.up;
        rb.AddForce(jumpDirection * jumpForce, ForceMode2D.Impulse);
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
        // Paprsek vychází VÝŠE než střed — ignoruje spodní část cube
        // Tím když dopadáme na roh bloku, boční check ho netrefí
        Vector2 position = (Vector2)transform.position + Vector2.right * 0.55f + Vector2.up * 0.2f;
        // Vector2.up * 0.2f = posuneme kontrolní bod výš o 20%
        // Spodních 20% cube je "bezpečná zóna" pro přistání na rozích

        Vector2 size = new Vector2(groundCheckRadius * 2f, 0.5f);
        // Výška boxu snížena z 0.7f na 0.5f
        // Menší box = méně falešných detekcí na rozích bloků

        Collider2D hit = Physics2D.OverlapBox(position, size, 0f, groundMask);

        if (hit != null)
        {
            if (hit.bounds.min.x > transform.position.x - 0.1f)
            {
                // Zabijeme pouze pokud vrchol bloku sahá výš než 
                // HORNÍ TŘETINA cube — ne polovina
                // transform.position.y + 0.15f = horní třetina
                float blockTop = hit.bounds.max.y;
                if (blockTop > transform.position.y + 0.15f)
                {
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
        if (rb.velocity.y < -24.2f)
            rb.velocity = new Vector2(rb.velocity.x, -24.2f);
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

        // Zrušíme rotaci — particles se nikdy netočí
        runParticles.transform.rotation = Quaternion.identity;
        // Quaternion.identity = žádná rotace = 0°

        // Pozice těsně u levého dolního rohu cube
        runParticles.transform.position = new Vector3(
            transform.position.x - 0.5f,  // vlevo
            transform.position.y - 0.4f,   // těsně pod středem, ne pod zemí
            transform.position.z
        );
    }

    public void SetGravity(bool flipped)
    {
        gravityFlipped = flipped;

        if (flipped)
        {
            // Otočíme gravitaci — Rigidbody padá nahoru
            rb.gravityScale = -cubeGravityScale;

            // Otočíme sprite — player vypadá že jede po stropě
            transform.localScale = new Vector3(1f, -1f, 1f);
        }
        else
        {
            // Vrátíme normální gravitaci
            rb.gravityScale = cubeGravityScale;
            transform.localScale = new Vector3(1f, 1f, 1f);
        }
    }

    // Uprav OnGrounded() aby fungoval i při otočené gravitaci:
   
    

}