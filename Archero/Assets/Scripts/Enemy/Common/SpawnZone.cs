using Common;
using UnityEngine;

namespace Enemy.Common
{
    public class SpawnZone : MonoBehaviour
    {
        [SerializeField] private SpawnProducerFactory.SpawnType spawnType;
        [SerializeField] private bool oneTimeSpawn;
        
        [SerializeField] private GameObject prefab;

        [SerializeField] private int minAmount;
        [SerializeField] private int maxAmount;

        [SerializeField] private float offsetY = 1;
        [SerializeField] private float space = 1;

        public int MinAmount => minAmount;
        public int MaxAmount => maxAmount;
        public float Space => space;

        private ISpawnProducerFactory factory;
        private bool spawned;

        void Start()
        {
            factory = GameManager.Instance.SpawnProducerFactory;

            GameManager.Instance.GameStatusChanged += s =>
            {
                spawned = false;
                if (GameManager.Instance.GameStatus == GameManager.Status.Hold)
                    Spawn();
            };

            if (GameManager.Instance.GameStatus == GameManager.Status.Hold)
                Spawn();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.O))
                Spawn();
        }

        public void Spawn()
        {
            if(oneTimeSpawn && spawned) return;

            spawned = true;
            factory.Get(spawnType).Produce(prefab, this);
        }

        public Quaternion GetRandomRotation()
        {
            var y = Random.Range(0f, 360f);
            return Quaternion.Euler(0, y, 0);
        }

        public Vector3 GetPositionInsideZone()
        {
            var zoneTransform = transform;
            var localScale = zoneTransform.localScale;
            var x = Random.Range(-localScale.x / 2, localScale.x / 2);
            var z = Random.Range(-localScale.z / 2, localScale.z / 2);

            var position = zoneTransform.position;
            var flooredZonePosition = new Vector3(position.x, -localScale.y / 2 + offsetY, position.z);
            return flooredZonePosition + new Vector3(x, 0, z);
        }
    }
}