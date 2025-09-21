using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class HeartUIManager : MonoBehaviour
{
    public GameObject heartPrefab;
    public Transform heartContainer;
    public Sprite fullHeart;
    public Sprite emptyHeart;

    private List<GameObject> hearts = new List<GameObject>();

    public void UpdateHearts(int currentHealth, int maxHealth)
    {
        // Calcula quantos cora��es ser�o necess�rios
        int heartCount = maxHealth;

        // Instancia cora��es se necess�rio
        while (hearts.Count < heartCount)
        {
            GameObject heart = Instantiate(heartPrefab, heartContainer);
            hearts.Add(heart);
        }

        // Atualiza os sprites dos cora��es
        for (int i = 0; i < hearts.Count; i++)
        {
            Image img = hearts[i].GetComponent<Image>();

            if (i < currentHealth)
                img.sprite = fullHeart;
            else
                img.sprite = emptyHeart;

            // Garante que o cora��o est� ativo (caso tenha passado a vida e depois voltado)
            img.enabled = i < heartCount;
        }
    }
}
