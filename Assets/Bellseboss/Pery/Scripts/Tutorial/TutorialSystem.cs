using System.Collections.Generic;
using System.Linq;
using _Scripts.Player;
using UnityEngine;
using UnityEngine.Events;

public class TutorialSystem : MonoBehaviour
{
    [SerializeField] private List<ActivableTutorial> activables;
    [SerializeField] private bool isFinished;
    public UnityEvent OnStart;
    public UnityEvent OnFinish;
    private TeaTime _flow;
    private bool _allFinished;
    private CharacterV2 _character;
    private bool _skip;
    private bool _canSkip;

    private void Start()
    {
        _flow = this.tt().Pause().Add(() =>
        {
            foreach (var activable in activables)
            {
                activable.Activate();
            }

            _skip = false;
            _character?.DisableControls();
        }).Loop(h =>
        {
            _canSkip = true;
            _allFinished = true;
            foreach (var unused in activables.Where(actionable => !actionable.IsFinished))
            {
                _allFinished = false;
            }

            if (_allFinished || _skip)
            {
                h.Break();
            }
        }).Add(() => { _character?.EnableControls(); }).Add(() =>
        {
            if (_skip)
            {
                foreach (var activable in activables)
                {
                    activable.Deactivate();
                }
            }

            _character = null;
            isFinished = true;
            OnFinish?.Invoke();
            _canSkip = false;
        });
        OnStart?.Invoke();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent<CharacterV2>(out var character) && !isFinished)
        {
            _character = character;
            StartTutorial();
        }
    }

    public void StartTutorial()
    {
        _flow.Play();
    }

    public void Skip()
    {
        if (_canSkip)
        {
            // Debug.Log($"Skip tutorial from {gameObject.transform.parent.name}");
            _skip = true;
        }
    }
}