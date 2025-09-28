using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[FoldableInspector]
public class PaintingChannelableAutoAttach : MonoBehaviour
{
    [Tooltip("Tag used to identify painting GameObjects. Leave empty to match any GameObject that has a Collider/Collider2D.")]
    [SerializeField] private string paintingTag = "ChannelablePainting";

    [Tooltip("If TRUE, include inactive GameObjects in the scan.")]
    [SerializeField] private bool includeInactive = true;

    [Tooltip("If > 0, manager will periodically rescan the scene every N seconds to handle dynamically spawned paintings. 0 = only scan once on Awake.")]
    [SerializeField] private float rescanInterval = 0f;

    [Tooltip("Only attach to GameObjects that have at least one of these components (helps avoid accidentally attaching to unrelated objects).")]
    [SerializeField] private bool requireCollider = true;

    [Header("Individual Component Settings")]
    [Tooltip("Seconds required to consider the channeling 'complete'")]
    [SerializeField] private float requiredChannelDuration = 2f;

    [Tooltip("If TRUE, pause the game when the object has been completed consecutively this many times.")]
    [SerializeField] private bool pauseOnConsecutiveCompletions = true;

    [Tooltip("Number of consecutive completions required to trigger pause.")]
    [SerializeField] private int consecutiveCompletionsToPause = 2;

    [Tooltip("If TRUE, apply the above settings to existing PaintingChannelable components found on matching GameObjects as well.")]
    [SerializeField] private bool applyToExistingComponents = true;

    private float rescanTimer;

    private void Awake()
    {
        DoScan();
        if (rescanInterval > 0f) rescanTimer = rescanInterval;
    }

    private void Update()
    {
        if (rescanInterval <= 0f) return;

        rescanTimer -= Time.unscaledDeltaTime;
        if (rescanTimer <= 0f)
        {
            rescanTimer = rescanInterval;
            DoScan();
        }
    }

    private void DoScan()
    {
        GameObject[] candidates;

        if (!string.IsNullOrEmpty(paintingTag) && TagExists(paintingTag) && !includeInactive)
        {
            // fast path for active objects only
            candidates = GameObject.FindGameObjectsWithTag(paintingTag);
        }
        else
        {
            // exhaustive scan across loaded scenes (respects includeInactive)
            var list = new List<GameObject>();
            for (int s = 0; s < SceneManager.sceneCount; s++)
            {
                var scene = SceneManager.GetSceneAt(s);
                if (!scene.isLoaded) continue;
                var roots = scene.GetRootGameObjects();
                foreach (var root in roots) CollectRecursively(root, list);
            }
            candidates = list.ToArray();
        }

        foreach (var go in candidates)
        {
            if (go == null) continue;

            // If a tag was provided and we performed an exhaustive scan, ensure this candidate matches the tag:
            if (!string.IsNullOrEmpty(paintingTag) && TagExists(paintingTag))
            {
                // CompareTag can throw if tag doesn't exist, so wrap defensively
                try
                {
                    if (!go.CompareTag(paintingTag)) continue;
                }
                catch
                {
                    // tag disappeared; skip tag filtering
                }
            }

            if (requireCollider && go.GetComponent<Collider>() == null && go.GetComponent<Collider2D>() == null)
            {
                // skip objects that can't be aimed at
                continue;
            }

            var existing = go.GetComponent<PaintingChannelable>();
            if (existing != null)
            {
                if (applyToExistingComponents)
                {
                    ApplySettingsTo(existing);
                    Debug.Log($"[PaintingChannelableAutoAttach] Applied settings to existing PaintingChannelable on '{go.name}'.");
                }
                continue;
            }

            // Add and configure
            var comp = go.AddComponent<PaintingChannelable>();
            if (comp != null)
            {
                ApplySettingsTo(comp);
                Debug.Log($"[PaintingChannelableAutoAttach] Attached PaintingChannelable to '{go.name}' and applied settings.");
            }
        }
    }

    private void ApplySettingsTo(PaintingChannelable comp)
    {
        if (comp == null) return;

        // PaintingChannelable uses private serialized fields. Use reflection to set them.
        var type = comp.GetType();
        try
        {
            SetPrivateField(type, comp, "requiredChannelDuration", requiredChannelDuration);
            SetPrivateField(type, comp, "pauseOnConsecutiveCompletions", pauseOnConsecutiveCompletions);
            SetPrivateField(type, comp, "consecutiveCompletionsToPause", consecutiveCompletionsToPause);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[PaintingChannelableAutoAttach] Failed to apply settings to '{comp.gameObject.name}': {ex.Message}");
        }
    }

    private void SetPrivateField(Type type, object instance, string fieldName, object value)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var field = type.GetField(fieldName, flags);
        if (field == null)
        {
            // Try base types as a fallback.
            var baseType = type.BaseType;
            while (field == null && baseType != null)
            {
                field = baseType.GetField(fieldName, flags);
                baseType = baseType.BaseType;
            }
        }

        if (field == null)
        {
            Debug.LogWarning($"[PaintingChannelableAutoAttach] Field '{fieldName}' not found on type '{type.FullName}'.");
            return;
        }

        // Convert where necessary (e.g. ints, floats, bools)
        try
        {
            var targetType = field.FieldType;
            var converted = Convert.ChangeType(value, targetType);
            field.SetValue(instance, converted);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[PaintingChannelableAutoAttach] Could not set field '{fieldName}' on '{instance.GetType().Name}': {ex.Message}");
        }
    }

    private void CollectRecursively(GameObject node, List<GameObject> outList)
    {
        if (node == null) return;
        outList.Add(node);
        for (int i = 0; i < node.transform.childCount; i++)
        {
            CollectRecursively(node.transform.GetChild(i).gameObject, outList);
        }
    }

    private bool TagExists(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return false;
        try
        {
            // FindWithTag will throw if the tag doesn't exist; we only use it to test existence
            GameObject.FindWithTag(tag);
            return true;
        }
        catch
        {
            return false;
        }
    }
}