// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using FluffyUnderware.DevTools;

namespace FluffyUnderware.Curvy.Controllers
{
    #region ### Controller Events ###

    [Serializable]
    public class ControllerEvent : UnityEventEx<CurvyController> { }


    //TODO Use CurvyControllerSwitchEvent
    //public class CurvyControllerSwitchEvent : UnityEventEx<CurvyControllerSwitchEventArgs> { }

    //public class CurvyControllerSwitchEventArgs : EventArgs
    //{
    //    /// <summary>
    //    /// The controller raising the event
    //    /// </summary>
    //    public CurvyController Controller { get; private set; }
    //    public CurvySpline SourceSpline { get; private set; }
    //    public CurvySpline DestinationSpline { get; private set; }
    //    public float TFOnSource { get; private set; }
    //    public float TFOnDestination { get; private set; }
    //    public CurvyControllerDirection DirectionOnSource { get; private set; }
    //    public CurvyControllerDirection DirectionOnDestination { get; private set; }
    //    public float SwitchTimeStart { get; private set; }
    //    public float SwitchDuration { get; private set; }
    //    public float SwitchProgression { get; private set; }


    //    public CurvyControllerSwitchEventArgs()
    //    {
    //    }

    //    public void Set(CurvyController controller, float switchTimeStart, float switchDuration, float switchProgression, CurvySpline sourceSpline, CurvySpline destinationSpline, float tfOnSource, float tfOnDestination, CurvyControllerDirection directionOnSource, CurvyControllerDirection directionOnDestination)
    //    {
    //        SwitchDuration = switchDuration;
    //        SwitchProgression = switchProgression;
    //        Controller = controller;
    //        SourceSpline = sourceSpline;
    //        DestinationSpline = destinationSpline;
    //        TFOnSource = tfOnSource;
    //        TFOnDestination = tfOnDestination;
    //        SwitchTimeStart = switchTimeStart;
    //        DirectionOnSource = directionOnSource;
    //        DirectionOnDestination = directionOnDestination;
    //    }
    //}

    #endregion
}