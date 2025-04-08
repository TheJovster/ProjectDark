using System.Collections;
using UnityEngine;

public class BloodDecal : MonoBehaviour
{
    [SerializeField] private float _fadeStartTime = 15.0f;
    [SerializeField] private float _lifetime = 30.0f;
    [SerializeField] private bool _randomRotation = true;
    [SerializeField] private Vector2 _randomScaleRange = new Vector2(0.8f, 1.2f);

    private Renderer _renderer;
    private Material _material;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();

        // Create a material instance to avoid affecting shared materials
        if (_renderer != null && _renderer.material != null)
        {
            _material = new Material(_renderer.material);
            _renderer.material = _material;
        }

        // Random rotation
        if (_randomRotation)
        {
            transform.rotation = Quaternion.Euler(
                transform.rotation.eulerAngles.x,
                transform.rotation.eulerAngles.y + Random.Range(0, 360f),
                transform.rotation.eulerAngles.z
            );
        }

        // Random scale
        float randomScale = Random.Range(_randomScaleRange.x, _randomScaleRange.y);
        transform.localScale *= randomScale;
    }

    private void Start()
    {
        // Start fade coroutine
        if (_fadeStartTime < _lifetime)
        {
            StartCoroutine(FadeOut());
        }

        // Destroy after lifetime
        Destroy(gameObject, _lifetime);
    }

    private IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(_fadeStartTime);

        if (_material != null)
        {
            // Store original color
            Color originalColor = _material.color;

            // Fade out over time
            float fadeDuration = _lifetime - _fadeStartTime;
            float elapsedTime = 0f;

            // Make sure material has proper rendering mode for transparency
            if (_material.HasProperty("_Mode"))
            {
                _material.SetFloat("_Mode", 2); // Fade mode
                _material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                _material.SetInt("_ZWrite", 0);
                _material.DisableKeyword("_ALPHATEST_ON");
                _material.EnableKeyword("_ALPHABLEND_ON");
                _material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                _material.renderQueue = 3000;
            }

            while (elapsedTime < fadeDuration)
            {
                float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);

                // Update color with new alpha
                Color color = originalColor;
                color.a = alpha;
                _material.color = color;

                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }
    }
}
