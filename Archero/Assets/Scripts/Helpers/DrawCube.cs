using UnityEngine;

namespace Helpers
{
    public class DrawCube : MonoBehaviour
    {
        private Transform currentTransform;
        void OnDrawGizmosSelected()
        {
            if (currentTransform == null)
                currentTransform = transform;
        
            Gizmos.color = new Color(1, 0, 0, 0.5f);
            Gizmos.DrawCube(currentTransform.position, currentTransform.localScale);
        }
    }
}