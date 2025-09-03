// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using JetBrains.Annotations;

namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// Additional Spline Request parameters
    /// </summary>
    public class CGDataRequestMetaCGOptions : CGDataRequestParameter
    {
        /// <summary>
        /// Whether Hard Edges should produce extra samples
        /// </summary>
        /// <remarks>This may result in extra samples at affected Control Points</remarks>
        [UsedImplicitly]
        [Obsolete("This option is now always assumed to be true")]
        public bool CheckHardEdges;

        /// <summary>
        /// Whether MaterialID's should be stored
        /// </summary>
        /// <remarks>This may result in extra samples at affected Control Points</remarks>
        [UsedImplicitly]
        [Obsolete("This option is now always assumed to be true")]
        public bool CheckMaterialID;

        /// <summary>
        /// Whether all Control Points should be included
        /// </summary>
        public bool IncludeControlPoints;

        /// <summary>
        /// Whether UVEdge, ExplicitU and custom U settings should be included
        /// </summary>
        [UsedImplicitly]
        [Obsolete("This option is now always assumed to be true")]
        public bool CheckExtendedUV;


        public CGDataRequestMetaCGOptions(bool checkEdges, bool checkMaterials, bool includeCP, bool extendedUV)
        {
#pragma warning disable 618
            CheckHardEdges = checkEdges;
#pragma warning restore 618
#pragma warning disable 618
            CheckMaterialID = checkMaterials;
#pragma warning restore 618
            IncludeControlPoints = includeCP;
#pragma warning disable 618
            CheckExtendedUV = extendedUV;
#pragma warning restore 618
        }

        public override bool Equals(object obj)
        {
            CGDataRequestMetaCGOptions O = obj as CGDataRequestMetaCGOptions;
            if (O == null)
                return false;
#pragma warning disable 618
            return CheckHardEdges == O.CheckHardEdges
                   && CheckMaterialID == O.CheckMaterialID
                   && IncludeControlPoints == O.IncludeControlPoints
                   && CheckExtendedUV == O.CheckExtendedUV;
#pragma warning restore 618
        }

        public override int GetHashCode()
        {
#pragma warning disable 618
            return new { A = CheckHardEdges, B = CheckMaterialID, C = IncludeControlPoints, D = CheckExtendedUV }
                .GetHashCode(); //OPTIM avoid array creation
#pragma warning restore 618
        }

        public override string ToString()
        {
#pragma warning disable 618
            return
                $"{nameof(CheckHardEdges)}: {CheckHardEdges}, {nameof(CheckMaterialID)}: {CheckMaterialID}, {nameof(IncludeControlPoints)}: {IncludeControlPoints}, {nameof(CheckExtendedUV)}: {CheckExtendedUV}";
#pragma warning restore 618
        }
    }
}