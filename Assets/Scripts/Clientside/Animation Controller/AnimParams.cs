using RiptideNetworking;
using RiptideNetworking.Utils;
using System;
using UnityEngine;

public class AnimParams : MonoBehaviour
{

    [MessageHandler((ushort)ServerToClientId.animFlags)]
    private static void SetFlags(Message message)
    {

        foreach(ushort playerId in Player.list.Keys)
        {

            Player player = Player.list[message.GetUShort()];

            if (player.IsLocalPlayer)
            {
                message.GetBool();
                message.GetBool();
            }
            else
            {
                Animator animator = player.model.GetComponent<Animator>();
                animator.SetBool("IsWalking", message.GetBool());
                animator.SetBool("IsJumping", message.GetBool());
            }

        }

    }

}
