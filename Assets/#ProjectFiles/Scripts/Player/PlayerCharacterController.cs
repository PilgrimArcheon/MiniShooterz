using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterMovement))]
[RequireComponent(typeof(CharacterShooter))]
[RequireComponent(typeof(HealthSystem))]
public class PlayerCharacterController : NetworkBehaviour, ICombat, IStates
{
    [Header("Character Settings")]
    [SerializeField] CharacterSetUp[] characterSetUp;
    [SerializeField] GameObject virtualCam;
    [SerializeField] GameObject[] teamId;
    PlayerInputHandler playerInputHandler;
    private CharacterMovement characterMovement;
    private CharacterShooter characterShooter;
    private AimingController aimingController;
    private HealthSystem healthSystem;
    private HUDControl hUDControl;
    private Animator animator;
    private Vector2 movementInput;
    private AudioListener audioListener;

    public string playerId;

    private CharacterVariables characterVar = new();
    States currentState;

    public int charId;
    public int playerTeam;  // Player team
    public int id; //User Id

    private void Awake()
    {
        characterMovement = GetComponent<CharacterMovement>();
        characterShooter = GetComponent<CharacterShooter>();
        healthSystem = GetComponent<HealthSystem>();
        hUDControl = GetComponent<HUDControl>();
        aimingController = GetComponent<AimingController>();
        animator = GetComponentInChildren<Animator>();

        playerInputHandler = GameObject.Find("PlayerInputHandler").GetComponent<PlayerInputHandler>();
        playerInputHandler.SetInputController();
    }

    public override void OnNetworkSpawn()
    {
        InvokeRepeating(nameof(UpdateCharVar), 0.15f, 2.5f);

        Debug.Log("Spawned Character " + OwnerClientId);
    }

    void OnEnable()
    {
        SetState(States.Base);
        movementInput = Vector3.zero;

        virtualCam.transform.SetParent(null);
    }

    bool hasSetUpVal;
    public void CharacterUserSetUp(string userId, int _charId, int team, int _id)
    {
        virtualCam.SetActive(IsOwner);

        if (!IsOwner) playerInputHandler = null;

        charId = _charId;
        playerId = userId;
        id = _id;

        gameObject.name = playerId;

        if (hasSetUpVal) return;

        SetTeams(team);
        SetUpCharacter(_charId);

        if (GameManager.Instance.gameStarted) Destroy(gameObject);

        hUDControl.SetUpHUD(userId, team);

        PlayerDetails playerDetails = new()
        {
            PlayerCharId = charId,
            PlayerName = userId,
            PlayerTeam = team,
            PlayerId = id
        };
        GameManager.Instance.AddToDetails(playerDetails);

        characterVar = new() { username = userId, charId = _charId, team = team, _id = _id };
        hasSetUpVal = true;

        if (IsOwner)
        {
            audioListener = GameObject.Find("AudioListener").GetComponent<AudioListener>();
        }
    }

    void SetUpCharacter(int charId)
    {
        foreach (var charSetup in characterSetUp)
        {
            charSetup.ActivateCharacter(false);
        }
        characterSetUp[charId].ActivateCharacter(true);
    }

    void SetTeams(int team)
    {
        playerTeam = team;

        int teamLook = playerTeam == GameManager.Instance.GetMyTeam ? 0 : 1;

        string teamLayer = playerTeam == GameManager.Instance.GetMyTeam ? "Team" : "Opp";
        string oppLayer = playerTeam == GameManager.Instance.GetMyTeam ? "Opp" : "Team";

        gameObject.layer = LayerMask.NameToLayer(teamLayer);

        teamId[teamLook].SetActive(true);

        healthSystem.SetUpHealth(id, playerTeam); // Set player's team in Health system    
        characterShooter.SetCharacterShooter(characterShooter.currentWeaponId, id, playerTeam); // Set player's weapon, Id && team in Shooter

        aimingController.oppLayer = LayerMask.GetMask(oppLayer); // Set player's Opp team
    }

    void UpdateCharVar()
    {
        if (IsServer)
        {
            UpdateCharVarClientRPC(characterVar.username, characterVar.charId, characterVar.team, characterVar._id);
        }
    }

    [Rpc(SendTo.Everyone)]
    void UpdateCharVarClientRPC(string username, int charId, int team, int _id)
    {
        CharacterUserSetUp(username, charId, team, _id);
    }

    private void Update()
    {
        if (!IsOwner) return;

        playerInputHandler.enabled = true;

        if (GameManager.Instance.isGameOver)
        {
            movementInput = Vector3.zero;
            characterMovement.SetMovementInput(movementInput);
            return;
        }

        HandleMovementInput();
        HandleAnimations();
        audioListener.transform.position = transform.position;
    }

    // Handle player movement using WASD 
    private void HandleMovementInput()
    {
        Vector2 moveVector = playerInputHandler.MovementInput;
        movementInput = new Vector2(moveVector.x, moveVector.y);
        characterMovement.SetMovementInput(movementInput);
    }

    private void HandleAnimations()
    {
        animator.SetBool("move", movementInput.magnitude > 0f);
        animator.SetFloat("weaponId", characterShooter.currentWeaponId);
    }

    public void HandleShooting(Vector3 aimDir)
    {
        States[] statesToCheck = new States[] { States.Shoot, States.TakingDamage };

        if (!CheckCurrentState(statesToCheck) && characterShooter.CanShoot)
        {
            SetState(States.Shoot);
            Vector3 shootDirection = (aimDir - transform.position).normalized;
            shootDirection.y = 0;
            transform.LookAt(aimDir);
            characterMovement.SetCanRotate(false);
            characterMovement.SetAimInput(new Vector2(shootDirection.x, shootDirection.z));
            characterShooter.TryShoot();
        }
    }

    #region Implemented Interface
    public void PerformShoot(float shootTime)
    {
        animator.SetLayerWeight(1, 1);
        animator.Play("Shoot", 1, 0f);
        StartCoroutine(SwitchStateDelay(States.Base, shootTime));
    }

    public void SetState(States state)
    {
        currentState = state;
    }

    public States GetStates()
    {
        return currentState;
    }

    public void TakeDamage()
    {
        if (healthSystem.currentHealth > 0)
        {
            SetState(States.TakingDamage);
            animator.Play("TakingDamage", 0, 0.25f);

            StartCoroutine(SwitchStateDelay(States.Base, 0.5f));
        }

        if (healthSystem.currentHealth <= 0)
        {
            SetState(States.TakingDamage);
            animator.Play("Die", 0, 0.25f);

            StartCoroutine(SwitchStateDelay(States.Base, 1.5f));
        }
    }

    public bool CheckCurrentState(States[] states)
    {
        foreach (States state in states)
        {
            if (currentState == state)
            {
                return true;
            }
        }
        return false;
    }

    private IEnumerator SwitchStateDelay(States state, float waitTime)
    {
        yield return new WaitForSeconds(waitTime + 0.5f);
        characterMovement.SetCanRotate(true);
        animator.SetLayerWeight(1, 0);
        SetState(state);
    }
    #endregion
}

[System.Serializable]
public class CharacterSetUp
{
    public GameObject[] BodyParts;

    public void ActivateCharacter(bool show)
    {
        foreach (var part in BodyParts)
        {
            part.SetActive(show);
        }
    }
}