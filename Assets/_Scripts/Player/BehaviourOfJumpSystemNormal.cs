using System;
using UnityEngine;

namespace _Scripts.Player
{
    public class BehaviourOfJumpSystemNormal : MonoBehaviour, IBehaviourOfJumpSystem
    {
        public Action OnAttack { get; set; }
        public Action OnMidAir { get; set; } // Se dispara al iniciar Decay (apex)
        public Action OnSustain { get; set; } // Se dispara al entrar a plateau para permitir ataques en aire
        public Action OnRelease { get; set; } // Se dispara al pasar a caída libre
        public Action OnEndJump { get; set; } // Se dispara al tocar el suelo

        private TeaTime _attack, _decay, _sustain, _release, _endJump;

        [Header("ADSR Times (seconds)")]
        [SerializeField] private float timeToAttack = 0.15f; // subida
        [SerializeField] private float timeToDecay = 0.20f;  // descenso suave al sustain
        [SerializeField] private float timeToSustain = 0.08f; // mantener altura para acciones
        [Tooltip("Tiempo máximo opcional antes de forzar EndJump aunque no haya suelo. <=0 para infinito")] [SerializeField]
        private float maxFallTime = 0f; // caída infinita por defecto

        [Header("Heights / Shape")]
        [SerializeField] private float maxHeight = 2.5f; // altura máxima en Attack
        [Range(0f,1f)] [SerializeField] private float sustainLevel = 0.65f; // fracción de max para plateau
        [SerializeField] private AnimationCurve attackCurve = AnimationCurve.EaseInOut(0,0,1,1); // subida
        [SerializeField] private AnimationCurve decayCurve = AnimationCurve.EaseInOut(0,1,1,0);  // curva apex -> plateau

        [Header("Release (Caída)")]
        [Tooltip("Velocidad vertical inicial negativa al iniciar Release (0 = deja que la gravedad acelere)")] [SerializeField]
        private float releaseStartVelocityDown = 0f;

        [Header("References")]
        [SerializeField] private FloorController floorController;

        // internos
        private Rigidbody _rb;
        private Transform _t;
        private float _baseY;
        private float _attackElapsed, _decayElapsed, _sustainElapsed, _releaseElapsed;
        private bool _falling; // indica si está en fase Release
        private float _releaseStartTime;

        public void Configure(Rigidbody rb, IJumpSystem jumpSystem)
        {
            _rb = rb;
            _t = rb.transform;
            BuildSequence(jumpSystem);
        }

        private void BuildSequence(IJumpSystem jumpSystem)
        {
            // ATTACK: subir desde base hasta altura máxima usando curva
            _attack = this.tt().Pause()
                .Add(() =>
                {
                    ResetTimers();
                    _rb.useGravity = false;
                    _falling = false;
                    var v = _rb.linearVelocity; v.y = 0f; _rb.linearVelocity = v;
                    _baseY = _t.position.y;
                })
                .Add(() => { OnAttack?.Invoke(); })
                .Loop(loop =>
                {
                    _attackElapsed += loop.deltaTime;
                    float dur = Mathf.Max(0.0001f, timeToAttack);
                    float t = Mathf.Clamp01(_attackElapsed / dur);
                    float h = maxHeight * Mathf.Clamp01(attackCurve.Evaluate(t));
                    SetY(_baseY + h);
                    if (t >= 1f) loop.Break();
                })
                .Add(() => { _decay.Play(); });

            // DECAY: curva desde apex (max) hacia plateau (sustainHeight)
            _decay = this.tt().Pause()
                .Add(() => { OnMidAir?.Invoke(); }) // apex alcanzado, inicia descenso
                .Loop(loop =>
                {
                    _decayElapsed += loop.deltaTime;
                    float dur = Mathf.Max(0.0001f, timeToDecay);
                    float t = Mathf.Clamp01(_decayElapsed / dur); // 0->1
                    float startH = maxHeight;
                    float endH = maxHeight * sustainLevel;
                    float factor = Mathf.Clamp01(decayCurve.Evaluate(t)); // 1->0 por defecto
                    float h = Mathf.Lerp(startH, endH, 1f - factor); // a medida que factor baja, nos acercamos a endH
                    SetY(_baseY + h);
                    if (t >= 1f) loop.Break();
                })
                .Add(() => { _sustain.Play(); });

            // SUSTAIN: mantener plateau para permitir acciones en aire
            _sustain = this.tt().Pause()
                .Add(() => { OnSustain?.Invoke(); })
                .Loop(loop =>
                {
                    _sustainElapsed += loop.deltaTime;
                    float h = maxHeight * sustainLevel;
                    SetY(_baseY + h);
                    if (_sustainElapsed >= Mathf.Max(0f, timeToSustain)) loop.Break();
                })
                .Add(() => { _release.Play(); });

            // RELEASE: caída infinita hasta tocar suelo (o maxFallTime opcional)
            _release = this.tt().Pause()
                .Add(() =>
                {
                    OnRelease?.Invoke();
                    _rb.useGravity = true;
                    _falling = true;
                    _releaseStartTime = Time.time;
                    if (releaseStartVelocityDown != 0f)
                    {
                        var v = _rb.linearVelocity; v.y = -Mathf.Abs(releaseStartVelocityDown); _rb.linearVelocity = v;
                    }
                })
                .Loop(loop =>
                {
                    _releaseElapsed += loop.deltaTime;
                    // fin si toca piso
                    if (floorController != null && floorController.IsTouchingFloor())
                    {
                        loop.Break();
                        return;
                    }
                    // fin opcional por tiempo máximo de caída
                    if (maxFallTime > 0f && Time.time - _releaseStartTime >= maxFallTime)
                    {
                        loop.Break();
                        return;
                    }
                })
                .Add(() => { _endJump.Play(); });

            // END: restaurar estado y eventos finales
            _endJump = this.tt().Pause()
                .Add(() =>
                {
                    _rb.useGravity = true; // asegurar
                    _falling = false;
                    ResetTimers();
                    OnEndJump?.Invoke();
                    jumpSystem.RestoreRotation();
                });
        }

        private void ResetTimers()
        {
            _attackElapsed = _decayElapsed = _sustainElapsed = _releaseElapsed = 0f;
        }

        private void SetY(float newY)
        {
            var p = _t.position; p.y = newY; _t.position = p;
        }

        public TeaTime GetAttack() => _attack;
        public TeaTime GetDecay() => _decay;
        public TeaTime GetSustain() => _sustain;
        public TeaTime GetRelease() => _release;
        public TeaTime GetEndJump() => _endJump;

        public void StopAll()
        {
            _attack.Stop();
            _decay.Stop();
            _sustain.Stop();
            _release.Stop();
            _endJump.Stop();
        }
    }
}

