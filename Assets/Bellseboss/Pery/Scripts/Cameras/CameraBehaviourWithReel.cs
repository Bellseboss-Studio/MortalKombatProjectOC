using System;
using System.Collections.Generic;
using Bellseboss.Angel;
using Bellseboss.Pery.Scripts.Input;
using Unity.Cinemachine;
using DG.Tweening;
using UnityEngine;

public class CameraBehaviourWithReel : MonoBehaviour
{
    [SerializeField] private CinemachineSmoothPath path;
    [SerializeField] private CinemachineVirtualCamera camera;
    [SerializeField] private CameraCollider[] cameraColliders;
    [SerializeField] private CameraCollider roomCollider;
    private ICameraBehaviour _cameraBehaviour;
    private int _index;
    public Action<int, bool, CinemachineVirtualCamera> ChangeCamera { get; set; }
    public CinemachineVirtualCameraBase Camera => camera;
    [SerializeField] private List<InRoomsTransition> inRoomsTransition;
    [SerializeField] private List<BetweenRoomsTransition> betweenRoomsTransitions;

    public List<BetweenRoomsTransition> BetweenRoomsTransitions => betweenRoomsTransitions;
    private Sequence _sequence;

    [Serializable]
    public class RoomCameras
    {
        public CameraCollider[] CameraColliders;
        [SerializeField] private List<InRoomsTransition> inRoomsTransition;
        [SerializeField] private List<BetweenRoomsTransition> betweenRoomsTransitions;
    }
    
    private void Start()
    {
        _sequence = DOTween.Sequence();
        /*Debug.Log($"CameraBehaviourWithReel:Start - {path.m_Waypoints.Length}");*/
        for (var i = 0; i < inRoomsTransition.Count; i++)
        {
            var cameraCollider = cameraColliders[i];
            var index = i;
            cameraCollider.ColliderEnter += (o, room) => OnColliderEnter(index);
        }
        /*Debug.Log($"CameraBehaviourWithReel:Start - {path.m_Waypoints.Length}");
        for (var i = 0; i < cameraColliders.Length; i++)
        {
            var cameraCollider = cameraColliders[i];
            var index = i;
            cameraCollider.ColliderEnter += (o) => OnColliderEnter(index);
        }
        */
        roomCollider.ColliderEnter += EnterToRoom;
        roomCollider.ColliderExit += ExitToRoom;
    }
    
    private void EnterToRoom(GameObject o, CameraCollider cameraCollider)
    {
        ChangeCamera?.Invoke(_index, true, camera);
    }

    private void ExitToRoom(GameObject o, CameraCollider cameraCollider)
    {
        ChangeCamera?.Invoke(_index, false, camera);
    }

    private void OnColliderEnter(int i)
    {
        _sequence.Kill();
        _sequence = DOTween.Sequence();
        _sequence.Insert(0,
            camera.transform.DOMove(inRoomsTransition[i].CameraPosition, inRoomsTransition[i].transitionTime)
                .SetEase(inRoomsTransition[i].easeType));


        /*//Debug.Log($"CameraBehaviourWithReel:OnColliderEnter: {i}");
        CinemachineTrackedDolly trackedDolly = camera.GetCinemachineComponent<CinemachineTrackedDolly>();
        if (trackedDolly != null)
        {
            trackedDolly.m_PathPosition = i;
        }else
        {
            //Debug.LogError("No CinemachineTrackedDolly component found in camera body.");
        }*/
    }

    public void Config(ICameraBehaviour cameraBehaviour, int index)
    {
        _index = index;
        _cameraBehaviour = cameraBehaviour;
    }
}