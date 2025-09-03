// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using JetBrains.Annotations;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.Generator
{
    public class CanvasState
    {
        public bool IsDragging;

        /// <summary>
        /// Was the Canvas drag initiated by the keyboard or the mouse?
        /// </summary>
        private bool isKeyboardDrag;

        public void Reset()
        {
            IsDragging = false;
            isKeyboardDrag = false;
        }

        public bool Update(
            Event @event)
        {
            bool isCanvasDragModifier = @event.keyCode == KeyCode.Space;
            EventType eventType = @event.type;
            bool eventWasUsed;
            if (IsDragging == false)
            {
                bool shouldStartKeyboardDrag = isCanvasDragModifier
                                               && eventType == EventType.KeyDown;
                bool shouldStartMouseDrag = eventType == EventType.MouseDown
                                            && @event.button == 2; //middle mouse button

                bool shouldStartDrag = shouldStartKeyboardDrag || shouldStartMouseDrag;
                if (shouldStartDrag)
                    StartDrag(shouldStartKeyboardDrag);
                eventWasUsed = shouldStartDrag;
            }
            else
            {
                bool shouldEndKeyboardDrag =
                    isKeyboardDrag
                    && eventType == EventType.KeyUp
                    && isCanvasDragModifier;
                bool shouldEndMouseDrag =
                    !isKeyboardDrag
                    && (eventType == EventType.MouseUp
                        //if mouse up happens outside of the canvas, it is not detected. We handle this case by detecting MouseMove when the mouse gets back inside the canvas.
                        || eventType == EventType.MouseMove);

                bool shouldEndDrag = shouldEndKeyboardDrag || shouldEndMouseDrag;
                if (shouldEndDrag)
                    EndDrag();
                eventWasUsed = shouldEndDrag;
            }

            return eventWasUsed;
        }

        [UsedImplicitly]
        private void StartDrag(
            bool isKeyboardInitiated)
        {
            IsDragging = true;
            isKeyboardDrag = isKeyboardInitiated;
        }

        private void EndDrag() =>
            IsDragging = false;
    }
}