using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UIElements;
using View.Characters;

namespace View
{
    public class CameraChange : MonoBehaviour
    {
        private Transform _pointToCamera;
        private CinemachineTargetGroup _targetGroup;
        private CinemachineFreeLook _camaraLibre, _camaraGrupal;
        private PlayerCharacter _playerCharacter;
        private bool _isCameraChanged;
        public bool IsCameraChanged => _isCameraChanged;
        private Transform _posicionDeCamaraInicial;
        [SerializeField] private float distancia, camy;
        
        public void AddGameObjectToTargetGroup(Transform transformm)
        {
            _targetGroup.AddMember(transformm, 1, 5);
        }
        
        public void RemoveGameObjectToTargetGroup(Transform transformm)
        {
            _targetGroup.RemoveMember(transformm);
        }

        public void LookEnemyAndPlayer()
        {
            _camaraGrupal.Priority = 11;
            _isCameraChanged = true;
            _playerCharacter.ChangeInputCustom(true);
        }

        public void FreeLookCamera()
        {
            _camaraGrupal.Priority = 1;
            _isCameraChanged = false;
            _playerCharacter.ChangeInputCustom(false);
        }

        public void Configure(CinemachineTargetGroup targetGroup, CinemachineFreeLook virtualCamera, CinemachineFreeLook camaraGrupal, Transform pointToCamera, PlayerCharacter playerCharacter)
        {
            _playerCharacter = playerCharacter;
            _targetGroup = targetGroup;
            _camaraLibre = virtualCamera;
            _camaraGrupal = camaraGrupal;
            _pointToCamera = pointToCamera;
            _targetGroup.AddMember(_playerCharacter.transform, 1, 4);
            //_posicionDeCamaraInicial.localPosition = posicionDeCamara.transform.position;
        }

        private void Update()
        {
            if (!_isCameraChanged) return;
            PositionateCamera();
        }

        private void PositionateCamera()
        {
            var posicion = Vector3.MoveTowards(_playerCharacter.transform.position, _playerCharacter.EnemiesInCombat[0].transform.position, distancia);
            posicion.y = _playerCharacter.transform.position.y + camy;
            //Debug.Log(posicion);
            _camaraGrupal.transform.position = posicion;
        }
    }
}