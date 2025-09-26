using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[FoldableInspector]
public class PaintingInteractableAutoAttach : MonoBehaviour
{
    [Tooltip("Tag used to identify painting GameObjects. Leave empty to match any GameObject that has a Collider/Collider2D.")]
    [SerializeField] private string paintingTag = "InteractablePainting";

    [Tooltip("If TRUE, include inactive GameObjects in the scan.")]
    [SerializeField] private bool includeInactive = true;

    [Tooltip("If > 0, manager will periodically rescan the scene every N seconds to handle dynamically spawned paintings. 0 = only scan once on Awake.")]
    [SerializeField] private float rescanInterval = 0f;

    [Tooltip("Only attach to GameObjects that have at least one of these components (helps avoid accidentally attaching to unrelated objects).")]
    [SerializeField] private bool requireCollider = true;

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
            candidates = GameObject.FindGameObjectsWithTag(paintingTag);
        }
        else
        {
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

            if (!string.IsNullOrEmpty(paintingTag) && TagExists(paintingTag))
            {
                try
                {
                    if (!go.CompareTag(paintingTag)) continue;
                }
                catch
                {
                    // If tag is removed or invalid, skip tag filtering
                }
            }

            if (requireCollider && go.GetComponent<Collider>() == null && go.GetComponent<Collider2D>() == null)
            {
                // Skip objects without any collider (can't be aimed at / interacted with)
                continue;
            }

            if (go.GetComponent<PaintingInteractable>() != null) continue;

            // Adding the component will honor the class's RequireComponent attribute (Unity will add required components automatically).
            go.AddComponent<PaintingInteractable>();
            Debug.Log($"[PaintingInteractableAutoAttach] Attached PaintingInteractable to '{go.name}'.");
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
            GameObject.FindWithTag(tag);
            return true;
        }
        catch
        {
            return false;
        }
    }
}