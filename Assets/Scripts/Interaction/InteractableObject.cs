using UnityEngine;
using UnityEngine.Events;

public class InteractableObject : MonoBehaviour, IInteractable
{
	[Header("Interaction Settings")]
	[SerializeField] private GameObject _popupPrefab;
	[SerializeField] private string _interactionPrompt = "Press E to interact";
	[SerializeField] private string _uniqueID = ""; // Can be set in inspector or generated
    
	[Header("Events")]
	[SerializeField] private UnityEvent _onInteractStart;
	[SerializeField] private UnityEvent _onInteractEnd;
	[SerializeField] private UnityEvent _onInteractTrigger;
    
	private void Awake()
	{
		// Generate a unique ID if none is provided
		if (string.IsNullOrEmpty(_uniqueID))
		{
			_uniqueID = $"{gameObject.name}_{GetInstanceID()}";
		}
	}
    
	public void OnInteractStart()
	{
		Debug.Log($"Started interaction with {gameObject.name}");
		_onInteractStart?.Invoke();
	}

	public void OnInteractEnd()
	{
		Debug.Log($"Ended interaction with {gameObject.name}");
		_onInteractEnd?.Invoke();
	}

	public void OnInteractTrigger()
	{
		Debug.Log($"Triggered interaction with {gameObject.name}");
		_onInteractTrigger?.Invoke();
	}

	public GameObject GetPopupPrefab()
	{
		return _popupPrefab;
	}
    
	// Added getter for interaction prompt
	public string GetInteractionPrompt()
	{
		return _interactionPrompt;
	}
    
	// Implementation of GetUniqueID from IInteractable
	public string GetUniqueID()
	{
		return _uniqueID;
	}
}