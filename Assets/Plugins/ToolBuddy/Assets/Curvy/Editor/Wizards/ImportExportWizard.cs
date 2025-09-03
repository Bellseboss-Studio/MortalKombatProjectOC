// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.ImportExport;
using FluffyUnderware.DevTools.Extensions;
using FluffyUnderware.DevToolsEditor;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor
{
    /// <summary>
    /// A window that allows exporting and importing splines as Json files
    /// </summary>
    public class ImportExportWizard : EditorWindow
    {
        /// <summary>
        /// Json version of the imported or exported splines
        /// </summary>
        private string serializedText = string.Empty;

        /// <summary>
        /// serializedText copy that iqs used in the UI display. Truncated if too long to avoid Unity error.
        /// </summary>
        private string displayedSerializedText = string.Empty;

        /// <summary>
        /// Defines if which coordinates should be read/written
        /// </summary>
        private CurvySerializationSpace coordinateSpace = CurvySerializationSpace.Global;

        private FileFormat fileFormat = FileFormat.JSON;

        private Vector2 scrollingPosition;

        private IDTInspectorNodeRenderer GUIRenderer;
        private DTGroupNode configurationGroup;
        private DTGroupNode actionsGroup;
        private DTGroupNode advancedActionsGroup;

        public static void Open()
        {
            ImportExportWizard win = GetWindow<ImportExportWizard>(
                true,
                "Import/Export splines"
            );
            win.minSize = new Vector2(
                350,
                340
            );
        }

        [UsedImplicitly]
        private void OnDisable() =>
            DTSelection.OnSelectionChange -= Repaint;

        [UsedImplicitly]
        private void OnEnable()
        {
            const string docLinkId = "import_export";

            GUIRenderer = new DTInspectorNodeDefaultRenderer();

            configurationGroup = new DTGroupNode("Configuration")
                { HelpURL = AssetInformation.DocsRedirectionBaseUrl + docLinkId };
            actionsGroup = new DTGroupNode("Actions") { HelpURL = AssetInformation.DocsRedirectionBaseUrl + docLinkId };
            advancedActionsGroup = new DTGroupNode("Advanced Actions")
                { HelpURL = AssetInformation.DocsRedirectionBaseUrl + docLinkId };

            DTSelection.OnSelectionChange += Repaint;
        }

        [UsedImplicitly]
        private void OnGUI()
        {
            List<CurvySpline> selectedSplines = Selection.GetFiltered(
                typeof(CurvySpline),
                SelectionMode.ExcludePrefab
            ).Where(o => o != null).Select(o => (CurvySpline)o).ToList();

            //actions
            bool export = false;
            bool import = false;
            bool readFromSelection = false;
            bool writeToSelection = false;
            bool readFromFile = false;
            bool writeToFile = false;
            string editedString = null;

            //Display window and read user commands
            {
                GUI.skin.label.wordWrap = true;
                GUILayout.Label("This window allows you to import/export splines from/to JSON text.");

                DTInspectorNode.IsInsideInspector = false;

                //Configuration
                GUIRenderer.RenderSectionHeader(configurationGroup);
                if (configurationGroup.ContentVisible)
                {
                    coordinateSpace = (CurvySerializationSpace)EditorGUILayout.EnumPopup(
                        "Coordinate space to use",
                        coordinateSpace,
                        GUILayout.Width(280)
                    );

                    FileFormat oldFileFormat = fileFormat;

                    fileFormat = (FileFormat)EditorGUILayout.EnumPopup(
                        "Format",
                        fileFormat,
                        GUILayout.Width(280)
                    );

                    if (fileFormat != oldFileFormat)
                        OnFileFormatChanged();
                }

                GUIRenderer.RenderSectionFooter(configurationGroup);


                //Actions
                GUIRenderer.RenderSectionHeader(actionsGroup);
                if (actionsGroup.ContentVisible)
                    switch (fileFormat)
                    {
                        case FileFormat.JSON:
                            GUI.enabled = selectedSplines.Count > 0;
                            export = GUILayout.Button("Export selected spline(s)");

                            GUI.enabled = true;
                            import = GUILayout.Button("Import");
                            break;
                        case FileFormat.SVG:
                            GUI.enabled = true;
                            import = GUILayout.Button("Import");
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                GUIRenderer.RenderSectionFooter(actionsGroup);


                //Advanced actions
                GUIRenderer.RenderSectionHeader(advancedActionsGroup);
                if (advancedActionsGroup.ContentVisible)
                {
                    switch (fileFormat)
                    {
                        case FileFormat.JSON:
                            GUI.enabled = selectedSplines.Count > 0;
                            readFromSelection = GUILayout.Button("Read selected spline(s)");

                            GUI.enabled = true;
                            readFromFile = GUILayout.Button("Read from file");

                            GUI.enabled = string.IsNullOrEmpty(serializedText) == false;
                            writeToSelection = GUILayout.Button("Write new spline(s)");

                            writeToFile = GUILayout.Button("Write to file");
                            break;
                        case FileFormat.SVG:
                            GUI.enabled = true;
                            readFromFile = GUILayout.Button("Read from file");

                            GUI.enabled = string.IsNullOrEmpty(serializedText) == false;
                            writeToSelection = GUILayout.Button("Write new spline(s)");

                            writeToFile = GUILayout.Button("Write to file");
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    GUI.enabled = true;
                    scrollingPosition = EditorGUILayout.BeginScrollView(
                        scrollingPosition,
                        GUILayout.MaxHeight(position.height - 100)
                    );
                    EditorGUI.BeginChangeCheck();
                    string modifiedString = EditorGUILayout.TextArea(
                        displayedSerializedText,
                        EditorStyles.textArea,
                        GUILayout.ExpandHeight(true)
                    );
                    if (GUI.changed)
                        editedString = modifiedString;
                    EditorGUI.EndChangeCheck();
                    EditorGUILayout.EndScrollView();
                }

                GUIRenderer.RenderSectionFooter(advancedActionsGroup);
                GUILayout.Space(5);

                if (configurationGroup.NeedRepaint || actionsGroup.NeedRepaint || advancedActionsGroup.NeedRepaint)
                    Repaint();

                if (readFromFile || readFromSelection)
                    GUI.FocusControl(null); //Keeping the focus prevents the textfield from refreshing
            }

            if (export)
            {
                readFromSelection = true;
                writeToFile = true;
            }

            if (import)
            {
                readFromFile = true;
                writeToSelection = true;
            }

            ProcessCommands(
                selectedSplines,
                readFromSelection,
                readFromFile,
                editedString,
                writeToSelection,
                writeToFile
            );
        }


        private void OnFileFormatChanged()
        {
            serializedText = string.Empty;
            displayedSerializedText = string.Empty;
            scrollingPosition = Vector2.zero;
        }


        private void ProcessCommands([NotNull] List<CurvySpline> selectedSplines, bool readFromSelection, bool readFromFile,
            [CanBeNull] string editedString, bool writeToSelection, bool writeToFile)
        {
            string fileExtension = fileFormat.ToString().ToLowerInvariant();

            if (readFromSelection || readFromFile || editedString != null)
            {
                if (readFromSelection || readFromFile)
                {
                    string raw;
                    {
                        if (readFromSelection)
                        {
                            if (selectedSplines.Count > 0)
                                switch (fileFormat)
                                {
                                    case FileFormat.JSON:
                                        raw = SplineJsonConverter.SplinesToJson(
                                            selectedSplines,
                                            coordinateSpace
                                        );
                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }
                            else
                                throw new InvalidOperationException(
                                    "Serialize Button should not be clickable when something other than splines is selected"
                                );
                        }
                        else
                        {
                            string fileToLoadFullName = EditorUtility.OpenFilePanel(
                                "Select file to load",
                                Application.dataPath,
                                fileExtension
                            );
                            if (String.IsNullOrEmpty(fileToLoadFullName)) //Happens when user cancel the file selecting window
                                raw = displayedSerializedText;
                            else
                                raw = File.ReadAllText(fileToLoadFullName);
                        }
                    }

                    serializedText = raw;
                }
                else
                    serializedText = editedString;

                displayedSerializedText = serializedText;
            }

            if (writeToSelection && string.IsNullOrEmpty(serializedText) == false)
            {
                CurvySpline[] splines;
                switch (fileFormat)
                {
                    case FileFormat.JSON:
                        splines = SplineJsonConverter.JsonToSplines(
                            serializedText,
                            coordinateSpace
                        );
                        break;
                    case FileFormat.SVG:
                        splines = SplineSvgConverter.SvgToSplines(
                            serializedText,
                            coordinateSpace
                        );
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                foreach (CurvySpline spline in splines)
                {
                    const string undoOperationName = "Deserialize";
                    Undo.RegisterCreatedObjectUndo(
                        spline.gameObject,
                        undoOperationName
                    );
                    spline.transform.UndoableSetParent(
                        Selection.activeTransform,
                        coordinateSpace == CurvySerializationSpace.Global,
                        undoOperationName
                    );
                }
            }
            else if (writeToFile)
            {
                string file = EditorUtility.SaveFilePanel(
                    "Save to...",
                    Application.dataPath,
                    String.Format(
                        "Splines_{0}.{1}",
                        DateTime.Now.ToString("yyyy-MMMM-dd HH_mm"),
                        fileExtension
                    ),
                    fileExtension
                );
                if (!string.IsNullOrEmpty(file))
                {
                    File.WriteAllText(
                        file,
                        serializedText
                    );
                    AssetDatabase.Refresh();
                }
            }
        }
    }
}