using UnityEngine;

public class LevelScroller : MonoBehaviour
{
    [SerializeField]
    public float speed = 5f;
    // Rychlost pohybu světa. Změníš v Inspectoru.

    void Update()
    {
        // Každý frame posuneme objekt doleva
        // Time.deltaTime = čas od posledního framu
        // BEZ deltaTime by rychlost závisela na FPS — s ním je vždy stejná
        transform.position += Vector3.left * speed * Time.deltaTime;
    }
}