using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

[Serializable]
public class SerializableVector3
{
	public float x;
	public float y;
	public float z;

	public SerializableVector3()
	{
		x = 0;
		y = 0;
		z = 0;
	}

	public SerializableVector3(Vector3 vector)
	{
		x = vector.x;
		y = vector.y;
		z = vector.z;
	}

	public Vector3 ToVector3()
	{
		return new Vector3(x, y, z);
	}
}

[Serializable]
public class SaveData
{
	public PlayerData PlayerData { get; set; } = new PlayerData();
	public EnemyData EnemyData { get; set; } = new EnemyData();
	public GameProgressionData GameProgressionData { get; set; } = new GameProgressionData();
	public GameSettingsData GameSettingsData { get; set; } = new GameSettingsData();
}

[Serializable] //store player data
public class PlayerData
{
	public SerializableVector3 Position;
	public SerializableVector3 Rotation;
	public int CurrentHealth;
	public int MaxHealth; 
	public Dictionary<string, bool> WeaponInventory;
	public Dictionary<Weapon.WeaponType, int> AmmoInventory;
	public int CurrentWeapon;
}

[Serializable] //store enemy data
public class EnemyData
{
	public SerializableVector3 Position;
	public SerializableVector3 Rotation;
	public int CurrentHealth;
	public int MaxHealth;
	public bool IsAlive;
}

[Serializable] //store data regarding the player progression through the game
public class GameProgressionData
{
	public int CurrentLevelIndex = 0;
	public List<int> CompletedLevels = new List<int>();
	public float PlayTime = 0.0f;
	//add kill count?
	public string LastPlayedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
}

[Serializable]
public class GameSettingsData
{
	public float MusicVolume = 1.0f;
	public float SFXVolume = 1.0f;
	public float MouseSensitivity = 1.0f;
	public bool InvertYAxis = false;
	public int TargetFrameRate = 60;
}


public class SaveSystem : MonoBehaviour
{
	public static SaveSystem Instance;

	[Header("Save Configuration")] [SerializeField]
	private string _saveFileName = "savegame.json";

	private SaveData _saveData = new SaveData();

	private string _saveFilePath => Path.Combine(Application.persistentDataPath, _saveFileName);

	#region Properties

	public SaveData SaveData => _saveData;

	#endregion

	public event Action<SaveData> OnSaveCompleted;

	public event Action<SaveData> OnLoadCompleted;

	//debug events - I think it's safer is I use loggers for now.
	public event Action<string> OnSaveError;
	public event Action<string> OnLoadError;

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
	}

	private void Update()
	{
		if (GameManager.Instance.IsPlaying && GameManager.Instance.CurrentGameState != GameManager.GameState.Paused)
		{
			_saveData.GameProgressionData.PlayTime += Time.deltaTime;
		}
	}

	public SaveData GetSaveData()
	{
		return _saveData;
	}

	public void NewGame()
	{
		//more to do
		//will have to contiinue later - I am really tired.
		SaveGame();
	}

	public void SaveGame()
	{
		Debug.Log("Saving game...");
	}

	public void LoadGame()
	{
		Debug.Log("Loading game...");
	}
}
