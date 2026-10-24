using System;
using System.Linq;
using _Scripts.Player;
using Bellseboss.Pery.Scripts.Input;
using Unity.Cinemachine;
using UnityEngine;

public class CameraBehaviour : MonoBehaviour, ICameraBehaviour
{
    [SerializeField] private CharacterV2 character;
    [SerializeField] private CameraBehaviourWithReel[] rooms;
    private CameraBehaviourWithReel _currentRoom;

    private void Start()
    {
        for (var i = 0; i < rooms.Length; i++)
        {
            var room = rooms[i];
            var index = i;
            room.Config(this, index);
            room.ChangeCamera += OnChangeCamera;
            room.Camera.gameObject.SetActive(false);
        }
        _currentRoom = rooms[0];
        _currentRoom.Camera.gameObject.SetActive(true);
        character.SetCamera(_currentRoom.Camera);
    }

    private void OnChangeCamera(int index, bool isOpen, CinemachineVirtualCamera camera)
    {
        if (isOpen)
        {
            /*foreach (var transition in _currentRoom.BetweenRoomsTransitions.Where(transition => transition.transitioningCamera == rooms[index]))
            {
                /*transition.cinemachineSmoothPath.#1#
            }
            _currentRoom.Camera.gameObject.SetActive(false);
            _currentRoom = rooms[index];
            _currentRoom.Camera.gameObject.SetActive(true);
            character.SetCamera(camera);*/
        }
    }
}