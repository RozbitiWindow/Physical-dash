using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // Zavolá se když hráč klikne na Play tlačítko
    // Přiřadíme tuto funkci tlačítku v Inspectoru
    public void OnPlayClicked()
    {
        // Načteme scénu LevelSelect
        // "LevelSelect" = přesný název scény jak je v Build Settings
        SceneManager.LoadScene("LevelSelect");
    }
}