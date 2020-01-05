using System;
using General.Common;
using Player;
using Unity.Entities;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Common
{
    public class PlayerSettings : MonoBehaviour
    {
        static PlayerSettings _instance;

        [Header("Game Object References")] 
        public GameObject coinPrefab;
        
        private Transform player;

        public static Vector3 PlayerPosition => _instance.player.position;
        public static float CoinSpeed => 1f;

        private Entity coin;
        public static Entity Coin => _instance.coin;

        public static event Action<int> CoinChanged;
        private int _coins;

        public int Coins
        {
            get => _coins;
            set
            {
                _coins = value;
                CoinChanged?.Invoke(_coins);
            }
        }


        void Awake()
        {
            if (_instance != null && _instance != this)
                Destroy(gameObject);
            else
                _instance = this;

        }

        private void Start()
        {
            GameManager.Instance.GameStatusChanged += status =>
            {
                if (status == GameManager.Status.Play)
                    AssignPlayer();
            };
        }

        private void AssignPlayer()
        {
            if (player == null)
                player = GameObject.FindWithTag("Player")?.transform;

            if (player != null)
            {
                coin = GameObjectConversionUtility.ConvertGameObjectHierarchy(coinPrefab, player.GetComponent<PlayerShooting>().Settings);
            }
        }

        public static Vector3 GetPositionAroundPlayer(float radius)
        {
            var playerPos = _instance.player.position;
            var angle = Random.Range(0f, 2 * Mathf.PI);
            var s = Mathf.Sin(angle);
            var c = Mathf.Cos(angle);

            return new Vector3(c * radius, 1.1f, s * radius) + playerPos;
        }

        public static void PlayerDied()
        {
            if (_instance.player == null)
                return;

            var playerMove = _instance.player.GetComponent<PlayerMovementAndLook>();
            playerMove.PlayerDied();

            _instance.player = null;
            Destroy(_instance.player);
        }

        public static bool IsPlayerDead()
        {
            return _instance.player == null;
        }

        public static void IncreaseCoin()
        {
            ++_instance.Coins;
        }
    }
}