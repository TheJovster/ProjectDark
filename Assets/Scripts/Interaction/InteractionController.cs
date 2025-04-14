using UnityEngine;

public class InteractionController : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float _interactionDistance = 3f;
    [SerializeField] private LayerMask _interactableLayers;
    [SerializeField] private KeyCode _interactKey = KeyCode.E;

    private Camera _playerCamera;
    private IInteractable _currentInteractable;
    private string _currentPopupId; // ID for tracking the active popup

    void Start()
    {
        _playerCamera = GetComponentInChildren<Camera>();
        if (_playerCamera == null)
        {
            _playerCamera = Camera.main;
        }
    }

    void Update()
    {
        CheckForInteractable();
        
        // Handle interaction input
        if (_currentInteractable != null && Input.GetKeyDown(_interactKey))
        {
            _currentInteractable.OnInteractTrigger();
        }
    }

    private void CheckForInteractable()
    {
        Ray ray = new Ray(_playerCamera.transform.position, _playerCamera.transform.forward);
        RaycastHit hit;
        
        // Debug ray
        Debug.DrawRay(ray.origin, ray.direction * _interactionDistance, Color.yellow);

        if (Physics.Raycast(ray, out hit, _interactionDistance, _interactableLayers))
        {
            // Try to get an interactable component from the hit object
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            
            if (interactable != null)
            {
                // If we're looking at a different interactable than before
                if (_currentInteractable != interactable)
                {
                    // End interaction with previous interactable
                    if (_currentInteractable != null)
                    {
                        EndInteraction();
                    }
                    
                    // Start interaction with new interactable
                    _currentInteractable = interactable;
                    StartInteraction();
                }
                
                return;
            }
        }
        
        // If we reach here, we're not looking at any interactable
        if (_currentInteractable != null)
        {
            EndInteraction();
        }
    }

    private void StartInteraction()
    {
        _currentInteractable.OnInteractStart();
        
        // Get popup prefab from interactable
        GameObject popupPrefab = _currentInteractable.GetPopupPrefab();
        InteractableObject interactObj = _currentInteractable as InteractableObject;
        
        if (popupPrefab != null && HUDManager.Instance != null)
        {
            // Generate a unique ID for this interaction
            _currentPopupId = $"interact_{_currentInteractable.GetUniqueID()}";
            
            // Get interaction text from InteractableObject if available
            string interactionText = interactObj != null ? interactObj.GetInteractionPrompt() : "Press E to interact";
            
            // Show the popup through HUDManager
            HUDManager.Instance.ShowPopup(_currentPopupId, interactionText, popupPrefab);
        }
    }

    private void EndInteraction()
    {
        _currentInteractable.OnInteractEnd();
        
        // Hide popup through HUDManager
        if (HUDManager.Instance != null && !string.IsNullOrEmpty(_currentPopupId))
        {
            HUDManager.Instance.HidePopup(_currentPopupId);
            _currentPopupId = null;
        }
        
        _currentInteractable = null;
    }
}



