using System;
using UnityEngine;

public class LevelIdentifier : MonoBehaviour
{
    [Header("Level Information")]
    [SerializeField] private GameObject _levelPrefab;
    [SerializeField] private string _levelName;
    [SerializeField] private int _levelIndex;
    
    
    #region Properties
    public GameObject LevelPrefab => _levelPrefab;
    public string LevelName => _levelName;
    public int LevelIndex => _levelIndex;
    #endregion

    private void Awake()
    {
        _levelPrefab = this.gameObject;
    }
}
