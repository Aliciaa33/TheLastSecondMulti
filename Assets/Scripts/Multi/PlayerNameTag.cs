using UnityEngine;
using TMPro;
using Photon.Pun;

public class PlayerNameTag : MonoBehaviourPun
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private GameObject nameTagCanvas;

    private Transform mainCameraTransform;

    void Start()
    {
        mainCameraTransform = Camera.main.transform;

        // Get the player's name from Photon
        string playerName = photonView.Owner != null
            ? photonView.Owner.NickName
            : ConnectToServer.Instance.GetPlayerName();

        if (nameText != null)
            nameText.text = playerName;

        // Hide name tag for local player — you don't need to see your own name
        if (photonView.IsMine || photonView.ViewID == 0)
            nameTagCanvas.SetActive(false);
    }

    void LateUpdate()
    {
        // Make the name tag always face the camera (billboard effect)
        if (mainCameraTransform != null && nameTagCanvas.activeSelf)
        {
            nameTagCanvas.transform.LookAt(
                nameTagCanvas.transform.position + mainCameraTransform.forward
            );
        }
    }
}