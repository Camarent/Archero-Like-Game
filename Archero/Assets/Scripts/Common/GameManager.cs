using System;
using Enemy.Common;
using UnityEngine;

namespace Common
{
    public sealed class GameManager : IDisposable
    {
        public enum Status
        {
            Restart,
            Hold,
            Play,
            Failed,
            Success
        }

        private static readonly Lazy<GameManager>
            Lazy =
                new Lazy<GameManager>
                    (() => new GameManager());

        public static GameManager Instance => Lazy.Value;

        public GameController monoHelper { get; set; }
        public ISpawnProducerFactory SpawnProducerFactory { get; set; }

        public Status GameStatus { get; private set; }
        public event Action<Status> GameStatusChanged;

        private GameManager()
        {
            SpawnProducerFactory = new SpawnProducerFactory();
        }

        public void Hold()
        {
            GameStatus = Status.Hold;
            monoHelper.Wait(Start, monoHelper.waitTime);
        }

        public void Start()
        {
            GameStatus = Status.Play;
            OnGameStatusChanged(GameStatus);
        }

        public void Restart()
        {
            GameStatus = Status.Restart;
            OnGameStatusChanged(GameStatus);

            Hold();
        }

        public void Success()
        {
            GameStatus = Status.Success;
            OnGameStatusChanged(GameStatus);
        }

        public void Failed()
        {
            GameStatus = Status.Failed;
            OnGameStatusChanged(GameStatus);

            if (monoHelper.autoRestart)
                Hold();
        }

        private void OnGameStatusChanged(Status newStatus)
        {
            Debug.Log($"Game Status changed: {GameStatus}");
            GameStatusChanged?.Invoke(newStatus);
        }

        public void Dispose()
        {
            SpawnProducerFactory.Dispose();
        }
    }
}