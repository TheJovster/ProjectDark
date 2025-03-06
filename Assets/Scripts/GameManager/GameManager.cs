
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [SerializeField]private GameState currentGameState;
    
    [SerializeField] private GameObject player;
    private GameObject playerInstance;
    [Header("Level Prefabs")]
    [SerializeField] private GameObject[] levelPrefabs;
    private GameObject currentLevelInstance = null;

    [Header("UI Elements")] 
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject loadingScreenPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Slider loadingProgressBar;
    [SerializeField] private Camera menuCamera;
    
    #region Properties

    public GameState CurrentGameState => currentGameState;
    
    #endregion
    
    public enum GameState
    {
        MainMenu,
        Loading,
        Playing,
        GameOver,
        Paused
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }

        SetGameState(GameState.MainMenu);
    }
    
    public void SetGameState(GameState newGameState)
    {
        currentGameState = newGameState;
        UpdateUIBasedOnGameState();
    }

    private void UpdateUIBasedOnGameState() //name is horrible - need to fix it
    {
        mainMenuPanel.SetActive(false);
        loadingScreenPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        pausePanel.SetActive(false);

        switch (currentGameState)
        {
            case GameState.MainMenu:
                mainMenuPanel.SetActive(true);
                Time.timeScale = 1f;
                ShowCursor();
                break;
            case GameState.Loading:
                loadingScreenPanel.SetActive(true);
                Time.timeScale = 1f;
                HideCursor();
                break;
            case GameState.Playing:
                Time.timeScale = 1f;
                HideCursor();
                break;
            case GameState.GameOver:
                gameOverPanel.SetActive(true);
                Time.timeScale = 1f;
                ShowCursor();
                break;
            case GameState.Paused: 
                pausePanel.SetActive(true); 
                Time.timeScale = 0f;
                break;
        }
    }

    public void LoadLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= levelPrefabs.Length)
        {
            Debug.LogError($"Invalid level index: {levelIndex}");
            return;
        }

        StartCoroutine(LoadLevelAsync(levelIndex));
    }

    private IEnumerator LoadLevelAsync(int levelIndex)
    {
        SetGameState(GameState.Loading);

        if (currentLevelInstance != null)
        {
            Destroy(currentLevelInstance);
        }

        float loadingProgress = 0f;
        while (loadingProgress < 1.0f)
        {
            loadingProgress += Time.deltaTime;
            loadingProgressBar.value = loadingProgress;
            yield return null;
        }

        currentLevelInstance = Instantiate(levelPrefabs[levelIndex]);
        playerInstance = Instantiate(player);
        menuCamera.GetComponent<AudioListener>().enabled = false; //this does not work properly I think - need to be careful with this one
        SetGameState(GameState.Playing);
    }
    
    public void ReturnToMainMenu()
    {
        // Destroy current level
        if (currentLevelInstance != null)
        {
            Destroy(currentLevelInstance);
        }

        // Set to main menu state
        if (playerInstance != null)
        {
            Destroy(playerInstance);
        }
        menuCamera.GetComponent<AudioListener>().enabled = true;
        SetGameState(GameState.MainMenu);
    }
    
    public void PauseGame()
    {
        if (CurrentGameState == GameState.Playing)
        {
            SetGameState(GameState.Paused);
            ShowCursor();
        }
    }

    public void ResumeGame()
    {
        if (CurrentGameState == GameState.Paused)
        {
            SetGameState(GameState.Playing);
            HideCursor();
        }
    }
    
    public void RestartLevel()
    {
        if (currentLevelInstance != null)
        {
            // Find the current level's index
            LevelIdentifier levelIdentifier = currentLevelInstance.GetComponent<LevelIdentifier>();
            
            if (levelIdentifier != null && levelIdentifier.LevelIndex >= 0)
            {
                // Reload the current level using its index
                LoadLevel(levelIdentifier.LevelIndex);
            }
            else
            {
                Debug.LogError("Cannot restart level: No valid level index found");
            }
        }
        else
        {
            Debug.LogWarning("No current level to restart");
        }
    }

    public void QuitApplication()
    {
        Application.Quit();
    }
    
    //general game UI
    
    public void HideCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void ShowCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }


}
