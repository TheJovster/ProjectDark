using System;
using UnityEngine;

public class InteractebleBooth : MonoBehaviour
{
	[SerializeField] private GameObject _testLight;
	private bool isActive = false;

	public void ToggleLight()
	{
		isActive = !isActive;
		
		_testLight.SetActive(isActive);
	}
}
