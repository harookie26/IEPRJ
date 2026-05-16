using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class CorruptPaintingRandomizer : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private GameObject corruptedPaintingCover;
    [Tooltip("Place materials in order: Main1, Main2, Main3, Main4")]
    [SerializeField] private List<Material> corruptedPaintingsTexture;

    [Header("Layer Settings")]
    [Tooltip("Type the EXACT spelling of your layer here (e.g. Chanellable, Channelable, etc.)")]
    [SerializeField] private string channelableLayerName = "Channelable";

    [Header("Rooms")]
    [SerializeField] private List<GameObject> Room1paintings;
    [SerializeField] private List<GameObject> Room2paintings;
    [SerializeField] private List<GameObject> Room3paintings;
    [SerializeField] private List<GameObject> Room4paintings;
    [SerializeField] private List<GameObject> Room5paintings;

    [Header("Debug")]
    [SerializeField] private bool isDebugMode = false;

    private Dictionary<GameObject, Material> originalMaterials = new Dictionary<GameObject, Material>();

    private void Awake()
    {
        RandomizeCorruptedPaintings();
    }

    private void RandomizeCorruptedPaintings()
    {
        List<List<GameObject>> allRooms = new List<List<GameObject>>
        {
            Room1paintings, Room2paintings, Room3paintings,
            Room4paintings, Room5paintings
        };

        // 1. Initial Pass: Remember originals and turn off old covers
        foreach (List<GameObject> room in allRooms)
        {
            if (room == null) continue;
            foreach (GameObject painting in room)
            {
                if (painting == null) continue;

                Renderer renderer = painting.GetComponent<Renderer>();

                if (renderer != null && !originalMaterials.ContainsKey(painting))
                {
                    originalMaterials[painting] = renderer.sharedMaterial;
                }

                if (renderer != null)
                {
                    renderer.material = originalMaterials[painting];
                }

                Transform existingCover = painting.transform.parent != null ?
                    painting.transform.parent.Find(corruptedPaintingCover.name + "_" + painting.name) : null;

                if (existingCover != null)
                {
                    existingCover.gameObject.SetActive(false);
                }
            }
        }

        // 2. Shuffle the rooms to pick 4 random locations
        List<List<GameObject>> shuffledRooms = new List<List<GameObject>>(allRooms);
        ShuffleList(shuffledRooms);

        int corruptedCount = 4;
        List<GameObject> chosenPaintings = new List<GameObject>();
        int roomIndex = 0;

        while (chosenPaintings.Count < corruptedCount && roomIndex < shuffledRooms.Count)
        {
            List<GameObject> currentRoom = shuffledRooms[roomIndex];
            roomIndex++;

            // Filter out null entries first
            List<GameObject> validPaintings = currentRoom.FindAll(p => p != null);
            if (validPaintings.Count == 0) continue;

            // Shuffle valid paintings so we can try each one without bias
            ShuffleList(validPaintings);
            bool added = false;
            foreach (GameObject candidate in validPaintings)
            {
                if (!chosenPaintings.Contains(candidate))
                {
                    chosenPaintings.Add(candidate);
                    added = true;
                    break;
                }
            }

            if (!added && isDebugMode)
                Debug.LogWarning($"[CorruptPaintingRandomizer] All paintings in a room were already chosen (duplicates across rooms). Skipping room.");
        }

        if (isDebugMode)
            Debug.Log($"[CorruptPaintingRandomizer] Chose {chosenPaintings.Count} paintings out of target {corruptedCount}.");

        // NOTE: We no longer shuffle the materials! 
        // This ensures Index 0 = Main1, Index 1 = Main2, etc.

        // 3. Apply the corruption
        for (int i = 0; i < chosenPaintings.Count; i++)
        {
            GameObject painting = chosenPaintings[i];

            // Assign the corresponding material (Main1, Main2...)
            Renderer renderer = painting.GetComponent<Renderer>();
            if (renderer != null && i < corruptedPaintingsTexture.Count)
            {
                renderer.material = corruptedPaintingsTexture[i];
            }

            // Spawn the Cover as a Sibling
            GameObject assignedCover = null;
            Transform existingCover = painting.transform.parent != null ?
                painting.transform.parent.Find(corruptedPaintingCover.name + "_" + painting.name) : null;

            if (existingCover != null)
            {
                assignedCover = existingCover.gameObject;
                assignedCover.SetActive(true);
            }
            else if (corruptedPaintingCover != null)
            {
                assignedCover = Instantiate(corruptedPaintingCover, painting.transform.position, painting.transform.rotation, painting.transform.parent);
                assignedCover.name = corruptedPaintingCover.name + "_" + painting.name;
                assignedCover.SetActive(true);
            }

            if (assignedCover == null)
            {
                Debug.LogError($"[CorruptPaintingRandomizer] assignedCover is null for painting '{painting.name}'. Skipping — check that corruptedPaintingCover is assigned in the Inspector.");
                continue;
            }

            assignedCover.transform.rotation = Quaternion.Euler(0, assignedCover.transform.rotation.eulerAngles.y, assignedCover.transform.rotation.eulerAngles.z);

            // Add the Channelable Component
            PaintingChannelable pc = painting.GetComponent<PaintingChannelable>();
            if (pc == null)
            {
                pc = painting.AddComponent<PaintingChannelable>();
            }

            // --- REFLECTION BLOCK ---
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

            // Because we didn't shuffle the materials, Index i maps perfectly to paint(i+1)
            FieldInfo idField = typeof(PaintingChannelable).GetField("paintingId", flags);
            if (idField != null) idField.SetValue(pc, $"paint{i + 1}");
            // ------------------------

            // Set Tag and Layer
            painting.tag = "ChannelablePainting";

            // Use the string from the Inspector to avoid typo bugs 
            int layerIndex = LayerMask.NameToLayer(channelableLayerName);
            if (layerIndex != -1)
            {
                painting.layer = layerIndex;
            }
            else
            {
                Debug.LogError($"[CorruptPaintingRandomizer] Layer '{channelableLayerName}' does not exist! Please check spelling.");
            }

            if (isDebugMode)
            {
                string matName = (i < corruptedPaintingsTexture.Count && corruptedPaintingsTexture[i] != null) ? corruptedPaintingsTexture[i].name : "UnknownMaterial";
                Debug.Log($"[CorruptPaintingRandomizer] Corrupted {painting.name}. Material: {matName} | Assigned ID: paint{i + 1}");
            }
        }
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
}