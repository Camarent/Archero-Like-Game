using Common;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Movement.Common
{
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Camera")] public Camera mainCamera;

        [Header("Movement")] public float speed = 4.5f;
        public LayerMask whatIsGround;

        [Header("Life Settings")] public float playerHealth = 1f;

        [Header("Animation")] public Animator playerAnimator;

        private Rigidbody _playerRigidbody;
        private bool _isDead;

        private Vector2 _currentMove;
        private Vector2 _currentLook;

        void Awake()
        {
            _playerRigidbody = GetComponent<Rigidbody>();
            if (!mainCamera) mainCamera = Camera.main;
            if (!playerAnimator) playerAnimator = GetComponent<Animator>();
        }

        private void OnMove(InputValue input)
        {
            _currentMove = input.Get<Vector2>();
        }


        private void FixedUpdate()
        {
            Move(_currentMove);
        }

        private void Move(Vector2 input)
        {
            if (_isDead)
                return;

            var inputDirection = new Vector3(input.x, 0, input.y);

            //Camera Direction
            var cameraTransform = mainCamera.transform;
            var cameraForward = cameraTransform.forward;
            var cameraRight = cameraTransform.right;

            cameraForward.y = 0f;
            cameraRight.y = 0f;

            //Try not to use var for roadshows or learning code
            var desiredDirection = cameraForward * inputDirection.z + cameraRight * inputDirection.x;

            //Why not just pass the vector instead of breaking it up only to remake it on the other side?
            MoveThePlayer(desiredDirection);
            AnimateThePlayer(desiredDirection);
        }

        void MoveThePlayer(Vector3 desiredDirection)
        {
            var movement = new Vector3(desiredDirection.x, 0f, desiredDirection.z);
            movement = speed * Time.deltaTime * movement.normalized;

            _playerRigidbody.MovePosition(transform.position + movement);
        }

        void AnimateThePlayer(Vector3 desiredDirection)
        {
            if (!playerAnimator)
                return;

            var movement = new Vector3(desiredDirection.x, 0f, desiredDirection.z);
            var forward = Vector3.Dot(movement, transform.forward);
            var strafe = Vector3.Dot(movement, transform.right);

            playerAnimator.SetFloat("Forward", forward);
            playerAnimator.SetFloat("Strafe", strafe);
        }

        void OnTriggerEnter(Collider theCollider)
        {
            if (!theCollider.CompareTag("Enemy"))
                return;

            if (--playerHealth <= 0)
                PlayerSettings.PlayerDied();
        }

        public void PlayerDied()
        {
            if (_isDead)
                return;

            _isDead = true;

            playerAnimator.SetTrigger("Died");
            _playerRigidbody.isKinematic = true;
            GetComponent<Collider>().enabled = false;
        }
    }
}