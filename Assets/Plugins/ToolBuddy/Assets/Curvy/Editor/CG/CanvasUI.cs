// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using System.IO;
using FluffyUnderware.Curvy.Generator;
using FluffyUnderware.DevTools.Extensions;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.Generator
{
    public class CanvasUI
    {
        //TODO DESIGN I don't see the need for this class, it seems like a random place methods where put in
        private static readonly CGClipboard Clipboard = new CGClipboard();

        private readonly CGGraph Parent;

        [NotNull]
        private readonly SortedDictionary<string, string> menuNameToTemplateMap = new SortedDictionary<string, string>();

        [NotNull] private CanvasSelection CanvasSelection => Parent.CanvasSelection;

        public CanvasUI(
            CGGraph parent)
        {
            Parent = parent;
            ReloadTemplates();
        }

        /// <summary>
        /// Reloads the available templates from the prefabs in the Templates folder
        /// </summary>
        public void ReloadTemplates()
        {
            menuNameToTemplateMap.Clear();
            string[] baseFolders;
            if (AssetDatabase.IsValidFolder(
                    "Assets/" + CurvyProject.Instance.CustomizationRootPath + CurvyProject.RELPATH_CGTEMPLATES
                ))
                baseFolders = new string[2]
                {
                    "Assets/" + CurvyEditorUtility.GetPackagePath("CG Templates"),
                    "Assets/" + CurvyProject.Instance.CustomizationRootPath + CurvyProject.RELPATH_CGTEMPLATES
                };
            else
                baseFolders = new string[1] { "Assets/" + CurvyEditorUtility.GetPackagePath("CG Templates") };

            string[] prefabs = AssetDatabase.FindAssets(
                "t:gameobject",
                baseFolders
            );

            foreach (string guid in prefabs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                // Store under a unique menu name
                string name = AssetDatabase.LoadAssetAtPath(
                    path,
                    typeof(Transform)
                ).name;
                string menuPath = Path.GetDirectoryName(path).Replace(
                    Path.DirectorySeparatorChar.ToString(),
                    "/"
                );
                foreach (string s in baseFolders)
                    menuPath = menuPath.TrimStart(s);
                menuPath = menuPath.TrimStart('/');

                string menuName = string.IsNullOrEmpty(menuPath)
                    ? name
                    : menuPath + "/" + name;
                int i = 0;
                while (menuNameToTemplateMap.ContainsKey(
                           i == 0
                               ? menuName
                               : menuName + i
                       ))
                    i++;
                menuNameToTemplateMap.Add(
                    i == 0
                        ? menuName
                        : menuName + i,
                    path
                );
            }
        }

        #region Add and connect module from link drag

        /// <summary>
        /// To handle users clicking on output slot instead of dragging link. With this offset the created module will not overlap the source module
        /// </summary>
        private const int AddedModuleLateralTranslation = 20;

        /// <summary>
        /// Show the Add Module menu but with a subset of modules, only the ones compatible with the given output slot
        /// </summary>
        /// <param name="forOutputSlot"> The output slot for which the modules list should be filtered for </param>
        public void ShowFilteredAddModuleMenu(
            [NotNull] CGModuleOutputSlot forOutputSlot)
        {
            if (forOutputSlot == null)
                throw new ArgumentNullException(nameof(forOutputSlot));

            GenericMenu menu = new GenericMenu();

            Type dataType = forOutputSlot.OutputInfo.DataType;
            while (typeof(CGData).IsAssignableFrom(dataType) && dataType != typeof(CGData))
            {
                AddSlotCompatibleModulesToMenu(
                    forOutputSlot,
                    dataType,
                    menu
                );

                dataType = dataType.BaseType;
            }

            menu.ShowAsContext();
        }

        public void ShowFilteredAddModuleMenu(
            [NotNull] CGModuleInputSlot forInputSlot)
        {
            if (forInputSlot == null)
                throw new ArgumentNullException(nameof(forInputSlot));


            GenericMenu menu = new GenericMenu();

            Type dataType = forInputSlot.InputInfo.DataType;
            AddSlotCompatibleModulesToMenu(
                forInputSlot,
                dataType,
                menu
            );

            TypeCache.TypeCollection inheritingTypes = TypeCache.GetTypesDerivedFrom(dataType);
            foreach (Type inheritingType in inheritingTypes)
                AddSlotCompatibleModulesToMenu(
                    forInputSlot,
                    inheritingType,
                    menu
                );

            menu.ShowAsContext();
        }

        private void AddSlotCompatibleModulesToMenu(
            CGModuleOutputSlot forOutputSlot,
            Type dataType,
            GenericMenu menu)
        {
            ModuleRelationshipMaps maps = ModuleRelationshipMaps.Instance;

            List<ModuleInfoAttribute> compatibleModules;
            bool dataTypeHasCompatibleModules = maps.InputTypeToModulesMap.TryGetValue(
                dataType,
                out compatibleModules
            );

            if (!dataTypeHasCompatibleModules)
                return;

            foreach (ModuleInfoAttribute moduleInfo in compatibleModules)
            {
                InputSlotInfo inputSlotInfo =
                    maps.ModuleAndInputTypeToInputSlotMap[moduleInfo][dataType];

                bool inputSlotModuleIsOnRequest = typeof(IOnRequestProcessing).IsAssignableFrom(
                    maps.ModuleInfoToModuleTypeMap[moduleInfo]
                );

                bool areSlotsCompatible =
                    CGModuleLink.AreSlotsCompatible(
                        inputSlotInfo,
                        inputSlotInfo.RequestDataOnly || inputSlotModuleIsOnRequest,
                        forOutputSlot.OutputInfo,
                        forOutputSlot.IsOnRequest
                    );

                if (!areSlotsCompatible)
                    continue;

                menu.AddItem(
                    new GUIContent(moduleInfo.MenuName),
                    false,
                    CTXOnAddAndConnectModuleToOutputLinkDrag,
                    new Tuple<ModuleInfoAttribute, CGModuleOutputSlot>(
                        moduleInfo,
                        forOutputSlot
                    )
                );
            }
        }

        private void AddSlotCompatibleModulesToMenu(
            CGModuleInputSlot forInputSlot,
            Type dataType,
            GenericMenu menu)
        {
            ModuleRelationshipMaps maps = ModuleRelationshipMaps.Instance;

            List<ModuleInfoAttribute> compatibleModules;
            bool dataTypeHasCompatibleModules = maps.OutputTypeToModulesMap.TryGetValue(
                dataType,
                out compatibleModules
            );

            if (!dataTypeHasCompatibleModules)
                return;

            foreach (ModuleInfoAttribute moduleInfo in compatibleModules)
            {
                OutputSlotInfo outputSlotInfo =
                    maps.ModuleAndOutputTypeToOutputSlotMap[moduleInfo][dataType];

                bool outputSlotIsOnRequest = typeof(IOnRequestProcessing).IsAssignableFrom(
                    maps.ModuleInfoToModuleTypeMap[moduleInfo]
                );

                bool areSlotsCompatible =
                    CGModuleLink.AreSlotsCompatible(
                        forInputSlot.InputInfo,
                        forInputSlot.IsOnRequest,
                        outputSlotInfo,
                        outputSlotIsOnRequest
                    );

                if (!areSlotsCompatible)
                    continue;

                menu.AddItem(
                    new GUIContent(moduleInfo.MenuName),
                    false,
                    CTXOnAddAndConnectModuleToInputLinkDrag,
                    new Tuple<ModuleInfoAttribute, CGModuleInputSlot>(
                        moduleInfo,
                        forInputSlot
                    )
                );
            }
        }

        private void CTXOnAddAndConnectModuleToOutputLinkDrag(
            [NotNull] object userData)
        {
            Tuple<ModuleInfoAttribute, CGModuleOutputSlot> tuple = (Tuple<ModuleInfoAttribute, CGModuleOutputSlot>)userData;
            ModuleInfoAttribute moduleInfo = tuple.Item1;
            CGModuleOutputSlot outputSlot = tuple.Item2;

            if (outputSlot == null)
                return;

            CGModule module = AddModule(ModuleRelationshipMaps.Instance.ModuleInfoToModuleTypeMap[moduleInfo]);

            module.Properties.Dimensions.position =
                Parent.Viewport.CanvasMousePosition
                //to handle users clicking on output slot instead of dragging link. With this offset the created module will not overlap the source module
                + (Vector2.right * AddedModuleLateralTranslation)
                //a fixed offset to make the created link end position closer to the link drag drop position. This is not the best way to do it (it does not calculate a precise position that would use the real slot position, but it is a good enough approximation)
                + (Vector2.down * CurvyStyles.ModuleWindowTitleHeight);

            foreach (CGModuleInputSlot inputSlot in module.Input)
                if (inputSlot.CanLinkTo(outputSlot))
                {
                    outputSlot.LinkTo(inputSlot);
                    return;
                }
        }

        private void CTXOnAddAndConnectModuleToInputLinkDrag(
            [NotNull] object userData)
        {
            Tuple<ModuleInfoAttribute, CGModuleInputSlot> tuple = (Tuple<ModuleInfoAttribute, CGModuleInputSlot>)userData;
            ModuleInfoAttribute moduleInfo = tuple.Item1;
            CGModuleInputSlot inputSlot = tuple.Item2;

            if (inputSlot == null)
                return;

            CGModule module = AddModule(ModuleRelationshipMaps.Instance.ModuleInfoToModuleTypeMap[moduleInfo]);

            module.Properties.Dimensions.position =
                Parent.Viewport.CanvasMousePosition
                + (Vector2.left * (module.Properties.MinWidth + AddedModuleLateralTranslation))
                //a fixed offset to make the created link end position closer to the link drag drop position. This is not the best way to do it (it does not calculate a precise position that would use the real slot position, but it is a good enough approximation)
                + (Vector2.down * CurvyStyles.ModuleWindowTitleHeight);

            foreach (CGModuleOutputSlot outputSlot in module.Output)
                if (inputSlot.CanLinkTo(outputSlot))
                {
                    inputSlot.LinkTo(outputSlot);
                    return;
                }
        }

        #endregion

        #region Contextual menu

        public void ContextMenu(
            Vector2 windowMousePosition)
        {
            ModuleRelationshipMaps moduleRelationshipMaps = ModuleRelationshipMaps.Instance;

            GenericMenu menu = new GenericMenu();
            // Add/<Modules>
            List<ModuleInfoAttribute> miNames =
                new List<ModuleInfoAttribute>(moduleRelationshipMaps.ModuleInfoToModuleTypeMap.Keys);

            foreach (ModuleInfoAttribute mi in miNames)
                AddMenuItem(
                    menu,
                    "Add/" + mi.MenuName,
                    CTXOnAddModule,
                    mi
                );
            // Add/<Templates>


            foreach (string tplName in menuNameToTemplateMap.Keys)
                AddMenuItem(
                    menu,
                    "Add Template/" + tplName,
                    CTXOnAddTemplate,
                    tplName
                );

            menu.AddSeparator("");
            AddMenuItem(
                menu,
                "Reset",
                CTXOnReset,
                CanvasSelection.SelectedModules.Count > 0
            );
            menu.AddSeparator("");
            AddMenuItem(
                menu,
                "Cut",
                () => CutSelection(this),
                CanvasSelection.SelectedModules.Count > 0
            );
            AddMenuItem(
                menu,
                "Copy",
                () => CopySelection(this),
                CanvasSelection.SelectedModules.Count > 0
            );
            AddMenuItem(
                menu,
                "Paste",
                () => PastSelection(this),
                !Clipboard.Empty
            );
            AddMenuItem(
                menu,
                "Duplicate",
                () => Duplicate(this),
                CanvasSelection.SelectedModules.Count > 0
            );
            menu.AddSeparator("");
            AddMenuItem(
                menu,
                "Delete",
                () => DeleteSelection(this),
                CanvasSelection.SelectedModules.Count > 0 || CanvasSelection.SelectedLink != null
            );
            menu.AddSeparator("");
            AddMenuItem(
                menu,
                "Select all",
                () => SelectAll(this)
            );

            menu.DropDown(
                new Rect(
                    windowMousePosition,
                    Vector2.zero
                )
            );
        }

        private void AddMenuItem(
            GenericMenu mnu,
            string item,
            GenericMenu.MenuFunction2 func,
            object userData,
            bool enabled = true)
        {
            if (enabled)
                mnu.AddItem(
                    new GUIContent(item),
                    false,
                    func,
                    userData
                );
            else
                mnu.AddDisabledItem(new GUIContent(item));
        }

        private void AddMenuItem(
            GenericMenu mnu,
            string item,
            GenericMenu.MenuFunction func,
            bool enabled = true)
        {
            if (enabled)
                mnu.AddItem(
                    new GUIContent(item),
                    false,
                    func
                );
            else
                mnu.AddDisabledItem(new GUIContent(item));
        }

        private void CTXOnReset()
        {
            foreach (CGModule mod in CanvasSelection.SelectedModules)
                mod.Reset();
        }

        private void CTXOnAddModule(
            object userData)
        {
            ModuleRelationshipMaps moduleRelationshipMaps = ModuleRelationshipMaps.Instance;
            ModuleInfoAttribute moduleInfo = (ModuleInfoAttribute)userData;
            CGModule mod = AddModule(moduleRelationshipMaps.ModuleInfoToModuleTypeMap[moduleInfo]);
            mod.Properties.Dimensions.position = Parent.Viewport.CanvasMousePosition;
        }


        private void CTXOnAddTemplate(
            object userData)
        {
            string tplPath;
            if (menuNameToTemplateMap.TryGetValue(
                    (string)userData,
                    out tplPath
                ))
                CGEditorUtility.LoadTemplate(
                    Parent.Generator,
                    tplPath,
                    Parent.Viewport.CanvasMousePosition
                );
        }

        #endregion

        #region shortcut/contextual menu shared commands

        public static void SelectAll(
            CanvasUI ui) =>
            ui.CanvasSelection.SetSelectionTo(ui.Parent.Modules);

        public static void DeleteSelection(
            CanvasUI ui)
        {
            ui.Delete(ui.CanvasSelection.SelectedObjects);
            ui.CanvasSelection.Clear();
        }

        public static void CopySelection(
            [NotNull] CanvasUI ui) =>
            Clipboard.CopyModules(ui.CanvasSelection.SelectedModules);

        public static void CutSelection(
            [NotNull] CanvasUI ui) =>
            Clipboard.CutModules(ui.CanvasSelection.SelectedModules);

        public static void PastSelection(
            [NotNull] CanvasUI ui)
        {
            if (Clipboard.Empty)
                return;

            // relative position between modules were kept, but take current mouse position as reference!
            Vector2 offset = ui.Parent.Viewport.CanvasMousePosition - Clipboard.Modules[0].Properties.Dimensions.position;
            ui.CanvasSelection.SetSelectionTo(
                Clipboard.PasteModules(
                    ui.Parent.Generator,
                    offset
                )
            );
        }

        public static void Duplicate(
            [NotNull] CanvasUI ui)
        {
            CopySelection(ui);
            PastSelection(ui);
            Clipboard.Clear();
        }

        #endregion

        #region Add/remove modules

        private CGModule AddModule(
            Type type)
        {
            CGModule mod = Parent.Generator.AddModule(type);
            Undo.RegisterCreatedObjectUndo(
                mod,
                "Create Module"
            );
            return mod;
        }

        /// <summary>
        /// Deletes a link or one or more modules (Undo-Aware!)
        /// </summary>
        /// <param name="objects"></param>
        private void Delete(
            params object[] objects)
        {
            if (objects == null || objects.Length == 0)
                return;

            if (objects[0] is CGModuleLink)
                Parent.Generator.DeleteLink((CGModuleLink)objects[0]);
            else
                foreach (CGModule m in objects)
                    m.Delete();
        }

        #endregion
    }
}