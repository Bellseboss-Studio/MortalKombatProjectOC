using System.Collections;
using UnityEngine;


namespace MortalKombat.Audio
{
    public class ActivatableSpeaker : ActivableTutorial, IFmodClient
    {
        [SerializeField] private NarratorDialogues m_Id;

        public override void Activate()
        {
            try
            {
                if (IsFinished) return;
                StartCoroutine(WentToFinish(FmodFacade.PlayToGetMilliseconds("event:/DX/", m_Id.ToString())));
            }
            catch
            {
                // Debug.Log("error on Activatable Speaker");
            }
            
        }

        private IEnumerator WentToFinish(int milliseconds = 0)
        {
            yield return new WaitForSeconds(milliseconds / 1000);
            Finish();
        }

        public override void Deactivate()
        {
            FmodFacade.Stop();
        }
    }

    public interface IFmodClient
    {
    }
}
