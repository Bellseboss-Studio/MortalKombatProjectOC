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
using FluffyUnderware.Curvy.Generator;
using FluffyUnderware.DevTools;
using FluffyUnderware.DevTools.Extensions;
using FluffyUnderware.DevToolsEditor;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FluffyUnderware.CurvyEditor.Generator
{
    public static class CGEditorUtility
    {
        public static void ShowOutputSlotsMenu(GenericMenu.MenuFunction2 func, Type filterSlotDataType = null)
        {
            GenericMenu mnu = new GenericMenu();
            CurvyGenerator[] generators = ObjectExt.FindObjectsOfTypeInCurrentStage<CurvyGenerator>(
                false,
                true
            );

            mnu.AddItem(
                new GUIContent("none"),
                false,
                func,
                null
            );

            foreach (CurvyGenerator gen in generators)
            {
                IEnumerable<CGModule> nonOnRequestModules = gen.Modules.Where(m => m is IOnRequestProcessing == false);

                IEnumerable<CGModuleOutputSlot> slots;
                if (filterSlotDataType == null)
                    slots = nonOnRequestModules.SelectMany(
                        m
                            => m.Output
                    );
                else
                    slots = nonOnRequestModules.SelectMany(
                        m
                            => m.Output.Where(
                                o
                                    => o.Info.DataType == filterSlotDataType
                                       || o.Info.DataType.IsSubclassOf(filterSlotDataType)
                            )
                    );

                foreach (CGModuleOutputSlot slot in slots)
                    mnu.AddItem(
                        new GUIContent($"{gen.name}/{slot.Module.ModuleName}/{slot.Info.DisplayName}"),
                        false,
                        func,
                        slot
                    );
            }

            mnu.ShowAsContext();
        }

        #region CopyModules

        /// <summary>
        /// Copies a list of CGModule instances to a target CurvyGenerator and updates their canvas positions.
        /// </summary>
        /// <param name="sourceModules">A list of CGModule instances to copy.</param>
        /// <param name="target">The target CurvyGenerator to copy the modules to.</param>
        /// <param name="positionOffset"> A Vector2 offset to apply to the copied modules' canvas positions.</param>
        /// <returns>A list of copied CGModule instances.</returns>
        [NotNull]
        public static List<CGModule> CopyModules([NotNull] [ItemNotNull] IList<CGModule> sourceModules,
            [NotNull] CurvyGenerator target, Vector2 positionOffset)
        {
            if (sourceModules == null)
                throw new ArgumentNullException(nameof(sourceModules));
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            DuplicateAndAddModulesToGenerator(
                sourceModules,
                target,
                positionOffset,
                out List<CGModule> copiedModules,
                out Dictionary<int, int> IDMapping
            );

            UpdateModuleLinks(
                copiedModules,
                IDMapping
            );

            target.Initialize(true);

            return copiedModules;
        }

        private static void DuplicateAndAddModulesToGenerator(IList<CGModule> modules,
            CurvyGenerator curvyGenerator,
            Vector2 positionOffset,
            out List<CGModule> copiedModules,
            out Dictionary<int, int> IDMapping)
        {
            IDMapping = new Dictionary<int, int>();
            copiedModules = new List<CGModule>();

            foreach (CGModule module in modules)
            {
                if (module == null)
                {
                    DTLog.LogWarning("[Curvy] Trying to copy a null module. Module will be ignored");
                    continue;
                }

                CGModule duplicatedModule = DuplicateModule(
                    module,
                    positionOffset,
                    curvyGenerator.transform
                );

                curvyGenerator.AddModule(duplicatedModule);

                copiedModules.Add(duplicatedModule);
                IDMapping.Add(
                    module.UniqueID,
                    duplicatedModule.UniqueID
                );
            }
        }

        private static CGModule DuplicateModule([NotNull] CGModule sourceModule, Vector2 positionOffset,
            [NotNull] Transform targetTransform)
        {
            CGModule duplicatedModule = sourceModule.DuplicateGameObject<CGModule>(targetTransform);
            duplicatedModule.name = sourceModule.name;
            duplicatedModule.Properties.Dimensions.position += positionOffset;
            return duplicatedModule;
        }

        private static void UpdateModuleLinks(List<CGModule> modules, Dictionary<int, int> IDMapping)
        {
            for (int moduleIndex = 0; moduleIndex < modules.Count; moduleIndex++)
            {
                CGModule module = modules[moduleIndex];
                int newID = module.UniqueID;

                for (int i = module.InputLinks.Count - 1; i >= 0; i--)
                {
                    // if target module was copied as well, change both IDs
                    int newTargetID;
                    if (IDMapping.TryGetValue(
                            module.InputLinks[i].TargetModuleID,
                            out newTargetID
                        ))
                        module.InputLinks[i].SetModuleIDIINTERNAL(
                            newID,
                            newTargetID
                        );
                    else // otherwise delete link
                        module.InputLinks.RemoveAt(i);
                }

                for (int i = module.OutputLinks.Count - 1; i >= 0; i--)
                {
                    // if target module was copied as well, change both IDs
                    int newTargetID;
                    if (IDMapping.TryGetValue(
                            module.OutputLinks[i].TargetModuleID,
                            out newTargetID
                        ))
                        module.OutputLinks[i].SetModuleIDIINTERNAL(
                            newID,
                            newTargetID
                        );
                    else // otherwise delete link
                        module.OutputLinks.RemoveAt(i);
                }
            }
        }

        #endregion

        public static bool CreateTemplate(IList<CGModule> modules, string absFilePath)
        {
            if (!Directory.Exists(Path.GetDirectoryName(absFilePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(absFilePath));
            // Convert absolute to relative path
            absFilePath = absFilePath.Replace(
                Application.dataPath,
                "Assets"
            );
            if (modules.Count == 0 || string.IsNullOrEmpty(absFilePath))
                return false;

            CurvyGenerator assetGenerator = CurvyGenerator.Create();
            assetGenerator.name = Path.GetFileNameWithoutExtension(absFilePath);
            CopyModules(
                modules,
                assetGenerator,
                Vector2.zero
            );
            foreach (CGModule mod in assetGenerator.Modules)
                mod.OnTemplateCreated();
            assetGenerator.ArrangeModules();
            PrefabUtility.SaveAsPrefabAsset(
                assetGenerator.gameObject,
                absFilePath
            );
            Object.DestroyImmediate(assetGenerator.gameObject);
            AssetDatabase.Refresh();
            return true;
        }

        public static List<CGModule> LoadTemplate(CurvyGenerator generator, string path, Vector2 positionOffset)
        {
            CurvyGenerator srcGen = AssetDatabase.LoadAssetAtPath(
                path,
                typeof(CurvyGenerator)
            ) as CurvyGenerator;
            if (srcGen)
                return CopyModules(
                    srcGen.Modules,
                    generator,
                    positionOffset
                );
            return null;
        }

        public static void SetModulesExpandedState(bool expanded, params CGModule[] modules)
        {
            foreach (CGModule mod in modules)
                mod.Properties.Expanded.target = expanded;
        }

        public static void SceneGUIPlot(Vector3[] vertices, int verticesCount, float size, Color col)
        {
            DTHandles.PushHandlesColor(col);
            for (int index = 0; index < verticesCount; index++)
            {
                Vector3 v = vertices[index];
                Handles.CubeHandleCap(
                    0,
                    v,
                    Quaternion.identity,
                    size * HandleUtility.GetHandleSize(v),
                    EventType.Repaint
                );
            }

            DTHandles.PopHandlesColor();
        }

        [UsedImplicitly]
        [Obsolete("Use the other overload or make a copy of this method")]
        public static void SceneGUIPlot(IList<Vector3> vertices, float size, Color col)
        {
            DTHandles.PushHandlesColor(col);
            for (int index = 0; index < vertices.Count; index++)
            {
                Vector3 v = vertices[index];
                Handles.CubeHandleCap(
                    0,
                    v,
                    Quaternion.identity,
                    size * HandleUtility.GetHandleSize(v),
                    EventType.Repaint
                );
            }

            DTHandles.PopHandlesColor();
        }

        public static void SceneGUILabels(Vector3[] vertices, int verticesCount, IList<string> labels, Color col, Vector2 offset)
        {
            Dictionary<Vector3, string> labelsByPos = new Dictionary<Vector3, string>();
            int ub = Mathf.Min(
                verticesCount,
                labels.Count
            );

            for (int i = 0; i < ub; i++)
            {
                string val;
                if (labelsByPos.TryGetValue(
                        vertices[i],
                        out val
                    ))
                    labelsByPos[vertices[i]] = val + "," + labels[i];
                else
                    labelsByPos.Add(
                        vertices[i],
                        labels[i]
                    );
            }

            GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
            style.normal.textColor = col;
            foreach (KeyValuePair<Vector3, string> kv in labelsByPos)
                Handles.Label(
                    DTHandles.TranslateByPixel(
                        kv.Key,
                        offset
                    ),
                    kv.Value,
                    style
                );
        }

        [UsedImplicitly]
        [Obsolete("Use the other overload or make a copy of this method")]
        public static void SceneGUILabels(IList<Vector3> vertices, IList<string> labels, Color col, Vector2 offset)
        {
            Dictionary<Vector3, string> labelsByPos = new Dictionary<Vector3, string>();
            int ub = Mathf.Min(
                vertices.Count,
                labels.Count
            );

            for (int i = 0; i < ub; i++)
            {
                string val;
                if (labelsByPos.TryGetValue(
                        vertices[i],
                        out val
                    ))
                    labelsByPos[vertices[i]] = val + "," + labels[i];
                else
                    labelsByPos.Add(
                        vertices[i],
                        labels[i]
                    );
            }

            GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
            style.normal.textColor = col;
            foreach (KeyValuePair<Vector3, string> kv in labelsByPos)
                Handles.Label(
                    DTHandles.TranslateByPixel(
                        kv.Key,
                        offset
                    ),
                    kv.Value,
                    style
                );
        }

        public static void SceneGUIPoly(IEnumerable<Vector3> vertices, Color col)
        {
            DTHandles.PushHandlesColor(col);

            Handles.DrawPolyLine(vertices as Vector3[]);
            DTHandles.PopHandlesColor();
        }
    }
}