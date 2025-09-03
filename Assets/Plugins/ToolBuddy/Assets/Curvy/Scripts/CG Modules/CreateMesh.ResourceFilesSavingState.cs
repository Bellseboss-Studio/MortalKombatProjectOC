// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using JetBrains.Annotations;

#if UNITY_EDITOR
namespace FluffyUnderware.Curvy.Generator.Modules
{
    public partial class CreateMesh
    {
        /// <summary>
        /// This state is used when saving both to the scene and the assets, to track what name the assets should be given.
        /// </summary>
        private class ResourceFilesSavingState
        {
            [NotNull] private string AssetsBasePath { get; }
            private int ResourceIndex { get; set; }
            private int ResourcesCount { get; }

            public ResourceFilesSavingState([NotNull] string assetsBasePath, int resourceIndex, int resourcesCount)
            {
                AssetsBasePath = assetsBasePath;
                ResourceIndex = resourceIndex;
                ResourcesCount = resourcesCount;
            }

            public void IncrementResourceIndex()
                => ResourceIndex++;

            [NotNull]
            public string GetAssetFullName()
                => GetAssetFullName(
                    AssetsBasePath,
                    ResourceIndex,
                    ResourcesCount
                );

            [NotNull]
            public static string GetAssetFullName(string assetsBasePath, int resourceIndex, int resourcesCount)
                => resourcesCount > 1
                    ? $"{assetsBasePath}-{resourceIndex:D3}.asset"
                    : $"{assetsBasePath}.asset";
        }
    }
}
#endif