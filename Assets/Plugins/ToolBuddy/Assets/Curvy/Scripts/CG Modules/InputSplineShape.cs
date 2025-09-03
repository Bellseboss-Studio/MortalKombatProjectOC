// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using FluffyUnderware.Curvy.Shapes;
using FluffyUnderware.DevTools;
using UnityEngine;

namespace FluffyUnderware.Curvy.Generator.Modules
{
    [ModuleInfo(
        "Input/Spline Shape",
        ModuleName = "Input Spline Shape",
        Description = "Spline Shape"
    )]
    [HelpURL(AssetInformation.DocsRedirectionBaseUrl + "cginputsplineshape")]
#pragma warning disable 618
    public class InputSplineShape : SplineInputModuleBase, IExternalInput, IOnRequestProcessing, IPathProvider
#pragma warning restore 618
    {
        [HideInInspector]
        [OutputSlotInfo(typeof(CGShape))]
        public CGModuleOutputSlot OutShape = new CGModuleOutputSlot();

        #region ### Serialized Fields ###

        [Tab(
            "General",
            Sort = 0
        )]
        [SerializeField, CGResourceManager("Shape")]
        [FieldCondition(
            nameof(m_Shape),
            null,
            false,
            ActionAttribute.ActionEnum.ShowWarning,
            "Missing Shape input"
        )]
        private CurvySpline m_Shape;

        #endregion

        #region ### Public Properties ###

        public CurvySpline Shape
        {
            get => m_Shape;
            set
            {
                if (m_Shape != value)
                {
                    m_Shape = value;
                    if (IsActiveAndEnabled) OnSplineAssigned();
                    ValidateStartAndEndCps();
                    Dirty = true;
                }
            }
        }

        public bool SupportsIPE => FreeForm;

        public bool FreeForm
        {
            get => Shape != null && Shape.GetComponent<CurvyShape>() == null;
            set
            {
                if (Shape != null)
                {
                    CurvyShape sh = Shape.GetComponent<CurvyShape>();
                    if (value && sh != null)
                        sh.Delete();
                    else if (!value && sh == null)
                        Shape.gameObject.AddComponent<CSCircle>();
                }
            }
        }

        #endregion


        #region ### IOnRequestPath ###

        public CGData[] OnSlotDataRequest(CGModuleInputSlot requestedBy, CGModuleOutputSlot requestedSlot,
            params CGDataRequestParameter[] requests)
        {
            CGDataRequestRasterization raster = GetRequestParameter<CGDataRequestRasterization>(ref requests);
            CGDataRequestMetaCGOptions options = GetRequestParameter<CGDataRequestMetaCGOptions>(ref requests);

            if (!raster || raster.RasterizedRelativeLength == 0)
                return Array.Empty<CGData>();
            CGData data = GetSplineData(
                Shape,
                false,
                raster,
                options
            );
            return data == null
                ? Array.Empty<CGData>()
                : new[] { data };
        }

        #endregion

        #region ### Public Methods ###

        public T SetManagedShape<T>() where T : CurvyShape2D
        {
            if (!Shape)
                Shape = (CurvySpline)AddManagedResource("Shape");

            CurvyShape sh = Shape.GetComponent<CurvyShape>();

            if (sh != null)
                sh.Delete();
            return Shape.gameObject.AddComponent<T>();
        }

        public void RemoveManagedShape()
        {
            if (Shape)
                DeleteManagedResource(
                    "Shape",
                    Shape
                );
        }

        #endregion

        #region ### Protected members ###

        protected override CurvySpline InputSpline
        {
            get => Shape;
            set => Shape = value;
        }

        protected override void OnSplineAssigned()
        {
            base.OnSplineAssigned();
            if (Shape)
                Shape.RestrictTo2D = true;
        }

        #endregion
    }
}