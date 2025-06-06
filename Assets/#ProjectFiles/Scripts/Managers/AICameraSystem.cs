using UnityEngine;
using Cinemachine;
using System.Collections.Generic;

public class AICameraSystem : MonoBehaviour
{
    public static AICameraSystem Instance;
    [SerializeField] CinemachineVirtualCamera[] virtualCameras;
    [SerializeField] CharacterMovement[] allPlayers;

    GameObject followedPlayer;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        InvokeRepeating(nameof(ShuffleFocusChar), 0.75f, Random.Range(5, 10f));
    }

    void Update()
    {
        if (followedPlayer != null)
        {
            if (!followedPlayer.activeSelf) ShuffleFocusChar();
        }   
    }

    // Update is called once per frame
    void ShuffleFocusChar()
    {
        if (NetcodeManager.Instance) return;
        
        if (GameManager.Instance.playersForTeamOne <= 0 || GameManager.Instance.playersForTeamTwo <= 0)
        {
            foreach (var vcam in virtualCameras)
            {
                vcam.m_Priority = 10;
            }

            allPlayers = FindObjectsOfType<CharacterMovement>();

            if (allPlayers.Length > 0)
            {
                CinemachineVirtualCamera virtualCam = virtualCameras[Random.Range(0, virtualCameras.Length)];

                List<CharacterMovement> activePlayers = new();

                foreach (var player in allPlayers)
                {
                    if (player.movementInput.magnitude > 0.1 
                        && player.gameObject.activeSelf)
                    {
                        activePlayers.Add(player);
                    }
                }

                if (activePlayers.Count != 0)
                {
                    virtualCam.m_Follow = activePlayers[Random.Range(0, activePlayers.Count)].transform;

                    virtualCam.m_Lens.FieldOfView = Random.Range(80, 95);
                    virtualCam.m_Priority = 15;
                }
            }
        }
    }
}
