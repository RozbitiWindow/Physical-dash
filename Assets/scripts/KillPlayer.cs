using UnityEngine;
using UnityEngine.SceneManagement;
// SceneManagement = knihovna pro práci se scénami (načítání, restart)

public class KillPlayer : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    // OnTriggerEnter2D se zavolá když se dotkne objekt s tagem "Player"
    // Rozdíl od OnCollisionEnter2D:
    //   Collision = fyzická srážka (objekty se odrazí)
    //   Trigger   = průchod skrz (detekujeme dotyk ale neodrážíme)
    {
        if (other.CompareTag("Player"))
        {
            // Znovu načte aktuální scénu = restart
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}