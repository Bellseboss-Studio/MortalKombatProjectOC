// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.Collections.Generic;
using System.Linq;
using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Pools;
using FluffyUnderware.Curvy.ThirdParty.LibTessDotNet;
using FluffyUnderware.Curvy.Utils;
using FluffyUnderware.DevTools;
using FluffyUnderware.DevToolsEditor;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor
{
    /// <summary>
    /// Wizard to export one or more splines to a mesh
    /// </summary>
    public class CurvySplineExportWizard : EditorWindow
    {
        private const int CLOSEDSHAPE = 0;
        private const int VERTEXLINE = 1;
        private const string Title = "Spline to Mesh";

        // SOURCES
        public List<SplinePolyLine> Curves = new List<SplinePolyLine>();
        public WindingRule Winding = WindingRule.EvenOdd;
        public string TriangulationMessage = string.Empty;

        private bool refreshNow = true;
        public int Mode;
        public Material Mat;
        public Vector2 UVOffset = Vector2.zero;
        public Vector2 UVTiling = Vector2.one;

        public bool UV2;
        public string MeshName = "CurvyMeshExport";

        public CurvySplineGizmos GizmoState;

        public GameObject previewGO;
        public MeshFilter previewMeshFilter;
        public MeshRenderer previewMeshRenderer;

        public Vector2 scroll;
        private readonly HashSet<CurvySpline> splines = new HashSet<CurvySpline>();

        private Mesh previewMesh
        {
            get => previewMeshFilter.sharedMesh;
            set => previewMeshFilter.sharedMesh = value;
        }

        public static void Create()
        {
            CurvySplineExportWizard win = GetWindow<CurvySplineExportWizard>(
                true,
                Title,
                true
            );
            win.Init(Selection.activeGameObject.GetComponent<CurvySpline>());
            win.minSize = new Vector2(
                500,
                390
            );
            SceneView.duringSceneGui -= win.Preview;
            SceneView.duringSceneGui += win.Preview;
        }

        [UsedImplicitly]
        private void OnEnable()
        {
            nSplines = new DTGroupNode("Splines") { HelpURL = AssetInformation.DocsRedirectionBaseUrl + "exportwizard" };
            nTexture = new DTGroupNode("Texture");
            nExport = new DTGroupNode("Export");

            GizmoState = CurvyGlobalManager.Gizmos;
            CurvyGlobalManager.Gizmos = CurvySplineGizmos.Curve;


            if (!previewGO)
            {
                previewGO = new GameObject("ExportPreview");
                previewGO.hideFlags = HideFlags.HideAndDontSave;
                previewMeshRenderer = previewGO.AddComponent<MeshRenderer>();
                previewMeshFilter = previewGO.AddComponent<MeshFilter>();
                if (!Mat)
                    Mat = CurvyUtility.GetDefaultMaterial();
                previewMeshRenderer.material = Mat;
            }
        }

        [UsedImplicitly]
        private void OnDisable() =>
            CurvyGlobalManager.Gizmos = GizmoState;

        [UsedImplicitly]
        private void OnDestroy()
        {
            SceneView.duringSceneGui -= Preview;
            foreach (SplinePolyLine crv in Curves)
                UnhookSpline(crv.Spline);
            Curves.Clear();
            SceneView.RepaintAll();
            DestroyImmediate(previewGO);
        }

        [UsedImplicitly]
        private void OnFocus()
        {
            SceneView.duringSceneGui -= Preview;
            SceneView.duringSceneGui += Preview;
        }

        private void Init(CurvySpline spline)
        {
            Curves.Add(new SplinePolyLine(spline));
            HookSpline(spline);
        }


        private Mesh clonePreviewMesh()
        {
            Mesh msh = new Mesh();

            Vector3[] previewMeshVertices = previewMesh.vertices;
            msh.vertices = previewMeshVertices;

            int[] previewMeshTriangles = previewMesh.triangles;
            msh.triangles = previewMeshTriangles;

            Vector2[] previewMeshUV = previewMesh.uv;
            msh.uv = previewMeshUV;

            Vector2[] previewMeshUV2 = previewMesh.uv2;
            msh.uv2 = previewMeshUV2;

            msh.RecalculateNormals();
            msh.RecalculateBounds();

            ArrayPools.Vector3.Free(previewMeshVertices);
            ArrayPools.Int32.Free(previewMeshTriangles);
            ArrayPools.Vector2.Free(previewMeshUV);
            ArrayPools.Vector2.Free(previewMeshUV2);
            return msh;
        }

        private void OnSourceRefresh(CurvySplineEventArgs e) =>
            refreshNow = true;

        private void HookSpline(CurvySpline spline)
        {
            if (!spline) return;
            spline.OnRefresh.AddListenerOnce(OnSourceRefresh);
            splines.Add(spline);
        }

        private void UnhookSpline(CurvySpline spline)
        {
            if (!spline) return;
            spline.OnRefresh.RemoveListener(OnSourceRefresh);
            splines.Remove(spline);
        }

        private readonly IDTInspectorNodeRenderer GUIRenderer = new DTInspectorNodeDefaultRenderer();
        private DTGroupNode nSplines;
        private DTGroupNode nTexture;
        private DTGroupNode nExport;
        private bool mNeedRepaint;

        [UsedImplicitly]
        private void OnGUI()
        {
            DTInspectorNode.IsInsideInspector = false;
            if (Curves.Count == 0)
                return;


            Mode = GUILayout.SelectionGrid(
                Mode,
                new[]
                {
                    new GUIContent(
                        "Closed Shape",
                        "Export a closed shape with triangles"
                    ),
                    new GUIContent(
                        "Vertex Line",
                        "Export a vertex line"
                    )
                },
                2
            );


            if (!string.IsNullOrEmpty(TriangulationMessage) && !TriangulationMessage.Contains("Angle must be >0"))
                EditorGUILayout.HelpBox(
                    TriangulationMessage,
                    MessageType.Error
                );

            scroll = EditorGUILayout.BeginScrollView(scroll);

            // OUTLINE
            GUIRenderer.RenderSectionHeader(nSplines);
            if (nSplines.ContentVisible)
            {
                Winding = (WindingRule)EditorGUILayout.EnumPopup(
                    "Winding",
                    Winding,
                    GUILayout.Width(285)
                );
                GUILayout.BeginHorizontal();
                GUILayout.Label(
                    new GUIContent(
                        "Spline"
                    ),
                    EditorStyles.boldLabel,
                    GUILayout.Width(140)
                );
                GUILayout.Label(
                    "Vertex Generation",
                    EditorStyles.boldLabel,
                    GUILayout.Width(160)
                );
                GUILayout.Label(
                    "Orientation",
                    EditorStyles.boldLabel
                );
                GUILayout.EndHorizontal();
                CurveGUI(Curves[0]);
                if (Mode == CLOSEDSHAPE)
                {
                    for (int i = 1; i < Curves.Count; i++)
                        CurveGUI(Curves[i]);
                    if (GUILayout.Button(
                            CurvyStyles.AddSmallTexture,
                            GUILayout.ExpandWidth(false)
                        ))
                        Curves.Add(new SplinePolyLine(null));
                }
            }

            mNeedRepaint = mNeedRepaint || nSplines.NeedRepaint;
            GUIRenderer.RenderSectionFooter(nSplines);

            // TEXTURING
            GUIRenderer.RenderSectionHeader(nTexture);
            if (nTexture.ContentVisible)
            {
                Mat = (Material)EditorGUILayout.ObjectField(
                    "Material",
                    Mat,
                    typeof(Material),
                    true,
                    GUILayout.Width(285)
                );
                UVTiling = EditorGUILayout.Vector2Field(
                    "Tiling",
                    UVTiling,
                    GUILayout.Width(285)
                );
                UVOffset = EditorGUILayout.Vector2Field(
                    "Offset",
                    UVOffset,
                    GUILayout.Width(285)
                );
            }

            GUIRenderer.RenderSectionFooter(nTexture);
            mNeedRepaint = mNeedRepaint || nTexture.NeedRepaint;
            // EXPORT
            GUIRenderer.RenderSectionHeader(nExport);
            if (nExport.ContentVisible)
            {
                MeshName = EditorGUILayout.TextField(
                    "Mesh Name",
                    MeshName,
                    GUILayout.Width(285)
                );
                UV2 = EditorGUILayout.Toggle(
                    "Add UV2",
                    UV2
                );

                GUILayout.BeginHorizontal();

                if (GUILayout.Button("Save as Asset"))
                {
                    string path = EditorUtility.SaveFilePanelInProject(
                        "Save Mesh",
                        MeshName + ".asset",
                        "asset",
                        "Choose a file location"
                    );
                    if (!string.IsNullOrEmpty(path))
                    {
                        Mesh msh = clonePreviewMesh();
                        if (msh)
                        {
                            msh.name = MeshName;
                            AssetDatabase.DeleteAsset(path);
                            AssetDatabase.CreateAsset(
                                msh,
                                path
                            );
                            AssetDatabase.SaveAssets();
                            AssetDatabase.Refresh();
                            DTLog.Log("[Curvy] Export: Mesh Asset saved!");
                        }
                    }
                }

                if (GUILayout.Button("Create GameObject"))
                {
                    Mesh msh = clonePreviewMesh();
                    if (msh)
                    {
                        msh.name = MeshName;
                        //todo use UndoableCreateGameObject?
                        GameObject go = new GameObject(
                            MeshName,
                            typeof(MeshRenderer),
                            typeof(MeshFilter)
                        );
                        go.GetComponent<MeshFilter>().sharedMesh = msh;
                        go.GetComponent<MeshRenderer>().sharedMaterial = Mat;
                        Selection.activeGameObject = go;
                        DTLog.Log("[Curvy] Export: GameObject created!");
                    }
                    else
                        DTLog.LogWarning("[Curvy] Export: Unable to triangulate spline!");
                }

                GUILayout.EndHorizontal();
            }

            GUIRenderer.RenderSectionFooter(nExport);
            mNeedRepaint = mNeedRepaint || nExport.NeedRepaint;
            EditorGUILayout.EndScrollView();
            refreshNow = refreshNow || GUI.changed;
            if (mNeedRepaint)
            {
                Repaint();
                mNeedRepaint = false;
            }
        }

        private void CurveGUI(SplinePolyLine curve)
        {
            GUILayout.BeginHorizontal();
            CurvySpline o = curve.Spline;
            curve.Spline = (CurvySpline)EditorGUILayout.ObjectField(
                curve.Spline,
                typeof(CurvySpline),
                true,
                GUILayout.Width(140)
            );

            if (o != curve.Spline)
                UnhookSpline(o);
            HookSpline(curve.Spline);

            curve.VertexMode = (SplinePolyLine.VertexCalculation)EditorGUILayout.EnumPopup(
                curve.VertexMode,
                GUILayout.Width(140)
            );
            GUILayout.Space(20);
            curve.Orientation = (ContourOrientation)EditorGUILayout.EnumPopup(curve.Orientation);
            if (GUILayout.Button(
                    new GUIContent(
                        CurvyStyles.DeleteSmallTexture,
                        "Remove"
                    ),
                    GUILayout.ExpandWidth(false)
                ))
            {
                if (curve.Spline)
                    UnhookSpline(curve.Spline);
                Curves.Remove(curve);
                refreshNow = true;
                GUIUtility.ExitGUI();
            }

            switch (curve.VertexMode)
            {
                case SplinePolyLine.VertexCalculation.ByAngle:
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(150);
                    float lw = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = 40;
                    curve.Angle = Mathf.Max(
                        0,
                        EditorGUILayout.FloatField(
                            "Angle",
                            curve.Angle,
                            GUILayout.Width(140)
                        )
                    );
                    EditorGUIUtility.labelWidth = 60;
                    GUILayout.Space(20);
                    curve.Distance = EditorGUILayout.FloatField(
                        "Min. Dist.",
                        curve.Distance,
                        GUILayout.Width(150)
                    );
                    EditorGUIUtility.labelWidth = lw;
                    if (curve.Angle == 0)
                    {
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(140);
                        EditorGUILayout.HelpBox(
                            "Angle must be >0",
                            MessageType.Error
                        );
                    }

                    break;
            }

            GUILayout.EndHorizontal();
        }

        [UsedImplicitly]
        private void Update()
        {
            if (Curves.Count == 0)
            {
                Close();
                return;
            }

            refreshNow = refreshNow || splines.Any(splineBase => splineBase.GlobalCoordinatesChangedThisFrame);

            if (refreshNow)
            {
                previewMeshRenderer.sharedMaterial = Mat;
                refreshNow = false;
                Spline2Mesh s2m = new Spline2Mesh();
                foreach (SplinePolyLine c in Curves)
                    if (c.Spline != null)
                        s2m.Lines.Add(c);

                s2m.Winding = Winding;
                s2m.VertexLineOnly = Mode == VERTEXLINE;

                s2m.UVOffset = UVOffset;
                s2m.UVTiling = UVTiling;
                s2m.UV2 = UV2;
                s2m.MeshName = MeshName;
                Mesh m;
                s2m.Apply(out m);
                previewMesh = m;

                TriangulationMessage = s2m.Error;
                string sTitle;
                if (previewMesh)
                {
                    if (previewMesh.triangles.Length > 0)
                        sTitle = string.Format(
                            "{2} ({0} Vertices, {1} Triangles)",
                            previewMeshFilter.sharedMesh.vertexCount,
                            previewMeshFilter.sharedMesh.triangles.Length / 3,
                            Title
                        );
                    else
                        sTitle = string.Format(
                            "{1} ({0} Vertices)",
                            previewMeshFilter.sharedMesh.vertexCount,
                            Title
                        );
                }
                else
                    sTitle = Title;

                titleContent = new GUIContent(sTitle);
                SceneView.RepaintAll();
            }
        }

        private void Preview(SceneView sceneView)
        {
            if (!previewMesh)
                return;

            Vector3[] vts = previewMesh.vertices;
            int[] tris = new int[0];
            if (Mode != VERTEXLINE)
                tris = previewMesh.triangles;
            Handles.color = Color.green;
            Handles.matrix = Matrix4x4.TRS(
                new Vector3(
                    0,
                    0,
                    0
                ),
                Quaternion.identity,
                Vector3.one
            );
            for (int i = 0; i < tris.Length; i += 3)
                Handles.DrawPolyLine(
                    vts[tris[i]],
                    vts[tris[i + 1]],
                    vts[tris[i + 2]],
                    vts[tris[i]]
                );

            Handles.color = Color.gray;
            for (int i = 0; i < vts.Length; i++)
                Handles.CubeHandleCap(
                    0,
                    vts[i],
                    Quaternion.identity,
                    HandleUtility.GetHandleSize(vts[i]) * 0.07f,
                    EventType.Repaint
                );
            ArrayPools.Vector3.Free(vts);
        }
    }
}