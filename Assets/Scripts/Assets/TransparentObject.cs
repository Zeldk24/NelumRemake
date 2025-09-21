using System.Collections;
using UnityEngine;

public class TransparentObject : MonoBehaviour
{
    [Range(0, 1)]
    [SerializeField] private float transparencyValue = 0.7f;
    [SerializeField] private float transparencyFadeTime = 4f;

    private SpriteRenderer spriteRenderer;
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.GetComponent<PlayerController>())
        {
            StartCoroutine(FadeTree(spriteRenderer, transparencyFadeTime, spriteRenderer.color.a, transparencyValue));
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.GetComponent<PlayerController>())
        {
            StartCoroutine(FadeTree(spriteRenderer, transparencyFadeTime, spriteRenderer.color.a, 1f));
        }
    }

    private IEnumerator FadeTree(SpriteRenderer spriteTransparency, float fadeTime, float startValue, float targetTransparency)
    {
        float timeElapsed = 0;
        
        while (timeElapsed < fadeTime)
        {
            

            timeElapsed += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startValue, targetTransparency, timeElapsed / fadeTime);
            spriteTransparency.color = new Color(spriteTransparency.color.r, spriteTransparency.color.g, spriteTransparency.color.b, newAlpha);
            yield return null;
        }


    }

}
