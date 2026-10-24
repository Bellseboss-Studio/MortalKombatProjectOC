using System;
using _Scripts.Player;
using Bellseboss.Angel.CombatSystem;
using Bellseboss.Pery.Scripts.Input;
using UnityEngine;
using Random = UnityEngine.Random;

public class AiControllerV2 : MonoBehaviour, IAiController
{
    [SerializeField] private float timeToWaitInThePosition;

    private IEnemyV2 _enemy;

    //Without player
    private TeaTime _idle, _getNextPath, _moveToTarget, _closeToTarget, _watch;
    private int _indexOfPath;

    private GameObject _target;

    //FindPlayer
    private TeaTime _findPlayer;

    //With player
    private TeaTime _watchPlayer,
        _getPositionAroundPlayer,
        _moveToPositionAroundPlayer,
        _waitToAttackPlayer,
        _getDirectionToPlayer,
        _moveForwardToPlayer,
        _attackPlayer;
    
    private TeaTime _fatality;

    private bool _isClose;
    private GameObject _randomPosition;
    private Vector3 _positionToPlayer;
    
    private bool _isInFatality;


    public void Configure(IEnemyV2 enemy, ref Action endStunt)
    {
        endStunt += EndStunt;
        _enemy = enemy;
        _enemy.OnReceiveDamage += OnReceiveDamage;

        _enemy.OnDead += EnemyOnOnDead;
        _enemy.OnPlayerDetected += isDetected =>
        {
            // Debug.Log($"OnPlayerDetected {isDetected}");

            StopAllStartIdle();
        };

        _enemy.OnPlayerInNearZone += isNear =>
        {
            Debug.Log($"OnPlayerInNearZone {isNear}");
            _isClose = isNear;
        };

        _idle = this.tt().Pause().Add(() =>
        {
            Debug.Log("Idle");
            _enemy.SetState(StatesOfEnemy.NORMAL);
            if (_enemy.IsDead)
            {
                _enemy.Died();
            }
            else
            {
                _findPlayer.Play();
            }
        });

        _findPlayer = this.tt().Pause().Add(() =>
        {
            if (!_enemy.GetPlayer())
            {
                //without player behavior
                Debug.Log("Player not found");
                _getNextPath.Play();
            }
            else
            {
                //With player behavior
                Debug.Log("Player found");
                _watchPlayer.Play();
            }
        });
        BehaviourWithoutPlayer();
        BehaviourWithPlayer();
        
        _fatality = this.tt().Pause().Add(() =>
        {
            Debug.Log("Fatality");
        });

        _randomPosition = new GameObject();

        StartAi();
    }

    private void StopAllStartIdle(bool startIdle = true)
    {
        //Stop all teatime
        Debug.Log($"name {gameObject.name}", gameObject);
        _idle.Stop();
        _findPlayer.Stop();
        _watchPlayer.Stop();
        _getPositionAroundPlayer.Stop();
        _moveToPositionAroundPlayer.Stop();
        _waitToAttackPlayer.Stop();
        _getDirectionToPlayer.Stop();
        _moveForwardToPlayer.Stop();
        _attackPlayer.Stop();
        _getNextPath.Stop();
        _moveToTarget.Stop();
        _watch.Stop();

        if (startIdle)
        {
            _idle.Play();
        }
    }

    private void OnReceiveDamage(StunInfo obj)
    {
        StopAllStartIdle(false);
        _enemy.SetInstaRotation(true);
        
    }

    private void BehaviourWithPlayer()
    {
        _watchPlayer = this.tt().Pause().Add(() =>
        {
            Debug.Log("WatchPlayer");
            _enemy.CanMove(false);
            _enemy.RotateToTargetIdle(_target, false);
            /*_enemy.RotateToTargetIdle(_enemy.GetPlayer().GetGameObject().gameObject, false);*/
        }).Loop(handler =>
        {
            if (_isClose)
            {
                handler.Break();
            }
        }).Add(() => { _getPositionAroundPlayer.Play(); });
        _getPositionAroundPlayer = this.tt().Pause().Add(() =>
        {
            Debug.Log("GetPositionAroundPlayer");
            _target = _enemy.GetPlayer().GetGameObject().gameObject;
            //Get position around player
            if (!_randomPosition)
            {
                _randomPosition = new GameObject();
            }

            _randomPosition.transform.position = GetRandomPositionAroundPoint(_target.transform.position, 5);
        }).Add(() => { _moveToPositionAroundPlayer.Play(); });

        _moveToPositionAroundPlayer = this.tt().Pause().Add(() =>
        {
            Debug.Log($"MoveToPositionAroundPlayer {_randomPosition.transform.position}");
            _enemy.MoveTo(_randomPosition);
        }).Loop(handler =>
        {
            var distance = Vector3.Distance(_enemy.GetGameObject().transform.position,
                _randomPosition.transform.position);
            if (distance < 1f)
            {
                handler.Break();
            }

            _enemy.MoveTo(_randomPosition);
        }).Add(() => { _waitToAttackPlayer.Play(); });

        _waitToAttackPlayer = this.tt().Pause().Add(() =>
        {
            Debug.Log($"WaitToAttackPlayer");
            _enemy.RotateToTargetIdle(_target, false);
            /*_enemy.RotateToTargetIdle(_enemy.GetPlayer().GetGameObject().gameObject, false);*/
        }).Add(2).Add(() => { _getDirectionToPlayer.Play(); });

        _getDirectionToPlayer = this.tt().Pause().Add(() =>
        {
            Debug.Log($"GetDirectionToPlayer");
            _positionToPlayer = _enemy.GetPlayer().GetGameObject().position;
            if (!_randomPosition)
            {
                _randomPosition = new GameObject();
            }

            _randomPosition.transform.position = GetPointBeyondTarget(_enemy.GetGameObject(),
                _enemy.GetPlayer().GetGameObject().gameObject, 3);
        }).Add(() => { _moveForwardToPlayer.Play(); });

        _moveForwardToPlayer = this.tt().Pause().Add(() =>
        {
            Debug.Log($"MoveForwardToPlayer");
            _enemy.SetState(StatesOfEnemy.ANGRY);
            _enemy.MoveTo(_randomPosition);
        }).Loop(handler =>
        {
            var distance = Vector3.Distance(_enemy.GetGameObject().transform.position, _positionToPlayer);
            if (distance < 1f)
            {
                handler.Break();
            }
        }).Add(() => { _attackPlayer.Play(); });

        _attackPlayer = this.tt().Pause().Add(() =>
        {
            Debug.Log($"AttackPlayer");
            _enemy.AttackPlayer();
        }).Loop(handler =>
        {
            var distance = Vector3.Distance(_enemy.GetGameObject().transform.position,
                _randomPosition.transform.position);
            if (distance < 1f)
            {
                handler.Break();
            }
        }).Add(() => { _idle.Play(); });
    }

    public Vector3 GetRandomPositionAroundPoint(Vector3 origin, float radius)
    {
        Vector2 randomPointInCircle = Random.insideUnitCircle * radius;
        Vector3 randomPosition = origin + new Vector3(randomPointInCircle.x, 0, randomPointInCircle.y);
        return randomPosition;
    }

    public Vector3 GetPointBeyondTarget(GameObject source, GameObject target, float distanceBeyond)
    {
        // Obtener la dirección de source a target
        Vector3 direction = (target.transform.position - source.transform.position).normalized;

        // Calcular un punto a 'distanceBeyond' unidades más allá de target en la misma dirección
        Vector3 pointBeyondTarget = target.transform.position + direction * distanceBeyond;

        return pointBeyondTarget;
    }

    private void BehaviourWithoutPlayer()
    {
        _getNextPath = this.tt().Pause().Add(() =>
        {
            _target = _enemy.Paths()[_indexOfPath];
            _indexOfPath++;
            if (_indexOfPath >= _enemy.Paths().Count)
            {
                _indexOfPath = 0;
            }

            _moveToTarget.Play();
        });

        _moveToTarget = this.tt().Pause().Add(() => { _enemy.MoveTo(_target); }).Loop(handle =>
        {
            var distance = Vector3.Distance(_enemy.GetGameObject().transform.position, _target.transform.position);
            if (distance < 1f)
            {
                handle.Break();
            }
        }).Add(() => { _watch.Play(); });

        _watch = this.tt().Pause().Add(() => { _enemy.RotateToTargetIdle(_target, false); }).Add(5)
            .Add(() => { _idle.Play(); });
    }

    private void EndStunt()
    {
        StopAllStartIdle();
        _enemy.SetInstaRotation(false);
    }

    private void EnemyOnOnDead(EnemyV2 obj)
    {
    }

    public void SetPlayer(CharacterV2 characterV2)
    {
    }

    public void TakeDamage()
    {
        StopAllStartIdle(false);
    }

    public void StopAI()
    {
        StopAllStartIdle(false);
    }

    public void Clean()
    {
        Destroy(_randomPosition);
    }

    public void StartAi()
    {
        _idle.Play();
    }
}