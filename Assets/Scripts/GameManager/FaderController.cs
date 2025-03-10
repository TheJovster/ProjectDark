using System;
using System.Collections;
using UnityEngine;

public class FaderController : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 0.5f;
    
    #region Properties
    
    public float FadeDuration => fadeDuration;

    #endregion
    
    private void Awake()
    {
        // Make sure this persists between scenes if needed
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
            
        // Start invisible
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
    }
    
    public IEnumerator FadeIn()
    {
        // Enable the canvas before fading
        canvasGroup.blocksRaycasts = true;
        
        float timeElapsed = 0;
        while (timeElapsed < fadeDuration)
        {
            canvasGroup.alpha = Mathf.Lerp(0, 1, timeElapsed / fadeDuration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        
        // Ensure we reach 1 exactly
        canvasGroup.alpha = 1;
    }
    
    public IEnumerator FadeOut()
    {
        float timeElapsed = 0;
        while (timeElapsed < fadeDuration)
        {
            canvasGroup.alpha = Mathf.Lerp(1, 0, timeElapsed / fadeDuration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        
        // Ensure we reach 0 exactly and disable blocking raycasts
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
    }
    
    // Utility method that returns after completing both fade operations
    public IEnumerator FadeInAndOut(float displayDuration = 0.5f)
    {
        yield return FadeIn();
        yield return new WaitForSeconds(displayDuration);
        yield return FadeOut();
    }
}
