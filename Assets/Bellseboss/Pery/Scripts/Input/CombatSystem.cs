using _Scripts.Player;
using UnityEngine;

namespace Bellseboss.Pery.Scripts.Input
{
    public class CombatSystem : MonoBehaviour
    {
        [SerializeField] private float runningDistanceQuickAttack;
        [SerializeField] private float runningDistancePowerAttack;
        [SerializeField] private Vector3 runningDirectionQuickAttack;
        [SerializeField] private Vector3 runningDirectionPowerAttack;
        //public bool CanQuickAttack => _combatSystemMediator.CanAttack();
        //public bool CanPowerAttack => _combatSystemMediator.CanAttack();
        
        private ICombatSystem _combatSystemMediator;

        public void Configure(ICombatSystem combatSystem)
        {
            _combatSystemMediator = combatSystem;
            //_combatSystemMediator.GetAttackSystem().OnEndAttack += OnEndJump;
        }

        private void OnEndJump()
        {
            _combatSystemMediator.EnableControls();
        }

        public void PowerAttack()
        {
            //_combatSystemMediator.PowerAttack(runningDistancePowerAttack, _combatSystemMediator.RotateToTarget(runningDirectionPowerAttack));
        }
        
        public void QuickAttack()
        {
            //_combatSystemMediator.QuickAttack(runningDistanceQuickAttack, _combatSystemMediator.RotateToTarget(runningDirectionQuickAttack));
        }
    }
}