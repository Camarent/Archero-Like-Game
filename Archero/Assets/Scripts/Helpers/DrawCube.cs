using UnityEngine;

namespace Helpers
{
    public class DrawCube : MonoBehaviour
    {
        [SerializeField]
        private Color Color = Color.red;
        
        private Transform currentTransform;
        
        void OnDrawGizmosSelected()
        {
            if (currentTransform == null)
                currentTransform = transform;
        
            Gizmos.color = Color;
            Gizmos.DrawCube(currentTransform.position, currentTransform.localScale);
        }
    }
}