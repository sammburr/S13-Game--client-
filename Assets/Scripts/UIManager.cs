using RiptideNetworking;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private static UIManager _singleton;

    public static UIManager Singleton
    {

        get => _singleton;
        private set
        {

            if (_singleton == null)
                _singleton = value;
            else if (_singleton != value)
            {
                Debug.LogWarning($"{nameof(UIManager)} instance already exsits, deleting the duplicate...");
                Destroy(value);
            }

        }

    }

    [Header("Connect")]
    [SerializeField] private GameObject connectUI;
    [SerializeField] private GameObject usernameField;
    [Header("HUD")]
    [SerializeField] private GameObject hudUI;
    [SerializeField] private GameObject[] images; 

    private void Awake()
    {

        Singleton = this;

    }

    public void ConnectButtonClicked()
    {

        Cursor.lockState = CursorLockMode.Locked;

        usernameField.GetComponent<TMP_InputField>().interactable = false;
        connectUI.SetActive(false);
        hudUI.SetActive(true);

        NetworkManager.Singleton.Connect();

    }

    public void BackToMain()
    {

        Cursor.lockState = CursorLockMode.None;

        usernameField.GetComponent<TMP_InputField>().interactable = true;
        hudUI.SetActive(false);
        connectUI.SetActive(true);


    }

    public void SendName()
    {

        Message message = Message.Create(MessageSendMode.reliable, (ushort)ClientToServerId.clientHandshake);
        message.AddString(usernameField.GetComponent<TMP_InputField>().text);

        NetworkManager.Singleton.client.Send(message);

    }

    public void UpdateHUD()
    {

        foreach (GameObject image in images)
        {

            image.GetComponent<Image>().color = new Color(1.0f, 1.0f, 1.0f);

        }

        images[Inventory.inventory[0] - 1].GetComponent<Image>().color = new Color(1.0f, 0.0f, 0.0f);

    }

}
