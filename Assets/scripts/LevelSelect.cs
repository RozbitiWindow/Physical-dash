using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelect : MonoBehaviour
{
    // Zavolá se při kliknutí na tlačítko levelu
    // Každé tlačítko předá jiný název scény
    public void LoadLevel(string levelName)
    {
        SceneManager.LoadScene(levelName);
    }

    // Zpět na hlavní menu
    public void BackToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}