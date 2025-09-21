using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
public class InputManager : MonoBehaviour
{
    
    [SerializeField] private InputActionAsset inputActionsAssets;

    private InputAction movementInput;

    private void Awake()
    {

        //[] Encontra os Mapas de ação no PlayerInputsActions
        var playerMap = inputActionsAssets.FindActionMap("Player");

        movementInput = playerMap.FindAction("Movement");

    }

    public Vector2 MovementInput()
    {
        return movementInput.ReadValue<Vector2>();
    }

    public void DisableMovement()
    {
        inputActionsAssets.FindActionMap("Player").Disable();
    }

    public void EnableMovement()
    {
        inputActionsAssets.FindActionMap("Player").Enable();
    }
}
