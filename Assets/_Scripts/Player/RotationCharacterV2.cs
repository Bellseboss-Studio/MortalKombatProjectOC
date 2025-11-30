using UnityEngine;

namespace _Scripts.Player
{
    internal class RotationCharacterV2 : MonoBehaviour
    {
        private GameObject _player;
        private GameObject _camera;
        private bool _isConfigured;
        private IRotationCharacterV2 _rotationCharacterV2;

        [Header("Rotación")] 
        [SerializeField] private float baseRotationSpeed = 8f;
        [SerializeField] private AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float accelerationTime = 0.3f; // tiempo en segundos para alcanzar velocidad completa

        [Header("Air Control")]
        [Tooltip("Multiplicador de velocidad de rotación durante el salto")] [SerializeField] 
        private float airRotationSpeedMultiplier = 0.7f;
        [Tooltip("Referencia al sistema de movimiento para detectar salto")] [SerializeField]
        private MovementRigidbodyV2 movementSystem;

        [Header("Input Quantization (sync with MovementRigidbodyV2)")]
        [Tooltip("Referencia al MovementRigidbodyV2 para obtener configuración de input")] [SerializeField]
        private MovementRigidbodyV2 movementReference;

        [Header("Rotation Stability")]
        [Tooltip("Ángulo mínimo de diferencia para cambiar la dirección objetivo (grados)")] [SerializeField]
        private float directionChangeThreshold = 5f;
        [Tooltip("Ángulo mínimo para aplicar rotación (grados)")] [SerializeField] 
        private float rotationThreshold = 1f;
        [Tooltip("Velocidad mínima de rotación para mantener estabilidad")] [SerializeField]
        private float minRotationVelocity = 0.1f;
        [Tooltip("Umbral de alineación entre dirección de movimiento y input (0-1, mayor = más estricto)")] [SerializeField]
        private float alignmentThreshold = 0.7f;

        [Header("Debug")]
        [Tooltip("Mostrar logs de depuración para alineación de rotación")] [SerializeField]
        private bool enableDebugLogs = false;

        private float _rotationVelocity; // valor 0–1 que aumenta cuando hay input
        private Vector2 _vector2;
        private Vector3 _lastDirection;
        private bool _canChangeDirection;
        private bool _canRotate;
        private bool _canRotateWhileAttack;
        private bool _isChangingDirection;

        private float _currentRotationSpeed;
        private float _forceRotation;
        
        // Control de sincronización
        private bool _usingSyncDirection = false;
        private Vector3 _syncDirection;
        private float _lastSyncTime;

        public void Configure(GameObject camera, GameObject player, IRotationCharacterV2 rotationCharacterV2,
            float forceRotation)
        {
            _camera = camera;
            _player = player;
            _rotationCharacterV2 = rotationCharacterV2;
            _forceRotation = forceRotation;
            _isConfigured = true;
            _canRotate = true;
        }

        public void Direction(Vector2 vector2) => _vector2 = vector2;
        public void Direction(Vector3 vector3) => _lastDirection = vector3;

        /// <summary>
        /// Recibe la dirección de movimiento ya calculada desde MovementRigidbodyV2 para perfecta sincronización
        /// </summary>
        public void SetMovementDirection(Vector3 worldDirection)
        {
            if (worldDirection.sqrMagnitude > 0.0001f)
            {
                Vector3 normalizedDirection = worldDirection.normalized;
                
                // Calcular la dirección esperada basada en el input actual
                Vector3 expectedDirection = CalculateMovementDirection(_vector2);
                
                // Solo usar la dirección sincronizada si está alineada con la expectativa del input
                if (expectedDirection != Vector3.zero)
                {
                    float alignment = Vector3.Dot(normalizedDirection, expectedDirection);
                    
                    if (alignment > alignmentThreshold) // Umbral de alineación configurable
                    {
                        _lastDirection = normalizedDirection;
                        _syncDirection = _lastDirection;
                        _usingSyncDirection = true;
                        _lastSyncTime = Time.time;
                        
                        if (enableDebugLogs)
                        {
                            Debug.Log($"[RotationCharacterV2] Sync direction accepted: {_lastDirection}, alignment: {alignment:F3}");
                        }
                    }
                    else
                    {
                        // Usar dirección esperada en lugar de sincronizada si hay desalineación
                        _lastDirection = expectedDirection;
                        _usingSyncDirection = false;
                        
                        if (enableDebugLogs)
                        {
                            Debug.Log($"[RotationCharacterV2] Sync direction rejected due to misalignment: {alignment:F3}, using expected: {expectedDirection}");
                        }
                    }
                }
                else
                {
                    // Fallback normal
                    _lastDirection = normalizedDirection;
                    _syncDirection = _lastDirection;
                    _usingSyncDirection = true;
                    _lastSyncTime = Time.time;
                }
            }
        }

        /// <summary>
        /// Calcula la dirección de movimiento usando EXACTAMENTE la misma lógica que InputMovementCustomV2.CalculateMovement
        /// </summary>
        private Vector3 CalculateMovementDirection(Vector2 input)
        {
            if (_camera == null) return Vector3.forward;
            
            // PASO 1: Cuantizar input igual que MovementRigidbodyV2
            Vector2 quantizedInput = Vector2.zero;
            quantizedInput.y = CalculateDirection(input.y, false);
            quantizedInput.x = CalculateDirection(input.x, false);
            
            if (quantizedInput.sqrMagnitude <= 0.0001f) return Vector3.zero;
            
            // PASO 2: EXACTAMENTE la misma lógica que InputMovementCustomV2
            var direction = _player.transform.position - _camera.transform.position;
            direction.y = 0; // Ignora la componente Y para mantener el movimiento en el plano XZ
            direction.Normalize();
            var right = new Vector3(direction.z, 0, -direction.x);
            var result = quantizedInput.x * right + quantizedInput.y * direction;
            
            if (result.sqrMagnitude > 0.0001f)
                result.Normalize();
            
            if (enableDebugLogs)
            {
                Debug.Log($"[RotationCharacterV2] RawInput={input}, QuantizedInput={quantizedInput}, CameraToPlayer={direction}, Right={right}, Result={result}");
            }
            
            return result;
        }

        /// <summary>
        /// Replica la misma lógica de cuantización que MovementRigidbodyV2.CalculateDirection
        /// </summary>
        private float CalculateDirection(float axis, bool isTarget)
        {
            var axisAbs = Mathf.Abs(axis);
            
            // Obtener valores del MovementRigidbodyV2 si está disponible
            float inputMin = 0.1f;
            float inputMax = 1f; 
            float minSpeed = 0.5f;
            float maxSpeed = 1f;
            
            if (movementReference != null)
            {
                // Acceder a los valores reales (necesitaríamos propiedades públicas en MovementRigidbodyV2)
                // Por ahora usamos valores estándar que coincidan
                inputMin = 0.1f; // inputMin por defecto
                inputMax = 2f;   // inputMax por defecto 
                minSpeed = 0.5f; // minSpeed por defecto
                maxSpeed = 1f;   // maxSpeed por defecto
            }
            
            if (axisAbs < inputMin) return 0;
            if (isTarget) return axis >= 0 ? minSpeed : -minSpeed;
            if (axisAbs < inputMax) return axis >= 0 ? minSpeed : -minSpeed;
            return axis >= 0 ? maxSpeed : -maxSpeed;
        }

        private void Update()
        {
            if (!_isConfigured || !_canRotate) return;

            // Si está rotando hacia un target (ataque, lock-on, etc)
            if (_canRotateWhileAttack)
            {
                Quaternion targetRotation = Quaternion.LookRotation(_lastDirection);
                _player.transform.rotation = Quaternion.Slerp(
                    _player.transform.rotation,
                    targetRotation,
                    _forceRotation * Time.deltaTime
                );
                return;
            }

            // Determinar si usar dirección sincronizada o calcular localmente
            bool hasInput = _vector2.sqrMagnitude > 0.01f;
            bool syncRecent = _usingSyncDirection && (Time.time - _lastSyncTime) < 0.1f;
            
            Vector3 desiredMoveDir;
            
            // SIEMPRE calcular la dirección esperada del input
            Vector3 expectedFromInput = CalculateMovementDirection(_vector2);
            
            if (syncRecent && expectedFromInput != Vector3.zero)
            {
                // Verificar alineación entre dirección sincronizada y esperada
                float alignment = Vector3.Dot(_syncDirection, expectedFromInput);
                
                if (alignment > alignmentThreshold)
                {
                    // Usar dirección sincronizada si está bien alineada
                    desiredMoveDir = _syncDirection;
                    if (enableDebugLogs)
                    {
                        Debug.Log($"[RotationCharacterV2] Using SYNC direction: {desiredMoveDir}, alignment: {alignment:F3}");
                    }
                }
                else
                {
                    // Priorizar dirección esperada si hay desalineación
                    desiredMoveDir = expectedFromInput;
                    if (enableDebugLogs)
                    {
                        Debug.Log($"[RotationCharacterV2] OVERRIDE sync due to misalignment: {alignment:F3}, using input direction: {desiredMoveDir}");
                    }
                }
            }
            else
            {
                // Fallback: usar dirección calculada del input
                desiredMoveDir = expectedFromInput;
                if (enableDebugLogs && hasInput)
                {
                    Debug.Log($"[RotationCharacterV2] Using LOCAL direction: {desiredMoveDir}");
                }
            }
            
            bool hasMovement = desiredMoveDir != Vector3.zero;
            
            if (hasMovement)
            {
                // Solo cambiar dirección si es significativamente diferente
                float angleDifference = Vector3.Angle(_lastDirection, desiredMoveDir);
                bool shouldUpdateDirection = _lastDirection == Vector3.zero || angleDifference > directionChangeThreshold;
                
                if (shouldUpdateDirection)
                {
                    _lastDirection = desiredMoveDir;
                    _rotationVelocity = 1f; // Activar rotación inmediata hacia nueva dirección
                    
                    if (enableDebugLogs)
                    {
                        Debug.Log($"[RotationCharacterV2] Direction changed by {angleDifference:F1}°, updating to: {_lastDirection}");
                    }
                }
                else
                {
                    // Dirección estable, mantener rotación actual
                    _rotationVelocity = Mathf.Max(_rotationVelocity - Time.deltaTime / accelerationTime, minRotationVelocity);
                }
            }
            else
            {
                _rotationVelocity -= Time.deltaTime / accelerationTime;   // Desacelera progresivamente
            }

            _rotationVelocity = Mathf.Clamp01(_rotationVelocity);
            float curveValue = rotationCurve.Evaluate(_rotationVelocity);
            _currentRotationSpeed = baseRotationSpeed * curveValue;

            // Aplicar multiplicador si está saltando
            if (movementSystem != null && movementSystem.IsJump)
            {
                _currentRotationSpeed *= airRotationSpeedMultiplier;
            }

            // Solo rotar si hay una dirección válida Y la velocidad de rotación es significativa
            if (_lastDirection != Vector3.zero && _rotationVelocity > 0.05f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(_lastDirection);
                
                // Solo aplicar rotación si el ángulo diferencia es mayor al umbral mínimo configurable
                float currentAngleDiff = Quaternion.Angle(_player.transform.rotation, targetRotation);
                
                if (currentAngleDiff > rotationThreshold)
                {
                    _player.transform.rotation = Quaternion.RotateTowards(
                        _player.transform.rotation,
                        targetRotation,
                        _currentRotationSpeed * Time.deltaTime
                    );
                    
                    if (enableDebugLogs)
                    {
                        Debug.Log($"[RotationCharacterV2] Rotating towards {_lastDirection}, angle diff: {currentAngleDiff:F1}°, speed: {_currentRotationSpeed:F1}");
                    }
                }
                else if (enableDebugLogs)
                {
                    Debug.Log($"[RotationCharacterV2] Rotation stable, angle diff: {currentAngleDiff:F1}°");
                }
            }
        }

        public void CanRotate(bool canRotate)
        {
            _vector2 = Vector2.zero;
            _canRotate = canRotate;
        }

        public bool CanRotate()
        {
            return _canRotate;
        }

        public void RotateToDirection(Vector3 direction)
        {
            //invert direction
            direction = -direction;
            direction = new Vector3(direction.x, 0, direction.z);
            _player.transform.rotation = Quaternion.LookRotation(direction);
            _lastDirection = direction;
        }

        public void ChangeDirection(Vector3 rotation)
        {
            _canChangeDirection = true;
            _lastDirection = rotation;
        }

        public void RestoreRotation()
        {
            _canChangeDirection = false;
        }

        public void CanRotateWhileAttack(bool canRotateWhileAttack)
        {
            _canRotateWhileAttack = canRotateWhileAttack;
        }

        public void RotateToLookTheTarget(Vector3 getTarget)
        {
            if (getTarget != Vector3.zero)
            {
                _lastDirection = getTarget - _player.transform.position;
                _lastDirection.y = 0;
            }
        }

        public void RotateInstant(Vector3 direction)
        {
            direction.y = 0;
            if (direction == Vector3.zero) return;

            _player.transform.rotation = Quaternion.LookRotation(direction);
            _lastDirection = direction;
        }
    }
}