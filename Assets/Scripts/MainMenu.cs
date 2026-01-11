using UnityEngine;
using UnityEngine.SceneManagement;
public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadSceneAsync("LevelOne");
    }
        public void GameControls()
    {
        SceneManager.LoadSceneAsync("Controls");
    }
            public void Home()
    {
        SceneManager.LoadSceneAsync("MainMenu");
    }
}
