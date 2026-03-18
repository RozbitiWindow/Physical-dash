using UnityEngine;

public class MusicManager : MonoBehaviour
{
    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void RestartMusic()
    {
        // Stop = zastaví hudbu
        // Play = spustí od začátku
        audioSource.Stop();
        audioSource.Play();
    }
}