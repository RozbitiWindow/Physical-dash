using UnityEngine;

public class WaveTrailFollower : MonoBehaviour
{
    [SerializeField] private Transform player;
    private TrailRenderer trail;

    void Start()
    {
        trail = GetComponent<TrailRenderer>();
        trail.enabled = false;
    }

    void LateUpdate()
    {
        if (!trail.enabled) return;
        // Sleduj pozici playeru každý frame
        transform.position = player.position;
    }

    public void EnableTrail()
    {
        trail.enabled = true;
    }

    public void DisableTrail()
    {
        trail.enabled = false;
        trail.Clear();
    }
}