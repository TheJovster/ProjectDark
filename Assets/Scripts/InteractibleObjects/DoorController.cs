using System.Collections;
using UnityEngine;

public class DoorController : MonoBehaviour
{
    [SerializeField] private Transform _doorTransform;
    [SerializeField] private bool _isOpen = false;
    [SerializeField] private float _openSpeed = 1.5f;
    [SerializeField] private AudioClip _openSound;
    [SerializeField] private AudioClip _interactSound;
    [SerializeField] private Vector3 _openPosition;
    private Vector3 _startPosition;

    private Coroutine _currentDoorRoutine;

    private void Start()
    {
        _startPosition = _doorTransform.localPosition;
    }

    public void OpenDoor()
    {
        if (_isOpen)
            return;
            
        if (_currentDoorRoutine != null)
            StopCoroutine(_currentDoorRoutine);
            
        _currentDoorRoutine = StartCoroutine(AnimateDoor(_startPosition, _openPosition));
        _isOpen = true;
    }

    public void CloseDoor()
    {
        if (!_isOpen)
            return;
            
        if (_currentDoorRoutine != null)
            StopCoroutine(_currentDoorRoutine);
            
        _currentDoorRoutine = StartCoroutine(AnimateDoor(_openPosition, _startPosition));
        _isOpen = false;
    }

    private IEnumerator AnimateDoor(Vector3 startPos, Vector3 endPos)
    {
        float time = 0;
        float duration = 1f / _openSpeed;
        
        while (time < duration)
        {
            _doorTransform.localPosition = Vector3.Lerp(startPos, endPos, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        
        _doorTransform.localPosition = endPos;
        _currentDoorRoutine = null;
    }

    public void PlayInteractSound()
    {
        AudioManager.Instance.PlayEffectDoubleVolume(_interactSound);
    }
    
    public void PlayOpenSound()
    {
        AudioManager.Instance.PlayEffectDoubleVolume(_openSound);
    }

    public void ToggleDoor()
    {
        PlayOpenSound();
        if (_isOpen)
            CloseDoor();
        else
            OpenDoor();
    }
}

