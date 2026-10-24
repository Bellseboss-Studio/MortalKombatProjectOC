using System;
using System.Collections.Generic;
using _Scripts.Player;
using Bellseboss.Pery.Scripts.Input;
using UnityEngine;

public class EnemyFactoryMonoV2 : MonoBehaviour
{
    public Action AllEnemiesAreDead;
    [SerializeField] private int numberOfEnemies;
    [SerializeField] private string idToCreate;
    [SerializeField] private GameObject positionToCreate;
    [SerializeField] private GameObject[] pathToFollow;
    private List<EnemyV2> _enemies = new List<EnemyV2>();
    private List<EnemyV2> _enemiesDead = new List<EnemyV2>();
    [SerializeField] private bool allEnemiesAreDead;
    public List<EnemyV2> Enemies => _enemies;

    public bool IsAllEnemiesAreDead => allEnemiesAreDead;
    
    public void Configure(EnemiesV2Factory factory, GameObject center)
    {
        if(numberOfEnemies == 0)
        {
            allEnemiesAreDead = true;
            AllEnemiesAreDead?.Invoke();
            return;
        }

        for (int i = 0; i < numberOfEnemies; i++)
        {
            var e = factory.Create(idToCreate);
            e.transform.position = positionToCreate.transform.position;
            e.transform.rotation = positionToCreate.transform.rotation;
            e.Configure(pathToFollow, center);
            e.OnDead += OnEnemyDead;
            _enemies.Add(e);
        }
    }

    private void OnEnemyDead(EnemyV2 enemy)
    {
        var allDead = _enemies.TrueForAll(enemy => enemy.IsDead);
        Debug.Log($"AllEnemiesAreDead {allDead}");
        _enemies.Remove(enemy);
        _enemiesDead.Add(enemy);
        if (allDead)
        {
            allEnemiesAreDead = true;
            AllEnemiesAreDead?.Invoke();
        }
    }

    public void SetPlayer(CharacterV2 characterV2)
    {
        foreach (var enemyV2 in _enemies)
        {
            enemyV2.SetPlayer(characterV2);
        }
    }

    public void IntoToFarZone(bool b)
    {
        foreach (var enemyV2 in _enemies)
        {
            enemyV2.IntoToFarZone(b);
        }
    }

    public void IntoToNearZone(bool b)
    {
        foreach (var enemyV2 in _enemies)
        {
            enemyV2.IntoToNearZone(b);
        }
    }

    public void Clean()
    {
        foreach (var enemyV2 in _enemies)
        {
            enemyV2.OnDead -= OnEnemyDead;
            enemyV2.Clean();
            Destroy(enemyV2.gameObject);
        }
        _enemies.Clear();
        foreach (var enemyV2 in _enemiesDead)
        {
            enemyV2.OnDead -= OnEnemyDead;
            enemyV2.Clean();
            Destroy(enemyV2.gameObject);
        }
        _enemiesDead.Clear();
        allEnemiesAreDead = false;
    }
}
