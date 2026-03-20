using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private float yOffset = 0f;
    [SerializeField] private float minY = 0f;

    private float fixedX;
    private float fixedY;
    private bool followY = true;

    void Start()
    {
        fixedX = transform.position.x;
        fixedY = transform.position.y;
    }

    public void SetFollowY(bool follow)
    {
        followY = follow;
        if (!follow)
        {
            // Zapamatujeme AKTUÁLNÍ Y kamery — ne playera
            fixedY = transform.position.y;
        }
    }

    void LateUpdate()
    {
        if (player == null) return;

        float targetY;

        if (followY)
        {
            targetY = player.position.y + yOffset;
            targetY = Mathf.Max(targetY, minY);
            targetY = Mathf.Lerp(transform.position.y, targetY, smoothSpeed * Time.deltaTime);
        }
        else
        {
            // Kamera stojí — ignorujeme pohyb playera
            targetY = fixedY;
        }

        transform.position = new Vector3(fixedX, targetY, transform.position.z);
    }
}