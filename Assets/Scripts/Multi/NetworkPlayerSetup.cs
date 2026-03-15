using UnityEngine;
using Photon.Pun;
using Cinemachine;
using StarterAssets;

public class NetworkPlayerSetup : MonoBehaviourPun
{
    void Start()
    {
        PhotonView pv = GetComponent<PhotonView>();

        bool isLocalPlayer = pv == null || pv.ViewID == 0 || pv.IsMine;

        if (isLocalPlayer)
            SetupLocalPlayer();
        else
            SetupRemotePlayer();
    }

    void SetupLocalPlayer()
    {
        var cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = true;

        var controller = GetComponent<ThirdPersonController>();
        if (controller != null) controller.enabled = true;

        // Retarget camera
        CinemachineVirtualCamera vCam = FindObjectOfType<CinemachineVirtualCamera>();
        if (vCam != null)
        {
            Transform camTarget = transform.Find("PlayerCameraRoot");
            if (camTarget != null)
            {
                vCam.Follow = camTarget;
                vCam.LookAt = camTarget;
            }
        }

        gameObject.tag = "Player";
        Debug.Log("SetupLocalPlayer complete");
    }

    void SetupRemotePlayer()
    {
        // This one disable is enough — nothing reads input without it
        var controller = GetComponent<ThirdPersonController>();
        if (controller != null) controller.enabled = false;

        // Still disable CharacterController so it doesn't fight Photon's position sync
        var cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        gameObject.tag = "Player";
    }
}