#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class EncodingConverter
{
    [MenuItem("Tools/Encoding/Convert All C# Scripts to UTF-8 (with BOM)")]
    public static void ConvertAllCsToUtf8Bom()
    {
        string defaultRoot = Application.dataPath;
        string selected = EditorUtility.OpenFolderPanel("Select folder to convert", defaultRoot, string.Empty);

        if (string.IsNullOrEmpty(selected))
            return;

        string projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Path.GetFullPath(selected).StartsWith(projectPath, StringComparison.OrdinalIgnoreCase))
        {
            EditorUtility.DisplayDialog("Invalid Path", "Please select a folder inside this project (e.g. Assets).", "OK");
            return;
        }

        string[] files = Directory.GetFiles(selected, "*.cs", SearchOption.AllDirectories);
        if (files.Length == 0)
        {
            EditorUtility.DisplayDialog("No C# Files", "No .cs files found in selected folder.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Confirm", $"Convert {files.Length} C# files to UTF-8 (BOM)?", "Yes", "No"))
            return;

        int converted = 0, skipped = 0, failed = 0;

        try
        {
            for (int i = 0; i < files.Length; i++)
            {
                string path = files[i];
                float progress = (i + 1f) / files.Length;
                EditorUtility.DisplayProgressBar("Converting to UTF-8 (BOM)...", path, progress);

                try
                {
                    byte[] raw = File.ReadAllBytes(path);
                    if (HasUtf8Bom(raw))
                    {
                        skipped++;
                        continue;
                    }

                    string text;
                    try
                    {
                        var utf8Strict = new UTF8Encoding(false, true);
                        text = utf8Strict.GetString(raw);
                    }
                    catch
                    {
                        text = Encoding.Default.GetString(raw);
                    }

                    var utf8Bom = new UTF8Encoding(true);
                    File.WriteAllText(path, text, utf8Bom);
                    converted++;
                }
                catch
                {
                    failed++;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        EditorUtility.DisplayDialog("Done", $"Converted: {converted}\nSkipped: {skipped}\nFailed: {failed}", "OK");
        AssetDatabase.Refresh();
    }

    private static bool HasUtf8Bom(byte[] bytes)
    {
        return bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
    }
}
#endif
