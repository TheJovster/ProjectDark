using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SpawnableObject
{
	public string name;
	public GameObject prefab;
	[Range(0, 100)]
	public float spawnProbability = 50f;
}