using System;
using UnityEngine;

public class LevelTransition : MonoBehaviour
{
	[SerializeField] private int levelToLoad;
	
	private void OnTriggerEnter(Collider other)
	{
		LoadNewLevel(other);
	}

	private void LoadNewLevel(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			GameManager.Instance.LoadLevel(levelToLoad);
		}
	}
}
