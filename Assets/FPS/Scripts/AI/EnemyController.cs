using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.FPS.Game;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Unity.FPS.AI
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyController : NetworkBehaviour
    {
        [System.Serializable]
        public struct RendererIndexData
        {
            public Renderer Renderer;
            public int MaterialIndex;

            public RendererIndexData(Renderer renderer, int index)
            {
                Renderer = renderer;
                MaterialIndex = index;
            }
        }

        [Header("Parameters")]
        [Tooltip("The Y height at which the enemy will be automatically killed (if it falls off of the level)")]
        public float SelfDestructYHeight = -20f;

        [Tooltip("The distance at which the enemy considers that it has reached its current path destination point")]
        public float PathReachingRadius = 2f;

        [Tooltip("The speed at which the enemy rotates")]
        public float OrientationSpeed = 10f;

        [Tooltip("Delay after death where the GameObject is destroyed (to allow for animation)")]
        public float DeathDuration = 0f;


        [Header("Weapons Parameters")] [Tooltip("Allow weapon swapping for this enemy")]
        public bool SwapToNextWeapon = false;

        [Tooltip("Time delay between a weapon swap and the next attack")]
        public float DelayAfterWeaponSwap = 0f;

        [Header("Eye color")] [Tooltip("Material for the eye color")]
        public Material EyeColorMaterial;

        [Tooltip("The default color of the bot's eye")] [ColorUsageAttribute(true, true)]
        public Color DefaultEyeColor;

        [Tooltip("The attack color of the bot's eye")] [ColorUsageAttribute(true, true)]
        public Color AttackEyeColor;

        [Header("Flash on hit")] [Tooltip("The material used for the body of the hoverbot")]
        public Material BodyMaterial;

        [Tooltip("The gradient representing the color of the flash on hit")] [GradientUsageAttribute(true)]
        public Gradient OnHitBodyGradient;

        [Tooltip("The duration of the flash on hit")]
        public float FlashOnHitDuration = 0.5f;

        [Header("Sounds")] [Tooltip("Sound played when recieving damages")]
        public AudioClip DamageTick;

        [Header("VFX")] [Tooltip("The VFX prefab spawned when the enemy dies")]
        public GameObject DeathVfx;

        [Tooltip("The point at which the death VFX is spawned")]
        public Transform DeathVfxSpawnPoint;

        [Header("Loot")] [Tooltip("The object this enemy can drop when dying")]
        public GameObject LootPrefab;

        [Tooltip("The chance the object has to drop")] [Range(0, 1)]
        public float DropRate = 1f;

        [Header("Debug Display")] [Tooltip("Color of the sphere gizmo representing the path reaching range")]
        public Color PathReachingRangeColor = Color.yellow;

        [Tooltip("Color of the sphere gizmo representing the attack range")]
        public Color AttackRangeColor = Color.red;

        [Tooltip("Color of the sphere gizmo representing the detection range")]
        public Color DetectionRangeColor = Color.blue;

        [Header("Patrol")]
        public List<Transform> PatrolPoints = new List<Transform>();
        private int _currentIndexOnPatrol = 0;

        [Header("Attack")]
        public float AttackRange = 10.0f;

        [Header("PlayerDetection")]
        public float RadiusDetection = 5.0f;
        public float TimerBetweenDetection = 1.0f;
        public float TimerToLoseDetection = 2.0f;
        private float _timeSinceLastDetection = 0.0f;
        private bool _detectedTarget = false;
        public float TimerToLoseDetectionOnHit = 5.0f;
        private float _timeSinceLastDetectionOnHit = 0.0f;
        private bool _detectedTargetOnHit = false;

        public GameObject DamagingPlayer;

        public UnityAction onAttack;
        public UnityAction onDetectedTarget;
        public UnityAction onLostTarget;
        public UnityAction onDamaged;

        List<RendererIndexData> m_BodyRenderers = new List<RendererIndexData>();
        MaterialPropertyBlock m_BodyFlashMaterialPropertyBlock;
        float m_LastTimeDamaged = float.NegativeInfinity;

        RendererIndexData m_EyeRendererData;
        MaterialPropertyBlock m_EyeColorMaterialPropertyBlock;

        public GameObject KnownDetectedTarget;
        public PlayerBehaviour KnownDetectedTargetBehaviour;
        public bool IsTargetInAttackRange;
        public bool IsSeeingTarget => DetectionModule.IsSeeingTarget;
        public bool HadKnownTarget => DetectionModule.HadKnownTarget;
        public NavMeshAgent NavMeshAgent { get; private set; }
        public DetectionModule DetectionModule { get; private set; }

        EnemyManager m_EnemyManager;
        ActorsManager m_ActorsManager;
        public HealthComponent m_Health;
        Actor m_Actor;
        Collider[] m_SelfColliders;
        GameFlowManager m_GameFlowManager;
        bool m_WasDamagedThisFrame;
        float m_LastTimeWeaponSwapped = Mathf.NegativeInfinity;
        int m_CurrentWeaponIndex;
        WeaponController m_CurrentWeapon;
        WeaponController[] m_Weapons;
        NavigationModule m_NavigationModule;

        void Start()
        {
            m_Health = GetComponent<HealthComponent>();

            NavMeshAgent = GetComponent<NavMeshAgent>();
            m_SelfColliders = GetComponentsInChildren<Collider>();

            // Subscribe to damage & death actions
            m_Health.OnDeath.AddListener(OnDie);
            m_Health.OnDamaged.AddListener(OnDamaged);

            // Find and initialize all weapons
            FindAndInitializeAllWeapons();
            var weapon = GetCurrentWeapon();
            weapon.ShowWeapon(true);

            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    if (renderer.sharedMaterials[i] == EyeColorMaterial)
                    {
                        m_EyeRendererData = new RendererIndexData(renderer, i);
                    }

                    if (renderer.sharedMaterials[i] == BodyMaterial)
                    {
                        m_BodyRenderers.Add(new RendererIndexData(renderer, i));
                    }
                }
            }

            m_BodyFlashMaterialPropertyBlock = new MaterialPropertyBlock();

            // Check if we have an eye renderer for this enemy
            if (m_EyeRendererData.Renderer != null)
            {
                m_EyeColorMaterialPropertyBlock = new MaterialPropertyBlock();
                m_EyeColorMaterialPropertyBlock.SetColor("_EmissionColor", DefaultEyeColor);
                m_EyeRendererData.Renderer.SetPropertyBlock(m_EyeColorMaterialPropertyBlock,
                    m_EyeRendererData.MaterialIndex);
            }
            StartCoroutine(Detect());
        }

        void Update()
        {
            IsTargetInAttackRange = KnownDetectedTarget != null &&
                                    !KnownDetectedTargetBehaviour.IsDead &&
                                    Vector3.Distance(transform.position, KnownDetectedTarget.transform.position) <=
                                    AttackRange;

            EnsureIsWithinLevelBounds();

            Color currentColor = OnHitBodyGradient.Evaluate((Time.time - m_LastTimeDamaged) / FlashOnHitDuration);
            m_BodyFlashMaterialPropertyBlock.SetColor("_EmissionColor", currentColor);
            foreach (var data in m_BodyRenderers)
            {
                data.Renderer.SetPropertyBlock(m_BodyFlashMaterialPropertyBlock, data.MaterialIndex);
            }

            m_WasDamagedThisFrame = false;
        }

        void EnsureIsWithinLevelBounds()
        {
            // at every frame, this tests for conditions to kill the enemy
            if (transform.position.y < SelfDestructYHeight)
            {
                Destroy(gameObject);
                return;
            }
        }

        private IEnumerator Detect()
        {
            if (_detectedTarget)
            {
                while (_timeSinceLastDetection < TimerToLoseDetection)
                {
                    _timeSinceLastDetection += Time.deltaTime;
                    yield return null;
                }
                OnLostTarget();
            }
            if (_detectedTargetOnHit)
            {
                while (_timeSinceLastDetectionOnHit < TimerToLoseDetectionOnHit)
                {
                    _timeSinceLastDetectionOnHit += Time.deltaTime;
                    yield return null;
                }
                OnLostTarget();
            }
            List<Collider> colliders = Physics.OverlapSphere(transform.position, RadiusDetection).ToList();
                foreach (var collider in colliders) 
                {
                    if (collider.TryGetComponent(out PlayerBehaviour playerBehaviour) && !playerBehaviour.IsDead)
                    {
                        KnownDetectedTarget = playerBehaviour.gameObject;
                        KnownDetectedTargetBehaviour = playerBehaviour;
                        _detectedTarget = true;
                        OnDetectedTarget();
                        break;
                    }
                }
            yield return new WaitForSeconds(TimerBetweenDetection);
            StartCoroutine(Detect());
        }

        public void OnLostTarget()
        {
            _detectedTarget = false;
            _detectedTargetOnHit = false;
            _timeSinceLastDetection = 0.0f;
            _timeSinceLastDetectionOnHit = 0.0f;
            KnownDetectedTarget = null;
            KnownDetectedTargetBehaviour = null;
            DamagingPlayer = null;

            onLostTarget.Invoke();

            // Set the eye attack color and property block if the eye renderer is set
            if (m_EyeRendererData.Renderer != null)
            {
                m_EyeColorMaterialPropertyBlock.SetColor("_EmissionColor", DefaultEyeColor);
                m_EyeRendererData.Renderer.SetPropertyBlock(m_EyeColorMaterialPropertyBlock,
                    m_EyeRendererData.MaterialIndex);
            }
        }

        void OnDetectedTarget()
        {
            onDetectedTarget.Invoke();

            // Set the eye default color and property block if the eye renderer is set
            if (m_EyeRendererData.Renderer != null)
            {
                m_EyeColorMaterialPropertyBlock.SetColor("_EmissionColor", AttackEyeColor);
                m_EyeRendererData.Renderer.SetPropertyBlock(m_EyeColorMaterialPropertyBlock,
                    m_EyeRendererData.MaterialIndex);
            }
        }

        public void OrientTowards(Vector3 lookPosition)
        {
            Vector3 lookDirection = Vector3.ProjectOnPlane(lookPosition - transform.position, Vector3.up).normalized;
            if (lookDirection.sqrMagnitude != 0f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation =
                    Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * OrientationSpeed);
            }
        }

        public Vector3 GetDestinationOnPath()
        {
            return PatrolPoints[_currentIndexOnPatrol].position;
        }

        public void SetNavDestination(Vector3 destination)
        {
            if (NavMeshAgent)
            {
                NavMeshAgent.SetDestination(destination);
            }
        }

        public void UpdatePathDestination(bool inverseOrder = false)
        {
            // Check if reached the path destination
            if ((transform.position - GetDestinationOnPath()).magnitude <= PathReachingRadius)
            {
                // increment path destination index
                _currentIndexOnPatrol =
                    inverseOrder ? (_currentIndexOnPatrol - 1) : (_currentIndexOnPatrol + 1);
                if (_currentIndexOnPatrol < 0)
                {
                    _currentIndexOnPatrol += PatrolPoints.Count;
                }

                if (_currentIndexOnPatrol >= PatrolPoints.Count)
                {
                    _currentIndexOnPatrol -= PatrolPoints.Count;
                }
            }
        }

        void OnDamaged(int damage)
        {
            if (!_detectedTargetOnHit)
            {
                _detectedTargetOnHit = true;
                KnownDetectedTarget = DamagingPlayer;
                KnownDetectedTargetBehaviour = DamagingPlayer.GetComponent<PlayerBehaviour>();
                OnDetectedTarget();
            }

            onDamaged?.Invoke();
            m_LastTimeDamaged = Time.time;
           
            // play the damage tick sound
            if (DamageTick && !m_WasDamagedThisFrame)
                //AudioUtility.CreateSFX(DamageTick, transform.position, AudioUtility.AudioGroups.DamageTick, 0f);
        
             m_WasDamagedThisFrame = true;
        
        }

        void OnDie()
        {
            // spawn a particle system when dying
            var vfx = Instantiate(DeathVfx, DeathVfxSpawnPoint.position, Quaternion.identity);
            Destroy(vfx, 5f);

            if (IsServer)
            {
                 // loot an object
                 if (TryDropItem())
                 {
                     NetworkObject loot = Instantiate(LootPrefab, transform.position, Quaternion.identity).GetComponent<NetworkObject>();
                     loot.Spawn();
                 }
                 // this will call the OnDestroy function
                 //Destroy(gameObject, DeathDuration);
                            
                 gameObject.GetComponent<NetworkObject>().Despawn();
            }
           
        }

        void OnDrawGizmosSelected()
        {
            // Path reaching range
            Gizmos.color = PathReachingRangeColor;
            Gizmos.DrawWireSphere(transform.position, PathReachingRadius);

            if (DetectionModule != null)
            {
                // Detection range
                Gizmos.color = DetectionRangeColor;
                Gizmos.DrawWireSphere(transform.position, DetectionModule.DetectionRange);

                // Attack range
                Gizmos.color = AttackRangeColor;
                Gizmos.DrawWireSphere(transform.position, DetectionModule.AttackRange);
            }
        }

        public void OrientWeaponsTowards(Vector3 lookPosition)
        {
            for (int i = 0; i < m_Weapons.Length; i++)
            {
                // orient weapon towards player
                Vector3 weaponForward = (lookPosition - m_Weapons[i].WeaponRoot.transform.position).normalized;
                m_Weapons[i].transform.forward = weaponForward;
            }
        }

        public bool TryAtack(Vector3 enemyPosition)
        {
            OrientWeaponsTowards(enemyPosition);

            if ((m_LastTimeWeaponSwapped + DelayAfterWeaponSwap) >= Time.time)
                return false;

            // Shoot the weapon
            bool didFire = GetCurrentWeapon().HandleShootInputs(false, true, false);

            if (didFire && onAttack != null)
            {
                onAttack.Invoke();

                if (SwapToNextWeapon && m_Weapons.Length > 1)
                {
                    int nextWeaponIndex = (m_CurrentWeaponIndex + 1) % m_Weapons.Length;
                    SetCurrentWeapon(nextWeaponIndex);
                }
            }

            return didFire;
        }

        public bool TryDropItem()
        {
            if (DropRate == 0 || LootPrefab == null)
                return false;
            else if (DropRate == 1)
                return true;
            else
                return (Random.value <= DropRate);
        }

        void FindAndInitializeAllWeapons()
        {
            // Check if we already found and initialized the weapons
            if (m_Weapons == null)
            {
                m_Weapons = GetComponentsInChildren<WeaponController>();
                DebugUtility.HandleErrorIfNoComponentFound<WeaponController, EnemyController>(m_Weapons.Length, this,
                    gameObject);

                for (int i = 0; i < m_Weapons.Length; i++)
                {
                    m_Weapons[i].Owner = gameObject;
                }
            }
        }

        public WeaponController GetCurrentWeapon()
        {
            FindAndInitializeAllWeapons();
            // Check if no weapon is currently selected
            if (m_CurrentWeapon == null)
            {
                // Set the first weapon of the weapons list as the current weapon
                SetCurrentWeapon(0);
            }

            DebugUtility.HandleErrorIfNullGetComponent<WeaponController, EnemyController>(m_CurrentWeapon, this,
                gameObject);

            return m_CurrentWeapon;
        }

        void SetCurrentWeapon(int index)
        {
            m_CurrentWeaponIndex = index;
            m_CurrentWeapon = m_Weapons[m_CurrentWeaponIndex];
            if (SwapToNextWeapon)
            {
                m_LastTimeWeaponSwapped = Time.time;
            }
            else
            {
                m_LastTimeWeaponSwapped = Mathf.NegativeInfinity;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(transform.position, RadiusDetection);
        }
    }
}