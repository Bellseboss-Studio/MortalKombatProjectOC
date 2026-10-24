using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MK.Rebuild.Editor
{
    /// <summary>
    /// Read-only migration diagnostics.
    ///
    /// Both menu items only INSPECT and WRITE REPORTS: nothing is moved, renamed or reimported,
    /// so running them is always safe. The audit shows what still points at the legacy tree; the
    /// manifest is the pre-move ledger reviewed before WU2 and replayed in reverse for rollback.
    /// </summary>
    public static class MigrationAudit
    {
        private const string RebuildRoot = "Assets/_MKRebuild/";
        private const string ScenesFolder = "Assets/_MKRebuild/Scenes";
        private const string PrefabsFolder = "Assets/_MKRebuild/Prefabs";
        private const string MigratedFolder = "Assets/_MKRebuild/Migrated";
        private const string AuditReportPath = MigratedFolder + "/legacy-references.md";
        private const string ManifestPath = MigratedFolder + "/manifest.md";

        private static readonly string[] LegacyScenes =
        {
            "Assets/_Scenes/intros/intro00.unity",
            "Assets/_Scenes/intros/intro01.unity",
            "Assets/_Scenes/MainMenu/mainMenu_.unity",
        };

        // Content that must not cross into the rebuild: third-party services, post-processing
        // stacks the demo dropped, and editor recovery leftovers.
        private static readonly string[] DropMarkers =
        {
            "PlayFab", "FMOD", "PostProcess", "PostProcessing", "_Recovery",
        };

        // Rebuilt by hand instead of moved: legacy menu canvases/panels, the legacy audio prefab,
        // the key-rebinding UI, PPv2 profiles and baked GI bound to the legacy scene's lightmaps.
        private static readonly string[] ReplaceMarkers =
        {
            "CanvasMenu_pery", "mainMenu_edenia_bake", "PanelExtra", "RebindUIPrefabAngel",
            "MainMenu_Audio", "Post_Menu",
        };

        // --- Audit -----------------------------------------------------------

        [MenuItem("MK/Migration/Audit Legacy References")]
        public static void AuditLegacyReferences()
        {
            List<string> scenes = FindAssets(ScenesFolder, "t:Scene");
            List<string> prefabs = FindAssets(PrefabsFolder, "t:Prefab");

            var referrers = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
            var owners = new List<string>(scenes);
            owners.AddRange(prefabs);

            foreach (string ownerPath in owners)
            {
                foreach (string dependency in AssetDatabase.GetDependencies(ownerPath, true))
                {
                    if (!dependency.StartsWith("Assets/", StringComparison.Ordinal)) continue;
                    if (dependency.StartsWith(RebuildRoot, StringComparison.Ordinal)) continue;

                    if (!referrers.TryGetValue(dependency, out SortedSet<string> sources))
                    {
                        sources = new SortedSet<string>(StringComparer.Ordinal);
                        referrers[dependency] = sources;
                    }

                    sources.Add(ownerPath);
                }
            }

            var counts = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (string dependency in referrers.Keys)
            {
                string extension = ExtensionOf(dependency);
                counts.TryGetValue(extension, out int current);
                counts[extension] = current + 1;
            }

            string report = BuildAuditReport(scenes, prefabs, referrers, counts);
            WriteReport(AuditReportPath, report);

            var console = new StringBuilder();
            console.AppendLine("[MigrationAudit] Legacy reference audit");
            console.AppendLine($"Scenes: {scenes.Count}  Prefabs: {prefabs.Count}");
            console.AppendLine($"External dependencies: {referrers.Count}");

            foreach (KeyValuePair<string, int> group in counts)
            {
                console.AppendLine($"  {group.Key}: {group.Value}");
            }

            foreach (string dependency in referrers.Keys)
            {
                console.AppendLine("  " + dependency);
            }

            console.AppendLine($"Report: {AuditReportPath}");
            Debug.Log(console.ToString());
        }

        private static string BuildAuditReport(
            List<string> scenes,
            List<string> prefabs,
            SortedDictionary<string, SortedSet<string>> referrers,
            SortedDictionary<string, int> counts)
        {
            var report = new StringBuilder();
            report.AppendLine("# Legacy reference audit — _MKRebuild");
            report.AppendLine();
            report.AppendLine("Read-only scan of every dependency of the `_MKRebuild` scenes and prefabs.");
            report.AppendLine("Any path listed here is still outside `_MKRebuild`; the baseline is expected to");
            report.AppendLine("fail before WU2-WU4 move the legacy art (the rebuild Leena prefab is the known case).");
            report.AppendLine();
            report.AppendLine($"- Scanned scenes: {scenes.Count}");
            report.AppendLine($"- Scanned prefabs: {prefabs.Count}");
            report.AppendLine($"- External dependencies: {referrers.Count}");
            report.AppendLine();
            report.AppendLine("## Counts by extension");
            report.AppendLine();
            report.AppendLine("| Extension | Count |");
            report.AppendLine("|---|---:|");

            foreach (KeyValuePair<string, int> group in counts)
            {
                report.AppendLine($"| `{group.Key}` | {group.Value} |");
            }

            if (counts.Count == 0) report.AppendLine("| — none — | 0 |");

            report.AppendLine();
            report.AppendLine("## Offending paths");
            report.AppendLine();

            if (referrers.Count == 0)
            {
                report.AppendLine("_None. Every dependency resolves under `Assets/_MKRebuild/`._");
                return report.ToString();
            }

            report.AppendLine("| Dependency | Referenced by |");
            report.AppendLine("|---|---|");

            foreach (KeyValuePair<string, SortedSet<string>> entry in referrers)
            {
                report.AppendLine($"| `{entry.Key}` | {string.Join("<br>", entry.Value.Select(Md))} |");
            }

            return report.ToString();
        }

        // --- Manifest --------------------------------------------------------

        [MenuItem("MK/Migration/Generate Move Manifest")]
        public static void GenerateMoveManifest()
        {
            var dependencies = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
            var missingScenes = new List<string>();
            int engineDependencies = 0;

            foreach (string scenePath in LegacyScenes)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
                {
                    missingScenes.Add(scenePath);
                    continue;
                }

                foreach (string dependency in AssetDatabase.GetDependencies(scenePath, true))
                {
                    if (!dependency.StartsWith("Assets/", StringComparison.Ordinal))
                    {
                        engineDependencies++;
                        continue;
                    }

                    if (!dependencies.TryGetValue(dependency, out SortedSet<string> sources))
                    {
                        sources = new SortedSet<string>(StringComparer.Ordinal);
                        dependencies[dependency] = sources;
                    }

                    sources.Add(Path.GetFileName(scenePath));
                }
            }

            var rows = new List<ManifestRow>(dependencies.Count);
            foreach (KeyValuePair<string, SortedSet<string>> entry in dependencies)
            {
                rows.Add(new ManifestRow
                {
                    AssetPath = entry.Key,
                    Guid = AssetDatabase.AssetPathToGUID(entry.Key),
                    Type = TypeOf(entry.Key),
                    ReferencedBy = entry.Value.ToList(),
                    Action = Classify(entry.Key),
                });
            }

            var ordered = rows
                .OrderBy(row => ClassOrder(row.Action))
                .ThenBy(row => row.AssetPath, StringComparer.Ordinal)
                .ToList();

            string manifest = BuildManifest(ordered, missingScenes, engineDependencies);
            WriteReport(ManifestPath, manifest);

            var console = new StringBuilder();
            console.AppendLine("[MigrationAudit] Move manifest generated (nothing was moved)");
            foreach (string action in new[] { "share", "replace", "drop", "keep" })
            {
                console.AppendLine($"  {action}: {ordered.Count(row => row.Action == action)}");
            }

            if (missingScenes.Count > 0)
            {
                console.AppendLine("  Missing legacy scenes: " + string.Join(", ", missingScenes));
            }

            console.AppendLine($"Manifest: {ManifestPath}");
            Debug.Log(console.ToString());
        }

        private static string BuildManifest(
            List<ManifestRow> rows, List<string> missingScenes, int engineDependencies)
        {
            var manifest = new StringBuilder();
            manifest.AppendLine("# Move manifest — legacy scenes (pre-move ledger)");
            manifest.AppendLine();
            manifest.AppendLine("Generated by `MK/Migration/Generate Move Manifest`. **Nothing was moved.**");
            manifest.AppendLine("This file is the rollback ledger: reversing every `share` row moves the asset back.");
            manifest.AppendLine();
            manifest.AppendLine("## Classification");
            manifest.AppendLine();
            manifest.AppendLine("| Class | Meaning |");
            manifest.AppendLine("|---|---|");
            manifest.AppendLine("| `share` | Legacy `Assets/` content to MOVE once into `Migrated/` with its `.meta` intact (GUID preserved). Reverse the move to roll back. |");
            manifest.AppendLine("| `replace` | Rebuilt by hand inside `_MKRebuild/`; do not move the legacy file (scripts, scenes, legacy menu canvas, scene-bound bake). |");
            manifest.AppendLine("| `drop` | Deliberately not carried over (PlayFab, FMOD, Post Processing, recovery leftovers). |");
            manifest.AppendLine("| `keep` | Already under `_MKRebuild/`; no move. |");
            manifest.AppendLine();
            manifest.AppendLine($"- Audited legacy scenes: {LegacyScenes.Length}");
            manifest.AppendLine($"- Engine/package dependencies skipped: {engineDependencies}");
            manifest.AppendLine($"- Manifest rows: {rows.Count}");
            manifest.AppendLine();

            if (missingScenes.Count > 0)
            {
                manifest.AppendLine("> **Warning:** these legacy scenes were not found and contributed no rows:");
                foreach (string scene in missingScenes) manifest.AppendLine($"> - `{scene}`");
                manifest.AppendLine();
            }

            manifest.AppendLine("## Counts");
            manifest.AppendLine();
            manifest.AppendLine("| Class | Rows |");
            manifest.AppendLine("|---|---:|");
            foreach (string action in new[] { "share", "replace", "drop", "keep" })
            {
                manifest.AppendLine($"| `{action}` | {rows.Count(row => row.Action == action)} |");
            }

            manifest.AppendLine();
            manifest.AppendLine("## Rows");
            manifest.AppendLine();
            manifest.AppendLine("| Class | Source | GUID | Type | Referenced by | Status |");
            manifest.AppendLine("|---|---|---|---|---|---|");

            foreach (ManifestRow row in rows)
            {
                string status = row.Action == "share" ? "pending" : "-";
                manifest.AppendLine(
                    $"| `{row.Action}` | `{row.AssetPath}` | `{row.Guid}` | {row.Type} | " +
                    $"{string.Join("<br>", row.ReferencedBy.Select(Md))} | {status} |");
            }

            return manifest.ToString();
        }

        private static string Classify(string assetPath)
        {
            if (assetPath.StartsWith(RebuildRoot, StringComparison.Ordinal)) return "keep";
            if (ContainsMarker(assetPath, DropMarkers)) return "drop";
            if (assetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) return "replace";
            if (assetPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)) return "replace";
            // The rebuild owns a new UI map; a legacy action asset must not be carried over.
            if (assetPath.EndsWith(".inputactions", StringComparison.OrdinalIgnoreCase)) return "replace";
            if (ContainsMarker(assetPath, ReplaceMarkers)) return "replace";

            // Everything else is art/audio/timeline/prefab content that the rebuilt scenes share
            // from Migrated/ — moved once, never copied.
            return "share";
        }

        private static int ClassOrder(string action)
        {
            switch (action)
            {
                case "share": return 0;
                case "replace": return 1;
                case "drop": return 2;
                default: return 3;
            }
        }

        private static bool ContainsMarker(string path, string[] markers)
        {
            for (int i = 0; i < markers.Length; i++)
            {
                if (path.IndexOf(markers[i], StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }

            return false;
        }

        // --- Helpers ---------------------------------------------------------

        private static List<string> FindAssets(string folder, string filter)
        {
            var paths = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets(filter, new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path)) paths.Add(path);
            }

            paths.Sort(StringComparer.Ordinal);
            return paths;
        }

        private static string ExtensionOf(string path)
        {
            string extension = Path.GetExtension(path);
            return string.IsNullOrEmpty(extension) ? "(none)" : extension.ToLowerInvariant();
        }

        private static string TypeOf(string path)
        {
            string extension = Path.GetExtension(path);
            return string.IsNullOrEmpty(extension)
                ? "asset"
                : extension.TrimStart('.').ToLowerInvariant();
        }

        private static void WriteReport(string path, string text)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            File.WriteAllText(path, text, new UTF8Encoding(false));
            AssetDatabase.Refresh();
        }

        private static string Md(string value)
        {
            return value.Replace("|", "\\|");
        }

        private sealed class ManifestRow
        {
            public string AssetPath;
            public string Guid;
            public string Type;
            public List<string> ReferencedBy;
            public string Action;
        }
    }
}
