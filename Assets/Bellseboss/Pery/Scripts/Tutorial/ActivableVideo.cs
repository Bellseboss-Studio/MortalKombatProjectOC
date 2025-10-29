using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class ActivableVideo : ActivableTutorial
{
    [SerializeField] VideoPlayer _videoPlayer;
    public override void Activate()
    {
        _videoPlayer.Play();
        StartCoroutine(WaitFinishedVideo(_videoPlayer.clip.length));
    }

    private IEnumerator WaitFinishedVideo(double clipLength)
    {
        yield return new WaitForSeconds((float)clipLength);
        Finish();
    }

    public override void Deactivate()
    {
        _videoPlayer.time = _videoPlayer.clip.length;
    }
}