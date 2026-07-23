using DustlineArena.Runtime.Common;
using DustlineArena.Runtime.Enemies;
using DustlineArena.Runtime.Health;
using DustlineArena.Runtime.Player;
using DustlineArena.Runtime.Weapons;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;

namespace DustlineArena.Runtime.Animation
{
    [DisallowMultipleComponent]
    public sealed class CharacterAnimationDriver : MonoBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int FireHash = Animator.StringToHash("Fire");
        private static readonly int HitHash = Animator.StringToHash("Hit");
        private static readonly int DeadHash = Animator.StringToHash("Dead");
        private static readonly int DodgeHash = Animator.StringToHash("Dodge");

        [SerializeField] private Animator animator;
        [SerializeField] private Rigidbody trackedBody;
        [SerializeField] private HealthComponent health;
        [SerializeField] private ProjectileWeapon weapon;
        [SerializeField] private PlayerMotor playerMotor;
        [SerializeField] private EnemyBrain enemyBrain;
        [SerializeField] private NavMeshAgent navMeshAgent;
        [SerializeField, Min(0.01f)] private float maxSpeed = 8f;
        [SerializeField, Min(0f)] private float movingHitReactSpeed = 0.15f;
        [SerializeField] private Transform movingHitReactRoot;
        [SerializeField, Min(0.01f)] private float movingHitReactDuration = 0.18f;
        [SerializeField, Min(0f)] private float movingHitReactRotation = 9f;
        [SerializeField, Min(0f)] private float movingHitReactOffset = 0.08f;
        [SerializeField] private Transform enemyAttackFeedbackRoot;
        [SerializeField, Min(0f)] private float enemyAttackPunchDistance;
        [SerializeField, Min(0.01f)] private float enemyAttackPunchDuration = 0.22f;
        [SerializeField, Min(0)] private int enemyAttackPunchVibrato = 2;
        [SerializeField, Range(0f, 1f)] private float enemyAttackPunchElasticity = 0.18f;
        [SerializeField, Min(0f)] private float enemyAttackPitch = 4f;

        private Vector3 movingHitReactBasePosition;
        private Quaternion movingHitReactBaseRotation;
        private Vector3 movingHitReactDirection;
        private float movingHitReactTimer;
        private Tween enemyAttackPositionTween;
        private Tween enemyAttackRotationTween;
        private bool hasDodgeParameter;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (trackedBody == null)
            {
                trackedBody = GetComponent<Rigidbody>();
            }

            if (health == null)
            {
                health = GetComponent<HealthComponent>();
            }

            if (weapon == null)
            {
                weapon = GetComponentInChildren<ProjectileWeapon>();
            }

            if (playerMotor == null)
            {
                playerMotor = GetComponent<PlayerMotor>();
            }

            if (enemyBrain == null)
            {
                enemyBrain = GetComponent<EnemyBrain>();
            }

            if (navMeshAgent == null)
            {
                navMeshAgent = GetComponent<NavMeshAgent>();
            }

            if (movingHitReactRoot == null && animator != null)
            {
                movingHitReactRoot = animator.transform;
            }

            if (enemyAttackFeedbackRoot == null && animator != null)
            {
                enemyAttackFeedbackRoot = animator.transform;
            }

            CacheMovingHitReactPose();
            CacheAnimatorParameters();
            EnsureLeftHandWeaponIK();
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Damaged += OnDamaged;
                health.Died += OnDied;
            }

            if (weapon != null)
            {
                weapon.Fired += OnFired;
            }

            if (playerMotor != null)
            {
                playerMotor.Dodged += OnDodged;
            }

            if (enemyBrain != null)
            {
                enemyBrain.Attacked += OnFired;
            }
        }

        private void OnDisable()
        {
            KillEnemyAttackTweens();
            ResetMovingHitReactPose();

            if (health != null)
            {
                health.Damaged -= OnDamaged;
                health.Died -= OnDied;
            }

            if (weapon != null)
            {
                weapon.Fired -= OnFired;
            }

            if (playerMotor != null)
            {
                playerMotor.Dodged -= OnDodged;
            }

            if (enemyBrain != null)
            {
                enemyBrain.Attacked -= OnFired;
            }
        }

        private void Update()
        {
            if (animator == null)
            {
                return;
            }

            Vector3 velocity = GetPlanarVelocity();
            animator.SetFloat(SpeedHash, velocity.magnitude / maxSpeed, 0.08f, Time.deltaTime);
        }

        private void LateUpdate()
        {
            ApplyMovingHitReact();
        }

        private void OnFired()
        {
            if (animator != null)
            {
                animator.SetTrigger(FireHash);
            }

            if (enemyBrain != null)
            {
                PlayEnemyAttackFeedback();
            }
        }

        private void OnDodged(Vector3 direction)
        {
            if (animator != null && hasDodgeParameter)
            {
                animator.ResetTrigger(HitHash);
                animator.SetTrigger(DodgeHash);
            }
        }

        private void OnDamaged(DamageInfo damage)
        {
            if (animator != null)
            {
                if (GetPlanarVelocity().sqrMagnitude > movingHitReactSpeed * movingHitReactSpeed)
                {
                    BeginMovingHitReact(damage.Direction);
                    return;
                }

                animator.SetTrigger(HitHash);
            }
        }

        private Vector3 GetPlanarVelocity()
        {
            Vector3 velocity = enemyBrain != null
                ? enemyBrain.PlanarVelocity
                : navMeshAgent != null && navMeshAgent.enabled
                    ? navMeshAgent.velocity
                    : trackedBody != null
                        ? trackedBody.velocity
                        : Vector3.zero;
            velocity.y = 0f;
            return velocity;
        }

        private void OnDied()
        {
            if (animator != null)
            {
                animator.SetBool(DeadHash, true);
            }

            ResetMovingHitReactPose();
        }

        private void BeginMovingHitReact(Vector3 hitDirection)
        {
            if (movingHitReactRoot == null)
            {
                return;
            }

            if (hitDirection.sqrMagnitude < 0.001f)
            {
                hitDirection = -transform.forward;
            }

            if (movingHitReactTimer <= 0f)
            {
                CacheMovingHitReactPose();
            }

            movingHitReactDirection = movingHitReactRoot.InverseTransformDirection(hitDirection.normalized);
            movingHitReactDirection.y = 0f;
            if (movingHitReactDirection.sqrMagnitude > 0.001f)
            {
                movingHitReactDirection.Normalize();
            }

            movingHitReactTimer = movingHitReactDuration;
        }

        private void ApplyMovingHitReact()
        {
            if (movingHitReactRoot == null || movingHitReactTimer <= 0f)
            {
                return;
            }

            movingHitReactTimer = Mathf.Max(0f, movingHitReactTimer - Time.deltaTime);
            float progress = 1f - movingHitReactTimer / movingHitReactDuration;
            float weight = Mathf.Sin(progress * Mathf.PI) * (1f - progress * 0.35f);

            Vector3 offset = -movingHitReactDirection * (movingHitReactOffset * weight);
            offset.y = 0f;

            float pitch = movingHitReactDirection.z * movingHitReactRotation * weight;
            float roll = -movingHitReactDirection.x * movingHitReactRotation * weight;
            Quaternion reactionRotation = Quaternion.Euler(pitch, 0f, roll);

            movingHitReactRoot.localPosition = movingHitReactBasePosition + offset;
            movingHitReactRoot.localRotation = movingHitReactBaseRotation * reactionRotation;

            if (movingHitReactTimer <= 0f)
            {
                ResetMovingHitReactPose();
            }
        }

        private void CacheMovingHitReactPose()
        {
            if (movingHitReactRoot == null)
            {
                return;
            }

            movingHitReactBasePosition = movingHitReactRoot.localPosition;
            movingHitReactBaseRotation = movingHitReactRoot.localRotation;
        }

        private void CacheAnimatorParameters()
        {
            hasDodgeParameter = false;
            if (animator == null)
            {
                return;
            }

            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.nameHash == DodgeHash)
                {
                    hasDodgeParameter = true;
                    return;
                }
            }
        }

        private void ResetMovingHitReactPose()
        {
            if (movingHitReactRoot == null)
            {
                return;
            }

            movingHitReactTimer = 0f;
            movingHitReactRoot.localPosition = movingHitReactBasePosition;
            movingHitReactRoot.localRotation = movingHitReactBaseRotation;
        }

        private void PlayEnemyAttackFeedback()
        {
            if (enemyAttackFeedbackRoot == null || (enemyAttackPunchDistance <= 0f && enemyAttackPitch <= 0f))
            {
                return;
            }

            KillEnemyAttackTweens();
            if (enemyAttackPunchDistance > 0f)
            {
                enemyAttackPositionTween = enemyAttackFeedbackRoot
                    .DOPunchPosition(Vector3.forward * enemyAttackPunchDistance, enemyAttackPunchDuration, enemyAttackPunchVibrato, enemyAttackPunchElasticity)
                    .SetTarget(this);
            }

            if (enemyAttackPitch > 0f)
            {
                enemyAttackRotationTween = enemyAttackFeedbackRoot
                    .DOPunchRotation(new Vector3(-enemyAttackPitch, 0f, 0f), enemyAttackPunchDuration, enemyAttackPunchVibrato, enemyAttackPunchElasticity)
                    .SetTarget(this);
            }
        }

        private void KillEnemyAttackTweens()
        {
            enemyAttackPositionTween?.Kill(true);
            enemyAttackRotationTween?.Kill(true);
            enemyAttackPositionTween = null;
            enemyAttackRotationTween = null;
        }

        private void EnsureLeftHandWeaponIK()
        {
            if (animator == null || weapon == null)
            {
                return;
            }

            TeamMember teamMember = GetComponent<TeamMember>();
            if (teamMember == null || teamMember.Team != TeamId.Player)
            {
                return;
            }

            if (!animator.TryGetComponent(out WeaponHandIK weaponHandIK))
            {
                weaponHandIK = animator.gameObject.AddComponent<WeaponHandIK>();
            }

            weaponHandIK.enabled = true;
            weaponHandIK.Configure(weapon, health);
        }
    }

    [RequireComponent(typeof(Animator))]
    public sealed class WeaponHandIK : MonoBehaviour
    {
        [SerializeField] private ProjectileWeapon weapon;
        [SerializeField] private HealthComponent health;
        [SerializeField, Range(0f, 1f)] private float rightHandPositionWeight;
        [SerializeField, Range(0f, 1f)] private float rightHandRotationWeight;
        [SerializeField, Range(0f, 1f)] private float leftHandPositionWeight = 0.84f;
        [SerializeField, Range(0f, 1f)] private float leftHandRotationWeight;
        [SerializeField, Range(0f, 1f)] private float elbowHintWeight;
        [SerializeField] private Vector3 rightElbowHintLocal = new Vector3(0.42f, 1.08f, 0.08f);
        [SerializeField] private Vector3 leftElbowHintLocal = new Vector3(-0.42f, 1.08f, 0.22f);

        private Animator animator;

        public void Configure(ProjectileWeapon configuredWeapon, HealthComponent configuredHealth)
        {
            weapon = configuredWeapon;
            health = configuredHealth;
        }

        private void Awake()
        {
            animator = GetComponent<Animator>();

            if (weapon == null)
            {
                weapon = GetComponentInParent<ProjectileWeapon>();
            }

            if (health == null)
            {
                health = GetComponentInParent<HealthComponent>();
            }
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (animator == null || weapon == null || !animator.isHuman || (health != null && !health.IsAlive))
            {
                ClearIK();
                return;
            }

            weapon.RefreshPlayerWeaponPose();

            if (rightHandPositionWeight > 0f || rightHandRotationWeight > 0f)
            {
                ApplyHandIK(AvatarIKGoal.RightHand, weapon.RightHandGrip, rightHandPositionWeight, rightHandRotationWeight);
            }

            if (weapon.UsesLeftHand)
            {
                ApplyHandIK(AvatarIKGoal.LeftHand, weapon.LeftHandGrip, leftHandPositionWeight, leftHandRotationWeight);
            }
            else
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
            }
            ApplyElbowHints();
        }

        private void ApplyHandIK(AvatarIKGoal goal, Transform target, float positionWeight, float rotationWeight)
        {
            if (target == null)
            {
                return;
            }

            animator.SetIKPositionWeight(goal, positionWeight);
            animator.SetIKRotationWeight(goal, rotationWeight);
            animator.SetIKPosition(goal, target.position);
            if (rotationWeight > 0f)
            {
                animator.SetIKRotation(goal, target.rotation);
            }
        }

        private void ApplyElbowHints()
        {
            if (elbowHintWeight <= 0f)
            {
                return;
            }

            Transform ownerRoot = weapon.OwnerRoot;
            animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, elbowHintWeight);
            animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, elbowHintWeight);
            animator.SetIKHintPosition(AvatarIKHint.RightElbow, ownerRoot.TransformPoint(rightElbowHintLocal));
            animator.SetIKHintPosition(AvatarIKHint.LeftElbow, ownerRoot.TransformPoint(leftElbowHintLocal));
        }

        private void ClearIK()
        {
            if (animator == null || !animator.isHuman)
            {
                return;
            }

            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
            animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, 0f);
            animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, 0f);
        }
    }
}
