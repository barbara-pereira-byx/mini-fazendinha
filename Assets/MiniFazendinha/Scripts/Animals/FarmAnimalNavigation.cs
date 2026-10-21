using System.Collections.Generic;
using UnityEngine;

namespace MiniFazendinha.Animals
{
    [DisallowMultipleComponent]
    public sealed class FarmAnimalNavigation : MonoBehaviour
    {
        Bounds walkBounds;
        Vector3 destination;
        float groundY;
        float bodyRadius = 0.35f;
        bool hasDestination;
        readonly RaycastHit[] hitBuffer = new RaycastHit[16];

        public Bounds WalkBounds => walkBounds;
        public bool HasDestination => hasDestination;
        public Vector3 Destination => destination;

        public void Configure(Bounds area, float inset, float radius)
        {
            walkBounds = area;
            walkBounds.Expand(new Vector3(-2f * inset, 0f, -2f * inset));
            groundY = area.min.y + 0.025f;
            bodyRadius = Mathf.Max(0.12f, radius * 0.72f);
            Vector3 p = transform.position;
            p.x = Mathf.Clamp(p.x, walkBounds.min.x, walkBounds.max.x);
            p.z = Mathf.Clamp(p.z, walkBounds.min.z, walkBounds.max.z);
            p.y = groundY;
            transform.position = p;
        }

        public bool ChooseWanderPoint()
        {
            if (walkBounds.size.x <= 0f || walkBounds.size.z <= 0f) return false;
            destination = new Vector3(
                Random.Range(walkBounds.min.x, walkBounds.max.x),
                groundY,
                Random.Range(walkBounds.min.z, walkBounds.max.z));
            hasDestination = true;
            return true;
        }

        public void SetDestination(Vector3 point)
        {
            destination = new Vector3(point.x, groundY, point.z);
            hasDestination = true;
        }

        public void ClearDestination() { hasDestination = false; }

        public bool Advance(float speed, float deltaTime, float separationRadius, IEnumerable<FarmAnimalController> others, bool allowOutside = false)
        {
            if (!hasDestination) return true;
            Vector3 position = transform.position;
            Vector3 toTarget = destination - position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude <= 0.035f)
            {
                hasDestination = false;
                return true;
            }

            Vector3 direction = toTarget.normalized;
            Vector3 repulsion = Vector3.zero;
            if (others != null)
            {
                foreach (var other in others)
                {
                    if (other == null || other.gameObject == gameObject || other.IsInsideShelter) continue;
                    Vector3 away = position - other.transform.position;
                    away.y = 0f;
                    float distance = away.magnitude;
                    if (distance > 0.001f && distance < separationRadius)
                        repulsion += away.normalized * ((separationRadius - distance) / separationRadius);
                }
            }
            if (repulsion.sqrMagnitude > 0.01f)
                direction = (direction + repulsion.normalized * 0.8f).normalized;

            float step = speed * deltaTime;
            Vector3 origin = position + Vector3.up * (bodyRadius + 0.08f);
            int hitCount = Physics.SphereCastNonAlloc(origin, bodyRadius, direction, hitBuffer, step + 0.08f, ~0, QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < hitCount; i++)
            {
                var hit = hitBuffer[i];
                if (hit.collider == null || hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform) ||
                    hit.collider.GetComponentInParent<FarmAnimalController>() != null ||
                    (allowOutside && hit.collider.GetComponentInParent<AnimalShelter>() != null) ||
                    hit.collider.GetComponentInParent<FeedingPoint>() != null) continue;
                if (hit.distance < nearest) nearest = hit.distance;
            }
            if (nearest < step + 0.04f)
            {
                hasDestination = false;
                return false;
            }

            Vector3 next = position + direction * step;
            next.y = groundY;
            if (!allowOutside)
            {
                next.x = Mathf.Clamp(next.x, walkBounds.min.x, walkBounds.max.x);
                next.z = Mathf.Clamp(next.z, walkBounds.min.z, walkBounds.max.z);
            }
            transform.position = next;
            Vector3 facing = next - position;
            facing.y = 0f;
            if (facing.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(facing), 220f * deltaTime);
            return false;
        }
    }
}
