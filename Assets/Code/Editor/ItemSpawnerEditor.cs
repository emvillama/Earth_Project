using UnityEditor;
using UnityEngine;

// Adds a "Species enable/disable" panel to the ItemSpawner inspector: one toggle per bird in
// the assigned biome, writing straight to each SoundProfile's `enabled` flag. Lets you turn
// species on/off from one place (live in Play mode) instead of opening each profile asset.
// Editor-only (this folder is excluded from player builds).
[CustomEditor(typeof(ItemSpawner))]
public class ItemSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var spawner = (ItemSpawner)target;
        BiomeProfileSet biome = spawner.biome;
        if (biome == null || biome.profiles == null || biome.profiles.Length == 0)
        {
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Species enable/disable", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Toggles the 'enabled' flag on each species' SoundProfile. " +
            "Takes effect live in Play mode. Bed/ambient layers are not listed.", MessageType.None);

        foreach (SoundProfile p in biome.profiles)
        {
            if (p == null || p.layer == SoundLayer.Bed)
            {
                continue;
            }
            string label = string.IsNullOrEmpty(p.displayName) ? p.name : p.displayName;
            EditorGUI.BeginChangeCheck();
            bool value = EditorGUILayout.ToggleLeft(label, p.enabled);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(p, "Toggle species " + label);
                p.enabled = value;
                EditorUtility.SetDirty(p);
            }
        }
    }
}
