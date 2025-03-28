using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [SerializeField]private GameState currentGameState;

    [SerializeField] private GameObject playerPrefab;
    [Header("Level Prefabs")]
    [SerializeField] private GameObject[] levelPrefabs;
    
    private GameObject playerInstance;
    private PlayerController _playerController;
    private GameObject currentLevelInstance;
    
    [Header("UI Elements")] 
    [SerializeField] private FaderController fader;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject loadingScreenPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject playerHUD;
    [SerializeField] private Slider loadingProgressBar;
    [SerializeField] private Camera menuCamera;

    [Header("Loading Simulation")] //TODO: expand this so the loading is actual, not simulated;
    [SerializeField, Range(1f, 120f)] private float loadingTime = 10.0f;

    [Header("Audio Clips")] 
    [SerializeField] private AudioClip _mainMenuMusic;
    [SerializeField] private AudioClip _loadingScreenMusic;
    [SerializeField] private AudioClip _gameOverMusic;
    [SerializeField] private AudioClip[] _levelMusic;
    
    private AudioListener menuCameraListener;
    private bool isPlaying = false;
    
    #region Properties

    public GameState CurrentGameState => currentGameState;
    public FaderController Fader => fader;
    public bool IsPlaying => isPlaying;
    
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
        Application.targetFrameRate = 60;
        Application.runInBackground = true;
        
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }

        menuCameraListener = menuCamera.gameObject.GetComponent<AudioListener>();
    }

    private void Start()
    {
        SetGameState(GameState.MainMenu);
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
                AudioManager.Instance.PlaySoundtrack(_mainMenuMusic);
                isPlaying = false;
                ShowCursor();
                break;
            case GameState.Loading:
                loadingScreenPanel.SetActive(true);
                AudioManager.Instance.StopSoundtrack();
                AudioManager.Instance.PlaySoundtrack(_loadingScreenMusic);
                isPlaying = false;
                HideCursor();
                break;
            case GameState.Playing:
                Time.timeScale = 1f;
                isPlaying = true;
                playerHUD.SetActive(true);
                HideCursor();
                break;
            case GameState.GameOver:
                gameOverPanel.SetActive(true);
                isPlaying = false;
                ShowCursor();
                break;
            case GameState.Paused: 
                pausePanel.SetActive(true);
                isPlaying = false;
                break;
            
        }
    }

    public void StartNewGame()
    {
        StartCoroutine(StartNewGameRoutine());
    }

    public IEnumerator StartNewGameRoutine()
    {
        LoadLevel(0);
        yield return null;
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
        if (playerInstance != null &&
            currentLevelInstance != null)
        {
            SaveSystem.Instance.SaveGame();
        }
        // If not already faded in, fade to black
        if (fader && fader.CurrentAlpha < 0.99f)
        {
            yield return fader.FadeIn();
        }
        // Set loading state
        loadingProgressBar.value = 0.0f;
        SetGameState(GameState.Loading);

        if (fader && fader.CurrentAlpha >= 0.99f)
        {
            yield return fader.FadeOut();
        }
        // Clean up existing level/player
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
        
        // Simulate loading time with progress bar - I need to figure out how to do proper content loading without relying on addressables. 
        float loadTime = loadingTime; //naming issue - fix later
        float elapsedTime = 0f;
        if (menuCamera && menuCamera.GetComponent<AudioListener>())
        {
            menuCamera.GetComponent<AudioListener>().enabled = false;
        }
        //sounds
        currentLevelInstance = Instantiate(levelPrefabs[levelIndex]);
        playerInstance = Instantiate(playerPrefab);
        _playerController = playerInstance.GetComponent<PlayerController>();
        HUDManager.Instance.UpdateAmmoCount(_playerController.WeaponInventory.CurrentWeapon.GetCurrentAmmoInMag(), 
            _playerController.WeaponInventory.CurrentWeapon.GetCurrentAmmoInInventory());
        //enemies 
        
        while (elapsedTime < loadTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / loadTime;
            
            loadingProgressBar.value = progress;
            
            yield return null;
        }
        
        loadingProgressBar.value = 1.0f;
        
        yield return new WaitForSeconds(0.2f);
        
        // Fade back in to reveal the level
        if (fader && fader.CurrentAlpha < 0.99f)
        {
            yield return fader.FadeIn();
        }
        SetGameState(GameState.Playing);
        
        if (currentLevelInstance != null &&
            playerInstance != null)
        {
            SaveSystem.Instance.LoadGame();
        }
        AudioManager.Instance.PlayLevelMusic(_levelMusic[levelIndex]);
        if (fader.CurrentAlpha >= 0.99f)
        {
            yield return fader.FadeOut();
        }

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
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
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


