using System.Collections.Generic;
using UnityEngine;

namespace Unity.FPS.Game
{
    public class DamageArea : MonoBehaviour
    {
        [Tooltip("Area of damage when the projectile hits something")]
        public float AreaOfEffectDistance = 5f;

        [Tooltip("Damage multiplier over distance for area of effect")]
        public AnimationCurve DamageRatioOverDistance;

        [Header("Debug")] [Tooltip("Color of the area of effect radius")]
        public Color AreaOfEffectColor = Color.red * 0.5f;

        public void InflictDamageInArea(float damage, Vector3 center, LayerMask layers,
            QueryTriggerInteraction interaction, GameObject owner)
        {
            List<HealthComponent> uniqueHealthComponents = new List<HealthComponent>();

            // Create a collection of unique health components that would be damaged in the area of effect (in order to avoid damaging a same entity multiple times)
            Collider[] affectedColliders = Physics.OverlapSphere(center, AreaOfEffectDistance, layers, interaction);
            foreach (var coll in affectedColliders)
            {
                HealthComponent healthComponent = coll.GetComponent<HealthComponent>();
                if (healthComponent)
                {
                    if (!uniqueHealthComponents.Contains(healthComponent))
                    {
                        uniqueHealthComponents.Add(healthComponent);
                    }
                }
            }

            // Apply damages with distance falloff
            foreach (HealthComponent uniqueHealthComponent in uniqueHealthComponents)
            {
                float distance = Vector3.Distance(uniqueHealthComponent.transform.position, transform.position);
                uniqueHealthComponent.TakeDamage((sbyte)(damage * DamageRatioOverDistance.Evaluate(distance / AreaOfEffectDistance)));
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = AreaOfEffectColor;
            Gizmos.DrawSphere(transform.position, AreaOfEffectDistance);
        }
    }
}