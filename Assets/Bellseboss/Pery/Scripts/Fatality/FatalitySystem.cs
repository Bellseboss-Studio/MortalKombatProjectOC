using System;
using System.Collections.Generic;
using Bellseboss.Pery.Scripts.Input;
using UnityEngine;
using Unity.Cinemachine;
using TMPro;
using UnityEngine.Playables;

public class FatalitySystem : MonoBehaviour, IFatalitySystem
{
    [SerializeField] private StatePatter statePatterFatality;
    [SerializeField] private CinemachineTargetGroup cinematicTargetGroup;
    [SerializeField] private PlayableDirector playableDirector;
    [SerializeField] private int countOfInputToRead;
    [SerializeField] private List<InputRead> inputsRead = new List<InputRead>();
    [SerializeField] private List<GroupInputs> inputsToValidate;
    [SerializeField] private CinemachineVirtualCameraBase cinematicVirtualCameraBase;
    [SerializeField] private GameObject panelTitleFatality, panelInputs;
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private UiFatalityInputs uiFatalityInputs;
    [SerializeField] private GameObject audioSourceFatality;
    [SerializeField] private CompositeToFatality compositeToFatality;
    public Action<INPUTS> OnInputPressed;
    private IFatality _characterV2;
    private ICharacterV2 _cV2;
    private bool _isFatality;
    private EnemyV2 enemy;
    private bool _isCinematicFinished;
    private bool canChangePosition;
    private float deltaTimeLocal;

    public void Configure(IFatality characterV2, ICharacterV2 cV2)
    {
        _characterV2 = characterV2;
        _cV2 = cV2;
        var dic = new Dictionary<STATE_FATALITY, StatePatterFatality>
        {
            { STATE_FATALITY.IDLE, new IdleState(this) },
            { STATE_FATALITY.CINEMATIC, new CinematicState(this) },
            { STATE_FATALITY.INPUTS, new WaitForInputs(this, countOfInputToRead) },
            { STATE_FATALITY.VALIDATE, new ValidateInputs(this, inputsToValidate) },
            { STATE_FATALITY.FATALITY, new FatalityState(this) },
            { STATE_FATALITY.NO_FATALITY, new NoFatalityState(this) }
        };
        statePatterFatality.Configure(this, STATE_FATALITY.IDLE, dic);
        statePatterFatality.StartStates();
        cinematicVirtualCameraBase.gameObject.SetActive(false);
        uiFatalityInputs.Configure(this);
    }

    public void Fatality()
    {
        _cV2.DisableControls();
        _isFatality = true;
        enemy = _characterV2.GetEnemyToKillWithFatality().GetComponent<EnemyV2>();
        cinematicTargetGroup.m_Targets = new[]
        {
            new CinemachineTargetGroup.Target {Object = _cV2.GetGameObject(), Radius = 1, Weight = 1},
            new CinemachineTargetGroup.Target {Object = enemy.transform, Radius = 1, Weight = 1}
        };
        cinematicVirtualCameraBase.gameObject.SetActive(true);
        enemy.DisableControls();
        enemy.DisableColliders();
        enemy.Mareado();
        compositeToFatality.StartEqualizePosition();
        compositeToFatality.Coordinator(_cV2.Model3DInstance, enemy.gameObject);
    }

    public bool IsStartFatality()
    {
        return _isFatality;
    }

    public void StartCinematic()
    {
        playableDirector.Play();
        ShowPanelTitle("Finish Them!");
    }

    public bool FinishedCinematic()
    {
        return playableDirector.state != PlayState.Playing;
    }

    public bool ReadInput(out INPUTS input)
    {
        if (_characterV2.ReadInput(out input))
        {
            OnInputPressed?.Invoke(input);
            return true;
        }
        return false;
    }

    public List<InputRead> GetInputs()
    {
        return inputsRead;
    }

    public void ContinueCinematic()
    {
        playableDirector.Resume();
    }

    public void EndOfFatality()
    {
        _isFatality = false;
        _cV2.EnableControls();
        cinematicVirtualCameraBase.gameObject.SetActive(false);
        inputsRead.Clear();
        canChangePosition = false;
        enemy.EnableColliders();
    }

    public void HidePanelTitle()
    {
        panelTitleFatality.SetActive(false);
    }

    public void ShowPanelInputs()
    {
        panelInputs.SetActive(true);
    }
    
    public void HidePanelInputs()
    {
        panelInputs.SetActive(false);
    }

    public void ShowPanelTitle()
    {
        panelTitleFatality.SetActive(true);
    }

    public void ShowPanelTitle(string title)
    {
        panelTitleFatality.SetActive(true);
        this.title.text = title;
    }

    public void CanReadInputs(bool b)
    {
        _characterV2.StartToReadInputs(b);
    }

    public void StartAudioFatality()
    {
        audioSourceFatality.SetActive(true);
    }

    public void RestartAllElements()
    {
        audioSourceFatality.SetActive(false);
        compositeToFatality.FinishFatality();
        _cV2.Model3DInstance.transform.localPosition = Vector3.zero;
        _cV2.Model3DInstance.transform.localRotation = Quaternion.identity;
        uiFatalityInputs.DefaultValue();
    }

    public void FatalityPlayer()
    {
        _characterV2.StartAnimationFatality();
        canChangePosition = true;
    }

    public void FatalityEnemy()
    {
        enemy.StartAnimationFatality();
        enemy.CanMove(false);
        enemy.CanRotate(false);
        enemy.DisableControls();
        enemy.DisableColliders();
    }

    public void FatalityComposite()
    {
        compositeToFatality.Fatality();
    }

    public void CanReadInputsToFatality(bool canRead)
    {
        _characterV2.StartToReadInputsToFatality(canRead);
    }

    public void PauseCinematic()
    {
        playableDirector.Pause();
    }
    
    
    private void Update()
    {
        /*if(!canChangePosition) return;
        enemy.SetPositionAndRotation(refOfEnemy);
        deltaTimeLocal += Time.deltaTime;
        if (deltaTimeLocal >= playableDirector.duration)
        {
            canChangePosition = false;
            _cV2.EnableControls();
        }*/
    }
}