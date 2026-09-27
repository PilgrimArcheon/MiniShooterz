using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterMovement))]
[RequireComponent(typeof(CharacterShooter))]
[RequireComponent(typeof(HealthSystem))]
public class PlayerCharacterController : MonoBehaviour, ICombat, IStates
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

    void OnEnable()
    {
        SetState(States.Base);
        movementInput = Vector3.zero;

        virtualCam.transform.SetParent(null);
    }

    bool hasSetUpVal;
    public void CharacterUserSetUp(string userId, int _charId, int team, int _id)
    {
        // virtualCam.SetActive(IsOwner);

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

        hasSetUpVal = true;

        audioListener = GameObject.Find("AudioListener").GetComponent<AudioListener>();
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

    private void Update()
    {
        playerInputHandler.enabled = true;

        if (GameManager.Instance.isGameOver)
        {
            movementInput = Vector3.zero;
            characterMovement.SetMovementInput(movementInput);
            return;
        }

        HandleMovementInput();
        HandleRotationState(); // <-- Added to control where the body looks while aiming
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

    // --- NEW METHOD ---
    private void HandleRotationState()
    {
        // If taking damage, let the animation handle it
        if (currentState == States.TakingDamage) return;

        bool isAiming = aimingController.IsAiming();

        if (isAiming)
        {
            // We are aiming! Lock the body rotation away from the movement direction
            characterMovement.SetCanRotate(false);

            // Tell the character to specifically look at the aiming crosshair
            Vector3 aimDir = (aimingController.aimFollow.position - transform.position).normalized;
            characterMovement.SetAimInput(new Vector2(aimDir.x, aimDir.z));
        }
        else if (currentState == States.Base)
        {
            // We released aim and aren't shooting, go back to normal walk direction
            characterMovement.SetCanRotate(true);
        }
    }

    private void HandleAnimations()
    {
        bool hasTarget = aimingController.IsAiming();
        animator.SetBool("hasTarget", hasTarget);

        animator.SetBool("move", movementInput.magnitude > 0f);
        animator.SetFloat("movement", movementInput.magnitude > 0f ? 1f : 0f);

        // Convert world input to local space relative to the character's rotation
        Vector3 worldMoveDir = new Vector3(movementInput.x, 0f, movementInput.y);
        Vector3 localMoveDir = transform.InverseTransformDirection(worldMoveDir);

        // Pass the local X (left/right) and local Z (forward/back) to the animator
        animator.SetFloat("moveX", hasTarget ? localMoveDir.x : 0f);
        animator.SetFloat("moveY", hasTarget ? localMoveDir.z : 0f);
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

    public void HandleAbility(Vector3 aimDir)
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
            characterShooter.TryUseAbility();
        }
    }

    #region Implemented Interface
    public void PerformShoot(float shootTime)
    {
        animator.SetLayerWeight(1, 1);
        animator.Play("Shoot", 1, 0f);
        StartCoroutine(SwitchStateDelay(States.Base, shootTime));
    }

    public void PerformAbility()
    {
        animator.SetLayerWeight(1, 1);
        animator.Play("Ability", 1, 0f);
        StartCoroutine(SwitchStateDelay(States.Base, 2f));
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

        // Let HandleRotationState manage CanRotate instead of doing it blindly here
        if (!aimingController.IsAiming())
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