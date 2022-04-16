using RiptideNetworking;
using UnityEngine;
using UnityEngine.InputSystem;


public class Inventory : MonoBehaviour
{

    public GameObject viewModelSocket;
    public GameObject weaponSocket;
    public static GameObject viewModelSocketStatic;

    public static ushort[] inventory = new ushort[9];
    // [ 0 1 2 3 4 5 6 7 8 9 ]
    //   A W S - - - - - - -

    private ClientInputActions clientControlScheme;

    private InputAction inv1;
    private InputAction inv2;
    private InputAction inv3;
    private InputAction inv4;
    private InputAction inv5;
    private InputAction inv6;
    private InputAction inv7;
    private InputAction inv8;
    private InputAction inv9;

    void Start()
    {

        viewModelSocketStatic = viewModelSocket;

        // starting inventory values
        inventory[0] = 1;
        inventory[1] = 1;
        inventory[2] = 2;
        inventory[3] = 0;
        inventory[4] = 0;
        inventory[5] = 0;
        inventory[6] = 0;
        inventory[7] = 0;
        inventory[8] = 0;

        clientControlScheme = new ClientInputActions();

        inv1 = clientControlScheme.Inventory.Inventory1;
        inv2 = clientControlScheme.Inventory.Inventory2;

        /*
        inv3 = clientControlScheme.Inventory.Inventory3;
        inv4 = clientControlScheme.Inventory.Inventory4;
        inv5 = clientControlScheme.Inventory.Inventory5;
        inv6 = clientControlScheme.Inventory.Inventory6;
        inv7 = clientControlScheme.Inventory.Inventory7;
        inv8 = clientControlScheme.Inventory.Inventory8;
        inv9 = clientControlScheme.Inventory.Inventory9;
        */

        inv1.Enable();
        inv2.Enable();

        inv1.performed += Inv1;
        inv2.performed += Inv2;

        UpdateViewModel();

    }

    private void OnDestroy()
    {

        inv1.Disable();
        inv2.Disable();

    }


    private void Inv1(InputAction.CallbackContext context)
    {

        // Make Weapon Slot 1 (Primary) the active weapon
        // [ 1, primary ID, secondary ID, ... ]
        inventory[0] = 1;

        //update viewmodel
        UpdateViewModel();

        //update UI
        UIManager.Singleton.UpdateHUD();

        //Send input to server
        SendInventoryInput(1);

    }


    private void Inv2(InputAction.CallbackContext context)
    {

        // Make Weapon Slot 2 (Secondary) the active weapon
        // [ 2, primary ID, secondary ID, ... ]
        inventory[0] = 2;

        UpdateViewModel();

        UIManager.Singleton.UpdateHUD();


        SendInventoryInput(2);

    }

    public static void UpdateViewModel()
    {

        //query inventory array
        ushort item = inventory[inventory[0]];
        //grab prefab from itemlist class
        GameObject itemPrefab = ItemList.items[item];
        //destory current

        for (int i = 0; i < viewModelSocketStatic.transform.childCount; i++)
        {

            GameObject.Destroy(viewModelSocketStatic.transform.GetChild(i).gameObject);

        }

        //instance new as child of correct bone
        GameObject itemInstace = Instantiate(itemPrefab, new Vector3(), new Quaternion(), viewModelSocketStatic.transform);
        //apply transformations
        itemInstace.transform.localScale = ItemList.itemViewModelScale[item];
        itemInstace.transform.localPosition = ItemList.itemViewModelPos[item];
        itemInstace.transform.localRotation = ItemList.itemViewModelRot[item];


    }

    public static void SendInventoryInput(ushort input)
    {

        // Send inventory array to server.

        Message message = Message.Create(MessageSendMode.reliable, (ushort)ClientToServerId.inventoryMap);

        message.AddUShort(input);

        NetworkManager.Singleton.client.Send(message);

    }

    [MessageHandler((ushort)ServerToClientId.serverInventory)]
    public static void VerifiyInventory(Message message)
    {

        // Compare and change if needed.
        ushort[] serverInventory = message.GetUShorts();

        // If [0] is diff we need to update the viewmodel

        if (serverInventory[0] != inventory[0])
        {

            Debug.LogWarning("Got different active item from server...");

            inventory[0] = serverInventory[0];

            UpdateViewModel();

        }


        inventory = serverInventory;

    }

    [MessageHandler((ushort)ServerToClientId.allServerInventory)]
    public static void ReciveAllInventory(Message message)
    {

        foreach(ushort _ in Player.list.Keys)
        {

            ushort playerId = message.GetUShort();
            ushort[] inv = message.GetUShorts();

            Player player = Player.list[playerId];

            if (!player.IsLocalPlayer)
            {

                // Interpret inventory

                for (int i = 0; i < player.itemSocket.transform.childCount; i++)
                {

                    GameObject.Destroy(player.itemSocket.transform.GetChild(i).gameObject);

                }

                Instantiate(ItemList.items[inv[inv[0]]], player.itemSocket.transform);

            }

        }

    }

}
