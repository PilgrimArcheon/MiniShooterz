using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterMovement))]
[RequireComponent(typeof(CharacterShooter))]
[RequireComponent(typeof(HealthSystem))]
public class AICharacterController : NetworkBehaviour, ICombat
{
    [Header("Character Settings")]
    [SerializeField] CharacterSetUp[] characterSetUp;
    [SerializeField] GameObject[] teamId;
    private CharacterMovement characterMovement;
    private CharacterShooter characterShooter;
    private HealthSystem healthSystem;
    private HUDControl hUDControl;

    private Transform targetTransform;
    private Vector3 patrolPosition;

    private Animator animator;

    private CharacterVariables characterVar = new();

    States currentState;

    [Header("AI Behavior Settings")]
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private LayerMask whatIsOpponent;
    [SerializeField] Vector2[] movePoints;
    [SerializeField] private bool opponentInSightRange;
    [SerializeField] private bool opponentInShootRange;
    public float detectionRadius = 10f;
    public float shootRadius = 5f;
    bool movePointSet;

    Vector2 movementInput;

    public int charId;
    public int aiTeam;  // AI team
    public int id;

    private void Awake()
    {
        characterMovement = GetComponent<CharacterMovement>();
        characterShooter = GetComponent<CharacterShooter>();
        healthSystem = GetComponent<HealthSystem>();
        hUDControl = GetComponent<HUDControl>();
        animator = GetComponentInChildren<Animator>();

        movePoints = GameManager.Instance.MovePoints;
    }

    private void OnEnable()
    {
        SetState(States.Base);

        opponentInShootRange = false;
        opponentInSightRange = false;
        patrolPosition = Vector3.zero;
        targetTransform = null;
        movePointSet = false;
    }

    public override void OnNetworkSpawn() { InvokeRepeating(nameof(UpdateCharVar), 0.15f, 2.5f); }

    bool hasSetUpVal;
    public void CharacterUserSetUp(string userId, int _charId, int team, int _id)
    {
        id = _id;
        charId = _charId;

        if (hasSetUpVal) return;

        SetUpCharacter(charId);
        SetTeams(team);

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
    }

    public void SetTeams(int team)
    {
        aiTeam = team;

        int teamLook = aiTeam == GameManager.Instance.GetMyTeam ? 0 : 1;

        string teamLayer = aiTeam == GameManager.Instance.GetMyTeam ? "Team" : "Opp";
        string oppLayer = aiTeam == GameManager.Instance.GetMyTeam ? "Opp" : "Team";

        gameObject.layer = LayerMask.NameToLayer(teamLayer);
        whatIsOpponent = LayerMask.GetMask(oppLayer);

        teamId[teamLook].SetActive(true);

        healthSystem.SetUpHealth(id, aiTeam); // Set AI's team in Health system      
        characterShooter.SetCharacterShooter(characterShooter.currentWeaponId, id, aiTeam); // Set AI's weapon, Id && team in Shooter
    }

    void SetUpCharacter(int rnd)
    {
        foreach (var charSetup in characterSetUp)
        {
            charSetup.ActivateCharacter(false);
        }
        characterSetUp[rnd].ActivateCharacter(true);
    }

    void UpdateCharVar()
    {
        if (IsOwner) UpdateCharVarRPC(characterVar.username, characterVar.charId, characterVar.team, characterVar._id);
    }

    [Rpc(SendTo.Everyone)]
    void UpdateCharVarRPC(string username, int charId, int team, int _id)
    {
        CharacterUserSetUp(username, charId, team, _id);
    }

    private void Update()
    {
        if (currentState != States.Base
            || GameManager.Instance.isGameOver)

        {
            characterMovement.SetMovementInput(Vector3.zero);
            return;
        }

        if (transform.position.x < movePoints[0].x
            || transform.position.x > movePoints[1].x) GetRandomMovePosition();
        else if (transform.position.z < movePoints[0].y
            || transform.position.z > movePoints[1].y) GetRandomMovePosition();

        CheckForOpponent();

        HandleAIBehavior();
        HandleAnimations();
        HandleObstacleEncounter();
    }

    private void CheckForOpponent()
    {
        opponentInSightRange = Physics.CheckSphere(transform.position, detectionRadius, whatIsOpponent);
        opponentInShootRange = Physics.CheckSphere(transform.position, shootRadius, whatIsOpponent);
    }

    // Handle AI behavior
    private void HandleAIBehavior()
    {
        if (!isEvading)
        {
            if (!opponentInSightRange && !opponentInShootRange) Patrol();
            if (opponentInSightRange && !opponentInShootRange) Chase();
            if (opponentInSightRange && opponentInShootRange) Shoot();
        }
        else Evade();

        characterMovement.SetMovementInput(movementInput);
    }

    private void HandleAnimations()
    {
        animator.SetBool("move", movementInput.magnitude > 0f);
        animator.SetFloat("weaponId", characterShooter.currentWeaponId);
    }

    // Move AI towards the RandomPosition
    private void Patrol()
    {
        if (!movePointSet) GetRandomMovePosition();
        else
        {
            if (Vector2.Distance(transform.position, patrolPosition) > 1f)
            {
                Vector3 moveDirection = (patrolPosition - transform.position).normalized;
                movementInput = new Vector2(moveDirection.x, moveDirection.z);
            }
            else movePointSet = false;
        }
    }

    private void HandleObstacleEncounter()
    {
        Vector3 startCast = new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z);
        Vector3 endCast = startCast + (transform.forward * 1.5f);

        if (Physics.CheckSphere(endCast, 0.75f, whatIsGround))
        {
            GetEvasion();
        }

        Debug.DrawLine(startCast, endCast, Color.white);
    }

    // Move AI towards the Opponent
    private void Chase()
    {
        if (targetTransform == null) GetClosestOpponent();
        else
        {
            if (Vector2.Distance(transform.position, targetTransform.position) > shootRadius)
            {
                Vector3 moveDirection = (targetTransform.position - transform.position).normalized;
                movementInput = new Vector2(moveDirection.x, moveDirection.z);
            }
        }
    }

    // Handle AI towards the Shooting Opponent
    private void Shoot()
    {
        if (targetTransform == null) GetClosestOpponent();
        else
        {
            movementInput = Vector3.zero;
            transform.LookAt(targetTransform);
            characterShooter.TryShoot();

            targetTransform = null;
            opponentInShootRange = false;
            GetEvasion(Random.Range(0, 1f), 1f);
        }
    }

    private void Evade()
    {
        if (Vector3.Distance(transform.position, evadePosition) > 0.5f)
        {
            Vector3 moveDirection = (evadePosition - transform.position).normalized;
            movementInput = new Vector2(moveDirection.x, moveDirection.z);

            if (characterShooter.CanShoot && opponentInShootRange) isEvading = false;
        }
        else isEvading = false;
    }

    //Handle Getting Random Patrol Position
    private void GetRandomMovePosition()
    {
        Vector3 startPos = transform.position;

        float rndX = Random.Range(movePoints[0].x, movePoints[1].x);
        float rndZ = Random.Range(movePoints[0].y, movePoints[1].y);

        patrolPosition = new Vector3(rndX, startPos.y, rndZ);

        movePointSet = true;
    }

    //Handle Getting Closest Opponent Position
    private void GetClosestOpponent()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius, whatIsOpponent);
        float distance = Mathf.Infinity;

        foreach (var col in colliders)
        {
            Transform opponentTrans = col.gameObject.transform;
            float distFromEnemy = Vector3.Distance(transform.position, opponentTrans.position);

            if (distFromEnemy < distance)
            {
                targetTransform = col.gameObject.transform;
                distance = distFromEnemy;
            }
        }
    }

    bool isEvading;

    Vector3 evadePosition;
    private void GetEvasion(float percentForEvasion = 0.1f, float evadeDist = 5)
    {
        if (percentForEvasion < 0.25f)
        {
            evadePosition = transform.position + (transform.right * Random.Range(-evadeDist, evadeDist));
            isEvading = true;
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

            GetEvasion(Random.Range(0, 1f));

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
        yield return new WaitForSeconds(waitTime);
        animator.SetLayerWeight(1, 0);
        SetState(state);
    }
    #endregion

    void OnDrawGizmos()
    {
        Vector3 startCast = new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z);
        Vector3 endCast = startCast + (transform.forward * 1.5f);
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(endCast, 0.75f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, shootRadius);
    }
}