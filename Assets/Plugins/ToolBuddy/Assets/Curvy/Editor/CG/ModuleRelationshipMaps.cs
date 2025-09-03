// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluffyUnderware.Curvy.Generator;
using FluffyUnderware.DevTools.Extensions;
using JetBrains.Annotations;

namespace FluffyUnderware.CurvyEditor.Generator
{
    /// <summary>
    /// Contains mappings between ModuleInfoAttributes, SlotInfoAttributes and Module Types
    /// </summary>
    public class ModuleRelationshipMaps
    {
        private static ModuleRelationshipMaps instance;

        public static ModuleRelationshipMaps Instance
        {
            get
            {
                if (instance != null)
                    return instance;

                instance = new ModuleRelationshipMaps();
                instance.BuildModuleInfoToModuleTypeMap();
                instance.BuildSlotRelatedMaps();
                return instance;
            }
        }

        private ModuleRelationshipMaps() { }

        /// <summary>
        /// Gets ModuleInfo->Module Type mapping
        /// </summary>
        [NotNull]
        public SortedDictionary<ModuleInfoAttribute, Type> ModuleInfoToModuleTypeMap { get; private set; } =
            new SortedDictionary<ModuleInfoAttribute, Type>();

        /// <summary>
        /// Gets Modules that accept a certain input data type
        /// </summary>
        [NotNull]
        public Dictionary<Type, List<ModuleInfoAttribute>> InputTypeToModulesMap { get; } =
            new Dictionary<Type, List<ModuleInfoAttribute>>();

        /// <summary>
        /// Gets Modules that output a certain output data type
        /// </summary>
        [NotNull]
        public Dictionary<Type, List<ModuleInfoAttribute>> OutputTypeToModulesMap { get; } =
            new Dictionary<Type, List<ModuleInfoAttribute>>();

        /// <summary>
        /// Used to get InputSlotInfo from (ModuleInfoAttribute,InputType) couples
        /// </summary>
        [NotNull]
        public Dictionary<ModuleInfoAttribute, Dictionary<Type, InputSlotInfo>> ModuleAndInputTypeToInputSlotMap { get; } =
            new Dictionary<ModuleInfoAttribute, Dictionary<Type, InputSlotInfo>>();

        /// <summary>
        /// Used to get OutputSlotInfo from (ModuleInfoAttribute,OutputType) couples
        /// </summary>
        [NotNull]
        public Dictionary<ModuleInfoAttribute, Dictionary<Type, OutputSlotInfo>> ModuleAndOutputTypeToOutputSlotMap { get; } =
            new Dictionary<ModuleInfoAttribute, Dictionary<Type, OutputSlotInfo>>();

        private void BuildModuleInfoToModuleTypeMap()
        {
            Dictionary<ModuleInfoAttribute, Type> allTypesWithModuleInfo =
                typeof(CGModule).GetAllTypesWithAttribute<ModuleInfoAttribute>();
            ModuleInfoToModuleTypeMap =
                new SortedDictionary<ModuleInfoAttribute, Type>(
                    allTypesWithModuleInfo
                );
        }

        private void BuildSlotRelatedMaps()
        {
            InputTypeToModulesMap.Clear();
            ModuleAndInputTypeToInputSlotMap.Clear();
            OutputTypeToModulesMap.Clear();
            ModuleAndOutputTypeToOutputSlotMap.Clear();

            foreach (KeyValuePair<ModuleInfoAttribute, Type> pair in ModuleInfoToModuleTypeMap)
            {
                ModuleInfoAttribute moduleInfo = pair.Key;
                Type moduleType = pair.Value;

                FieldInfo[] moduleFields = moduleType.GetFields(BindingFlags.Public | BindingFlags.Instance);

                IEnumerable<InputSlotInfo> validInputSlotInfos =
                    GetValidSlotInfos<CGModuleInputSlot, InputSlotInfo>(moduleFields);
                IEnumerable<OutputSlotInfo> validOutputSlotInfos =
                    GetValidSlotInfos<CGModuleOutputSlot, OutputSlotInfo>(moduleFields);

                foreach (InputSlotInfo inputInfo in validInputSlotInfos)
                {
                    UpdateInputTypeToModulesMapWith(
                        inputInfo.DataType,
                        moduleInfo
                    );

                    UpdateModuleAndInputTypeToInputSlotMapWith(
                        moduleInfo,
                        inputInfo.DataType,
                        inputInfo
                    );
                }

                foreach (OutputSlotInfo outputInfo in validOutputSlotInfos)
                {
                    UpdateOutputTypeToModulesMapWith(
                        outputInfo.DataType,
                        moduleInfo
                    );

                    UpdateModuleAndOutputTypeToOutputSlotMapWith(
                        moduleInfo,
                        outputInfo.DataType,
                        outputInfo
                    );
                }
            }
        }

        private static IEnumerable<InfoType> GetValidSlotInfos<SlotType, InfoType>(
            FieldInfo[] moduleFields)
        {
            IEnumerable<InfoType> validInputSlotInfos;
            {
                IEnumerable<FieldInfo> inputSlotFields =
                    moduleFields.Where(f => f.FieldType == typeof(SlotType));

                IEnumerable<object[]> allInputSlotInfos = inputSlotFields.Select(
                    f => f.GetCustomAttributes(
                        typeof(InfoType),
                        true
                    )
                );

                validInputSlotInfos = allInputSlotInfos
                    .Where(attributes => attributes.Length > 0)
                    .Select(attributes => (InfoType)attributes[0]);
            }
            return validInputSlotInfos;
        }

        private void UpdateInputTypeToModulesMapWith(
            [NotNull] Type inputType,
            [NotNull] ModuleInfoAttribute moduleInfo)
        {
            List<ModuleInfoAttribute> modulesAcceptingInputType;
            if (!InputTypeToModulesMap.TryGetValue(
                    inputType,
                    out modulesAcceptingInputType
                ))
            {
                modulesAcceptingInputType = new List<ModuleInfoAttribute>();
                InputTypeToModulesMap.Add(
                    inputType,
                    modulesAcceptingInputType
                );
            }

            modulesAcceptingInputType.Add(moduleInfo);
        }

        private void UpdateOutputTypeToModulesMapWith(
            [NotNull] Type outputType,
            [NotNull] ModuleInfoAttribute moduleInfo)
        {
            List<ModuleInfoAttribute> modulesAcceptingOutputType;
            if (!OutputTypeToModulesMap.TryGetValue(
                    outputType,
                    out modulesAcceptingOutputType
                ))
            {
                modulesAcceptingOutputType = new List<ModuleInfoAttribute>();
                OutputTypeToModulesMap.Add(
                    outputType,
                    modulesAcceptingOutputType
                );
            }

            modulesAcceptingOutputType.Add(moduleInfo);
        }

        private void UpdateModuleAndInputTypeToInputSlotMapWith(
            [NotNull] ModuleInfoAttribute moduleInfo,
            [NotNull] Type inputType,
            [NotNull] InputSlotInfo inputInfo)
        {
            if (ModuleAndInputTypeToInputSlotMap.ContainsKey(moduleInfo) == false)
                ModuleAndInputTypeToInputSlotMap[moduleInfo] =
                    new Dictionary<Type, InputSlotInfo>();
            if (ModuleAndInputTypeToInputSlotMap[moduleInfo].ContainsKey(inputType) == false)
                ModuleAndInputTypeToInputSlotMap[moduleInfo][inputType] = inputInfo;
        }

        private void UpdateModuleAndOutputTypeToOutputSlotMapWith(
            [NotNull] ModuleInfoAttribute moduleInfo,
            [NotNull] Type outputType,
            [NotNull] OutputSlotInfo outputInfo)
        {
            if (ModuleAndOutputTypeToOutputSlotMap.ContainsKey(moduleInfo) == false)
                ModuleAndOutputTypeToOutputSlotMap[moduleInfo] =
                    new Dictionary<Type, OutputSlotInfo>();
            if (ModuleAndOutputTypeToOutputSlotMap[moduleInfo].ContainsKey(outputType) == false)
                ModuleAndOutputTypeToOutputSlotMap[moduleInfo][outputType] = outputInfo;
        }
    }
}