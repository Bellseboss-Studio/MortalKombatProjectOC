using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class ActivableVideo : ActivableTutorial
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private string nameOfVideo;

    public override void Activate()
    {
        if (videoPlayer == null)
        {
            Debug.LogError("[ActivableVideo] VideoPlayer no asignado.");
            return;
        }

        string path = System.IO.Path.Combine(Application.streamingAssetsPath, nameOfVideo);
        videoPlayer.url = path;

        // Prepara el video
        videoPlayer.Prepare();
        videoPlayer.prepareCompleted += OnVideoPrepared;
    }

    private void OnVideoPrepared(VideoPlayer source)
    {
        videoPlayer.prepareCompleted -= OnVideoPrepared; // evitar múltiples suscripciones
        videoPlayer.Play();

        // Usa videoPlayer.length — es válido una vez preparado
        StartCoroutine(WaitFinishedVideo(videoPlayer.length));
    }

    private IEnumerator WaitFinishedVideo(double clipLength)
    {
        if (clipLength <= 0)
        {
            Debug.LogWarning("[ActivableVideo] Duración desconocida, usando fallback de 5 segundos.");
            clipLength = 10f;
        }

        yield return new WaitForSeconds((float)clipLength);
        Finish();
    }

    public override void Deactivate()
    {
        if (videoPlayer.isPlaying)
            videoPlayer.Stop();
    }
}