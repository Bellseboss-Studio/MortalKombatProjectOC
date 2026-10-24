using System.Collections.Generic;
using _Scripts.Player;
using Bellseboss.Pery.Scripts.Input;
using UnityEngine;
using UnityEngine.InputSystem;

public class DebugService : MonoBehaviour, IDebugService
{
    [SerializeField] private bool _isActionPressed;
    [SerializeField] private GameObject activateGameobject;
    private List<IFightZone> _fightZones;
    private bool _isLoadRagePressed;
    private bool _isLoadLifePressed;

    private void Awake()
    {
        if (FindObjectsOfType<DebugService>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        ServiceLocator.Instance.RegisterService<IDebugService>(this);
        activateGameobject.SetActive(false);
        _fightZones = new List<IFightZone>();
    }

    public void OnAction(InputAction.CallbackContext context)
    {
        _isActionPressed = (context.performed || context.started) && context.ReadValue<float>() > 0.5f;
        activateGameobject.SetActive(_isActionPressed);
    }

    public void F1(InputAction.CallbackContext context)
    {
        if (!CanUse(context)) return;
        foreach (var fightZone in _fightZones)
        {
            fightZone.Restart();
        }
    }

    public void F2(InputAction.CallbackContext context)
    {
        if (!CanUse(context)) return;
        foreach (var fightZone in _fightZones)
        {
            fightZone.SetLifeAllEnemiesTo(1);
        }
    }

    public void F3(InputAction.CallbackContext context)
    {
        if (!CanUse(context)) return;
        _isLoadRagePressed = !_isLoadRagePressed;
        if (_isLoadRagePressed)
        {
            ServiceLocator.Instance.GetService<IPlayer>().LoadRageTo(100);
        }
        else
        {
            ServiceLocator.Instance.GetService<IPlayer>().LoadRageTo(1);
        }
    }

    public void F4(InputAction.CallbackContext context)
    {
        if (!CanUse(context)) return;
        _isLoadLifePressed = !_isLoadLifePressed;
        if (_isLoadLifePressed)
        {
            ServiceLocator.Instance.GetService<IPlayer>().LoadLifeTo(100);
        }
        else
        {
            ServiceLocator.Instance.GetService<IPlayer>().LoadLifeTo(1);
        }
    }

    private bool CanUse(InputAction.CallbackContext context)
    {
        var result = _isActionPressed && context.started && context.ReadValue<float>() > 0.5f;
        // Debug.Log(
        //     $"_isActionPressed: {_isActionPressed} context.started: {context.started} context.ReadValue<float>(): {context.ReadValue<float>()} result: {result}");
        return result;
    }

    public void RegisterFightZone(IFightZone fightZone)
    {
        if (!_fightZones.Contains(fightZone))
            _fightZones.Add(fightZone);
    }
}

public interface IDebugService
{
    void RegisterFightZone(IFightZone fightZone);
}