using System;
using UnityEngine;

public class LevelIdentifier : MonoBehaviour
{
    [Header("Level Information")]
    [SerializeField] private string _levelName;
    [SerializeField] private int _levelIndex;
    
    #region Properties
    public string LevelName => _levelName;
    public int LevelIndex => _levelIndex;
    #endregion

    public void SetLevelInfo(string levelName, int levelIndex)
    {
        _levelName = levelName;
        _levelIndex = levelIndex;
    }
}
