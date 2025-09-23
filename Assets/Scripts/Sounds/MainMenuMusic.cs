using UnityEngine;

public class MainMenuMusic : MonoBehaviour
{
    void Start()
    {
        SoundManager.Instance.PlayMusic("terrasArboreas", 1f, 15f);
    }

}
