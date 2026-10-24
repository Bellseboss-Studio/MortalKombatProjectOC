using System;
using UnityEngine;

[RequireComponent(typeof(TutorialSystem))]
public class SkipTutorial : MonoBehaviour
{
    private TutorialSystem tutorialSystem;

    private void Awake()
    {
        tutorialSystem = GetComponent<TutorialSystem>();
    }

    public void Skip()
    {
        tutorialSystem.Skip();
    }
}