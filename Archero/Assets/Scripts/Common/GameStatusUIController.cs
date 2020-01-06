using System.Collections;
using System.Collections.Generic;
using Common;
using TMPro;
using UnityEngine;

public class GameStatusUIController : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI text;
    void Start()
    {
        GameManager.Instance.GameStatusChanged += StatusChanged;
        StatusChanged(GameManager.Instance.GameStatus);
    }

    public void StatusChanged(GameManager.Status status)
    {
        if (status == GameManager.Status.Play)
            text.gameObject.SetActive(false);
        else
            text.gameObject.SetActive(true);

        text.text = $"Game Status: {status.ToString()}";
    }
}
