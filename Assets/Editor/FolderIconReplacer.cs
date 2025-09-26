using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class FolderIconReplacer
{
    // Map folder name -> overlay icon (in-memory). You can expand to patterns.
    static Dictionary<string, Texture2D> s_mappings = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);

    // Reflection cache for internal SetIconForObject (if available)
    static MethodInfo s_setIconMethod;

    // Toggle: draw overlay icons in Project window
    public static bool DrawOverlay { get; set; } = true;

    static FolderIconReplacer()
    {
        // Example: preload a test icon if present
        var test = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/Icons/FolderIcon_Odin.png");
        if (test != null)
            s_mappings["Odin"] = test;

        // try to find internal SetIconForObject (flexible search)
        s_setIconMethod = FindSetIconForObjectMethod();

        // Subscribe to drawing callback
        EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemOnGUI;
    }

    static MethodInfo FindSetIconForObjectMethod()
    {
        // Try common known locations first.
        // 1) EditorGUIUtility.SetIconForObject
        var egType = typeof(EditorGUIUtility);
        var m = egType.GetMethod("SetIconForObject", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        if (m != null)
        {
            Debug.Log($"FolderIconReplacer: using EditorGUIUtility.{m.Name} as icon setter.");
            return m;
        }

        // 2) UnityEditorInternal.InternalEditorUtility.SetIconForObject (type loaded by name to avoid direct compile dependency)
        var internalType = Type.GetType("UnityEditorInternal.InternalEditorUtility, UnityEditor");
        if (internalType != null)
        {
            m = internalType.GetMethod("SetIconForObject", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (m != null)
            {
                Debug.Log($"FolderIconReplacer: using {internalType.FullName}.{m.Name} as icon setter.");
                return m;
            }
        }

        // 3) Broader scanning for any static method with "icon" in the name and (Object, Texture/ Object) signature.
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try { types = asm.GetTypes(); }
            catch { continue; }

            foreach (var type in types)
            {
                // quick filter: only editor-like types
                if (!type.FullName?.Contains("UnityEditor") ?? true)
                    continue;

                MethodInfo[] methods;
                try { methods = type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public); }
                catch { continue; }

                foreach (var mm in methods)
                {
                    if (mm.Name.IndexOf("icon", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    var ps = mm.GetParameters();
                    if (ps.Length == 2 &&
                        typeof(UnityEngine.Object).IsAssignableFrom(ps[0].ParameterType) &&
                        (typeof(Texture).IsAssignableFrom(ps[1].ParameterType) || typeof(UnityEngine.Object).IsAssignableFrom(ps[1].ParameterType)))
                    {
                        Debug.Log($"FolderIconReplacer: using {type.FullName}.{mm.Name} as icon setter.");
                        return mm;
                    }
                }
            }
        }

        Debug.LogWarning("FolderIconReplacer: SetIconForObject method not found in this Unity version.");
        return null;
    }

    static void OnProjectWindowItemOnGUI(string guid, Rect selectionRect)
    {
        if (!DrawOverlay || s_mappings.Count == 0)
            return;

        // Only draw during repaint to avoid GUI state issues
        if (Event.current.type != EventType.Repaint)
            return;

        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(path))
            return;

        if (!AssetDatabase.IsValidFolder(path))
            return;

        string folderName = System.IO.Path.GetFileName(path);

        foreach (var kv in s_mappings)
        {
            // simple match: folderName contains key
            if (folderName.IndexOf(kv.Key, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var icon = kv.Value;
                if (icon == null)
                    continue;

                // Draw on the left where Unity's folder icon is normally drawn (covers default)
                // Adjust offsets/sizes for your editor theme and font scale. 16x16 is typical.
                float iconSize = 16f;
                var iconRect = new Rect(selectionRect.x, selectionRect.y + (selectionRect.height - iconSize) * 0.5f, iconSize, iconSize);

                // In some Project layouts (one-column vertical list) the icon is slightly indented.
                // If you need exact alignment for your setup, tweak the x offset or compute based on EditorGUIUtility.singleLineHeight.
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
                break;
            }
        }
    }

    // Public helper that EditorWindow uses
    public static void RegisterMapping(string match, Texture2D icon)
    {
        if (string.IsNullOrEmpty(match))
            return;
        if (icon == null)
        {
            s_mappings.Remove(match);
            return;
        }
        s_mappings[match] = icon;
        EditorApplication.RepaintProjectWindow();
    }

    // Try to set the icon persistently on the folder asset (calls internal API)
    public static bool SetPersistentIconForFolder(string assetPath, Texture2D icon)
    {
        // ensure we try to find method if not cached yet (handles domain reloads)
        if (s_setIconMethod == null)
            s_setIconMethod = FindSetIconForObjectMethod();

        if (s_setIconMethod == null)
        {
            Debug.LogWarning("FolderIconReplacer: persistent icon API not found; falling back to overlay-only drawing.");
            return false;
        }

        var assetObj = AssetDatabase.LoadMainAssetAtPath(assetPath);
        if (assetObj == null)
        {
            Debug.LogWarning("Asset not found at path: " + assetPath);
            return false;
        }

        try
        {
            var parameters = s_setIconMethod.GetParameters();
            object secondArg = icon;
            var paramType = parameters[1].ParameterType;

            // If the internal method wants a UnityEngine.Object or Texture type this will be fine.
            // If it expects something else, try to coerce where reasonable.
            if (paramType == typeof(Texture) || paramType == typeof(Texture2D) || paramType == typeof(UnityEngine.Object))
            {
                // ok
            }
            else
            {
                // attempt a best-effort conversion (rare)
                try
                {
                    secondArg = Convert.ChangeType(icon, paramType);
                }
                catch
                {
                    secondArg = null;
                }
            }

            // Invoke with (assetObject, icon/null) to set or clear the icon
            s_setIconMethod.Invoke(null, new object[] { assetObj, secondArg });
            // Refresh project view so icon change is visible
            EditorApplication.RepaintProjectWindow();
            return true;
        }
        catch (TargetInvocationException tie)
        {
            Debug.LogWarning("Failed to set persistent icon (invocation): " + tie.InnerException?.Message ?? tie.Message);
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("FolderIconReplacer: Failed to set persistent icon via internal API: " + ex.Message);
            return false;
        }
    }
}

public class FolderIconManagerWindow : EditorWindow
{
    string selectedFolderPath;
    Texture2D chosenIcon;
    string matchText = "";

    // Local callback used to repaint when project window changes
    private void ProjectWindowCallback(string guid, Rect rect) => Repaint();

    [MenuItem("Window/Folder Icon Manager")]
    public static void ShowWindow()
    {
        GetWindow<FolderIconManagerWindow>("Folder Icon Manager");
    }

    void OnEnable()
    {
        UpdateSelection();
        // Subscribe a method that matches the expected signature so we can unsubscribe later
        EditorApplication.projectWindowItemOnGUI += ProjectWindowCallback;
    }

    void OnDisable()
    {
        // Unsubscribe the same method
        EditorApplication.projectWindowItemOnGUI -= ProjectWindowCallback;
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Quick overlay + persistent icon tool", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Use Selected Folder"))
        {
            UpdateSelection();
        }
        if (GUILayout.Button("Ping Selected"))
        {
            if (!string.IsNullOrEmpty(selectedFolderPath))
                EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(selectedFolderPath));
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField("Selected Folder:", EditorStyles.label);
        EditorGUILayout.SelectableLabel(selectedFolderPath ?? "<none>", GUILayout.Height(16));

        EditorGUILayout.Space();

        chosenIcon = (Texture2D)EditorGUILayout.ObjectField("Icon Texture", chosenIcon, typeof(Texture2D), false);
        matchText = EditorGUILayout.TextField("Match Text (simple)", matchText);
        EditorGUILayout.HelpBox("If folder name contains Match Text, manager can draw overlay or assign persistent icon.", MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Register Overlay Mapping"))
        {
            if (!string.IsNullOrEmpty(matchText) && chosenIcon != null)
            {
                FolderIconReplacer.RegisterMapping(matchText, chosenIcon);
            }
            else
            {
                Debug.LogWarning("Specify match text and an icon texture first.");
            }
        }

        if (GUILayout.Button("Unregister Mapping"))
        {
            if (!string.IsNullOrEmpty(matchText))
            {
                FolderIconReplacer.RegisterMapping(matchText, null);
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Assign Persistent Icon to Selected Folder"))
        {
            if (string.IsNullOrEmpty(selectedFolderPath) || chosenIcon == null)
            {
                Debug.LogWarning("Select a folder and pick an icon texture first.");
            }
            else
            {
                bool ok = FolderIconReplacer.SetPersistentIconForFolder(selectedFolderPath, chosenIcon);
                if (ok)
                    Debug.Log("Persistent icon assigned (may be editor-internal).");
            }
        }

        if (GUILayout.Button("Toggle Overlay Drawing"))
        {
            FolderIconReplacer.DrawOverlay = !FolderIconReplacer.DrawOverlay;
            Repaint();
            Debug.Log("Overlay drawing: " + FolderIconReplacer.DrawOverlay);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        if (GUILayout.Button("Refresh Project Window"))
            EditorApplication.RepaintProjectWindow();
    }

    void UpdateSelection()
    {
        var obj = Selection.activeObject;
        if (obj == null)
        {
            selectedFolderPath = null;
            return;
        }

        string path = AssetDatabase.GetAssetPath(obj);
        if (AssetDatabase.IsValidFolder(path))
        {
            selectedFolderPath = path;
        }
        else
        {
            // If a file is selected, use its parent folder
            var dir = System.IO.Path.GetDirectoryName(path);
            selectedFolderPath = dir;
        }
    }
}