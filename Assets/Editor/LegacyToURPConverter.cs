// LegacyToURPConverter.cs
// Place this file inside any Editor/ folder in your project (e.g. Assets/Editor/).
// Access it via: Tools > Convert Legacy Diffuse to URP Lit

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class LegacyToURPConverter : EditorWindow
{
    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------
    private const string LEGACY_SHADER  = "Legacy Shaders/Diffuse";
    private const string URP_LIT_SHADER = "Universal Render Pipeline/Lit";

    // Legacy property names
    private const string LEGACY_COLOR    = "_Color";
    private const string LEGACY_MAIN_TEX = "_MainTex";

    // URP Lit property names
    private const string URP_BASE_COLOR = "_BaseColor";
    private const string URP_BASE_MAP   = "_BaseMap";

    // -------------------------------------------------------------------------
    // Window state
    // -------------------------------------------------------------------------
    private Vector2 _scroll;
    private List<Material> _found   = new List<Material>();
    private List<string>   _log     = new List<string>();
    private bool           _searched;

    // -------------------------------------------------------------------------
    // Menu entry
    // -------------------------------------------------------------------------
    [MenuItem("Tools/Convert Legacy Diffuse to URP Lit")]
    public static void OpenWindow()
    {
        var win = GetWindow<LegacyToURPConverter>("Legacy → URP Converter");
        win.minSize = new Vector2(480, 340);
        win.Show();
    }

    // -------------------------------------------------------------------------
    // GUI
    // -------------------------------------------------------------------------
    private void OnGUI()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Legacy Shaders/Diffuse  →  URP/Lit Converter",
            EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            "Scans every material in the project and converts matching shaders.",
            EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.Space(6);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("1. Scan Project", GUILayout.Height(28)))
                Scan();

            GUI.enabled = _searched && _found.Count > 0;
            if (GUILayout.Button($"2. Convert ({_found.Count} material{(_found.Count == 1 ? "" : "s")})",
                GUILayout.Height(28)))
                Convert();
            GUI.enabled = true;

            if (GUILayout.Button("Clear Log", GUILayout.Height(28)))
            {
                _log.Clear();
                _found.Clear();
                _searched = false;
                Repaint();
            }
        }

        EditorGUILayout.Space(6);

        _scroll = EditorGUILayout.BeginScrollView(_scroll,
            GUILayout.ExpandHeight(true));

        foreach (var line in _log)
        {
            bool isError   = line.StartsWith("[ERROR]");
            bool isSuccess = line.StartsWith("[OK]");
            bool isWarning = line.StartsWith("[WARN]");

            var style = new GUIStyle(EditorStyles.label)
            {
                wordWrap  = true,
                richText  = true,
                fontSize  = 11
            };

            string coloured = isError   ? $"<color=#e05252>{line}</color>"
                            : isSuccess ? $"<color=#6abf69>{line}</color>"
                            : isWarning ? $"<color=#f5a623>{line}</color>"
                            : line;

            EditorGUILayout.LabelField(coloured, style);
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField(
            "All converted materials are automatically saved. Undo is NOT supported — " +
            "make a backup or use source control before converting.",
            EditorStyles.wordWrappedMiniLabel);
    }

    // -------------------------------------------------------------------------
    // Scan
    // -------------------------------------------------------------------------
    private void Scan()
    {
        _found.Clear();
        _log.Clear();
        _searched = true;

        Shader legacyShader = Shader.Find(LEGACY_SHADER);
        if (legacyShader == null)
        {
            _log.Add($"[WARN] Could not find shader \"{LEGACY_SHADER}\" in the project. " +
                     "Make sure Built-in RP packages are still present.");
            Repaint();
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Material");
        _log.Add($"Scanning {guids.Length} material asset(s)…");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat == null || mat.shader == null)
                continue;

            if (mat.shader.name == LEGACY_SHADER)
            {
                _found.Add(mat);
                _log.Add($"  Found: {path}");
            }
        }

        _log.Add(_found.Count > 0
            ? $"\n[SCAN COMPLETE] {_found.Count} material(s) ready for conversion."
            : "\n[SCAN COMPLETE] No Legacy Diffuse materials found.");

        Repaint();
    }

    // -------------------------------------------------------------------------
    // Convert
    // -------------------------------------------------------------------------
    private void Convert()
    {
        Shader urpShader = Shader.Find(URP_LIT_SHADER);
        if (urpShader == null)
        {
            _log.Add($"[ERROR] URP Lit shader \"{URP_LIT_SHADER}\" not found. " +
                     "Make sure the Universal RP package is installed.");
            Repaint();
            return;
        }

        int success = 0;
        int failed  = 0;

        foreach (Material mat in _found)
        {
            if (mat == null)
            {
                _log.Add("[WARN] Skipped a null material reference.");
                continue;
            }

            string assetPath = AssetDatabase.GetAssetPath(mat);

            try
            {
                // ── 1. Cache legacy values ─────────────────────────────────
                Color   legacyColor = mat.HasProperty(LEGACY_COLOR)
                                      ? mat.GetColor(LEGACY_COLOR)
                                      : Color.white;

                Texture legacyTex   = mat.HasProperty(LEGACY_MAIN_TEX)
                                      ? mat.GetTexture(LEGACY_MAIN_TEX)
                                      : null;

                Vector2 texOffset   = mat.HasProperty(LEGACY_MAIN_TEX)
                                      ? mat.GetTextureOffset(LEGACY_MAIN_TEX)
                                      : Vector2.zero;

                Vector2 texScale    = mat.HasProperty(LEGACY_MAIN_TEX)
                                      ? mat.GetTextureScale(LEGACY_MAIN_TEX)
                                      : Vector2.one;

                // ── 2. Assign URP shader ───────────────────────────────────
                mat.shader = urpShader;

                // ── 3. Map properties ──────────────────────────────────────
                // Main color → Base Color
                if (mat.HasProperty(URP_BASE_COLOR))
                    mat.SetColor(URP_BASE_COLOR, legacyColor);

                // Main texture → Base Map (preserving tiling/offset)
                if (mat.HasProperty(URP_BASE_MAP))
                {
                    mat.SetTexture(URP_BASE_MAP, legacyTex);
                    mat.SetTextureOffset(URP_BASE_MAP, texOffset);
                    mat.SetTextureScale(URP_BASE_MAP,  texScale);
                }

                // ── 4. Save ────────────────────────────────────────────────
                EditorUtility.SetDirty(mat);
                AssetDatabase.SaveAssetIfDirty(mat);

                _log.Add($"[OK] Converted: {assetPath}");
                success++;
            }
            catch (System.Exception ex)
            {
                _log.Add($"[ERROR] Failed on {assetPath}: {ex.Message}");
                failed++;
            }
        }

        AssetDatabase.Refresh();
        _found.Clear();

        _log.Add($"\n[DONE] {success} converted, {failed} failed.");
        Repaint();
    }
}
#endif