using System.Collections;
using System.Collections.Generic;
using FMOD;
using FMOD.Studio;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class GetEventLength : MonoBehaviour
{
    public FMODUnity.EventReference eventReference;
    public FMOD.Studio.EventInstance eventInstance; 
    private int length;
    void Start()
    {
        eventInstance = FMODUnity.RuntimeManager.CreateInstance(eventReference);
        eventInstance.start();
        eventInstance.getDescription(out var eventDescription);
        eventDescription.getLength(out var length);
        // Debug.Log($"Length of Event: {length}ms");
        eventInstance.release();
    }
}
