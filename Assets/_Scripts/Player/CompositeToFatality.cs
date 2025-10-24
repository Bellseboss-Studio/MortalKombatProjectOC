using UnityEngine;

namespace _Scripts.Player
{
    public class CompositeToFatality : MonoBehaviour
    {
        [SerializeField] private GameObject refPlayer;
        [SerializeField] private GameObject refEnemyToKill;
        [SerializeField] private Animator compositeAnimator;
        private GameObject player;
        private GameObject enemyToKill;
        private bool isFatality;

        public void Coordinator(GameObject player, GameObject enemyToKill)
        {
            this.player = player;
            this.enemyToKill = enemyToKill;
        }

        public void Fatality()
        {
            compositeAnimator.SetTrigger("Fatality");
        }

        public void StartEqualizePosition()
        {
            isFatality = true;
        }

        public void FinishFatality()
        {
            isFatality = false;
        }

        private void Update()
        {
            if (!isFatality) return;
            player.transform.position = refPlayer.transform.position;
            player.transform.rotation = refPlayer.transform.rotation;
            enemyToKill.transform.position = refEnemyToKill.transform.position;
            enemyToKill.transform.rotation = refEnemyToKill.transform.rotation;
        }
    }
}