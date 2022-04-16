using RiptideNetworking;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class Player : MonoBehaviour
{

    public static Dictionary<ushort, Player> list = new Dictionary<ushort, Player>();

    private ClientInputActions clientControlScheme;

    private InputAction playerMovementInput;
    private InputAction playerLookInput;

    public ushort Id { get; private set; }
    public bool IsLocalPlayer { get; private set; }
    public GameObject model;

    public TextMeshProUGUI nameTag;

    private string username;


    #region ClientInputSettings

    //Client side input settings
    public GameObject currentCamera;
    public CharacterController characterController;
    public GameObject groundCheck;

    public float mouseSensitivity = 100f;
    public float characterSpeed = 10f;
    public float gravity = -9.81f;
    public float jumpHeight = 3f;

    public float groundDist = 0.4f;
    public LayerMask groundMask;

    public GameObject itemSocket;

    float currentCameraXRotation = 0f;

    #endregion

    private void OnDestroy()
    {

        list.Remove(Id);
        playerMovementInput.Disable();
        playerLookInput.Disable();

    }

    public static void Spawn(ushort id, string username, Vector3 position)
    {

        Player player;
        if (id == NetworkManager.Singleton.client.Id)
        {

            player = Instantiate(GameLogic.Singleton.LocalPlayerPrefab, position, Quaternion.identity).GetComponent<Player>();
            player.IsLocalPlayer = true;

            player.clientControlScheme = new ClientInputActions();
            player.playerMovementInput = player.clientControlScheme.Player.Move;
            player.playerMovementInput.Enable();
            player.playerLookInput = player.clientControlScheme.Player.Look;
            player.playerLookInput.Enable();
        }
        else
        {

            player = Instantiate(GameLogic.Singleton.PlayerPrefab, position, Quaternion.identity).GetComponent<Player>();
            player.IsLocalPlayer = false;

        }

        player.name = $"Player {id} ({(string.IsNullOrEmpty(username) ? "Guest" : username)})";
        player.Id = id;
        player.username = string.IsNullOrEmpty(username) ? $"Guest {id}" : username;

        if (player.nameTag != null)
            player.nameTag.text = username;

        list.Add(id, player);        

    }

    //h  v space
    private float[] inputMap = { 0, 0, 0};
    //x  y
    private float[] mouseMap = { 0, 0 };

    private Vector3 velocity;
    private bool isGrounded;

    Dictionary<uint, float[]> inputMapHistory = new Dictionary<uint, float[]>();


    public void processInput() // Func only ran for the local player by NetworkManager every client tick.
    {

        #region InputMap

        Keyboard keyboard = Keyboard.current;

        //Need to fill out input map for keys

        inputMap[0] = playerMovementInput.ReadValue<Vector2>()[0];
        inputMap[1] = playerMovementInput.ReadValue<Vector2>()[1]; 
        if (keyboard.spaceKey.isPressed)
            inputMap[2] = 1.0f;
        else
            inputMap[2] = 0.0f;

        //Need to fill out input map for mouse
        mouseMap[0] = currentCamera.transform.localEulerAngles.x; // the camera stores x mouse rotation.
        mouseMap[1] = transform.localEulerAngles.y; // the root object stores y mouse rotation.

        #endregion

        #region ClientSideInputProcessing

        //Need to process input on client side

        #region Mouse

        float mouseX = playerLookInput.ReadValue<Vector2>()[0] * mouseSensitivity * Time.fixedDeltaTime;
        float mouseY = playerLookInput.ReadValue<Vector2>()[1] * mouseSensitivity * Time.fixedDeltaTime;

        currentCameraXRotation -= mouseY;
        currentCameraXRotation = Mathf.Clamp(currentCameraXRotation, -65f, 65f); // Clamps between -90deg and 90deg
        currentCamera.transform.localRotation = Quaternion.Euler(currentCameraXRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);

        #endregion

        #region KeyBoard

        //check if on floor
        isGrounded = Physics.CheckSphere(groundCheck.transform.position, groundDist, groundMask);
        Vector3 move = transform.right * inputMap[0] + transform.forward * inputMap[1];

        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;


        characterController.Move(move * characterSpeed * Time.fixedDeltaTime);

        //jumping
        if (inputMap[2] == 1.0f && isGrounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);


        //gravity 
        velocity.y += gravity * Time.fixedDeltaTime;

        characterController.Move(velocity * Time.fixedDeltaTime);

        #endregion

        #endregion

        #region Messages

        //Need to send input map to server

        Message message = Message.Create(MessageSendMode.unreliable, (ushort)ClientToServerId.inputMap);

        message.AddUInt(NetworkManager.Singleton.LocalTick); //PREFACE WITH LOCAL TICK, (this *should* be ahead of the server).
        message.AddFloats(inputMap); //KEYBOARD FIRST
        message.AddFloats(mouseMap); //ROTATION INFORMATION SECOND

        NetworkManager.Singleton.client.Send(message);

        #endregion


        //  \/ \/ CURRENTLY NOT BEING USED, IT SHOULD BE FOR SMOOTHER CLIENT CATCH UP \/ \/

        //Need to keep input history

        inputMapHistory.Add(NetworkManager.Singleton.LocalTick, inputMap);

        //Need to recieve server data
        //Need to compare and fix client data
        //Need to set server data for other players

        //  /\ /\ CURRENTLY NOT BEING USED, IT SHOULD BE FOR SMOOTHER CLIENT CATCH UP /\ /\

    }

    #region Messages

    [MessageHandler((ushort)ServerToClientId.playerSpawned)]
    private static void SpawnPlayer(Message message)
    {

        Spawn(message.GetUShort(), message.GetString(), message.GetVector3());

    }

    [MessageHandler((ushort)ServerToClientId.playerPosition)]
    private static void GetTransforms(Message message)
    {

        

        foreach (ushort _ in list.Keys)
        {
            ushort playerId = message.GetUShort();
            Vector3 pos = message.GetVector3();

            Player player = list[playerId];
            if (player.IsLocalPlayer)
            {
                player.GetComponent<CharacterController>().enabled = false;

                float advOffset = ((Mathf.Abs(player.transform.position.x - pos.x)) + (Mathf.Abs(player.transform.position.y - pos.y)) + (Mathf.Abs(player.transform.position.z - pos.z)))/ 3.0f;

                if (advOffset < 0.01f)
                    player.transform.position = pos;
                else
                    player.transform.position = Vector3.Lerp(player.transform.position, pos, 5.0f * advOffset * Time.fixedDeltaTime); //lerp for big movments
                player.GetComponent<CharacterController>().enabled = true;
            }
            else
            {

                player.transform.position = pos;

            }

        }

    }


    [MessageHandler((ushort)ServerToClientId.playerHeads)]
    private static void GetHeads(Message message)
    {


        foreach(ushort _ in list.Keys)
        {

            ushort playerId = message.GetUShort();
            float rotX = message.GetFloat();
            float rotY = message.GetFloat();

            Player player = list[playerId];
            if (!player.IsLocalPlayer)
            {

                player.currentCamera.transform.localEulerAngles = new Vector3(rotX, 0.0f, 0.0f);
                player.transform.localEulerAngles = new Vector3(0.0f, rotY, 0.0f);

            }

        }

    }

    [MessageHandler((ushort)ServerToClientId.playerMovmentSettings)]
    private static void SetPlayerMovmentSettings(Message message)
    {


        float charSpeed, grav, jumpH, grndDist;
        charSpeed = message.GetFloat();
        grav = message.GetFloat();
        //jumpH = message.GetFloat();
        //grndDist = message.GetFloat();


        foreach (Player player in list.Values)
        {

            player.characterSpeed = charSpeed;

            player.gravity = grav;

            //player.jumpHeight = jumpH;

            //player.groundDist = grndDist;

        }

    }

    #endregion

}