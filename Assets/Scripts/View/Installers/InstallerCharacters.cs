using System;
using Unity.Cinemachine;
using FactoryCharacterFiles;
using InputSystemCustom;
using ServiceLocatorPath;
using UnityEngine;
using UnityEngine.InputSystem;
using View.Characters;

namespace View.Installers
{
    public class InstallerCharacters : MonoBehaviour
    {
        [SerializeField] private CharactersConfiguration charactersConfiguration;
        [SerializeField] private string idCharacter;
        [SerializeField] private CinemachineVirtualCamera cameraMain;
        [SerializeField] private CinemachineFreeLook cameraMainFreeLook, secondCamera;
        [SerializeField] private CinemachineInputProvider _cinemachineInputProvider;
        [SerializeField] private CinemachineTargetGroup group;
        private GameObject player, pointFar;
        private InputActionReference inputCamera;
        private bool isInPause;

        private void Awake()
        {
            Application.targetFrameRate = 60;
        }

        private void Start()
        {
            var charactersFactory = new CharactersFactory(Instantiate(charactersConfiguration));
            var character = (PlayerCharacter) charactersFactory.Create(idCharacter)
                .WithInput(TypeOfInputs.PlayerControl).InPosition(transform.position)
                .WithMainCamera(cameraMainFreeLook.gameObject)
                .Build();
            character.ConfigureCameras(cameraMainFreeLook, secondCamera, group);
            cameraMainFreeLook.Follow = character.GetPointToCamera();
            cameraMainFreeLook.LookAt = character.GetPointToCamera();
            //ServiceLocator.Instance.GetService<IObserverUI>().Observer(character, character);
            ServiceLocator.Instance.GetService<IPauseMainMenu>().onPause += OnPause;
            //inputCamera = _cinemachineInputProvider.XYAxis;
        }

        private void OnPause(bool ispause)
        {
            _cinemachineInputProvider.XYAxis = ispause ? null : inputCamera;
        }
    }
}
