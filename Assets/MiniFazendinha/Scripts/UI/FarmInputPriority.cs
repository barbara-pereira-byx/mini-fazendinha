using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MiniFazendinha.UI
{
    // The same raycast policy is used by camera, planting and world interaction.
    public static class FarmInputPriority
    {
        static readonly List<RaycastResult> hits = new List<RaycastResult>();
        static void Raycast(Vector2 point)
        {
            hits.Clear();
            if (EventSystem.current != null)
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
        }
        public static bool IsOverUI(Vector2 point) { Raycast(point); return hits.Count > 0; }
        public static bool CapturesScroll(Vector2 point)
        {
            Raycast(point);
            foreach (var hit in hits)
            {
                var scroll = hit.gameObject.GetComponentInParent<ScrollRect>();
                if (scroll != null && scroll.isActiveAndEnabled) return true;
                if (hit.gameObject.GetComponentInParent<FarmScrollCapture>() != null) return true;
            }
            return false;
        }
    }
    public sealed class FarmScrollCapture : MonoBehaviour { }
}
