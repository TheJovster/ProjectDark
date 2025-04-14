using System;
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HUDPopup : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI _textField;
    [SerializeField] private Button _closeButton;
    
    [Header("Animation")]
    [SerializeField] private float _fadeInTime = 0.3f;
    [SerializeField] private float _fadeOutTime = 0.3f;
    [SerializeField] private float _autoCloseTime = 0f; // 0 means no auto-close
    
    [Header("Position")]
    [SerializeField] private bool _useCustomPosition = false;
    [SerializeField] private Vector2 _customPosition;
    
    private string _popupId;
    private CanvasGroup _canvasGroup;
    private RectTransform _rectTransform;
    private Coroutine _autoCloseCoroutine;
    
    // Event that fires when popup is closed
    public event Action OnPopupClosed;
    
    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        _rectTransform = GetComponent<RectTransform>();
        
        // Set up close button if assigned
        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(Close);
        }
    }
    
    public void Initialize(string id, string text)
    {
        _popupId = id;
        SetText(text);
        
        if (_useCustomPosition && _rectTransform != null)
        {
            _rectTransform.anchoredPosition = _customPosition;
        }
        
        // Start visible animations
        gameObject.SetActive(true);
        _canvasGroup.alpha = 0f;
        StartCoroutine(FadeIn());
        
        // Start auto-close timer if set
        if (_autoCloseTime > 0)
        {
            _autoCloseCoroutine = StartCoroutine(AutoClose());
        }
    }
    
    public void SetText(string text)
    {
        if (_textField != null)
        {
            _textField.text = text;
        }
    }
    
    public void Close()
    {
        // Cancel auto-close if it's running
        if (_autoCloseCoroutine != null)
        {
            StopCoroutine(_autoCloseCoroutine);
            _autoCloseCoroutine = null;
        }
        
        StartCoroutine(FadeOut());
    }
    
    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        
        while (elapsed < _fadeInTime)
        {
            _canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / _fadeInTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        _canvasGroup.alpha = 1f;
    }
    
    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        
        while (elapsed < _fadeOutTime)
        {
            _canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / _fadeOutTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        _canvasGroup.alpha = 0f;
        
        // Notify any listeners that we've closed
        OnPopupClosed?.Invoke();
        
        // Destroy the popup GameObject
        Destroy(gameObject);
    }
    
    private IEnumerator AutoClose()
    {
        yield return new WaitForSeconds(_autoCloseTime);
        Close();
    }
}