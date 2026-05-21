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
    [SerializeField] private List<GameObject> Room1paintings;
    [SerializeField] private List<GameObject> Room2paintings;
    [SerializeField] private List<GameObject> Room3paintings;
    [SerializeField] private List<GameObject> Room4paintings;
    [SerializeField] private List<GameObject> Room5paintings;

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
            Room1paintings, Room2paintings, Room3paintings, Room4paintings, Room5paintings
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
        int corruptedCount = 4;
        activeChosenPaintings.Clear();

        List<List<GameObject>> allRooms = new List<List<GameObject>>
        {
            Room1paintings, Room2paintings, Room3paintings, Room4paintings, Room5paintings
        };

        List<List<GameObject>> shuffledRooms = new List<List<GameObject>>(allRooms);
        ShuffleList(shuffledRooms);

        // Pass 1
        foreach (var room in shuffledRooms)
        {
            if (activeChosenPaintings.Count >= corruptedCount) break;
            if (room == null || room.Count == 0) continue;

            List<GameObject> validPaintings = room.FindAll(p => p != null && !activeChosenPaintings.Contains(p));
            if (validPaintings.Count > 0)
            {
                ShuffleList(validPaintings);
                activeChosenPaintings.Add(validPaintings[0]);
            }
        }

        // Pass 2
        if (activeChosenPaintings.Count < corruptedCount)
        {
            List<GameObject> remainingPool = new List<GameObject>();
            foreach (var p in allPaintingsInLevel)
            {
                if (p != null && !activeChosenPaintings.Contains(p)) remainingPool.Add(p);
            }
            ShuffleList(remainingPool);

            foreach (var p in remainingPool)
            {
                if (activeChosenPaintings.Count >= corruptedCount) break;
                activeChosenPaintings.Add(p);
            }
        }
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
                renderer.material = corruptedPaintingsTexture[i];
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

            if (assignedCover != null) assignedCover.transform.rotation = Quaternion.Euler(0, assignedCover.transform.rotation.eulerAngles.y, assignedCover.transform.rotation.eulerAngles.z);

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
}