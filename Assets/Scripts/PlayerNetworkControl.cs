using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LeakyAbstraction;
using System;

public class PlayerNetworkControl : NetworkBehaviour
{

    public float moveSpeed = 30;
    public Rigidbody2D rigidbody2d;
    public ParticleSystem hitFX;

    public float movement;
    public float oldMovement;

    [SyncVar]
    public string slot;

    [SyncVar(hook = nameof(ClientGetUpdatePlayerPoints))]
    public int points = 0;

    // 1 is normal speed.
    [SyncVar(hook = nameof(ClientGetAdditionSpeed))]
    public float additionMoveSpeed = 1;
    [SyncVar]
    public float effectTime = 0;

    public Animator animator;
    public SpriteRenderer spriteRenderer;


    private SharedData _sharedData;

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        var gs = GameObject.Find("SharedData");
        if(gs != null)
        {
            _sharedData = gs.GetComponent<SharedData>();
            Debug.Log("Get shared data");
        }
    }

    [ServerCallback]
    public void ServerUpdatePlayerPoints(int points)
    {
        this.points += points;
    }

    // call back for other object to ref
    public static event Action<PlayerNetworkControl, int> OnUpdatePoints;
    [ClientCallback]
    public void ClientGetUpdatePlayerPoints(int oldVal, int newVal)
    {
        points = newVal;
        if(oldVal < newVal)
        {
            SoundManager.Instance.PlaySound(GameSound.GotCandy);
        }
        OnUpdatePoints.Invoke(this, points);
    }

    [ServerCallback]
    public void ServerSetAdditionSpeed(float additionSpeed, float effectTime)
    {
        // if player still in effect of other we will need to check
        if(this.effectTime > 0)
        {
            // if user was got stunned
            if(this.additionMoveSpeed <= float.Epsilon)
            {
                // and got another stun then we extend the time
                if(additionSpeed <= float.Epsilon)
                {
                    this.effectTime += effectTime;
                }
                else
                {
                    // otherwise we just extend 0.5 sec
                    this.effectTime += 0.5f;
                }
            }else if(this.additionMoveSpeed < 1) {
                // if user was got slowed
                // and got another stun then we stun user and extend 0.5f
                if (additionSpeed <= float.Epsilon)
                {
                    this.additionMoveSpeed = additionSpeed;
                    this.effectTime = effectTime + 0.5f;
                }
                else
                {
                    // otherwise we just extend 1 sec
                    this.effectTime += 1f;
                }
            }
            else
            {
                //weird case just reset effect time
                this.effectTime = 0;
            }
        }
        else
        {
            this.additionMoveSpeed = additionSpeed;
            this.effectTime = effectTime;
        }
    }

    [ClientRpc]
    public void RpcPlayHitFX()
    {
        if(hitFX != null)
        {
            var hitObj = Instantiate(hitFX);
            hitObj.transform.parent = this.transform;
            hitObj.transform.localPosition = new Vector3(0, 0.1f, -0.12f);
            hitObj.transform.localScale = new Vector3(1, 1, 1);
        }
    }

    [ServerCallback]
    private void Update()
    {
        if(effectTime > 0 && additionMoveSpeed < 1)
        {
            effectTime -= Time.deltaTime;
            // reset when it done
            if(effectTime <= float.Epsilon)
            {
                effectTime = 0;
                additionMoveSpeed = 1;
            }
        }
    }

    // call back for other object to ref
    [ClientCallback]
    public void ClientGetAdditionSpeed(float oldVal, float newVal)
    {
        additionMoveSpeed = newVal;
        if (newVal < 1)
        {
            animator.SetTrigger("GotHit");
            SoundManager.Instance.PlaySound(GameSound.Hit);
        }
    }

    public void Initialize()
    {
        animator.SetBool("IsPlayerA", slot == "A");
    }


    [Command]
    public void CmdUpdateMovementState(float movement)
    {
        this.movement = movement;
        RpcReceiveMovement(movement);
    }

    [ClientRpc]
    public void RpcReceiveMovement(float movement)
    {
        this.movement = movement;
    }

    void FixedUpdate()
    {
        if (_sharedData != null && _sharedData.state != GameState.STARTED) return;
        // only let the local player control.
        if (isLocalPlayer)
        {
            movement = Input.GetAxisRaw("Horizontal");
            rigidbody2d.velocity = new Vector2(movement, 0) * (moveSpeed * additionMoveSpeed) * Time.fixedDeltaTime;

            if(oldMovement != movement)
            {
                PlayerNetworkControl player = NetworkClient.connection.identity.GetComponent<PlayerNetworkControl>();
                player.CmdUpdateMovementState(movement);
                oldMovement = movement;
            }
            
            // update for other player
            if (rigidbody2d.velocity.x != 0)
            {
                spriteRenderer.flipX = rigidbody2d.velocity.x < 0;
            }
            animator.SetFloat("Movement", Mathf.Abs(rigidbody2d.velocity.x));
        } else {
            //Debug.Log("Other player Movement: " + movement);
            spriteRenderer.flipX = movement < 0;
            animator.SetFloat("Movement", Mathf.Abs(movement));
        }
    }

    public void ResetHitAnimation()
    {
        animator.ResetTrigger("GotHit");
    }
}
