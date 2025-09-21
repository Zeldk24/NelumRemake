using UnityEngine;

public class FogMaskController : MonoBehaviour
{
    public Transform playerTransform; // Arraste o Transform do seu objeto Player aqui

    void Update()
    {
        if (playerTransform != null)
        {
            // Envia a posi��o do jogador (em coordenadas de tela) para todos os shaders
            // O nome "_PlayerScreenPos" � a chave!
            Shader.SetGlobalVector("_PlayerScreenPos", Camera.main.WorldToViewportPoint(playerTransform.position));
        }
    }
}