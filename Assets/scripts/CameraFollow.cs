using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform player;
    // Přetáhni sem Player objekt v Inspectoru

    [SerializeField] private float smoothSpeed = 5f;
    // Plynulost sledování — vyšší = rychlejší

    [SerializeField] private float yOffset = 0f;
    // Offset Y — pokud chceš kameru trochu výš nebo níž než player

    [SerializeField] private float minY = 0f;
    // Minimální Y kamery — kamera nepůjde níž než tato hodnota
    // Nastav na výšku základní země aby kamera neklesla pod level

    private float fixedX;
    // X pozice kamery zůstane vždy stejná — player se nehýbe v X

    void Start()
    {
        // Zapamatujeme si startovní X pozici kamery
        fixedX = transform.position.x;
    }

    void LateUpdate()
    {
        // LateUpdate se volá PO Update() — ideální pro kameru
        // Tím kamera sleduje player až poté co se pohnul

        if (player == null) return;

        // Cílová Y pozice = Y playera + offset
        float targetY = player.position.y + yOffset;

        // Clamp = omezíme Y aby neklesla pod minY
        targetY = Mathf.Max(targetY, minY);

        // Plynulé přiblížení k cílové pozici
        float smoothY = Mathf.Lerp(transform.position.y, targetY, smoothSpeed * Time.deltaTime);

        // X zůstává fixní, Y sleduje playera, Z zůstává stejné (-10 pro 2D)
        transform.position = new Vector3(fixedX, smoothY, transform.position.z);
    }
}