using UnityEngine;

public class MusicManager : MonoBehaviour
{
    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.Play(); // spustí hudbu na startu
        }
    }

    public void RestartMusic()
    {
        // Stop = zastaví hudbu
        // Play = spustí od začátku
        audioSource.Stop();
        audioSource.Play();
    }
}