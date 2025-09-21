using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LevelNameManager : MonoBehaviour
{
    [SerializeField] private Animator prologueAnim;
    [SerializeField] private GameObject prologueObject;
    private Image imageText;
    void Start()
    {
        imageText = GetComponentInChildren<Image>();
        
        StartCoroutine(DelayPrologue());
    }

    private IEnumerator DelayPrologue()
    {
        imageText.color = new Color(1f, 1f, 1f, 0f);

        yield return new WaitForSeconds(2);

        float duration = 3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {

            elapsed += Time.deltaTime;
            imageText.color = new Color(1f, 1f, 1f, Mathf.Lerp(0f, 1f, elapsed / duration));
            yield return null;

            prologueAnim.SetBool("Play", true);
            imageText.color = new Color(1f, 1f, 1f, 1f);

        }

        imageText.color = new Color(1f, 1f, 1f, 1f);

        yield return new WaitForSeconds(2);

        duration = 2f;
        elapsed = 0f;

        while (elapsed < duration)
        {

            elapsed += Time.deltaTime;
            imageText.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0f, elapsed / duration));
            yield return null;

            prologueAnim.SetBool("Play", true);
            imageText.color = new Color(1f, 1f, 1f, 0f);

        }

        imageText.color = new Color(1f, 1f, 1f, 0f);

    }
}
