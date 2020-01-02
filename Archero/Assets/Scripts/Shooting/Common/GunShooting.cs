using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.VFX;

namespace Shooting.Common
{
    public class GunShooting : MonoBehaviour
    {
        [Header("Shoot Point")] [SerializeField] protected Transform shootPoint;

        [Header("Bullet")] [SerializeField] private GameObject bulletPrefab;

        [Header("Trail")] [SerializeField] private Material material;
        [Header("Shoot VFX")] [SerializeField] private VisualEffect shootVfx;
        [Header("Shoot VFX")] [SerializeField] private AudioSource shootAudio;

        [Header("Settings")] [SerializeField] private float fireRate = 0.1f;

        public GameObjectConversionSettings settings { get; set; }

        private float timer;

        protected EntityManager manager;
        protected Entity bulletEntity;

        protected virtual void Start()
        {
            manager = World.DefaultGameObjectInjectionWorld.EntityManager;

            bulletEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(bulletPrefab, settings);
        }

        private void Update()
        {
            timer += Time.deltaTime;
        }

        public virtual void Shoot()
        {
            if (timer < fireRate) return;

            SpawnBullet();

            if (shootVfx != null)
                SpawnVFX();

            if (shootAudio != null)
                SpawnAudio();
        }

        protected virtual void SpawnBullet()
        {
            var bullet = manager.Instantiate(bulletEntity);

            manager.SetComponentData(bullet, new Translation {Value = shootPoint.position});
            manager.SetComponentData(bullet, new Rotation {Value = Quaternion.FromToRotation(Vector3.forward, shootPoint.forward)});

            //manager.AddComponentData(bullet, new LineSegment(math.float3(shootPoint.position), math.float3(shootPoint.position+Vector3.forward*3)));
            //manager.AddSharedComponentData(bullet, new LineStyle { material = material });
        }

        protected virtual void SpawnVFX()
        {
            shootVfx.Play();
        }

        protected virtual void SpawnAudio()
        {
            shootAudio.PlayDelayed(300);
        }
    }
}