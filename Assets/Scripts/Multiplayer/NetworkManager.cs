using RiptideNetworking;
using RiptideNetworking.Utils;
using System;
using UnityEngine;


public enum ServerToClientId : ushort
{

    heartbeat = 1,
    serverHandshake = 2,
    playerSpawned = 3,
    playerPosition = 4,
    playerHeads = 5,
    animFlags = 6,
    playerMovmentSettings = 7,
    serverInventory = 8,
    allServerInventory = 9

}


public enum ClientToServerId : ushort
{

    clientHandshake = 1,
    inputMap = 2,
    inventoryMap = 3

}


public class NetworkManager : MonoBehaviour
{
    private static NetworkManager _singleton;

    private uint localTick;  // Where are we currently
    private uint serverTick; // Where is the server currently

    public uint LocalTick { get { return localTick; } }
    public uint ServerTick { get { return serverTick; } }

    // Use these to adjust client side prediction...

    private uint warpTicks = 0; // Where we store how many ticks we need to warp through to get back up to speed.
    private uint skipTicks = 0; // Where we store how many ticks we need to skip to slow down to server speed.

    [Header("Sync And Prediction Params")]
    [SerializeField] private uint initTickOffset;

    [Header("Server Data")]
    [SerializeField] private string ip;
    [SerializeField] private ushort port;

    public static NetworkManager Singleton
    {

        get => _singleton;
        private set
        {

            if (_singleton == null)
                _singleton = value;
            else if (_singleton != value)
            {
                Debug.LogWarning($"{nameof(NetworkManager)} instance already exsits, deleting the duplicate...");
                Destroy(value);
            }

        }

    }

    public Client client { get; private set; }

    private void Awake()
    {

        Singleton = this;

    }

    private void Start()
    {
        Application.targetFrameRate = 60;

        RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);

        client = new Client();
        client.Connected += DidConnect;
        client.ConnectionFailed += FailedToConnect;
        client.ClientDisconnected += PlayerLeft;
        client.Disconnected += DidDisconnect;

    }

    #region ProcessingLoops

    private void FixedUpdate()
    {

        client.Tick(); // Riptide handles the server events

        #region TickManagment

        // Warp and skip ticks acordingly so that client is ahead of the server.
        // We use warpTicks and skipTicks to run multiple ticks at the same time and miss out ticks respectively

        if (Singleton.warpTicks > 0)
        {

            Debug.LogWarning($"Warping {Singleton.warpTicks} Ticks");

            for (uint i=0; i < Singleton.warpTicks; i++)
            {

                Singleton.Tick();

            }

            Singleton.warpTicks = 0;

        }
        else if (Singleton.skipTicks > 0)
        {

            Debug.LogWarning($"Skipping {Singleton.skipTicks} Ticks");

            skipTicks -= 1;

        }
        else
        {

            Singleton.Tick();

        }

        #endregion

    }


    private void Tick()
    {

        Singleton.localTick++; // increment tick

        // Handle and send input, (keep input history)

        foreach(Player player in Player.list.Values)
        {

            if (player.IsLocalPlayer)
                player.processInput(); // Check each input for the local player
                                       // This function also sends the input and performs the necessary client prediction

        }

        // Need history struct
        // Need input handle method in player
        // Need client prediction comparasion to server transforms

    }

    #endregion

    #region ServerEvents

    private void OnApplicationQuit()
    {

        client.Disconnect();

    }

    public void Connect()
    {

        client.Connect($"{ip}:{port}");

    }

    private void DidConnect(object sender, EventArgs e)
    {

        UIManager.Singleton.SendName();

    }

    private void FailedToConnect(object sender, EventArgs e)
    {

        UIManager.Singleton.BackToMain();

    }

    private void PlayerLeft(object sender, ClientDisconnectedEventArgs e)
    {

        Destroy(Player.list[e.Id].gameObject);

    }

    private void DidDisconnect(object sender, EventArgs e)
    {

        foreach (Player player in Player.list.Values)
            Destroy(player.gameObject);

        UIManager.Singleton.BackToMain();

    }

    #endregion

    #region Messages

    //Will only run on connect to server after we send our handshake message
    [MessageHandler((ushort)ServerToClientId.serverHandshake)] 
    private static void HandShake(Message message)
    {

        Singleton.serverTick = Singleton.localTick = message.GetUInt();

        Singleton.localTick += Singleton.initTickOffset; // Add the inital offset amount

    }

    [MessageHandler((ushort)ServerToClientId.heartbeat)]
    private static void HeartBeat(Message message)
    {

        Singleton.serverTick = message.GetUInt();

        //Need to calculate how far ahead we should be.
        //Compare server tick to current client tick....

        if (Singleton.localTick > Singleton.serverTick + 5) // is greater than +2 ticks: need to wait (tickdiff - 2) ticks
        {

            Singleton.skipTicks = (Singleton.localTick - Singleton.serverTick) - 5;

        }
        else if (Singleton.localTick < Singleton.serverTick - 5) // is less than -2 ticks: need to warp (tickdiff + 2) ticks
        {

            Singleton.warpTicks = (Singleton.serverTick - Singleton.localTick) + 5;

        }
        
        // Overall this should keep the client about +2 ~ +3 ticks ahead of the server.
        // Need to test on clients not on the same network...

    }

    #endregion

}
