﻿using TMPro;
using UnityEngine;

namespace Common
{
    public class CoinTextController : MonoBehaviour
    {
        [SerializeField] private string startText;
        [SerializeField] private TextMeshProUGUI text;

        void Start()
        {
            PlayerSettings.CoinChanged += IncreaseCoin;
        }

        private void IncreaseCoin(int coinCount)
        {
            text.text = $"{startText}{coinCount}";
        }
    }
}
