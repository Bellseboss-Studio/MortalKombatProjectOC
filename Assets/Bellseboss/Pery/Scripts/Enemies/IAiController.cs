using System;
using _Scripts.Player;
using Bellseboss.Pery.Scripts.Input;

internal interface IAiController
{
    void Configure(IEnemyV2 enemyV2, ref Action onEndStunt);
    void StartAi();
    void SetPlayer(CharacterV2 characterV2);
    void TakeDamage();
    void StopAI();
    void Clean();
}