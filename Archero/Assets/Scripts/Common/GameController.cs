using System;
using System.Collections;
using UnityEngine;

namespace Common
{
    public class GameController : MonoBehaviour
    {
        public bool autoRestart;
        public float waitTime = 3f;

        private void Awake()
        {
            GameManager.Instance.monoHelper = this;
            GameManager.Instance.Hold();
        }

        private IEnumerator WaitCoroutine(Action callback, float time)
        {
            yield return new WaitForSeconds(time);
            callback?.Invoke();
        }

        public void Wait(Action callback, float time)
        {
            StartCoroutine(WaitCoroutine(callback, time));
        }

        private void OnDestroy()
        {
            GameManager.Instance.Dispose();
        }
    }
}