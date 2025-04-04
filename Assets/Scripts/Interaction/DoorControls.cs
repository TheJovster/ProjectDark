using UnityEngine;

public class DoorControls : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform _doorTransform;
    [SerializeField] private bool _isOpen = false;
    [SerializeField] private float _openSpeed = 30.0f;
    [SerializeField] private Vector3 _desiredPosition;
    
    
    public void Interact()
    {
        if (!_isOpen)
        {
            _isOpen = !_isOpen;
            _doorTransform.Translate(0f, _desiredPosition.y, 0.0f);
        }
    }
}
