using System;
using Unity.Cinemachine;
using FMODUnity;
using UnityEngine;
using UnityEngine.Serialization;

namespace MortalKombat.Audio
{
    public class DollyCartAudio : MonoBehaviour
    {
        [SerializeField] private CinemachinePathBase m_PathToFollow;
        [SerializeField] private CinemachineDollyCart m_CinemachineDollyCart; 
        [SerializeField] private StudioEventEmitter m_Emitter; 
        [SerializeField] private float m_MaxDistance = 30f;
        [SerializeField] private bool m_UseEmitterDistance = false;

        CinemachinePathBase.PositionUnits m_PositionUnits = CinemachinePathBase.PositionUnits.PathUnits;

        private Vector3 m_PlayerPosition;
        private float m_PositionInPathUnits;
        private const string m_DistanceParameterName = "Distance";

        
        private void Awake()
        {
            if (!m_Emitter)
            {
                m_Emitter = GetComponent<StudioEventEmitter>();
            }

            if (m_Emitter.IsPlaying())
            {
                m_Emitter.Stop();
            }

            if (m_UseEmitterDistance)
            {
                m_MaxDistance = m_Emitter.OverrideMaxDistance;
            }
        }

        public void SetCartPositionPosition(Vector3 position)
        {
            m_PlayerPosition = position;
            SetCartPosition(m_PathToFollow.FindClosestPoint(m_PlayerPosition, 0, -1, 10));
            UpdateDistanceParameter();
            PlaySFX();
        }

        private float CalculateDistanceFromPlayer()
        {
            float distance = Vector3.Distance(m_PlayerPosition, transform.position);
            return Mathf.Max(0, distance);
        }
        
        void SetCartPosition(float distanceAlongPath)
        {
            m_PositionInPathUnits = m_PathToFollow.StandardizeUnit(distanceAlongPath, m_PositionUnits);
            m_CinemachineDollyCart.m_Position = m_PositionInPathUnits;
            transform.position = m_PathToFollow.EvaluatePositionAtUnit(m_PositionInPathUnits, m_PositionUnits);
            transform.rotation = m_PathToFollow.EvaluateOrientationAtUnit(m_PositionInPathUnits, m_PositionUnits);
        }
        
        float NormalizeDistanceValue(float value, float min = 0f, float max = 30f)
        {
            if (value > max)
            {
                value = max;
            }
            return (value - min) / (max - min);
        }

        private void UpdateDistanceParameter()
        {
            m_Emitter.SetParameter(m_DistanceParameterName, NormalizeDistanceValue(CalculateDistanceFromPlayer()), true); //
        }

        public void PlaySFX()
        {
            if (m_Emitter.IsPlaying() && CalculateDistanceFromPlayer() > m_MaxDistance)
            {
                m_Emitter.Stop();
            }
            
            if (!m_Emitter.IsPlaying() && CalculateDistanceFromPlayer() <= m_MaxDistance)
            {
                m_Emitter.Play();
            }    
        }
    }
}