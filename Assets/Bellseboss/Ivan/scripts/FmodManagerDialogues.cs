using System;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using FMOD.Studio;

namespace MortalKombat.Audio
{
    public class FmodManagerDialogues : IFmodManagerDx
    {
        private const string m_DxFolder = "event:/DX/";
        private EventInstance m_EventInstance;
        
        public FmodManagerDialogues(string dialogueToPlay)
        {
            m_EventInstance = RuntimeManager.CreateInstance(m_DxFolder + dialogueToPlay);
            m_EventInstance.setCallback(CallbackMethod);
        }

        private FMOD.RESULT CallbackMethod(EVENT_CALLBACK_TYPE type, IntPtr eventInstance, IntPtr parameters)
        {
            // Debug.Log($"CallbackMethod: {type}");
            if (type == EVENT_CALLBACK_TYPE.STOPPED)
            {
                // Code to execute when the SFX stops
                // Debug.Log("SFX has stopped.");
            }

            return FMOD.RESULT.OK;
        }
        public void PlaySfx()
        {
            m_EventInstance.start();
            m_EventInstance.release();
        }

        public PLAYBACK_STATE GetStatus()
        {
            m_EventInstance.getPlaybackState(out var state);
            return state;
        }
        
    }
}