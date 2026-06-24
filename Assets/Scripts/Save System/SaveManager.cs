using System.Text;
using System.Threading.Tasks;
using Unity.PlatformToolkit;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    [Header("References")]
    public PlayerSaveHandler playerSaveHandler;

    private ISavingSystem savingSystem;
    private const string SAVE_FILE_NAME = "playerdata";

    // For testing purposes, we'll hardcode a default save slot if we aren't using the Courier
    private const string DEFAULT_SAVE_SLOT = "save1";

    async void Start()
    {
        GameState.EndCutscene();

        if (EventBroadcaster.Instance != null)
        {
            EventBroadcaster.Instance.PostEvent(EventNames.CutsceneEvents.CUTSCENE_END);
        }

        await InitializePlatformToolkit();

        if (!string.IsNullOrEmpty(SaveCourier.SaveSlotToLoad))
        {
            Debug.Log($"<color=cyan>Courier told us to load: {SaveCourier.SaveSlotToLoad}</color>");

            // 1. TURN THE SHIELD ON (Blocks all triggers)
            SaveCourier.IsLoadingSave = true;

            await LoadGameAsync(SaveCourier.SaveSlotToLoad);

            // 2. TURN THE SHIELD OFF (Player is now safely teleported)
            SaveCourier.IsLoadingSave = false;

            SaveCourier.SaveSlotToLoad = "";
        }
        else
        {
            Debug.Log("<color=green>Starting a fresh New Game.</color>");
            playerSaveHandler.InitializeNewGame();
        }
    }

    private async Task InitializePlatformToolkit()
    {
        // Wake up the API
        await PlatformToolkit.Initialize();

        // Check if the current device actually supports local saving
        if (PlatformToolkit.Capabilities.LocalSaving)
        {
            // Bypass accounts entirely and just grab the local disk saver!
            savingSystem = PlatformToolkit.LocalSaving;
            Debug.Log("<color=green>Platform Toolkit Ready: Local Saving Enabled.</color>");
        }
        else
        {
            Debug.LogError("Uh oh! Local saving is not supported on this specific platform.");
        }
    }

    void Update()
    {
        // Keep your hotkeys for testing!
        if (Input.GetKeyDown(KeyCode.F5))
        {
            // If the courier is empty, fallback to the default slot
            string slotToSave = string.IsNullOrEmpty(SaveCourier.SaveSlotToLoad) ? DEFAULT_SAVE_SLOT : SaveCourier.SaveSlotToLoad;
            SaveGameAsync(slotToSave);
        }
    }

    public void ToggleSaveGame(string slotToSave)
    {
        slotToSave = string.IsNullOrEmpty(SaveCourier.SaveSlotToLoad) ? DEFAULT_SAVE_SLOT : SaveCourier.SaveSlotToLoad;
        SaveGameAsync(slotToSave);
    }

    // --- WRITING TO LOCAL DISK ---
    public async void SaveGameAsync(string slotName)
    {
        if (savingSystem == null) return;

        GameSaveData data = playerSaveHandler.GetSaveData();
        GlobalSaveSystem.CaptureInto(data);

        string json = JsonUtility.ToJson(data);
        try
        {
            await using (var writeable = await savingSystem.OpenSaveWritable(slotName))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                await writeable.WriteFile(SAVE_FILE_NAME, bytes);
                await writeable.Commit();
            }
            Debug.Log($"<color=cyan>Game Saved locally to slot: {slotName}!</color>");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Save failed: {e.Message}");
        }
    }

    // --- READING FROM LOCAL DISK ---
    public async Task LoadGameAsync(string slotName)
    {
        if (savingSystem == null) return;

        try
        {
            string json = null;
            await using (var readable = await savingSystem.OpenSaveReadable(slotName))
            {
                byte[] bytes = await readable.ReadFile(SAVE_FILE_NAME);
                json = Encoding.UTF8.GetString(bytes);
            }

            if (!string.IsNullOrEmpty(json))
            {
                GameSaveData loadedData = JsonUtility.FromJson<GameSaveData>(json);
                GlobalSaveSystem.RestoreFrom(loadedData);
                playerSaveHandler.LoadSaveData(loadedData);

                Debug.Log($"<color=cyan>Game Loaded locally from slot: {slotName}!</color>");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Load failed (File might not exist yet): {e.Message}");
        }
    }
}
