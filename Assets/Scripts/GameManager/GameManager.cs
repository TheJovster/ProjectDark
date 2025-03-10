using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
public class GameManager : MonoBehaviour
{
    [System.Serializable]
    public class AddressableLevel
    {
        public string addressableKey;
        public int levelIndex;
        public string displayName;
    }
    
    public static GameManager Instance { get; private set; }
    [SerializeField]private GameState currentGameState;
    
    private GameObject playerInstance;
    private GameObject currentLevelInstance;
    
    [Header("Level References")]
    [SerializeField] private AddressableLevel[] levels;
    [SerializeField] private string playerAddressableKey = "Player";

    [Header("UI Elements")] 
    [SerializeField] private FaderController fader;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject loadingScreenPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject playerHUD;
    [SerializeField] private Slider loadingProgressBar;
    [SerializeField] private Camera menuCamera;

    private AudioListener menuCameraListener;
    
    #region Properties

    public GameState CurrentGameState => currentGameState;
    public FaderController Fader => fader;
    
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
        menuCameraListener = menuCamera.gameObject.GetComponent<AudioListener>();
    }
    
    public void SetGameState(GameState newGameState)
    {
        currentGameState = newGameState;
        UpdateUI();
    }

    private void UpdateUI()
    {
        mainMenuPanel.SetActive(false);
        loadingScreenPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        pausePanel.SetActive(false);
        playerHUD.SetActive(false);

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
                playerHUD.SetActive(true);
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
        if (levelIndex < 0 || levelIndex >= levels.Length)
        {
            Debug.LogError($"Invalid level index: {levelIndex}");
            return;
        }

        StartCoroutine(LoadLevelAddressable(levelIndex));
    }

     private IEnumerator LoadLevelAddressable(int levelIndex)
     { 
         fader.FadeIn();
         SetGameState(GameState.Loading);
        
        // Find the correct level reference
        AddressableLevel levelToLoad = null;
        foreach (var level in levels)
        {
            if (level.levelIndex == levelIndex)
            {
                levelToLoad = level;
                break;
            }

            fader.FadeOut();
        }
        
        if (levelToLoad == null)
        {
            Debug.LogError($"No configured level with index {levelIndex}");
            yield break;
        }



        // Cleanup
        if (currentLevelInstance != null)
        {
            Destroy(currentLevelInstance);
            currentLevelInstance = null;
        }
        
        if (playerInstance != null)
        {
            Destroy(playerInstance);
            playerInstance = null;
        }
        
        // Start the loading operations
        AsyncOperationHandle<GameObject> levelLoadHandle = 
            Addressables.LoadAssetAsync<GameObject>(levelToLoad.addressableKey);
        AsyncOperationHandle<GameObject> playerLoadHandle = 
            Addressables.LoadAssetAsync<GameObject>(playerAddressableKey);
        
        // Track loading progress
        float totalProgress = 0f;
        bool levelLoaded = false;
        bool playerLoaded = false;
        
        while (!levelLoaded || !playerLoaded)
        {
            // Update level load progress
            if (!levelLoaded)
            {
                if (levelLoadHandle.IsDone)
                {
                    levelLoaded = true;
                }
            }
            
            // Update player load progress
            if (!playerLoaded)
            {
                if (playerLoadHandle.IsDone)
                {
                    playerLoaded = true;
                }
            }
            
            // Calculate combined progress
            float levelProgress = levelLoaded ? 0.6f : levelLoadHandle.PercentComplete * 0.6f;
            float playerProgress = playerLoaded ? 0.4f : playerLoadHandle.PercentComplete * 0.4f;
            totalProgress = levelProgress + playerProgress;
            
            // Update Progress Bar
            loadingProgressBar.value = totalProgress;
            yield return null;
        }
        
        // if error loading
        if (levelLoadHandle.Status != AsyncOperationStatus.Succeeded || 
            playerLoadHandle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError("Failed to load one or more assets!");
            
            //Release handles
            Addressables.Release(levelLoadHandle);
            Addressables.Release(playerLoadHandle);
            
            ReturnToMainMenu();
            yield break;
        }
        
        // Instantiate the objects now that they're loaded
        // Give the UI a moment to show 100% progress
        loadingProgressBar.value = 1.0f;
        yield return fader.FadeIn();
        yield return new WaitForSeconds(0.5f);
        
        
        GameObject levelPrefab = levelLoadHandle.Result;
        GameObject playerPrefab = playerLoadHandle.Result;
        
        currentLevelInstance = Instantiate(levelPrefab);
        playerInstance = Instantiate(playerPrefab);
        
        // Add the LevelIdentifier component if it doesn't exist
        LevelIdentifier levelId = currentLevelInstance.GetComponent<LevelIdentifier>();
        if (levelId == null)
        {
            levelId = currentLevelInstance.AddComponent<LevelIdentifier>();
            levelId.SetLevelInfo(levelToLoad.displayName, levelToLoad.levelIndex);
        }
        
        // Disable Audio Listener on Main Menu Camera and set game state to Playing
        menuCameraListener.enabled = false;
        SetGameState(GameState.Playing);
        
        Addressables.Release(levelLoadHandle);
        Addressables.Release(playerLoadHandle);

        yield return fader.FadeOut();

    }
    public void ReturnToMainMenu()
    {
        StartCoroutine(ReturnToMainMenuRoutine());
        
    }
    
    
    private IEnumerator ReturnToMainMenuRoutine()
    {
        // Fade to black
        currentGameState = GameState.Playing;
        yield return fader.FadeIn();
    
        // Destroy level and player
        if (currentLevelInstance)
        {
            Destroy(currentLevelInstance);
            currentLevelInstance = null;
        }
    
        if (playerInstance)
        {
            Destroy(playerInstance);
            playerInstance = null;
        }
    
        // Enable menu camera audio
        menuCameraListener.enabled = true;
    
        // Set game state
        SetGameState(GameState.MainMenu);
    
        // Fade back in to show menu
        yield return fader.FadeOut();
        yield return null;
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
