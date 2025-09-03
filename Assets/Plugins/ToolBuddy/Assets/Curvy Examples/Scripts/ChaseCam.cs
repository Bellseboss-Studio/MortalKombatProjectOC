// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.DevTools;
using JetBrains.Annotations;
using UnityEngine;

namespace FluffyUnderware.Curvy.Examples
{
    [ExecuteAlways]
    public class ChaseCam : MonoBehaviour
    {
        public Transform LookAt;
        public Transform MoveTo;
        public Transform RollTo;

        [Positive]
        public float ChaseTime = 0.5f;


        private Vector3 mVelocity;
        private Vector3 mRollVelocity;

#if UNITY_EDITOR
        [UsedImplicitly]
        private void Update()
        {
            if (!Application.isPlaying)
            {
                if (MoveTo)
                    transform.position = MoveTo.position;
                if (LookAt)
                {
                    if (!RollTo) transform.LookAt(LookAt);
                    else
                        transform.LookAt(
                            LookAt,
                            RollTo.up
                        );
                }
                // if (RollTo)
                //     transform.rotation = Quaternion.Euler (new Vector3 (transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, RollTo.rotation.eulerAngles.z));
            }
        }
#endif

        // Update is called once per frame
        [UsedImplicitly]
        private void LateUpdate()
        {
            if (MoveTo)
                transform.position = Vector3.SmoothDamp(
                    transform.position,
                    MoveTo.position,
                    ref mVelocity,
                    ChaseTime
                );
            if (LookAt)
            {
                if (!RollTo) transform.LookAt(LookAt);
                else
                    transform.LookAt(
                        LookAt,
                        Vector3.SmoothDamp(
                            transform.up,
                            RollTo.up,
                            ref mRollVelocity,
                            ChaseTime
                        )
                    );
            }
            // if (RollTo)
            //     transform.rotation = Quaternion.Euler (new Vector3 (transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, RollTo.rotation.eulerAngles.z));
        }
    }
}