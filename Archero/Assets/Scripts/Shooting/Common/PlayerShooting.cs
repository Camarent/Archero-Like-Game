using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace General.Common
{
    public class PlayerShooting : MonoBehaviour
    {
        [SerializeField] private List<GunShooting> weapons;

        [SerializeField] private GunShooting currentGun;

        private int currentIndex;
        private BlobAssetStore store;

        private void Awake()
        {
            store = new BlobAssetStore();

            var settings = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, store);

            foreach (var weapon in weapons)
            {
                weapon.settings = settings;
                weapon.gameObject.SetActive(false);
            }

            if (weapons.Count > 0)
                ActivateGun(0);
        }

        void OnFire()
        {
            if (!Settings.IsPlayerDead())
                currentGun.Shoot();
        }

        void OnSwitchWeapon()
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