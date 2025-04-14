using UnityEngine;
using TMPro;

public class TextPopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _textField;
    [SerializeField] private float _displayTime = 3f;
    [SerializeField] private float _fadeInTime = 0.5f;
    [SerializeField] private float _fadeOutTime = 0.5f;
    
    private CanvasGroup _canvasGroup;
    
    void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null && GetComponent<Canvas>() != null)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }
    
    void Start()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            StartCoroutine(FadeIn());
        }
    }
    
    public void SetText(string text)
    {
        if (_textField != null)
        {
            _textField.text = text;
        }
    }
    
    private System.Collections.IEnumerator FadeIn()
    {
        float elapsed = 0f;
        
        while (elapsed < _fadeInTime)
        {
            _canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / _fadeInTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        _canvasGroup.alpha = 1f;
        
        if (_displayTime > 0)
        {
            yield return new WaitForSeconds(_displayTime);
            StartCoroutine(FadeOut());
        }
    }
    
    private System.Collections.IEnumerator FadeOut()
    {
        float elapsed = 0f;
        
        while (elapsed < _fadeOutTime)
        {
            _canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / _fadeOutTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        _canvasGroup.alpha = 0f;
    }
}
