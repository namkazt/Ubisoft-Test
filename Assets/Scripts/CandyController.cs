using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LeakyAbstraction;

public class CandyController : NetworkBehaviour
{
    public float moveSpeed = 100;
    public Rigidbody2D rigidbody;
    [SyncVar]
    public string candyName;

    public HallowenNetworkManager server;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        rigidbody.velocity = Vector2.down * moveSpeed * Time.fixedDeltaTime;
    }

    [ServerCallback]
    private void OnCollisionEnter2D(Collision2D col)
    {
        var pController = col.gameObject.GetComponent<PlayerNetworkControl>();
        if (pController != null)
        {
            pController.RpcPlayHitFX();
            switch (candyName)
            {
                case "Candy1":
                    pController.ServerUpdatePlayerPoints(1);
                    //player.CmdAddPoints(player.gameObject, 1);
                    break;
                case "Candy2":
                    pController.ServerUpdatePlayerPoints(3);
                    //player.CmdAddPoints(player.gameObject, 3);
                    break;
                case "Candy3":
                    pController.ServerUpdatePlayerPoints(6);
                    //player.CmdAddPoints(player.gameObject, 7);
                    break;
                case "Danger1":
                    pController.ServerSetAdditionSpeed(0, 1f);
                    // Stun Effect: 2 seconds
                    break;
                case "Danger2":
                    pController.ServerSetAdditionSpeed(0.4f, 2f);
                    // Slow Effect: 3 seconds
                    break;
                default:
                    Debug.Log("Unknow candy name: " + candyName);
                    break;
            }

            // destroy object
            server.candies.Remove(gameObject.GetInstanceID().ToString());
            NetworkServer.Destroy(gameObject);
        }
    }

    [ServerCallback]
    void OnTriggerEnter2D(Collider2D col)
    {
        if(col.gameObject.name == "Bottom")
        {
            server.candies.Remove(gameObject.GetInstanceID().ToString());
            NetworkServer.Destroy(gameObject);
            return;
        }
    }
}
