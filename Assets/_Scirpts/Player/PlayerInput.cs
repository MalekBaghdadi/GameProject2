using UnityEngine;

/// <summary>
/// Handles all player input and communicates it to the PlayerController.
/// Separates input detection from movement logic.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerInput : MonoBehaviour
{
    private PlayerController playerController;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        
        if (playerController == null)
        {
            Debug.LogError("PlayerInput: PlayerController component not found!");
        }
    }

    private void Update()
    {
        HandleMovementInput();
        HandleSprintInput();
    }

    private void HandleMovementInput()
    {
        // Get WASD / Arrow Key input
        float horizontal = Input.GetAxisRaw("Horizontal"); // A/D or Left/Right arrows
        float vertical = Input.GetAxisRaw("Vertical");     // W/S or Up/Down arrows
        
        Vector2 inputVector = new Vector2(horizontal, vertical);
        
        // Send to PlayerController
        playerController.HandleMoveInput(inputVector);
    }

    private void HandleSprintInput()
    {
        // Check if Left Shift is being held
        bool isSprinting = Input.GetKey(KeyCode.LeftShift);
        
        // Send to PlayerController
        playerController.HandleSprint(isSprinting);
    }
}