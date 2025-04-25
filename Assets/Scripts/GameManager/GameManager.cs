using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [SerializeField]private GameState _currentGameState;

    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private GameObject _minimapCamera;
    
    [Header("Level Prefabs")]
    [SerializeField] private GameObject[] _levelPrefabs;
    
    private GameObject _playerInstance;
    private GameObject _minimapCameraInstance;
    private PlayerController _playerController;
    private GameObject _currentLevelInstance;
    private Transform _currentLevelPlayerSpawnPoint;
    
    [FormerlySerializedAs("fader")]
    [Header("UI Elements")] 
    [SerializeField] private FaderController _fader;
    [SerializeField] private GameObject _mainMenuPanel;
    [SerializeField] private GameObject _loadingScreenPanel;
    [SerializeField] private GameObject _gameOverPanel;
    [SerializeField] private GameObject _pausePanel;
    [SerializeField] private GameObject _playerHUD;
    [SerializeField] private Slider _loadingProgressBar;
    [SerializeField] private Camera _menuCamera;

    [Header("UI Elements - Player")] 
    [SerializeField] private Image _healthBarImage;
    [SerializeField] private Image _staminaBarImage;
    
    [Header("Loading Simulation")] //TODO: expand this so the loading is actual, not simulated;
    [SerializeField, Range(1f, 120f)] private float _loadingTime = 10.0f;

    [Header("Audio Clips")] 
    [SerializeField] private AudioClip _mainMenuMusic;
    [SerializeField] private AudioClip _loadingScreenMusic;
    [SerializeField] private AudioClip _gameOverMusic;
    [SerializeField] private AudioClip[] _levelMusic;
    
    private AudioListener _menuCameraListener;
    private bool _isPlaying = false;
    
    #region Properties

    public GameState CurrentGameState => _currentGameState;
    public FaderController Fader => _fader;
    public bool IsPlaying => _isPlaying;
    
    public Transform PlayerInstance => _playerInstance.transform;
    
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

        _menuCameraListener = _menuCamera.gameObject.GetComponent<AudioListener>();
    }

    private void Start()
    {
        SetGameState(GameState.MainMenu);
    }
    
    public void SetGameState(GameState newGameState)
    {
        _currentGameState = newGameState;
        UpdateUI();
    }

    private void UpdateUI()
    {
        _mainMenuPanel.SetActive(false);
        _loadingScreenPanel.SetActive(false);
        _gameOverPanel.SetActive(false);
        _pausePanel.SetActive(false);
        _playerHUD.SetActive(false);

        switch (_currentGameState)
        {
            case GameState.MainMenu:
                _mainMenuPanel.SetActive(true);
                AudioManager.Instance.PlaySoundtrack(_mainMenuMusic);
                _isPlaying = false;
                ShowCursor();
                break;
            case GameState.Loading:
                _loadingScreenPanel.SetActive(true);
                AudioManager.Instance.StopSoundtrack();
                AudioManager.Instance.PlaySoundtrack(_loadingScreenMusic);
                _isPlaying = false;
                HideCursor();
                break;
            case GameState.Playing:
                Time.timeScale = 1f;
                _isPlaying = true;
                _playerHUD.SetActive(true);
                HideCursor();
                break;
            case GameState.GameOver:
                _gameOverPanel.SetActive(true);
                _isPlaying = false;
                ShowCursor();
                break;
            case GameState.Paused: 
                _isPlaying = false;
                _pausePanel.SetActive(true);
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
        if (levelIndex < 0 || levelIndex >= _levelPrefabs.Length)
        {
            Debug.LogError($"Invalid level index: {levelIndex}");
            return;
        }
        StartCoroutine(LoadLevelAsync(levelIndex));
    }

     
    private IEnumerator LoadLevelAsync(int levelIndex)
    {
        if (_playerInstance != null &&
            _currentLevelInstance != null)
        {
            SaveSystem.Instance.SaveGame();
        }
        // If not already faded in, fade to black
        if (_fader && _fader.CurrentAlpha < 0.99f)
        {
            yield return _fader.FadeIn();
        }
        // Set loading state
        _loadingProgressBar.value = 0.0f;
        SetGameState(GameState.Loading);

        if (_fader && _fader.CurrentAlpha >= 0.99f)
        {
            yield return _fader.FadeOut();
        }
        // Clean up existing level/player
        if (_currentLevelInstance != null) 
        {
            Destroy(_currentLevelInstance);
            _currentLevelInstance = null;
        }
        
        if (_playerInstance != null) 
        {
            Destroy(_playerInstance);
            _playerInstance = null;
        }
        
        // Simulate loading time with progress bar - I need to figure out how to do proper content loading without relying on addressables. 
        float loadTime = _loadingTime; //naming issue - fix later
        float elapsedTime = 0f;
        if (_menuCamera && _menuCamera.GetComponent<AudioListener>())
        {
            _menuCamera.GetComponent<AudioListener>().enabled = false;
        }
        //sounds
        _currentLevelInstance = Instantiate(_levelPrefabs[levelIndex]);
        _currentLevelPlayerSpawnPoint = _currentLevelInstance.GetComponent<LevelIdentifier>().PlayerSpawnLocation;
        _playerInstance = Instantiate(_playerPrefab, _currentLevelPlayerSpawnPoint.position, Quaternion.identity);
        _playerController = _playerInstance.GetComponent<PlayerController>();
        _minimapCameraInstance = Instantiate(_minimapCamera, new Vector3(0f, 0f, 0f), Quaternion.identity);
        _minimapCameraInstance.transform.SetParent(_currentLevelInstance.transform);
        _minimapCameraInstance.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        _minimapCameraInstance.GetComponent<MinimapFollowCamera>().SetPlayer(_playerInstance);
        HUDManager.Instance.UpdateAmmoCount(_playerController.WeaponInventory.CurrentWeapon.GetCurrentAmmoInMag(), 
            _playerController.WeaponInventory.CurrentWeapon.GetCurrentAmmoInInventory());
        HUDManager.Instance.UpdateWeaponName(_playerController.WeaponInventory.CurrentWeapon.WeapoonName);
        HUDManager.Instance.ToggleFireModeIcon(_playerController.WeaponInventory.CurrentWeapon.IsSemi);
        SetHealthBarFill(_playerInstance.GetComponent<Stats>().CurrentHealth,
            _playerInstance.GetComponent<Stats>().MaxHealth);
        SetStaminaBarFill(_playerInstance.GetComponent<Stats>().CurrentStamina, 
            _playerInstance.GetComponent<Stats>().MaxStamina);
        //enemies 
        
        while (elapsedTime < loadTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / loadTime;
            
            _loadingProgressBar.value = progress;
            
            yield return null;
        }
        
        _loadingProgressBar.value = 1.0f;
        
        yield return new WaitForSeconds(0.2f);
        
        // Fade back in to reveal the level
        if (_fader && _fader.CurrentAlpha < 0.99f)
        {
            yield return _fader.FadeIn();
        }
        SetGameState(GameState.Playing);
        
        if (_currentLevelInstance != null &&
            _playerInstance != null)
        {
            SaveSystem.Instance.LoadGame();
        }
        AudioManager.Instance.PlayLevelMusic(_levelMusic[levelIndex]);
        if (_fader.CurrentAlpha >= 0.99f)
        {
            yield return _fader.FadeOut();
        }

    }
    
    public void ReturnToMainMenu()
    {
        StartCoroutine(ReturnToMainMenuRoutine());
    }

    public void SetHealthBarFill(float currentHealth, float maxHealth)
    {
        _healthBarImage.fillAmount = currentHealth / maxHealth;
    }

    public void SetStaminaBarFill(float currentStamina, float maxStamina)
    {
        _staminaBarImage.fillAmount = currentStamina / maxStamina;
    }
    
    private IEnumerator ReturnToMainMenuRoutine()
    {
        // Fade to black
        _currentGameState = GameState.Loading;
        yield return _fader.FadeIn();
    
        // Destroy level and player
        if (_currentLevelInstance)
        {
            Destroy(_currentLevelInstance);
            _currentLevelInstance = null;
        }
    
        if (_playerInstance)
        {
            Destroy(_playerInstance);
            _playerInstance = null;
        }

        if (_minimapCameraInstance)
        {
            Destroy(_minimapCameraInstance);
            _minimapCameraInstance = null;
        }
    
        // Enable menu camera audio
        _menuCameraListener.enabled = true;
    
        // Set game state
        SetGameState(GameState.MainMenu);
    
        // Fade back in to show menu
        yield return _fader.FadeOut();
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
        if (_currentLevelInstance != null)
        {
            // Find the current level's index
            LevelIdentifier levelIdentifier = _currentLevelInstance.GetComponent<LevelIdentifier>();
            
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


