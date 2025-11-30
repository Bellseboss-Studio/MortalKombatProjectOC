using Bellseboss.Pery.Scripts.Input;
using UnityEngine;

namespace _Scripts.Player
{
    public class MovementRigidbodyV2 : MonoBehaviour
    {
        [SerializeField] private float force;
        [SerializeField] private FloorController floorController;


        //[SerializeField] private AttackMovementSystem attackMovementSystem;
        [SerializeField] private JumpSystem jumpSystem;
        [Range(0, 1)] [SerializeField] private float inputMin;
        [Range(0, 2)] [SerializeField] private float inputMax;
        [Range(0, 1f)] [SerializeField] private float minSpeed;
        [Range(0.5f, 1)] [SerializeField] private float maxSpeed;
        [SerializeField] private bool isScalableWall;

        // --- NUEVAS CONFIGURACIONES PARA ACELERACIÓN/DESACELERACIÓN ---
        [Header("Acceleration / Deceleration (time-based)")]
        [Tooltip("Tiempo en segundos para ir de 0 a velocidad máxima")]
        [SerializeField] private float timeToMaxSpeed = 0.35f;

        [Tooltip("Tiempo en segundos para ir de velocidad actual a 0")]
        [SerializeField] private float timeToStop = 0.25f;

        [Tooltip("Curva opcional para modelar la aceleración (0..1)")]
        [SerializeField] private AnimationCurve accelerationCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [Tooltip("Curva para la desaceleración (0..1) donde 1=vel completa frenada")]
        [SerializeField] private AnimationCurve decelerationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);



        // velocidad/estado interno
        private Vector3 _currentVelocityXZ;
        private float _currentSpeed; // magnitud horizontal

        // timers
        private float _accelTimer = 0f;
        private float _decelTimer = 0f;

        [Header("Dirección / Giro")]
        [Tooltip("Velocidad de giro en grados por segundo hacia nueva dirección de input")]
        [SerializeField] private float turnSpeed = 320f;

        [Header("Control en el aire")]
        [Tooltip("Factor de control horizontal cuando NO toca el suelo (0=sin control, 1=igual que suelo)")]
        [SerializeField] private float airControlFactor = 0.66f;

        [Header("Umbrales de eventos de animación")]
        [Tooltip("Velocidad horizontal para disparar OnStartRunning")]
        [SerializeField] private float startRunEventSpeed = 0.1f;
        [Tooltip("Velocidad horizontal por debajo de la cual se dispara OnStopRunning")]
        [SerializeField] private float stopRunEventSpeed = 0.05f;


        // --- Referencias / estado original ---
        private Rigidbody _rigidbody;
        private float _speedRun, _speedWalk;
        private InputMovementCustomV2 _inputMovementCustom;
        private Vector2 _lastDirection;
        private bool _isConfigured;
        [SerializeField] private bool _canMove;
        private GameObject _camera;
        private bool _isTarget;
        private IMovementRigidBodyV2 _movementRigidBodyV2;
        public bool IsJump => jumpSystem != null && jumpSystem.IsJump(); // devuelve estado real del sistema de salto
        private float _velocityOfAnimation;
        private Vector3 _scalableWallFordWard;
        
        // Referencias para sincronización
        private RotationCharacterV2 _rotationSystem;

        // trackeo para disparos únicos de animaciones
        private bool _hasTriggeredStartRun = false;
        private bool _hasTriggeredStopRun = true; // inicia en true para evitar disparo inmediato de stop

        // Estado para desaceleración
        private Vector3 _decelDirection;
        private float _startDecelSpeed;
        private bool _hadInputPreviousFrame;

        // --- NUEVAS VARIABLES Y CONFIGURACIONES ---
        [Header("Responsiveness / Anti-Slide")]
        [Tooltip("Multiplicador de frenado cuando no hay input ( >1 frena más rápido)")]
        [SerializeField] private float brakeMultiplier = 2.2f;
        [Tooltip("Multiplicador extra cuando el jugador empuja en dirección opuesta")]
        [SerializeField] private float oppositeBrakeMultiplier = 4.0f;
        [Tooltip("Velocidad mínima a la que hacemos snap a 0 para evitar deslizamiento")]
        [SerializeField] private float snapStopSpeed = 0.025f;
        [Tooltip("Ángulo (grados) a partir del cual hacemos pivot rápido en vez de giro suave")]
        [SerializeField] private float snapTurnAngle = 110f;
        [Tooltip("Usar curva de desaceleración original en lugar del frenado lineal reforzado")]
        [SerializeField] private bool useCurveDeceleration = false;



        private enum MovementStyle { Modern, ShaolinMonks }
        [Header("Movement Style")]
        [SerializeField] private MovementStyle movementStyle = MovementStyle.Modern;
        [Tooltip("Transform objetivo al que se mira en modo target (lock-on)")]
        [SerializeField] private Transform targetLock;
        [Header("Shaolin Monks Style Params")]
        [SerializeField] private float monksAccelerationTime = 0.1f;
        [SerializeField] private float monksDecelerationTime = 0.12f;
        [SerializeField] private float monksPivotAngle = 75f; // ángulo para pivot rápido
        [SerializeField] private float strafeSpeedMultiplier = 0.85f; // velocidad lateral frente al objetivo
        [SerializeField] private float backwardSpeedMultiplier = 0.9f; // velocidad al retroceder frente al objetivo
        [SerializeField] private float monksCardinalDeadZone = 0.25f; // dead zone para decidir cardinal
        [SerializeField] private bool instantFaceTarget = true; // rotar instantáneo hacia el objetivo
        [SerializeField] private bool normalizeDiagonal = true; // mantener misma magnitud en diagonales

        [Header("Debug / Ajustes")]
        [Tooltip("Mostrar logs de depuración de dirección de movimiento")] [SerializeField]
        private bool enableMovementDebugLogs = false;

        public void Configure(Rigidbody rigidBody, float speedWalk, float speedRun, GameObject camera,
            IMovementRigidBodyV2 movementRigidBodyV2, StatisticsOfCharacter statisticsOfCharacter)
        {
            _rigidbody = rigidBody;
            _speedWalk = speedWalk;
            _speedRun = speedRun;
            _inputMovementCustom = new InputMovementCustomV2(force);
            _isConfigured = true;
            _camera = camera;
            _movementRigidBodyV2 = movementRigidBodyV2;
            _canMove = true;
            
            // Buscar automáticamente el RotationCharacterV2 en el mismo GameObject
            _rotationSystem = GetComponent<RotationCharacterV2>();
            if (_rotationSystem == null)
            {
                _rotationSystem = GetComponentInParent<RotationCharacterV2>();
            }
            
            if (floorController == null)
            {
                // Debug.LogWarning("[MovementRigidbodyV2] floorController no asignado en el inspector.");
                _isConfigured = false;
                return;
            }
            if (jumpSystem == null)
            {
                // Debug.LogWarning("[MovementRigidbodyV2] jumpSystem no asignado en el inspector.");
                _isConfigured = false;
                return;
            }
            floorController.Configure(this.gameObject);
            jumpSystem.Configure(rigidBody, movementRigidBodyV2, floorController);
            //attackMovementSystem.Configure(rigidBody, statisticsOfCharacter, movementRigidBodyV2);
            floorController.OnFall = Fall;
            floorController.OnRecovery = Recovery;
            floorController.OnTouchingFloorChanged = TouchingFloorChanged;
        }

        private void Recovery() => _movementRigidBodyV2.PlayerRecovery();
        private void Fall() => _movementRigidBodyV2.PlayerFall();

        private void TouchingFloorChanged(bool isTouching)
        {
            if (_movementRigidBodyV2.IsAttacking() || jumpSystem.IsJump()) return;
            if (isTouching) _movementRigidBodyV2.PlayerRecoveryV2(); else _movementRigidBodyV2.PlayerFallV2();
        }

        public void IsScalableWall(bool pIsScalableWall, float pForceToGravitate, Vector3 direction)
        {
            // Ajusta flags locales de escalada de pared. No se llama a JumpSystem (método no disponible).
            isScalableWall = pIsScalableWall;
            _scalableWallFordWard = direction;
            // Si JumpSystem necesitara saberlo, implementar método correspondiente allí.
        }

        private float CalculateDirection(float axis, bool isTarget)
        {
            var axisAbs = Mathf.Abs(axis);
            if (axisAbs < inputMin) return 0;
            if (isTarget) return axis >= 0 ? minSpeed : -minSpeed;
            if (axisAbs < inputMax) return axis >= 0 ? minSpeed : -minSpeed;
            return axis >= 0 ? maxSpeed : -maxSpeed;
        }

        // -----------------------------
        // Movimiento principal (llamado en FixedUpdate)
        // -----------------------------
        private void Move()
        {
            if (movementStyle == MovementStyle.ShaolinMonks)
            {
                ProcessShaolinMonksMovement();
                return;
            }

            // 1) Input cuantizado
            var result = Vector2.zero;
            result.y = CalculateDirection(_lastDirection.y, _isTarget);
            result.x = CalculateDirection(_lastDirection.x, _isTarget);
            bool hasInput = result.sqrMagnitude > 0.0001f;

            // 2) Decide caminar vs correr
            var absX = Mathf.Abs(result.x);
            var absY = Mathf.Abs(result.y);
            var isRunning = absX >= maxSpeed || absY >= maxSpeed;
            float usedSpeed = isRunning ? _speedRun : _speedWalk;

            // 3) Direccion y velocidad objetivo (magnitud plena) usando helper existente
            var desiredWorld = _inputMovementCustom.CalculateMovement(result, usedSpeed, _camera, _rigidbody.gameObject);
            Vector3 targetXZ = new Vector3(desiredWorld.x, 0, desiredWorld.z);
            float fullTargetSpeed = targetXZ.magnitude;
            Vector3 targetDir = fullTargetSpeed > 0.0001f ? targetXZ.normalized : _currentVelocityXZ.normalized;

            if (enableMovementDebugLogs && hasInput)
            {
                Debug.Log($"[MovementRigidbodyV2] Input={result}, TargetDir={targetDir}, DesiredWorld={desiredWorld}");
            }

            // Detectar ángulo respecto a dirección actual (para pivot)
            float angleToTarget = (_currentVelocityXZ.sqrMagnitude > 0.0001f && fullTargetSpeed > 0.0001f)
                ? Vector3.Angle(_currentVelocityXZ.normalized, targetDir)
                : 0f;
            bool oppositeDirection = angleToTarget > 150f; // casi opuesto
            bool pivotTurn = angleToTarget >= snapTurnAngle;

            if (hasInput)
            {
                if (!_hadInputPreviousFrame)
                {
                    _accelTimer = 0f;
                    _decelTimer = 0f;
                    _hasTriggeredStopRun = true;
                    _startDecelSpeed = _currentSpeed; // capturamos por si pivot ocurre inmediatamente
                }

                // Si la dirección es casi opuesta aplicamos frenado agresivo antes de volver a acelerar
                if (oppositeDirection)
                {
                    float pivotRate = (_speedRun / Mathf.Max(0.0001f, timeToStop)) * oppositeBrakeMultiplier;
                    _currentSpeed = Mathf.MoveTowards(_currentSpeed, 0f, pivotRate * Time.fixedDeltaTime);
                    // Snap a cero para evitar arrastre residual
                    if (_currentSpeed <= snapStopSpeed) { _currentSpeed = 0f; _accelTimer = 0f; }
                    _currentVelocityXZ = _currentSpeed > 0f ? _currentVelocityXZ.normalized * _currentSpeed : Vector3.zero;

                    if (_currentSpeed == 0f)
                    {
                        // una vez frenado cambiamos directamente la dirección a target
                        _currentVelocityXZ = targetDir * 0f; // arrancará en aceleración normal
                    }
                }
                else
                {
                    _accelTimer += Time.fixedDeltaTime;
                    _decelTimer = 0f;
                    float accelNorm = Mathf.Clamp01(_accelTimer / Mathf.Max(0.0001f, timeToMaxSpeed));
                    float accelFactor = accelerationCurve.Evaluate(accelNorm);
                    float currentTargetSpeed = fullTargetSpeed * accelFactor;

                    // Giro: pivot rápido si supera snapTurnAngle, si no giro suave
                    Vector3 currentDir = _currentVelocityXZ.sqrMagnitude > 0.0001f ? _currentVelocityXZ.normalized : targetDir;
                    Vector3 rotatedDir;
                    if (pivotTurn)
                        rotatedDir = targetDir; // snap directo
                    else
                    {
                        float maxRadians = turnSpeed * Mathf.Deg2Rad * Time.fixedDeltaTime;
                        rotatedDir = Vector3.RotateTowards(currentDir, targetDir, maxRadians, 0f);
                    }

                    _currentVelocityXZ = rotatedDir * currentTargetSpeed;
                    _currentSpeed = _currentVelocityXZ.magnitude;
                }

                if (!_hasTriggeredStartRun && _currentSpeed >= startRunEventSpeed)
                {
                    _hasTriggeredStartRun = true; _hasTriggeredStopRun = false; _movementRigidBodyV2.OnStartRunning();
                }
            }
            else
            {
                if (_hadInputPreviousFrame)
                {
                    // inicio fase de desaceleración
                    _decelTimer = 0f;
                    _accelTimer = 0f;
                    _startDecelSpeed = _currentSpeed;
                    _decelDirection = _currentSpeed > 0.0001f ? _currentVelocityXZ.normalized : _decelDirection;
                }

                if (useCurveDeceleration)
                {
                    _decelTimer += Time.fixedDeltaTime;
                    float decelNorm = Mathf.Clamp01(_decelTimer / Mathf.Max(0.0001f, timeToStop));
                    float decelFactor = decelerationCurve.Evaluate(decelNorm);
                    float remainingSpeed = Mathf.Lerp(_startDecelSpeed, 0f, decelFactor);
                    _currentSpeed = remainingSpeed;
                }
                else
                {
                    // Frenado lineal reforzado por brakeMultiplier
                    float decelRate = (_speedRun / Mathf.Max(0.0001f, timeToStop)) * brakeMultiplier;
                    _currentSpeed = Mathf.MoveTowards(_currentSpeed, 0f, decelRate * Time.fixedDeltaTime);
                }

                if (_currentSpeed <= snapStopSpeed) _currentSpeed = 0f; // snap final
                _currentVelocityXZ = _decelDirection * _currentSpeed;

                if (!_hasTriggeredStopRun && _currentSpeed <= stopRunEventSpeed)
                {
                    _hasTriggeredStopRun = true; _hasTriggeredStartRun = false; _movementRigidBodyV2.OnStopRunning();
                }
            }

            _hadInputPreviousFrame = hasInput;

            // 5) Aplicar velocidad al rigidbody (mantener Y)
            Vector3 finalVel = new Vector3(_currentVelocityXZ.x, _rigidbody.linearVelocity.y, _currentVelocityXZ.z);
            bool onFloor = floorController.IsTouchingFloor();
            if (!_movementRigidBodyV2.IsJumpingInWall())
            {
                if (onFloor)
                    _rigidbody.linearVelocity = finalVel;
                else
                    _rigidbody.linearVelocity = new Vector3(finalVel.x * airControlFactor, finalVel.y, finalVel.z * airControlFactor);
            }

            // 6) Variables para animación
            _velocityOfAnimation = _speedRun > 0.0001f ? (_currentSpeed / _speedRun) : 0f;
            
            // 7) Sincronizar rotación con dirección de movimiento
            if (_rotationSystem != null && hasInput)
            {
                _rotationSystem.SetMovementDirection(targetDir);
            }
        }

        public void Direction(Vector2 vector2) => _lastDirection = vector2;

        private void Update()
        {
            if (!_isConfigured || !_canMove) return;
            if (_movementRigidBodyV2.IsAttacking()) return;
            _movementRigidBodyV2.UpdateAnimation(floorController.IsTouchingFloor(), isScalableWall);
            if (!floorController.IsTouchingFloor() && jumpSystem.IsJump() && isScalableWall)
            {
                jumpSystem.ChangeRotation(_scalableWallFordWard);
            }
            // Rotación hacia objetivo en modo target
            if (_isTarget && targetLock != null)
            {
                Vector3 toTarget = targetLock.position - transform.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.0001f)
                {
                    Quaternion desired = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
                    if (instantFaceTarget)
                        transform.rotation = desired;
                    else
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, desired, turnSpeed * Time.deltaTime);
                }
            }
            else if (_isTarget && targetLock == null)
            {
                // target perdido
                _isTarget = false;
            }
        }

        private void FixedUpdate()
        {
            if (!_isConfigured || !_canMove || _movementRigidBodyV2.IsAttacking()) return;
            Move();
        }

        public void IsTarget(bool isTarget) => _isTarget = isTarget;
        public float GetVelocity() => _rigidbody.linearVelocity.magnitude / 10f; // mantiene escala legado

        public void AddForce(Vector3 runningDirection, float runningDistance,
            AttackMovementSystem.TypeOfAttack typeOfAttack)
        {
            _rigidbody.linearVelocity = Vector3.zero;
            // Se elimina variable globalDirection sin uso para limpiar warnings. Mantener lógica futura comentada.
            _canMove = false;
            //Vector3 globalDirection = transform.TransformDirection(runningDirection.normalized);
            //attackMovementSystem.Attack(globalDirection * runningDistance, typeOfAttack);
        }

        public void CanMove(bool canMove)
        {
            _canMove = canMove;
            if (!canMove)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _lastDirection = Vector2.zero;
                _accelTimer = 0f; _decelTimer = 0f; _currentVelocityXZ = Vector3.zero; _currentSpeed = 0f;
            }
        }

        public void Jump() => jumpSystem.Jump(floorController.IsTouchingFloor(), isScalableWall, _scalableWallFordWard);
        public JumpSystem GetJumpSystem() => jumpSystem;
        public float GetVelocityFloat() => _velocityOfAnimation;
        public void ChangeToNormalJump() { /* opcional: IsScalableWall(false, 0, Vector3.zero); */ }
        public void ExitToWall() { isScalableWall = false; /*jumpSystem.ExitToWall();*/ }
        public bool IsJumpingFromADRS() => jumpSystem.IsJump();
        public float GetXZVelocity() 
        { 
            Vector3 v = new Vector3(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z); 
            float result = v.magnitude / 10f;
            // Debug.Log($"[MovementRigidbodyV2] GetXZVelocity: rawVelocity={v.magnitude:F3}, scaledResult={result:F3}");
            return result;
        }

        // Debug rápido en contexto
        [ContextMenu("DebugPrintState")]
        private void DebugPrintState()
        {
            // Debug.Log($"[MovementRigidbodyV2] speed={_currentSpeed:F2} accelT={_accelTimer:F2} decelT={_decelTimer:F2} input={_lastDirection} onFloor={floorController.IsTouchingFloor()} jumping={jumpSystem.IsJump()} wall={isScalableWall}");
        }

        private Vector2 QuantizeCardinal(Vector2 input)
        {
            // Convierte input analógico a direcciones cardinales/diagonales limpias estilo beat'em up
            if (input.sqrMagnitude < monksCardinalDeadZone * monksCardinalDeadZone) return Vector2.zero;
            Vector2 norm = input.normalized;
            float ax = Mathf.Abs(norm.x);
            float ay = Mathf.Abs(norm.y);
            // Permitir diagonales si ambos ejes son suficientemente grandes
            float diagonalThreshold = 0.55f; // si ambos > 0.55 consideramos diagonal
            if (ax > diagonalThreshold && ay > diagonalThreshold)
            {
                // Diagonal exacta: redondeamos a signos 1/-1
                norm.x = norm.x > 0 ? 1f : -1f;
                norm.y = norm.y > 0 ? 1f : -1f;
                if (normalizeDiagonal) norm = norm.normalized; // para misma velocidad
                return norm;
            }
            // Cardinal: elegir el eje dominante
            if (ax > ay)
            {
                return new Vector2(norm.x > 0 ? 1f : -1f, 0f);
            }
            else
            {
                return new Vector2(0f, norm.y > 0 ? 1f : -1f);
            }
        }

        private void ProcessShaolinMonksMovement()
        {
            // Input digitalizado cardinal/diagonal
            Vector2 raw = _lastDirection;
            Vector2 q = QuantizeCardinal(raw);
            bool hasInput = q != Vector2.zero;

            // Walk/Run: asumimos siempre run para estilo dinámico, puedes limitar si quieres
            float usedSpeed = _speedRun;

            // Direccion deseada en mundo (usa cámara si existe para que adelante sea hacia adelante de cámara)
            Vector3 desiredWorld = _camera != null
                ? (_camera.transform.forward * q.y + _camera.transform.right * q.x)
                : (transform.forward * q.y + transform.right * q.x);
            desiredWorld.y = 0f;
            Vector3 targetDir = desiredWorld.sqrMagnitude > 0.0001f ? desiredWorld.normalized : _currentVelocityXZ.normalized;

            // Si hay target lock aplicamos ajustes de strafe/back
            if (_isTarget && targetLock != null)
            {
                // Recalcular respecto al forward actual (ya rotado hacia target en Update)
                Vector3 fwd = transform.forward; fwd.y = 0f; fwd.Normalize();
                Vector3 right = transform.right; right.y = 0f; right.Normalize();
                Vector3 composed = fwd * q.y + right * q.x;
                composed.y = 0f;
                targetDir = composed.sqrMagnitude > 0.0001f ? composed.normalized : targetDir;
                // Escalas de velocidad específicas
                if (q.x != 0 && q.y == 0) usedSpeed *= strafeSpeedMultiplier; // strafe
                else if (q.y < 0 && q.x == 0) usedSpeed *= backwardSpeedMultiplier; // retroceso
            }

            float fullTargetSpeed = usedSpeed;
            float angleToTarget = (_currentVelocityXZ.sqrMagnitude > 0.0001f && fullTargetSpeed > 0.0001f)
                ? Vector3.Angle(_currentVelocityXZ.normalized, targetDir)
                : 0f;
            bool pivotTurn = angleToTarget >= monksPivotAngle;
            bool oppositeDirection = angleToTarget > 140f;

            if (hasInput)
            {
                if (!_hadInputPreviousFrame)
                {
                    _accelTimer = 0f; _decelTimer = 0f; _startDecelSpeed = _currentSpeed; _hasTriggeredStopRun = true;
                }

                if (oppositeDirection)
                {
                    // pivot brake rápido
                    float pivotRate = (usedSpeed / Mathf.Max(0.0001f, monksDecelerationTime)) * oppositeBrakeMultiplier;
                    _currentSpeed = Mathf.MoveTowards(_currentSpeed, 0f, pivotRate * Time.fixedDeltaTime);
                    if (_currentSpeed <= snapStopSpeed) _currentSpeed = 0f;
                    _currentVelocityXZ = _currentSpeed > 0 ? _currentVelocityXZ.normalized * _currentSpeed : Vector3.zero;
                    if (_currentSpeed == 0) _accelTimer = 0f; // reinicia aceleración luego
                }

                _accelTimer += Time.fixedDeltaTime;
                float accelNorm = Mathf.Clamp01(_accelTimer / Mathf.Max(0.0001f, monksAccelerationTime));
                float accelFactor = accelNorm; // lineal (puedes usar curva si quieres)
                float targetSpeedNow = fullTargetSpeed * accelFactor;

                Vector3 currentDir = _currentVelocityXZ.sqrMagnitude > 0.0001f ? _currentVelocityXZ.normalized : targetDir;
                Vector3 finalDir = pivotTurn ? targetDir : Vector3.RotateTowards(currentDir, targetDir, turnSpeed * Mathf.Deg2Rad * Time.fixedDeltaTime, 0f);

                // Si todavía frenando por pivot, aceleración empieza tras llegar a cero
                if (oppositeDirection && _currentSpeed > 0f)
                {
                    // mantener frenado, no adoptar nueva dirección todavía
                }
                else
                {
                    _currentVelocityXZ = finalDir * targetSpeedNow;
                    _currentSpeed = _currentVelocityXZ.magnitude;
                }

                if (!_hasTriggeredStartRun && _currentSpeed >= startRunEventSpeed)
                {
                    _hasTriggeredStartRun = true; _hasTriggeredStopRun = false; _movementRigidBodyV2.OnStartRunning();
                }
            }
            else
            {
                if (_hadInputPreviousFrame)
                {
                    _decelTimer = 0f; _startDecelSpeed = _currentSpeed; _decelDirection = _currentVelocityXZ.sqrMagnitude > 0.0001f ? _currentVelocityXZ.normalized : _decelDirection;
                }
                _decelTimer += Time.fixedDeltaTime;
                float decelRate = (usedSpeed / Mathf.Max(0.0001f, monksDecelerationTime)) * brakeMultiplier;
                _currentSpeed = Mathf.MoveTowards(_currentSpeed, 0f, decelRate * Time.fixedDeltaTime);
                if (_currentSpeed <= snapStopSpeed) _currentSpeed = 0f;
                _currentVelocityXZ = _decelDirection * _currentSpeed;

                if (!_hasTriggeredStopRun && _currentSpeed <= stopRunEventSpeed)
                {
                    _hasTriggeredStopRun = true; _hasTriggeredStartRun = false; _movementRigidBodyV2.OnStopRunning();
                }
            }

            _hadInputPreviousFrame = hasInput;

            // Aplicar al rigidbody (mantener Y)
            Vector3 finalVel = new Vector3(_currentVelocityXZ.x, _rigidbody.linearVelocity.y, _currentVelocityXZ.z);
            bool onFloor = floorController.IsTouchingFloor();
            if (!_movementRigidBodyV2.IsJumpingInWall())
            {
                if (onFloor) _rigidbody.linearVelocity = finalVel; else _rigidbody.linearVelocity = new Vector3(finalVel.x * airControlFactor, finalVel.y, finalVel.z * airControlFactor);
            }
            _velocityOfAnimation = _speedRun > 0.0001f ? (_currentSpeed / _speedRun) : 0f;
        }
    }
}
