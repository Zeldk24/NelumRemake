// VFXManager.cs - VERS�O ATUALIZADA PARA ADDRESSABLES

using UnityEngine;
using System.Collections.Generic;
// Adicione os usings para Addressables
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void PlayVfx(string vfxAddress, Vector3 position, Quaternion rotation)
    {
      
        if (string.IsNullOrEmpty(vfxAddress))
        {
            return;
        }

     
        AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(vfxAddress, position, rotation);

       
        handle.Completed += (op_handle) =>
        {
            if (op_handle.Status == AsyncOperationStatus.Succeeded)
            {
               
                GameObject instance = op_handle.Result;
                Destroy(instance, 1f); 

                /*[] Assim que o objeto � destruido, o sistema de Addressables j� consegue detectar que ele n�o ser� mais utilizado e assim
                     libera automaticamente a m�moria.
                 */

            }
            else
            {
                Debug.LogError($"Falha ao instanciar o VFX do endere�o: {vfxAddress}");
            }
        };
    }
}