using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class PlayerMovementAndLook : MonoBehaviour
    {
        [Header("Camera")] public Camera mainCamera;

        [Header("Movement")] public float speed = 4.5f;
        public LayerMask whatIsGround;

        private Transform playerTransform;
        private Rigidbody playerRigidbody;
        private bool isDead;

        private Vector2 currentMove;
        private Vector2 currentLook;

        void Awake()
        {
            playerTransform = transform;
            playerRigidbody = GetComponent<Rigidbody>();
            if (!mainCamera) mainCamera = Camera.main;
        }

        private void OnMove(InputValue input)
        {
            currentMove = input.Get<Vector2>();
        }

        private void OnLook(InputValue input)
        {
            currentLook = input.Get<Vector2>();
        }


        private void FixedUpdate()
        {
            Move();
            TurnThePlayer();
        }

        void Move()
        {
            if (isDead)
                return;

            var inputDirection = new Vector3(currentMove.x, 0, currentMove.y);

            var playerTransformForward = playerTransform.forward;
            var playerTransformRight = playerTransform.right;

            playerTransformForward.y = 0f;
            playerTransformRight.y = 0f;

            var desiredDirection = playerTransformForward * inputDirection.z + playerTransformRight * inputDirection.x;
            desiredDirection.y = 0f;
            MoveThePlayer(inputDirection);
        }

        void MoveThePlayer(Vector3 movement)
        {
            movement = movement.normalized * (speed * Time.deltaTime);

            playerRigidbody.MovePosition(transform.position + movement);
        }

        void TurnThePlayer()
        {
            var worldPoint = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            
            if (!Physics.Raycast(worldPoint, Vector3.down, out var hit, float.MaxValue, whatIsGround)) return;

            var playerToMouse = hit.point - transform.position;
            playerToMouse.y = 0f;
            playerToMouse.Normalize();

            var newRotation = Quaternion.LookRotation(playerToMouse);
            playerRigidbody.MoveRotation(newRotation);
        }

        public void PlayerDied()
        {
            if (isDead)
                return;

            isDead = true;
        }
    }
}