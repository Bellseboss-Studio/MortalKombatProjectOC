using System.Collections.Generic;
using UnityEngine;

public class ActivableEnableDisableObjects : ActivableTutorial
{
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