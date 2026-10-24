using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivableEnableDisableObjectsWithTime : ActivableTutorial
{
    [SerializeField] private float timeToWait;
    [SerializeField] private List<GameObject> objectsToEnable;
    [SerializeField] private bool isEnable = true;

    private void Start()
    {
        foreach (var obj in objectsToEnable)
        {
            obj.SetActive(!isEnable);
        }
    }

    public override void Activate()
    {
        if(IsFinished) return;
        foreach (var obj in objectsToEnable)
        {
            obj.SetActive(isEnable);
        }
        StartCoroutine(WaitForTime());
    }

    private IEnumerator WaitForTime()
    {
        yield return new WaitForSeconds(timeToWait);
        foreach (var o in objectsToEnable)
        {
            o.SetActive(!isEnable);
        }
        Finish();
    }

    public override void Deactivate()
    {
        foreach (var obj in objectsToEnable)
        {
            obj.SetActive(!isEnable);
        }
    }
}