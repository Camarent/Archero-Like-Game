using System.Collections.Generic;
using Common;
using Unity.Entities;
using UnityEngine;

namespace Shooting.Common
{
    public class PlayerShooting : MonoBehaviour
    {
        [SerializeField] private List<GunShooting> weapons;

        [SerializeField] private GunShooting currentGun;

        public GameObjectConversionSettings Settings { get; private set; }
        
        private int currentIndex;
        private BlobAssetStore store;

        private void Start()
        {
            store = new BlobAssetStore();

            Settings = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, store);

            foreach (var weapon in weapons)
            {
                weapon.settings = Settings;
                weapon.gameObject.SetActive(false);
                weapon.Initialize();
            }

            if (weapons.Count > 0)
                ActivateGun(0);
        }

        public void OnFire()
        {
            if (!PlayerSettings.IsPlayerDead())
                currentGun.Shoot();
        }

        public void OnSwitchWeapon()
        {
            currentGun.gameObject.SetActive(false);

            ++currentIndex;

            if (currentIndex >= weapons.Count)
                currentIndex = 0;

            ActivateGun(currentIndex);
        }

        private void ActivateGun(int index)
        {
            currentGun = weapons[index];
            currentGun.gameObject.SetActive(true);
        }

        private void OnDestroy()
        {
            store.Dispose();
        }
    }
}