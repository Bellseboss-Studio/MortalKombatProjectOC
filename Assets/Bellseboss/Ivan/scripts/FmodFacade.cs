using FMOD.Studio;
using FMODUnity;

namespace MortalKombat.Audio
{
    public class FmodFacade
    {
        private static EventInstance mEventInstance;

        public static int PlayToGetMilliseconds(string mUiFolder, string dialogueToPlay)
        {
            mEventInstance = RuntimeManager.CreateInstance(mUiFolder + dialogueToPlay);
            mEventInstance.start();
            mEventInstance.release();
            mEventInstance.getDescription(out var description);
            description.getLength(out var length);
            return length;
        }
        
        public static void PlayOneShot(string eventDx, string sfxToPlay)
        {
            RuntimeManager.PlayOneShot($"{eventDx}{sfxToPlay}");
        }

        public static void Stop()
        {
            mEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        }
    }
}