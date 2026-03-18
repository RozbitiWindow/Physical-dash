using UnityEngine;

public class SawBlade : MonoBehaviour
{
    [Header("Rotace")]
    [SerializeField] private float rotationSpeed = 200f;
    // Stupně za sekundu — záporné = opačný směr

    void Update()
    {
        // Točíme se kolem osy Z (2D)
        // Space.Self = rotace relativně k sobě, ne ke světu
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime, Space.Self);
    }
}