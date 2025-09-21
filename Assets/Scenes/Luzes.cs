//using Unity.VisualScripting;
//using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Luzes : MonoBehaviour
{
    [SerializeField] private Light2D[] meuArray;
   


    private void Ligar()
    {
        foreach (Light2D light in meuArray)
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                light.enabled = !light.enabled;

                light.color = Random.ColorHSV();

                light.transform.position = new Vector3(Random.Range(1f, 10f), Random.Range(1f, 10f), Random.Range(1f, 10f));
            }


        }
    }

    private void Update()
    {
        Ligar();   

    }
}
