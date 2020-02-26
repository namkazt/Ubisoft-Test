using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LeakyAbstraction;

[System.Serializable]
public class GameResult
{
    public float playerAPoint;
    public float playerBPoint;
}

[System.Serializable]
public enum GameState { PREPARE, STARTING, STARTED, FINISHED }

[AddComponentMenu("")]
public class HallowenNetworkManager : NetworkManager
{
    [Header("Game Play Config")]
    public Transform playerASpawnPoint;
    public Transform playerBSpawnPoint;
    public float candySpawnHeight;
    public float candySpawnStartX;
    public float candySpawnEndX;
    public SharedData sharedData;

    public Dictionary<string, bool> usedSlot = new Dictionary<string, bool>() {
        { "A" , false },
        { "B" , false },
    };

    public GameState state;

    public override void OnStartServer()
    {
        base.OnStartServer();

        state = GameState.PREPARE;
        sharedData.state = state;
    }

    public Dictionary<string, PlayerNetworkControl> players = new Dictionary<string, PlayerNetworkControl>();

    public override void OnServerConnect(NetworkConnection conn)
    {
        if (state >= GameState.STARTING)
        {
            conn.Disconnect();
            return;
        }

        base.OnServerConnect(conn);
    }
    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        if(state >= GameState.STARTING)
        {
            return;
        }

        // add player at correct spawn position
        Transform start = numPlayers == 0 ? playerASpawnPoint : playerBSpawnPoint;
        GameObject player = Instantiate(playerPrefab, start.position, start.rotation);
        player.transform.parent = transform;
        // init player
        PlayerNetworkControl playerController = player.GetComponent<PlayerNetworkControl>();
        // assign slot
        playerController.slot = usedSlot["A"] ? "B" : "A";
        usedSlot[playerController.slot] = true;
        playerController.Initialize();
        players.Add(playerController.slot, playerController);

        NetworkServer.AddPlayerForConnection(conn, player);


        // Start game if two players
        if (numPlayers == 2 && state < GameState.STARTING)
        {
            state = GameState.STARTING;
            // send message to all player that game state is changed to start
            sharedData.state = state;
            sharedData.startInTime = 3f;
            InvokeRepeating(nameof(OnUpdateCountDown), 1, 1);
        }
    }

    public void OnUpdateCountDown()
    {
        sharedData.startInTime -= 1;
        if (sharedData.startInTime == 0)
        {
            state = GameState.STARTED;
            sharedData.state = state;
            CancelInvoke(nameof(OnUpdateCountDown));

            ServerStartTheGame();
        }
    }

    void ServerStartTheGame()
    {
        sharedData.gameTime = 30f;
        // Start generating updates
        InvokeRepeating(nameof(SpawnCandy), 1, 2);
    }

    private void Update()
    {
        if(state == GameState.STARTED)
        {
            sharedData.gameTime -= Time.deltaTime;

            if(sharedData.gameTime <= 0)
            {
                CancelInvoke(nameof(SpawnCandy));

                // destroy all candies left
                foreach (GameObject go in candies.Values)
                {
                    NetworkServer.Destroy(go);
                }
                candies.Clear();

                var result = new GameResult();
                result.playerAPoint = players["A"].points;
                result.playerBPoint = players["B"].points;
                
                state = GameState.FINISHED;
                sharedData.state = state;
                sharedData.gameTime = 0;
                sharedData.gameResult = result;
            }
        }
    }

   

    public static readonly string[] candiesName = {"Candy1", "Candy2", "Candy3", "Danger1", "Danger2"};


    public Dictionary<string, GameObject> candies = new Dictionary<string, GameObject>();
    void SpawnCandy()
    {
        int rIdx = Random.Range(0, candiesName.Length);
        var pos = new Vector3(Random.Range(candySpawnStartX, candySpawnEndX), candySpawnHeight, 0);
        var candy = Instantiate(spawnPrefabs.Find(prefab => prefab.name == candiesName[rIdx]), pos, Quaternion.identity);
        var controller = candy.GetComponent<CandyController>();
        controller.server = this;
        controller.candyName = candiesName[rIdx];
        candies.Add(candy.GetInstanceID().ToString(), candy);
        NetworkServer.Spawn(candy);
    }

    public override void OnServerDisconnect(NetworkConnection conn)
    {
        if (conn.identity == null) return;
        var dcPlayer = conn.identity.GetComponent<PlayerNetworkControl>();
        if (dcPlayer == null) return;
        Debug.Log("Player: " + dcPlayer.slot + " quit the game");

        // remove player
        usedSlot[dcPlayer.slot] = false;
        players.Remove(dcPlayer.slot);

        // check for game state
        switch (state)
        {
            case GameState.PREPARE:
                // if game is not started yet then we do not need to do anything
                break;
            case GameState.STARTING:
                // if game is starting then mean there is already 2 players
                // we need reset game state back to Prepare
                CancelInvoke(nameof(OnUpdateCountDown));

                state = GameState.PREPARE;
                sharedData.state = state;
                break;
            case GameState.STARTED:
                // if game is already started mean we need to cancel SpawnCandy and destroy all candy

                CancelInvoke(nameof(SpawnCandy));
                // destroy all canndies
                foreach (GameObject go in candies.Values)
                {
                    NetworkServer.Destroy(go);
                }
                candies.Clear();

                // we show result screen with one winner and player quit reason
                var result = new GameResult();
                result.playerAPoint = dcPlayer.slot == "A" ? -1 : players["A"].points;
                result.playerBPoint = dcPlayer.slot == "B" ? -1 : players["B"].points;

                state = GameState.FINISHED;
                sharedData.state = state;
                sharedData.gameTime = 0;
                sharedData.gameResult = result;

                break;
            case GameState.FINISHED:
                // if game is finished mean all cancel callback and thing is done. we just need to check 
                // if number player is zero then game reset back to Prepare
                if (numPlayers == 0)
                {
                    state = GameState.PREPARE;
                    sharedData.state = state;
                }
                break;
        }

        base.OnServerDisconnect(conn);
    }
}
