using System;
using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerInputHandler : MonoBehaviour
{

    public static PlayerInputHandler Instance { get; private set; }

    [Header("InputActionAsset")]
    [SerializeField] private InputActionAsset playerControls;

    [Header("Action Map Name Reference")]
    [SerializeField] private string actionMapName = "Gameplay";

    [Header("Action Name References")]
    [SerializeField] private string movement = "Movement";
    [SerializeField] private string look = "Looking";

    private InputAction moveAction;
    private InputAction lookAction;

    public InputAction MoveAction => moveAction;
    public InputAction LookAction => lookAction;

    public Vector2 moveInput { get; private set; }
    public Vector2 lookInput { get; private set; }


    void Start()
    {
        
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InputActionMap mapReference = playerControls.FindActionMap(actionMapName);
        moveAction = mapReference.FindAction(movement);
        lookAction = mapReference.FindAction(look);

        SubscribeToEvent();
    }

    private void SubscribeToEvent()
    {
        moveAction.performed += inputinfo => moveInput = inputinfo.ReadValue<Vector2>();
        moveAction.canceled += inputinfo => moveInput = Vector2.zero;

        lookAction.performed += inputinfo => lookInput = inputinfo.ReadValue<Vector2>();
        lookAction.canceled += inputinfo => lookInput = Vector2.zero;
    }

    private void OnEnable()
    {
        playerControls.FindActionMap(actionMapName).Enable();
    }

    private void OnDisable()
    {
        playerControls.FindActionMap(actionMapName).Disable();
    }
}
