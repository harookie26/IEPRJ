using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;

public class CorruptPaintingRandomizer : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private GameObject corruptedPaintingCover;
    [Tooltip("Place materials in order: Main1, Main2, Main3, Main4")]
    [SerializeField] private List<Material> corruptedPaintingsTexture;

    [Header("Layer Settings")]
    [SerializeField] private string channelableLayerName = "Channelable";

    [Header("Rooms")]
    [SerializeField] private List<GameObject> Room1paintings; // Gallery 2 , 4 paintings
    [SerializeField] private List<GameObject> Room2paintings; // Gallery 3 , 3 paintings
    [SerializeField] private List<GameObject> Room3paintings; // Gallery 4 , 5 paintings
    [SerializeField] private List<GameObject> Room4paintings; // Office , 1 painting

    [Header("Debug")]
    [SerializeField] private bool isDebugMode = true;
    [SerializeField] private TextMeshProUGUI debugText;

    private Dictionary<GameObject, Material> originalMaterials = new Dictionary<GameObject, Material>();
    private List<GameObject> allPaintingsInLevel = new List<GameObject>();
    private List<GameObject> activeChosenPaintings = new List<GameObject>();

    private void Awake()
    {
        List<List<GameObject>> allRooms = new List<List<GameObject>>
        {
            Room1paintings, Room2paintings, Room3paintings, Room4paintings
        };

        foreach (var room in allRooms)
        {
            if (room != null)
            {
                foreach (var painting in room)
                {
                    if (painting != null && !allPaintingsInLevel.Contains(painting))
                    {
                        allPaintingsInLevel.Add(painting);

                        Renderer renderer = painting.GetComponent<Renderer>();
                        if (renderer != null && !originalMaterials.ContainsKey(painting))
                        {
                            originalMaterials[painting] = renderer.sharedMaterial;
                        }
                    }
                }
            }
        }
    }

    public void InitializeFresh()
    {
        if (isDebugMode) Debug.Log("[Randomizer] Fresh start! Running Randomizer.");
        RunRandomizer();
    }

    private void Update()
    {
        if (debugText != null)
        {
            debugText.text = GameObject.FindAnyObjectByType<PaintingChannelable>() != null ? "ChannelablePaintings Present: " + GameObject.FindObjectsOfType<PaintingChannelable>().Length : "No ChannelablePaintings found.";
        }
    }

    public void RunRandomizer()
    {
        Debug.Log($"[Randomizer] RunRandomizer called from: {System.Environment.StackTrace}");
        activeChosenPaintings.Clear();

        // Local helper function to safely pick 1 random painting from a given room
        void PickAndAddFromRoom(List<GameObject> room, string roomName)
        {
            if (room != null && room.Count > 0)
            {
                // Filter out any null references just to be safe
                List<GameObject> validPaintings = room.FindAll(p => p != null);
                if (validPaintings.Count > 0)
                {
                    ShuffleList(validPaintings);
                    activeChosenPaintings.Add(validPaintings[0]);
                    if (isDebugMode) Debug.Log($"[Randomizer] Picked {validPaintings[0].name} for {roomName}");
                }
                else
                {
                    Debug.LogWarning($"[Randomizer] {roomName} has no valid paintings!");
                }
            }
            else
            {
                Debug.LogWarning($"[Randomizer] {roomName} list is null or empty!");
            }
        }

        // Spawn Corrupted Painting 1 in Room 1
        PickAndAddFromRoom(Room1paintings, "Room 1");

        // Spawn Corrupted Painting 2 in Room 2
        PickAndAddFromRoom(Room2paintings, "Room 2");

        // Spawn Corrupted Painting 3 in Room 3
        PickAndAddFromRoom(Room3paintings, "Room 3");

        // Spawn Corrupted Painting 4 in Room 4
        PickAndAddFromRoom(Room4paintings, "Room 4");

        // Apply the corruption. 
        // Because of the order above:
        // activeChosenPaintings[0] gets corruptedPaintingsTexture[0]
        // activeChosenPaintings[1] gets corruptedPaintingsTexture[1]
        // etc.
        ApplyCorruptionToChosenList();
    }

    private void ApplyCorruptionToChosenList()
    {
        PaintbrushChanneller channeller = FindFirstObjectByType<PaintbrushChanneller>();
        Debug.Log($"[Randomizer] ApplyCorruptionToChosenList called. Completed IDs: {string.Join(", ", channeller?.completedPaintingIds ?? new System.Collections.Generic.HashSet<string>())}");

        for (int i = 0; i < activeChosenPaintings.Count; i++)
        {
            GameObject painting = activeChosenPaintings[i];
            string assignedID = $"paint{i + 1}";

            Renderer renderer = painting.GetComponent<Renderer>();
            if (renderer != null && i < corruptedPaintingsTexture.Count)
            {
                // 1. Get a copy of the current materials array
                Material[] materialsArray = renderer.materials;

                // 2. Ensure the painting actually has an Element 1 (index 1) slot allocated
                if (materialsArray.Length > 1)
                {
                    materialsArray[1] = corruptedPaintingsTexture[i]; // Set Element 1
                }
                else
                {
                    Debug.LogWarning($"[Randomizer] {painting.name} does not have an Element 1 slot in its Renderer!");
                    // Optional fallback: Overwrite Element 0 if Element 1 doesn't exist
                    materialsArray[0] = corruptedPaintingsTexture[i];
                }

                // 3. Assign the modified array back to the renderer
                renderer.materials = materialsArray;
            }

            GameObject assignedCover = null;
            Transform existingCover = painting.transform.parent != null ? painting.transform.parent.Find(corruptedPaintingCover.name + "_" + painting.name) : null;

            if (existingCover != null)
            {
                assignedCover = existingCover.gameObject;
                assignedCover.SetActive(true);
            }
            else if (corruptedPaintingCover != null)
            {
                assignedCover = Instantiate(corruptedPaintingCover, painting.transform.position, painting.transform.rotation, painting.transform.parent);
                assignedCover.name = corruptedPaintingCover.name + "_" + painting.name;
            }

            if (assignedCover != null)
            {
                // The painting is an FBX, so we need to tilt the cover 90 degrees 
                // relative to whatever rotation it just copied.
                // If it faces the wrong way, change 90f to -90f!
                assignedCover.transform.Rotate(90f, 0f, 0f, Space.Self);
            }

            PaintingChannelable pc = painting.GetComponent<PaintingChannelable>();
            if (pc == null) pc = painting.AddComponent<PaintingChannelable>();

            // Check if player already fixed this in a previous save!
            if (channeller != null && channeller.HasCompletedPainting(assignedID))
            {
                Transform completedCover = painting.transform.parent != null
                    ? painting.transform.parent.Find(corruptedPaintingCover.name + "_" + painting.name)
                    : null;
                if (completedCover != null) completedCover.gameObject.SetActive(false);

                painting.tag = "Untagged";
                painting.layer = 0;
                continue;
            }

            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            if (assignedCover != null)
            {
                FieldInfo coverField = typeof(PaintingChannelable).GetField("coverObject", flags);
                if (coverField != null) coverField.SetValue(pc, assignedCover);
            }

            FieldInfo durationField = typeof(PaintingChannelable).GetField("requiredChannelDuration", flags);
            if (durationField != null) durationField.SetValue(pc, 5f);

            FieldInfo pauseField = typeof(PaintingChannelable).GetField("pauseOnConsecutiveCompletions", flags);
            if (pauseField != null) pauseField.SetValue(pc, false);

            FieldInfo idField = typeof(PaintingChannelable).GetField("paintingId", flags);
            if (idField != null) idField.SetValue(pc, assignedID);

            painting.tag = "ChannelablePainting";

            int layerIndex = LayerMask.NameToLayer(channelableLayerName);
            if (layerIndex != -1) painting.layer = layerIndex;
        }
    }

    public void OnPaintingCompleted(GameObject painting)
    {
        painting.tag = "Untagged";
        painting.layer = 0;
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    // --- SAVE AND LOAD METHODS ---

    public PaintingRandomizerSaveData GetSaveData()
    {
        List<string> namesToSave = new List<string>();
        foreach (GameObject painting in activeChosenPaintings)
        {
            if (painting != null) namesToSave.Add(painting.name);
        }

        return new PaintingRandomizerSaveData
        {
            savedPaintingNames = namesToSave
        };
    }

    public void LoadSaveData(PaintingRandomizerSaveData data)
    {
        Debug.Log($"[Randomizer] LoadSaveData called from: {System.Environment.StackTrace}");

        if (data == null || data.savedPaintingNames == null || data.savedPaintingNames.Count == 0)
        {
            if (isDebugMode) Debug.Log("[Save System] No saved paintings found. Running fresh randomizer.");
            RunRandomizer();
            return;
        }

        if (isDebugMode) Debug.Log($"[Save System] Loaded {data.savedPaintingNames.Count} saved paintings!");

        activeChosenPaintings.Clear();

        // Because names are unique, this easily finds the exact 4 paintings!
        foreach (string savedName in data.savedPaintingNames)
        {
            GameObject foundObj = allPaintingsInLevel.Find(p => p != null && p.name == savedName);
            if (foundObj != null)
            {
                activeChosenPaintings.Add(foundObj);
            }
            else
            {
                Debug.LogWarning($"[Save System] Could not find painting named {savedName} in the level! Did you rename it?");
            }
        }

        ApplyCorruptionToChosenList();
    }

    public List<GameObject> GetActiveCorruptedPaintings()
    {
        return activeChosenPaintings;
    }

    public void ApplyCheckpointPhase(int phase)
    {
        int completedCount = Mathf.Clamp(phase - 5, 0, activeChosenPaintings.Count);

        for (int i = 0; i < activeChosenPaintings.Count; i++)
        {
            GameObject painting = activeChosenPaintings[i];

            bool completed = i < completedCount;

            SetPaintingCompletedState(painting, completed);
        }
    }

    private void SetPaintingCompletedState(GameObject painting, bool completed)
    {
        if (painting == null) return;

        Transform cover =
            painting.transform.parent.Find(
                corruptedPaintingCover.name + "_" + painting.name);

        if (completed)
        {
            if (cover != null)
                cover.gameObject.SetActive(false);

            painting.tag = "Untagged";
            painting.layer = 0;
        }
        else
        {
            if (cover != null)
                cover.gameObject.SetActive(true);

            painting.tag = "ChannelablePainting";

            int layerIndex =
                LayerMask.NameToLayer(channelableLayerName);

            if (layerIndex != -1)
                painting.layer = layerIndex;
        }
    }

    public Transform GetPaintingSpawnTransform(int index)
    {
        if (index < 0 || index >= activeChosenPaintings.Count)
            return null;

        return activeChosenPaintings[index]?.transform;
    }
}
