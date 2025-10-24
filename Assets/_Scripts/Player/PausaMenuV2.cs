using ServiceLocatorPath;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Scripts.Player
{
    public class PausaMenuV2 : MonoBehaviour, IPauseMainMenu
    {
        private bool isPaused;

        public PauseMenu.OnPause onPause { get; set; }

        private void Awake()
        {
            ServiceLocator.Instance.RegisterService<IPauseMainMenu>(this);
            onPause += p => { isPaused = p; };
        }

        private void OnDestroy()
        {
            ServiceLocator.Instance.UnregisterService<IPauseMainMenu>();
        }


        public void OnPause(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                isPaused = !isPaused;
                onPause?.Invoke(isPaused);
            }
        }
    }
}