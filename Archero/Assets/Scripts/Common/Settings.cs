using System;
using Movement.Common;
using Player;
using UnityEngine;
using Random = UnityEngine.Random;

public class Settings : MonoBehaviour
{
	static Settings instance;

	[Header("Game Object References")]
	public Transform player;

	public static Vector3 PlayerPosition => instance.player.position;

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
		if (instance != null && instance != this)
			Destroy(gameObject);
		else
			instance = this;

		if (player == null)
			player = GameObject.FindWithTag("Player")?.transform;
	}

	public static Vector3 GetPositionAroundPlayer(float radius)
	{
		var playerPos = instance.player.position;
		var angle = Random.Range(0f, 2 * Mathf.PI);
		var s = Mathf.Sin(angle);
		var c = Mathf.Cos(angle);
		
		return new Vector3(c * radius, 1.1f, s * radius) + playerPos;
	}

	public static void PlayerDied()
	{
		if (instance.player == null)
			return;

		var playerMove = instance.player.GetComponent<PlayerMovementAndLook>();
		playerMove.PlayerDied();

		instance.player = null;
	}

	public static bool IsPlayerDead()
	{
		return instance.player == null;
	}
}
