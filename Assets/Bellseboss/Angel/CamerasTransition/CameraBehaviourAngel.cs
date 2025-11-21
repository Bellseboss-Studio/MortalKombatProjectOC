using System;
using System.Collections.Generic;
using System.Linq;
using _Scripts.Interact;
using Bellseboss.Angel;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;

public class CameraBehaviourAngel : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera camera;
    [SerializeField] private List<Room> rooms;
    private Sequence _sequence;
    private CameraCollider _currentRoom;
    private bool _transitioningToRoom;

    [Serializable]
    public class Room
    {
        public string name;
        public CameraCollider roomCollider;
        public List<InRoomsTransition> inRoomsTransitions;
        public List<BetweenRoomsTransition> betweenRoomsTransitions;
    }

    private void Awake()
    {
        _sequence = DOTween.Sequence();

        foreach (var room in rooms)
        {
            room.roomCollider.ColliderEnter += EnterToRoom;
            room.roomCollider.ColliderExit += ExitToRoom;
        }

        foreach (var room in rooms)
        {
            for (var i = 0; i < room.inRoomsTransitions.Count; i++)
            {
                var inRoomTransition = room.inRoomsTransitions[i];
                var index = i;
                inRoomTransition.cameraCollider.ColliderEnter += (o, room) => OnColliderEnter(index);
            }
        }
        
        _currentRoom = rooms[0].roomCollider;
    }

    private void EnterToRoom(GameObject o, CameraCollider room)
    {
        RoomTransition(room, _currentRoom);
        _currentRoom = room;
    }

    private void RoomTransition(CameraCollider finishingRoom, CameraCollider currentRoom)
    {
        Transform[] cameraPoints = { };
        var found = false;
        var betweenRoomsTransition = new BetweenRoomsTransition();

        foreach (var room in rooms.Where(room => room.roomCollider == currentRoom))
        {
            foreach (var transition in room.betweenRoomsTransitions.Where(transition =>
                         transition.finishingRoom == finishingRoom))
            {
                cameraPoints = transition.cameraPoints;
                betweenRoomsTransition = transition;
                found = true;
                break;
            }
        }

        if (!found) return;
        var pathPoints = new Vector3[cameraPoints.Length];

        for (int i = 0; i < cameraPoints.Length; i++)
        {
            pathPoints[i] = cameraPoints[i].position;
        }

        _sequence.Kill();
        _sequence = DOTween.Sequence();
        _sequence.Insert(0,
            camera.transform.DOPath(pathPoints, betweenRoomsTransition.transitionTime, PathType.CatmullRom)
                .SetEase(betweenRoomsTransition.easeType));

        _sequence.OnComplete(() => _transitioningToRoom = false);
        _transitioningToRoom = true;
    }

    private void ExitToRoom(GameObject o, CameraCollider room)
    {
    }

    private void OnColliderEnter(int i)
    {
        if (_transitioningToRoom) return;
        InRoomsTransition inRoomTransition = new InRoomsTransition();

        foreach (var room in rooms.Where(room => room.roomCollider == _currentRoom))
        {
            inRoomTransition = room.inRoomsTransitions[i];
        }

        _sequence.Kill();
        _sequence = DOTween.Sequence();
        _sequence.Insert(0,
            camera.transform.DOMove(inRoomTransition.CameraPosition, inRoomTransition.transitionTime)
                .SetEase(inRoomTransition.easeType));
    }
}