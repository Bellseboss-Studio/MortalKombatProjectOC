using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

public class ActivableCinematic : ActivableTutorial
{
    [SerializeField] private PlayableDirector playableDirector;
    public override void Activate()
    {
        if(IsFinished) return;
        playableDirector.Play();
        StartCoroutine(WaitForCinematic());
    }

    private IEnumerator WaitForCinematic()
    {
        yield return new WaitForSeconds((float)playableDirector.duration);
        Finish();
    }

    public override void Deactivate()
    {
        //change the time and play the last frame
        playableDirector.time = (float)playableDirector.duration;
    }
}