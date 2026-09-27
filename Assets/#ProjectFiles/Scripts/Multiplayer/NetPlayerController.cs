using UnityEngine;
using System.Collections;
using ArcNet;

[RequireComponent(typeof(ArcNetView))]
[RequireComponent(typeof(ArcNetTransformView))] // Syncs position/rotation automatically
[RequireComponent(typeof(CharacterMovement))]
[RequireComponent(typeof(CharacterShooter))]
public class NetPlayerController : ArcNetBehaviour, ICombat, IStates
{
    [Header("Character Settings")]
    [SerializeField] CharacterSetUp[] characterSetUp;
    [SerializeField] GameObject virtualCam;
    
    private CharacterMovement characterMovement;
    private CharacterShooter characterShooter;
    private Animator animator;
    private PlayerInputHandler playerInputHandler;
    
    private Vector2 movementInput;
    private States currentState;

    public int charId;
    public int playerTeam; 
    public int id; // ActorNumber

    private void Awake()
    {
        characterMovement = GetComponent<CharacterMovement>();
        characterShooter = GetComponent<CharacterShooter>();
        animator = GetComponentInChildren<Animator>();
        
        // Ensure InputHandler exists
        var inputObj = GameObject.Find("PlayerInputHandler");
        if(inputObj != null) playerInputHandler = inputObj.GetComponent<PlayerInputHandler>();
    }

    public void CharacterUserSetUp(string userId, int _charId, int team, int _id)
    {
        charId = _charId;
        playerTeam = team;
        id = _id;
        gameObject.name = $"Player_{userId}_{_id}";

        SetUpCharacter(_charId);
        
        // ONLY activate the camera if this is OUR local player
        if (virtualCam != null) virtualCam.SetActive(IsMine);

        // Sync setup to other players who might join late or are already in the room
        arcNetView.RPC(nameof(RPC_SyncSetup), RpcTarget.OthersBuffered, charId, playerTeam, id);
    }

    [ArcRPC]
    private void RPC_SyncSetup(int syncedCharId, int syncedTeam, int syncedId)
    {
        charId = syncedCharId;
        playerTeam = syncedTeam;
        id = syncedId;
        SetUpCharacter(charId);
    }

    void SetUpCharacter(int cId)
    {
        foreach (var charSetup in characterSetUp) charSetup.ActivateCharacter(false);
        if(cId < characterSetUp.Length) characterSetUp[cId].ActivateCharacter(true);
    }

    private void Update()
    {
        // THE GOLDEN RULE: If it's not mine, don't read inputs
        if (!IsMine) return;

        if (NetGameManager.Instance != null && NetGameManager.Instance.isGameOver)
        {
            characterMovement.SetMovementInput(Vector3.zero);
            return;
        }

        HandleMovementInput();
        HandleAnimations();
    }

    private void HandleMovementInput()
    {
        if (playerInputHandler == null) return;
        Vector2 moveVector = playerInputHandler.MovementInput;
        movementInput = new Vector2(moveVector.x, moveVector.y);
        characterMovement.SetMovementInput(movementInput);
    }

    private void HandleAnimations()
    {
        // Set local animator. ArcNetAnimatorView will sync this to others if attached.
        animator.SetBool("move", movementInput.magnitude > 0f);
        animator.SetFloat("weaponId", characterShooter.currentWeaponId);
    }

    // Called by AimingController
    public void HandleShooting(Vector3 aimDir)
    {
        if (!IsMine) return;

        States[] statesToCheck = new States[] { States.Shoot, States.TakingDamage };
        if (!CheckCurrentState(statesToCheck) && characterShooter.CanShoot)
        {
            // Execute locally instantly for responsiveness
            ExecuteShoot(aimDir);
            
            // Tell everyone else to play the shoot logic
            arcNetView.RPC(nameof(RPC_ExecuteShoot), RpcTarget.Others, aimDir);
        }
    }

    [ArcRPC]
    private void RPC_ExecuteShoot(Vector3 aimDir)
    {
        ExecuteShoot(aimDir);
    }

    private void ExecuteShoot(Vector3 aimDir)
    {
        SetState(States.Shoot);
        Vector3 shootDirection = (aimDir - transform.position).normalized;
        shootDirection.y = 0;
        
        transform.LookAt(aimDir);
        characterMovement.SetCanRotate(false);
        characterMovement.SetAimInput(new Vector2(shootDirection.x, shootDirection.z));
        characterShooter.TryShoot(); // This spawns the bullet locally on ALL clients
    }

    // --- State Interface Implementations ---
    public void SetState(States state) => currentState = state;
    public States GetStates() => currentState;
    public bool CheckCurrentState(States[] states)
    {
        foreach (States s in states) if (currentState == s) return true;
        return false;
    }

    public void PerformShoot(float shootTime)
    {
        animator.SetLayerWeight(1, 1);
        animator.Play("Shoot", 1, 0f);
        StartCoroutine(SwitchStateDelay(States.Base, shootTime));
    }

    public void PerformAbility() { /* Same pattern as shoot */ }
    public void TakeDamage() { /* Handled by NetHealthSystem */ }

    private IEnumerator SwitchStateDelay(States state, float waitTime)
    {
        yield return new WaitForSeconds(waitTime + 0.5f);
        characterMovement.SetCanRotate(true);
        animator.SetLayerWeight(1, 0);
        SetState(state);
    }
}