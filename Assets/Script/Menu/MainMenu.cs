using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("场景设置")]
    [SerializeField] private int gameSceneIndex = 1;

    public void StartGame()
    {
        SceneManager.LoadScene(gameSceneIndex);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("游戏已退出");
    }
}