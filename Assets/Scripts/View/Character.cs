using Unity.Cinemachine;
using InputSystemCustom;
using ServiceLocatorPath;
using UnityEngine;
using View.Characters;

namespace View
{
    public abstract class Character : CharacterV1
    {
        [SerializeField] protected string id;
        [SerializeField] protected GameObject model3D;
        [SerializeField] protected Rigidbody rb;
        [SerializeField] protected float speedGlobal;
        [SerializeField] protected float velocityOfAttack;
        [SerializeField] protected float life;
        [SerializeField] protected RuntimeAnimatorController controller;
        [SerializeField] protected CharacterAnimatorController animatorController;
        [SerializeField] protected float power;
        [SerializeField] private string nameOfAnimationTriggerForApplyDamage;
        [SerializeField] protected float smoodTimeRotation;
        protected Animator animator;
        
        //Deprecated
        protected ControllerAnimationPlayer animatorControllerPlayer;
        
        [SerializeField] protected float speed;
        [SerializeField] protected float forceRotation;
        protected InputCustom _inputCustom;
        public delegate void OnInputChanged(Vector2 input);
        public delegate void OnAddingEnergyEvent(float energy);
        public delegate void OnInputButton();
        public OnInputChanged OnInputChangedExtend;
        public OnInputChanged OnCameraMovementExtend;
        public OnInputButton OnLeftShitOn;
        public OnInputButton OnLeftShitOff;
        public OnInputButton OnPunchEvent;
        public OnInputButton OnKickEvent;
        public OnInputButton OnAimEvent;
        public OnInputButton OnFinishedAnimatorFight;
        public OnInputButton OnFinishedAnimatorDamage;
        public OnAddingEnergyEvent OnAddingEnergy;
        private Vector2 _inputValue;
        protected Vector3 movementPlayer;
        private Transform cameraForward;
        public bool CanAnimateDamage;
        protected GameObject instantiate;
        protected GameObject _mainCamera;
        public bool IsInPause { get; private set; }

        public delegate void OnEnterDamage(float damage);
        public event OnEnterDamage OnEnterDamageEvent;

        public string Id => id;
        public GameObject Model3D => model3D;

        protected virtual void Start()
        {
            instantiate = Instantiate(model3D, transform);
            animator = instantiate.GetComponent<Animator>();
            CanAnimateDamage = true;
            ServiceLocator.Instance.GetService<IPauseMainMenu>().onPause += OnPause;
            animatorController.Configurate(this);
        }

        private void OnPause(bool ispause)
        {
            animatorController.PauseGame(ispause);
            IsInPause = ispause;
            OnAimEventInPlayer();
        }

        protected virtual void OnAimEventInPlayer() {}

        private void OnFinishedAnimatorDamageCharacter()
        {
            CanAnimateDamage = true;
            FinishedAnimatorDamage();
        }

        protected virtual void FinishedAnimatorDamage()
        {
            
        }

        protected abstract void UpdateLegacy();
        protected void Update()
        {
            if (IsInPause)
            {
                rb.linearVelocity = Vector3.zero;    
                return;
            }

            var rbVelocity = _inputCustom.InputCalculateForTheMovement(_inputCustom.GetLasPosition()) * (Time.deltaTime * speedGlobal);
            rbVelocity.y = rb.linearVelocity.y;
            rb.linearVelocity = rbVelocity;
            animatorController.Movement(_inputCustom.GetVelocity(), _inputCustom.GetDirection());
            UpdateLegacy();
        }

        private void Rotating (float horizontal, float vertical)
        {
            var targetDirection = new Vector3(horizontal, 0f, vertical);
            var targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
            var newRotation = Quaternion.Lerp(rb.rotation, targetRotation, forceRotation * Time.deltaTime);
            rb.MoveRotation(newRotation);
        }
        
        protected Quaternion RotatingLocal (Vector3 targetDirection)
        {
            //var targetDirection = new Vector3(horizontal, 0f, vertical);
            
            var targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
            var newRotation = Quaternion.Lerp(rb.rotation, targetRotation, forceRotation * Time.deltaTime);
            //rb.MoveRotation(newRotation);
            return newRotation;
        }
        
        private void Rotating (float angle)
        {
            var targetRotation = Quaternion.Euler(0, angle, 0);;
            transform.rotation = targetRotation;
        }

        public void Configure(InputCustom inputCustom, GameObject cameraParameter)
        {
            _mainCamera = cameraParameter;
            _inputCustom = inputCustom;
            cameraForward = transform;
            ValidationsCritical();
            ConfigureExplicit();
        }

        private void ValidationsCritical()
        {
        }

        protected abstract void ConfigureExplicit();

        public InputCustom GetInput()
        {
            return _inputCustom;
        }

        public Transform GetTransform()
        {
            return transform;
        }
        
        public Transform GetTransformProtagonist()
        {
            return instantiate.transform;
        }
        
        public void Move(Vector3 movementVector2)
        {
            movementPlayer = movementVector2;
        }

        public void CameraMovement(Vector2 input)
        {
            _inputValue = input;
        }

        public void SetCameraForward(Transform cameraTransform)
        {
            cameraForward = cameraTransform;
        }

        public void NormalMotionInCamera()
        {
            if (cameraForward.gameObject.TryGetComponent<CinemachineFreeLook>(out var cinemachineFreeLook))
            {
                cinemachineFreeLook.m_XAxis.m_MaxSpeed = 300;    
                cinemachineFreeLook.m_YAxis.m_MaxSpeed = 1;    
            }
            else
            {
                Debug.Log("Es nulo");
            }
        }

        public void SlowMotionInCamera()
        {
            if (cameraForward.gameObject.TryGetComponent<CinemachineFreeLook>(out var cinemachineFreeLook))
            {
                cinemachineFreeLook.m_XAxis.m_MaxSpeed = 50;
                cinemachineFreeLook.m_YAxis.m_MaxSpeed = .3f;
            }
            else
            {
                Debug.Log("Es nulo");
            }
        }

        public void ApplyDamage(float damage)
        {
            life -= damage;
            VerifyLife();
            AnimateDamage();
            OnEnterDamageEvent?.Invoke(damage);
            
        }

        private void AnimateDamage()
        {
            if (CanAnimateDamage)
            {
                animatorController.Damage();
                CanAnimateDamage = false;
            }
        }

        public float GetLife()
        {
            return life;
        }

        public abstract float GetDamageForKick();
        public abstract float GetDamageForPunch();

        public float GetSmoothTimeRotation()
        {
            return smoodTimeRotation;
        }
        
        protected void VerifyLife()
        {
            if (life<= 0)
            {
                ServiceLocator.Instance.GetService<IPauseMainMenu>().onPause -= OnPause;
                Muerte();
            }
        }

        protected abstract void Muerte();

        public abstract Vector3 GetDirectionWithObjective();

        public virtual void AddEnergy()
        {
            
        }

        public Animator GetAnimator()
        {
            return animator;
        }

        public RuntimeAnimatorController GetController()
        {
            return controller;
        }
    }
}