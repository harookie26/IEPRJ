using System.Collections.Generic;
using UnityEngine;

public class PlayerSaveHandler : MonoBehaviour
{
    [Header("References")]
    public PlayerMovement movement;
    public PlayerStateMachine stateMachine;
    public Flashlight flashlight;
    public LockedDoorInteractable lockedDoor;
    public DrawerInteractable drawer;
    public PlayerCollectibleManager collectibleManager;
    public DialogueTriggerManager dialogueTriggerManager;
    public InteractionTrigger enemyIntroTrigger;
    public HintManager hintManager;
    public TutorialManager tutorialManager;
    public EnemyStateMachine enemyStateMachine; // Add this reference for the enemy state machine
    public PaintbrushChanneller paintbrushChanneller;
    public CorruptPaintingRandomizer corruptedPaintingsRandomizer;

    private void Awake()
    {
        // Auto-assign references if they are on the same GameObject
        if (movement == null) movement = GetComponent<PlayerMovement>();
        if (stateMachine == null) stateMachine = GetComponent<PlayerStateMachine>();
        if (flashlight == null) flashlight = GetComponentInChildren<Flashlight>();
        if (drawer == null) drawer = GetComponent<DrawerInteractable>();
        if (lockedDoor == null) lockedDoor = GetComponent<LockedDoorInteractable>();
        if (collectibleManager == null) collectibleManager = GetComponent<PlayerCollectibleManager>();
        if (dialogueTriggerManager == null) dialogueTriggerManager = GetComponent<DialogueTriggerManager>();
        if (enemyIntroTrigger == null) enemyIntroTrigger = GetComponent<InteractionTrigger>();
        if (enemyStateMachine == null) enemyStateMachine = GetComponent<EnemyStateMachine>();
        if (paintbrushChanneller == null) paintbrushChanneller = GetComponent<PaintbrushChanneller>();
        if (corruptedPaintingsRandomizer == null) corruptedPaintingsRandomizer = GetComponent<CorruptPaintingRandomizer>();
    }

    public void InitializeNewGame()
    {
        corruptedPaintingsRandomizer.InitializeFresh();
    }

    // Gathers data from all scripts into one package
    public GameSaveData GetSaveData()
    {
        return new GameSaveData
        {
            position = movement.transform.position,
            // You can safely grab Yaw and Pitch from your PlayerMovement script
            bodyYaw = movement.transform.eulerAngles.y,
            cameraPitch = movement.CameraPitch,

            // Get data from other managers
            collectedIds = new List<string>(collectibleManager.CollectedIds),
            playerState = stateMachine.CurrentState,
            flashlight = flashlight.GetSaveData(),
            lockeddoorState = lockedDoor.GetSaveData(),
            drawerState = drawer.GetSaveData(),
            dialogue = dialogueTriggerManager.GetSaveData(),
            enemyIntro = enemyIntroTrigger.GetSaveData(),
            hint = hintManager.GetSaveData(),
            tutorial = tutorialManager.GetSaveData(),
            //enemyState = enemyStateMachine.GetSaveData(), // Get enemy state data
            paintbrush = paintbrushChanneller.GetSaveData(), // Get paintbrush state data
            randomizedPaintings = corruptedPaintingsRandomizer.GetSaveData() // Get randomized paintings data
        };
    }

    // Distributes the package back to all the scripts
    public void LoadSaveData(GameSaveData data)
    {
        Debug.Log($"[Master Save] PlayerSaveHandler triggered. Is data null? {data == null}");

        if (data == null)
        {
            corruptedPaintingsRandomizer.InitializeFresh();
            return;
        }

        // 1. MOVE DIALOGUE TO THE TOP FOR TESTING
        Debug.Log("[Master Save] Attempting to load Dialogue...");
        dialogueTriggerManager.LoadSaveData(data.dialogue);
        Debug.Log("[Master Save] Dialogue loaded successfully. Moving to next scripts...");

        // 2. The rest of the scripts
        movement.LoadSaveData(data.position, data.bodyYaw, data.cameraPitch);
        collectibleManager.LoadSaveData(data.collectedIds);
        stateMachine.LoadSaveData(data.playerState);
        flashlight.LoadSaveData(data.flashlight);
        lockedDoor.LoadSaveData(data.lockeddoorState);
        drawer.LoadSaveData(data.drawerState);
        enemyIntroTrigger.LoadSaveData(data.enemyIntro);
        hintManager.LoadSaveData(data.hint);
        tutorialManager.LoadSaveData(data.tutorial);
        enemyStateMachine.LoadSaveData(data.enemyState);
        paintbrushChanneller.LoadSaveData(data.paintbrush);
        corruptedPaintingsRandomizer.LoadSaveData(data.randomizedPaintings);
        Debug.Log("[Master Save] ALL player data loaded successfully!");
    }
}