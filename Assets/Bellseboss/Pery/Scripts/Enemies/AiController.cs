using System;
using _Scripts.Player;
using Bellseboss.Pery.Scripts.Input;
using UnityEngine;
using View;

internal class AiController : MonoBehaviour
{
    [SerializeField] private float timeToWaitToChangePath;
    private IEnemyV2 _enemy;
    private TeaTime _idle, _numberOfPath, _moving, _stayNearOfTarget, _watch;
    private TeaTime _watchEnemy, _nearOfEnemy, _died;
    private GameObject _target;
    private bool _isNearOfTarget;
    private int _indexOfPath;
    private bool _enemyIsDetected;
    private bool _enemyIsNear;
    private float _deltaTimeLocal;
    private bool _isDeath;



    public void Configure(IEnemyV2 enemy, ref Action endStunt)
    {
        endStunt += EndStunt;
        _indexOfPath = 0;
        _enemy = enemy;
        if(_enemy.Paths().Count == 0)
        {
            throw new Exception("Paths is empty");
        }
        _target = _enemy.Paths()[_indexOfPath];
        _enemy.OnDead += EnemyOnOnDead;
        _enemy.OnArriveToTarget += () =>
        {
            _isNearOfTarget = true;
        };
        
        _enemy.OnPlayerDetected += isDetected =>
        {
            if (_isDeath) return;
            _idle.Stop();
            _numberOfPath.Stop();
            _moving.Stop();
            _stayNearOfTarget.Stop();
            _watch.Stop();
            _watchEnemy.Stop();
            _nearOfEnemy.Stop();
            
            if(isDetected)
            {
                _enemy.CanMove(false);
                _watchEnemy.Play();
            }
            else
            {
                _idle.Play();
                _enemy.SetState(StatesOfEnemy.NORMAL);
            }
            _enemyIsDetected = isDetected;
        };
        
        _enemy.OnPlayerInNearZone += isNear =>
        {
            if (_isDeath) return;
            Debug.Log("entro is near");
            enemy.GetPlayer().GetIntoEnemyZone(gameObject, isNear);
            if(isNear)
            {
                _numberOfPath.Play();
            }
            else
            {
                _idle.Stop();
                _numberOfPath.Stop();
                _moving.Stop();
                _stayNearOfTarget.Stop();
                _watch.Stop();
                _watchEnemy.Stop();
                _nearOfEnemy.Stop();
                
                _watchEnemy.Play();
            }
            _enemyIsNear = isNear;
        };
        
        _idle = this.tt().Pause().Add(() =>
        {
            _numberOfPath.Play();
        });
        _numberOfPath = this.tt().Pause().Add(() =>
        {
            if (_isDeath) return;
            if(_enemy.Paths().Count > 0 || _enemy.GetPlayer() != null)
            {
                if (_enemy.GetPlayer() != null)
                {
                    _target = _enemy.GetPlayer().gameObject;
                    _enemy.SetState(StatesOfEnemy.ANGRY);
                }
                else
                {
                    _target =_enemy.Paths()[_indexOfPath];
                }
                _moving.Play();
            }
            else
            {
                _idle.Play();   
            }
        });
        
        _moving = this.tt().Pause().Add(() =>
        {
            _enemy.MoveTo(_target);
        }).Add(() =>
        {
            _stayNearOfTarget.Play();
        });
        _stayNearOfTarget = this.tt().Pause().Add(() =>
        {
        }).Wait(()=>_isNearOfTarget).Add(() =>
        {
            _isNearOfTarget = false;
            if(_enemy.GetPlayer() != null)
            {
                _enemy.CanMove(false);
                /*Debug.Log("aqui");*/
                _target = _enemy.GetPlayer().gameObject;
                _enemy.AttackPlayer();
                _stayNearOfTarget.Stop();
            }
            else
            {
                _enemy.TriggerAnimation("watch");
                _enemy.RotateToTargetIdle(_target, false);
            }
        }).Add(timeToWaitToChangePath).Add(() =>
        {
            if(_enemy.Paths().Count > 1)
            {
                if(_enemy.Paths().Count > _indexOfPath + 1)
                {
                    _indexOfPath++;
                }
                else
                {
                    _indexOfPath = 0;
                }
                _target = _enemy.Paths()[_indexOfPath];
                _moving.Play();
            }
            else
            {
                _watch.Play();
            }
        });
        
        _watch = this.tt().Pause().Add(() =>
        {
            _enemy.RotateToTargetIdle(_target, false);
        }).Wait(()=>_enemyIsDetected).Add(() =>
        {
            _watchEnemy.Play();
        });
        
        _watchEnemy = this.tt().Pause().Add(() =>
        {
            _enemy.CanMove(false);
            _enemy.MoveTo(_enemy.GetPlayer().gameObject);
        }).Wait(()=>_enemyIsNear).Add(() =>
        {
            _enemy.MoveTo(_enemy.GetPlayer().gameObject);
        }).Wait(()=>_isNearOfTarget).Add(() =>
        {
            _nearOfEnemy.Play();
        });
        
        _nearOfEnemy = this.tt().Pause().Add(() =>
        {
            _watchEnemy.Play();
        }); 
        
        _died = this.tt().Pause().Add(() =>
        {
            _idle.Stop();
            _numberOfPath.Stop();
            _moving.Stop();
            _stayNearOfTarget.Stop();
            _watch.Stop();
            _watchEnemy.Stop();
            _nearOfEnemy.Stop();
        }).Add(() =>
        {
            _enemy.CanMove(false);
            _enemy.TriggerAnimation("dead");
        }).Add(() =>
        {
            _enemy.Died();
        });

        StartAi();
    }

    private void EndStunt()
    {
        StartAi();
    }

    private void EnemyOnOnDead(EnemyV2 obj)
    {
        Debug.Log("AiController: Enemy is dead");
        _isDeath = true;
        /*_enemy.CanRotate(false);*/
        _died.Play();
    }

    public void SetPlayer(CharacterV2 characterV2)
    {
        
    }

    public void TakeDamage()
    {
        
    }

    public void StopAI()
    {
        
    }

    public void Clean()
    {
        
    }

    public void StartAi()
    {
        if (_isDeath) return;
        _idle.Play();
    }
    
}

public enum StatesOfEnemy
{
    ANGRY,
    NORMAL,
    SCARED
}