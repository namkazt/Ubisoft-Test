using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Collections.Generic;


public class SharedData : NetworkBehaviour
{
    [Header("UI Ref Objects")]
    public GameObject uiLoadingText;
    public GameObject uiPlayingPanel;
    public GameObject uiResultPanel;

    public Text uiPlayerPoints;
    public Text uiGameTime;
    public Text uiResultText;
    public Text uiPlayerAPoints;
    public Text uiPlayerBPoints;

    [SyncVar(hook = nameof(OnGameStateChanged))]
    public GameState state;

    [SyncVar(hook = nameof(OnStartTimeChanged))]
    public float startInTime = 3f;

    [SyncVar(hook = nameof(OnGameTimeChanged))]
    public float gameTime = 0f;

    [SyncVar(hook = nameof(OnGameResultChanged))]
    public GameResult gameResult = null;

    private void Start()
    {
        PlayerNetworkControl.OnUpdatePoints += OnPlayerPointUpdated;
    }

    public void OnGameResultChanged(GameResult old, GameResult gameResult)
    {
        if (isServer)
        {
            Debug.Log("Called from server");
        }

        PlayerNetworkControl player = NetworkClient.connection.identity.GetComponent<PlayerNetworkControl>();
        if (player.slot == "A")
        {
            uiResultText.text = gameResult.playerAPoint > gameResult.playerBPoint ? "You Win" : (gameResult.playerAPoint == gameResult.playerBPoint ? "Match Draw" : "You Lose");
        }
        else
        {
            uiResultText.text = gameResult.playerAPoint < gameResult.playerBPoint ? "You Win" : (gameResult.playerAPoint == gameResult.playerBPoint ? "Match Draw" : "You Lose");
        }
        uiPlayerAPoints.text = gameResult.playerAPoint < 0 ? "Leaved" : Mathf.RoundToInt(gameResult.playerAPoint).ToString();
        uiPlayerBPoints.text = gameResult.playerBPoint < 0 ? "Leaved" : Mathf.RoundToInt(gameResult.playerBPoint).ToString();
    }


    public void OnPlayerPointUpdated(PlayerNetworkControl player, int points)
    {
        if(player.hasAuthority)
        {
            uiPlayerPoints.text = "Player Points: " + points.ToString();
        }
    }

    public void OnStartTimeChanged(float oldVal, float newVal) {

        uiLoadingText.GetComponent<Text>().text = "Start in " + Mathf.RoundToInt(newVal).ToString();
    }
    public void OnGameTimeChanged(float oldVal, float newVal)
    {
        uiGameTime.GetComponent<Text>().text = Mathf.RoundToInt(newVal).ToString();
    }

    public void OnGameStateChanged(GameState oldState, GameState newState) {
        switch (newState)
        {
            case GameState.PREPARE:
                {
                    uiLoadingText.GetComponent<Text>().text = "Wait for other player...";
                    uiLoadingText.SetActive(true);
                    uiPlayingPanel.SetActive(false);
                    uiResultPanel.SetActive(false);
                    break;
                }
            case GameState.STARTING:
                {
                    uiLoadingText.GetComponent<Text>().text = "Start in " + Mathf.RoundToInt(startInTime).ToString();
                    uiLoadingText.SetActive(true);
                    uiPlayingPanel.SetActive(false);
                    uiResultPanel.SetActive(false);
                    break;
                }
            case GameState.STARTED:
                {
                    uiLoadingText.SetActive(false);
                    uiPlayingPanel.SetActive(true);
                    uiResultPanel.SetActive(false);
                    break;
                }
            case GameState.FINISHED:
                {
                    uiLoadingText.SetActive(false);
                    uiPlayingPanel.SetActive(false);
                    uiResultPanel.SetActive(true);
                    break;
                }
        }
    }
}
