using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float xOffset = 5f;   // kamera před playerem
    [SerializeField] private float yOffset = 0f;
    [SerializeField] private float smoothSpeedY = 5f;
    [SerializeField] private float minY = 0f;

    private float fixedY;
    private bool followY = true;

    void Start()
    {
        fixedY = transform.position.y;
    }

    public void SetFollowY(bool follow)
    {
        followY = follow;
        if (!follow)
            fixedY = transform.position.y;
    }

    void LateUpdate()
    {
        if (player == null) return;

        // X — okamžitě bez Lerpu, žádné sekání
        float targetX = player.position.x + xOffset;

        float targetY;
        if (followY)
        {
            targetY = player.position.y + yOffset;
            targetY = Mathf.Max(targetY, minY);
            targetY = Mathf.Lerp(transform.position.y, targetY, smoothSpeedY * Time.deltaTime);
        }
        else
        {
            targetY = fixedY;
        }

        transform.position = new Vector3(targetX, targetY, transform.position.z);
    }
}