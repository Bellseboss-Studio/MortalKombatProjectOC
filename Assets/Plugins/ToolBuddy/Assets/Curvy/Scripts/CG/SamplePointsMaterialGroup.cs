// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.Collections.Generic;

namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// A section of one or more patches, all sharing the same MaterialID
    /// </summary>
    public class SamplePointsMaterialGroup
    {
        public int MaterialID;

        public List<SamplePointsPatch> Patches;

        public int TriangleCount
        {
            get
            {
                int cnt = 0;
                for (int p = 0; p < Patches.Count; p++)
                    cnt += Patches[p].TriangleCount;
                return cnt;
            }
        }

        public int StartVertex => Patches[0].Start;

        public int EndVertex => Patches[Patches.Count - 1].End;

        public int VertexCount => (EndVertex - StartVertex) + 1;

        public SamplePointsMaterialGroup(int materialID) : this(
            materialID,
            new List<SamplePointsPatch>()
        ) { }

        public SamplePointsMaterialGroup(int materialID, List<SamplePointsPatch> patches)
        {
            MaterialID = materialID;
            Patches = patches;
        }

        public void GetLengths(CGVolume volume, out float worldLength, out float uLength)
        {
            worldLength = 0;
            for (int v = StartVertex; v < EndVertex; v++)
                worldLength += (volume.Vertices.Array[v + 1] - volume.Vertices.Array[v]).magnitude;
            uLength = volume.CrossCustomValues.Array[EndVertex] - volume.CrossCustomValues.Array[StartVertex];
        }

        /// <summary>
        /// Returns a clone of the current instance.
        /// </summary>
        public SamplePointsMaterialGroup Clone() =>
            new SamplePointsMaterialGroup(
                MaterialID,
                new List<SamplePointsPatch>(Patches)
            );
    }
}