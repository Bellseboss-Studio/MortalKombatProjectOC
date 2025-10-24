using System;
using _Scripts.Player;
using Bellseboss.Pery.Scripts.Input;
using Unity.Cinemachine;
using UnityEngine;

public class CameraTrigger : MonoBehaviour, ICameraTrigger
{
    public CinemachineVirtualCameraBase Camera => camera;
    public Action<int> ChangeCamera { get; set; }
    [SerializeField] private CinemachineVirtualCameraBase camera;
    [SerializeField] private CameraCollider cameraCollider;
    private int _index;
    

    private void Reset()
    {
        camera = GetComponentInChildren<CinemachineVirtualCameraBase>();
        cameraCollider = GetComponentInChildren<CameraCollider>();
    }

    public void Config(CharacterV2 character, int i)
    {
        _index = i;
        var transform1 = character.transform;
        camera.LookAt = transform1;
        cameraCollider.ColliderEnter += OnColliderEnter;
    }

    private void OnColliderEnter(GameObject o, CameraCollider room)
    {
        Debug.Log($"CameraTrigger:OnColliderEnter: {_index}");
        ChangeCamera?.Invoke(_index);
    }
}