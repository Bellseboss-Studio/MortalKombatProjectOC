using System;
using System.Collections;
using UnityEngine;

namespace _Scripts.Player
{
    /// <summary>
    /// Controlador centralizado de animaciones del jugador.
    /// No se exponen parámetros del Animator ni triggers externos.
    /// Toda animación se ejecuta a través de métodos concretos.
    /// </summary>
    public class AnimationController : MonoBehaviour
    {
        public event Action OnFinishAnimation;

        [Header("Animator Reference")] [SerializeField]
        private Animator animator;

        [Header("Root Transform (for flip/orientation)")] [SerializeField]
        private Transform visualRoot;

        [Header("Config")] [SerializeField, Tooltip("Velocidad mínima para caminar")]
        private float walkThreshold = 0.1f;

        [SerializeField, Tooltip("Velocidad mínima para correr")]
        private float runThreshold = 3f;

        [SerializeField, Tooltip("Duración de transición entre animaciones")]
        private float transitionDuration = 0.1f;

        private bool isFacingRight = true;
        private string currentComboIndex;

        private static class States
        {
            public const string Idle = "Idle";
            public const string StartMove = "StartMove";
            public const string Walk = "Walk";
            public const string Run = "Run";
            public const string StopMove = "StopMove";
            public const string Stunt = "Stunt";
            public const string GetFatality = "Get_Fatality";

            public const string JumpStart = "Jump_Start";
            public const string JumpApex = "Jump_Apex";
            public const string JumpFall = "Jump_Fall";
            public const string JumpLand = "Jump_Land";
            public const string WallJump = "Wall_Jump";
            public const string WallSlide = "Wall_Slide";

            public const string Hit = "Hit";
            public const string Death = "Death";
            public const string Watch = "Watch";

            public const string Activate = "Activate";
        }

        #region === Core Control ===

        public void Configure(Animator animatorRef, Transform visualRootRef = null)
        {
            animator = animatorRef;
            visualRoot = visualRootRef;
        }

        private void Play(string stateName, float fade, Action onFinish)
        {
            if (!animator)
            {
                // Debug.LogError("[AnimationController] Animator no configurado");
                return;
            }

            // Debug.Log($"[AnimationController] Playing {stateName}");

            animator.CrossFade(stateName, fade);

            if (onFinish != null)
                StartCoroutine(WaitForClip(stateName, onFinish));
        }

        private IEnumerator WaitForClip(string clipName, Action onFinish)
        {
            var clips = animator.runtimeAnimatorController.animationClips;
            foreach (var clip in clips)
            {
                if (clip.name == clipName)
                {
                    yield return new WaitForSeconds(clip.length / animator.speed);
                    onFinish?.Invoke();
                    yield break;
                }
            }
        }

        #endregion

        #region === Movement ===

        private string currentMovementState;
        private bool isInJumpSequence; // Flag para evitar interrumpir animaciones de salto

        /// <summary>
        /// Controla automáticamente la animación de movimiento según la velocidad.
        /// Evita reproducir la misma animación múltiples veces por frame.
        /// NO interrumpe animaciones de salto en curso.
        /// </summary>
        public void UpdateMovementAnimation(float velocity, Action onFinish = null)
        {
            if (!animator) return;

            // Si estamos en secuencia de salto, no cambiar animaciones de movimiento
            if (isInJumpSequence) 
            {
                // Debug.Log($"[AnimationController] UpdateMovementAnimation BLOCKED by jumpSequence (velocity={velocity:F3})");
                return;
            }

            string nextState = GetMovementState(velocity);

            // Si no cambia de estado Y no es null (que indica reset), no hacemos nada
            if (nextState == currentMovementState && currentMovementState != null) 
            {
                // Debug.Log($"[AnimationController] UpdateMovementAnimation: no change needed (current={currentMovementState}, next={nextState})");
                return;
            }

            // Debug.Log($"[AnimationController] UpdateMovementAnimation: changing from '{currentMovementState}' to '{nextState}'");
            currentMovementState = nextState;
            Play(nextState, transitionDuration, onFinish);
        }

        /// <summary>
        /// Fuerza una actualización inmediata del estado de movimiento.
        /// Útil cuando se necesita sincronizar después de eventos como aterrizaje.
        /// </summary>
        public void ForceUpdateMovementAnimation(float velocity, Action onFinish = null)
        {
            if (!animator) return;

            string nextState = GetMovementState(velocity);
            
            // Forzamos el cambio aunque sea el mismo estado
            currentMovementState = nextState;
            Play(nextState, transitionDuration, onFinish);
        }

        /// <summary>
        /// Determina el estado de movimiento según la velocidad actual.
        /// </summary>
        private string GetMovementState(float velocity)
        {
            // Debug.Log($"[AnimationController] GetMovementState: velocity={velocity:F3}, walkThreshold={walkThreshold}, runThreshold={runThreshold}");
            
            if (velocity <= walkThreshold)
            {
                // Debug.Log($"[AnimationController] → Idle (velocity <= {walkThreshold})");
                return States.Idle;
            }

            if (velocity > walkThreshold && velocity < runThreshold)
            {
                // Debug.Log($"[AnimationController] → Walk (velocity between {walkThreshold} and {runThreshold})");
                return States.Walk;
            }

            if (velocity >= runThreshold)
            {
                // Debug.Log($"[AnimationController] → Run (velocity >= {runThreshold})");
                return States.Run;
            }

            // Debug.Log($"[AnimationController] → Idle (fallback)");
            return States.Idle;
        }

        private void PlayIdle(Action onFinish = null)
        {
            if (currentMovementState == States.Idle) return;
            currentMovementState = States.Idle;
            Play(States.Idle, transitionDuration, onFinish);
        }

        private void PlayStartMove(Action onFinish = null)
        {
            if (currentMovementState == States.StartMove) return;
            currentMovementState = States.StartMove;
            Play(States.StartMove, transitionDuration, onFinish);
        }

        private void PlayStopMove(Action onFinish = null)
        {
            if (currentMovementState == States.StopMove) return;
            currentMovementState = States.StopMove;
            Play(States.StopMove, transitionDuration, onFinish);
        }

        #endregion

        #region === Jump System ===

        public void PlayJumpStart(Action onFinish = null)
        {
            isInJumpSequence = true;
            Play(States.JumpStart, transitionDuration, onFinish);
        }

        public void PlayJumpApex(Action onFinish = null)
        {
            // Mantenemos isInJumpSequence = true
            Play(States.JumpApex, transitionDuration, onFinish);
        }

        public void PlayJumpFall(Action onFinish = null)
        {
            // Mantenemos isInJumpSequence = true
            Play(States.JumpFall, transitionDuration, onFinish);
        }

        public void PlayJumpLand(Action onFinish = null)
        {
            // Terminamos la secuencia de salto ANTES de reproducir la animación
            // Esto permite que UpdateMovementAnimation funcione inmediatamente
            isInJumpSequence = false;
            currentMovementState = null; // Reset para forzar cambio de estado
            
            Play(States.JumpLand, transitionDuration, onFinish);
        }

        public void PlayWallJump(Action onFinish = null)
        {
            isInJumpSequence = true;
            Play(States.WallJump, transitionDuration, onFinish);
        }

        public void PlayWallSlide(Action onFinish = null)
        {
            isInJumpSequence = true;
            Play(States.WallSlide, transitionDuration, onFinish);
        }

        /// <summary>
        /// Fuerza el fin de la secuencia de salto (útil para casos especiales)
        /// </summary>
        public void ForceEndJumpSequence()
        {
            isInJumpSequence = false;
        }

        #endregion

        #region === Combat ===

        private string currentAttackState;
        private bool isAttacking;

        /// <summary>
        /// Ejecuta una animación de combo si no está en curso o si cambió de ataque.
        /// </summary>
        public void PlayComboAttack(string comboIndex, Action onFinish = null)
        {
            string attackState = $"Attack_{comboIndex}";

            // Si ya está en el mismo ataque, no hacemos nada
            if (isAttacking && currentAttackState == attackState)
                return;

            // Debug.Log($"[AnimationController] Reproduciendo ataque: {attackState}");

            currentAttackState = attackState;
            isAttacking = true;

            Play(attackState, transitionDuration, () =>
            {
                isAttacking = false;
                onFinish?.Invoke();
            });
        }

        /// <summary>
        /// Reproduce la animación de recibir daño.
        /// </summary>
        public void PlayHit(Action onFinish = null)
        {
            isAttacking = false;
            currentAttackState = null;
            Play(States.Hit, transitionDuration, onFinish);
        }

        /// <summary>
        /// Reproduce la animación de muerte.
        /// </summary>
        public void PlayDeath(Action onFinish = null)
        {
            isAttacking = false;
            currentAttackState = null;
            Play(States.Death, 0.2f, onFinish);
        }

        /// <summary>
        /// Indica si actualmente se está ejecutando una animación de ataque.
        /// </summary>
        public bool IsInAttackAnimation()
        {
            if (!animator || string.IsNullOrEmpty(currentAttackState))
                return false;

            var info = animator.GetCurrentAnimatorStateInfo(0);
            return info.IsName(currentAttackState);
        }

        /// <summary>
        /// Cancela cualquier ataque activo y vuelve al estado Idle.
        /// </summary>
        public void CancelAttack()
        {
            isAttacking = false;
            currentAttackState = null;
            Play(States.Idle, transitionDuration, null);
        }

        #endregion


        #region === Interaction ===

        public void PlayActivation(string activationAnimation, Action onFinish = null)
        {
            Play(activationAnimation, transitionDuration, onFinish);
        }

        public void PlayWatch(Action onFinish = null)
        {
            Play(States.Watch, transitionDuration, onFinish);
        }

        public void PlayStunt(Action onFinish = null)
        {
            Play(States.Stunt, transitionDuration, onFinish);
        }

        public void PlayGetFatality(Action onFinish = null)
        {
            Play(States.GetFatality, transitionDuration, onFinish);
        }

        public void PlayFatality(Action onFinish = null)
        {
            Play(States.GetFatality, transitionDuration, onFinish);
        }

        #endregion

        #region === Utility ===

        public bool IsPlaying(string stateName)
        {
            if (!animator) return false;
            var info = animator.GetCurrentAnimatorStateInfo(0);
            return info.IsName(stateName);
        }

        /// <summary>
        /// Indica si actualmente se está ejecutando una secuencia de salto
        /// </summary>
        public bool IsInJumpSequence()
        {
            return isInJumpSequence;
        }

        public void StopAllAnimations(Action onFinish = null)
        {
            isInJumpSequence = false;
            isAttacking = false;
            currentAttackState = null;
            currentMovementState = null;
            Play(States.Idle, transitionDuration, onFinish);
        }

        #endregion
    }
}