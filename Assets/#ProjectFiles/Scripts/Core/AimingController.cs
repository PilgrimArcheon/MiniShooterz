using UnityEngine;

public class AimingController : MonoBehaviour
{
    [Header("Aim Settings")]
    public Transform aimPivot;
    public Transform aimFollow;
    public LayerMask oppLayer;
    public LayerMask groundLayer;
    public LayerMask hitMask;
    public float autoAimRange = 10f;
    public float aimDeadzone = 0.2f;
    public bool joyStick;

    private PlayerInputHandler playerInputHandler;
    private CharacterShooter characterShooter;
    private Camera mainCamera;

    [SerializeField] private GameObject aimIndicator;
    [SerializeField] private GameObject abilityAimIndicator;

    private bool startedAiming;
    private bool isAbilityAim;
    private Vector3 aimInput;

    private void Start()
    {
        mainCamera = Camera.main;
        playerInputHandler = GameObject.Find("PlayerInputHandler").GetComponent<PlayerInputHandler>();
        characterShooter = GetComponent<CharacterShooter>();

        hitMask = oppLayer + groundLayer;
    }

    private void Update()
    {
        aimIndicator.SetActive(false);
        abilityAimIndicator.SetActive(false);

        joyStick = playerInputHandler.playerInput.currentControlScheme == "Gamepad";

        // Priority: if ability aiming is active, ignore normal aiming
        if (playerInputHandler.IsAbilityInput)
            HandleAbilityAiming();
        else HandleNormalAiming();
        // Keep pivot aligned horizontally with the player
        aimPivot.position = new Vector3(transform.position.x, aimPivot.position.y, transform.position.z);
    }

    #region --- NORMAL SHOOT FLOW ---
    private void HandleNormalAiming()
    {
        if (!characterShooter.CanShoot) return;

        bool isAiming = playerInputHandler.IsAimingInput;
        aimIndicator.SetActive(isAiming);

        if (isAiming) UpdateAim(false);   // Normal shoot
        if (startedAiming && !isAiming) FireWeapon();
    }

    private void FireWeapon()
    {
        ShootInDirection(aimFollow.position);
        startedAiming = false;
        playerInputHandler.ResetAbility();
    }
    #endregion

    #region --- ABILITY FLOW ---
    private void HandleAbilityAiming()
    {
        if (!characterShooter.CanUseAbility) return;

        bool isAiming = playerInputHandler.IsAimingInput;
        abilityAimIndicator.SetActive(isAiming);

        if (isAiming)
        {
            isAbilityAim = true;
            UpdateAim(true);
        }

        if (startedAiming && !isAiming) FireAbility();
    }

    private void FireAbility()
    {
        StartAbilitySpawn(aimFollow.position);
        isAbilityAim = false;
        startedAiming = false;
        playerInputHandler.ResetAbility();
    }
    #endregion

    #region --- AIMING CORE LOGIC ---
    private void UpdateAim(bool isAbility)
    {
        float aimMaxDistance = isAbility ? characterShooter.ability.areaOfEffectMaxDistance : characterShooter.maxDistance;

        Vector3 inputDir = GetInputDirection();
        if (inputDir.sqrMagnitude > 0.01f)
            aimFollow.position = aimPivot.position + (inputDir * aimMaxDistance);

        // Store aiming input
        aimInput = aimFollow.position - aimPivot.position;
        aimInput.y = 0f;

        CalculateAim(isAbility);

        if (!startedAiming) startedAiming = true;

        if (aimInput.magnitude > aimDeadzone && aimPivot != null)
            RotateToDirection(aimInput.normalized);
    }

    private Vector3 GetInputDirection()
    {
        if (!joyStick && !playerInputHandler.IsMobile)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hitInfo, Mathf.Infinity, groundLayer))
            {
                Vector3 dir = hitInfo.point - transform.position;
                dir.y = 0;
                return dir.normalized;
            }
            return Vector3.zero;
        }
        else
        {
            Vector2 input = playerInputHandler.AimDirectionInput;
            Vector3 dir = new(input.x, 0f, input.y);
            return dir.normalized;
        }
    }

    private void CalculateAim(bool isAbility)
    {
        Vector3 origin = aimPivot.position;
        Vector3 direction = aimInput;
        Vector3 endPoint;

        Ray ray = new(origin, direction);
        float radius = 1f;

        if (Physics.SphereCast(ray, radius, out RaycastHit hit, characterShooter.maxDistance, hitMask))
            endPoint = new Vector3(hit.point.x, origin.y, hit.point.z);
        else
            endPoint = aimFollow.position;

        if (isAbility) UpdateAbilityVfx(endPoint);
        else UpdateWeaponVfx(origin, endPoint);
    }
    #endregion

    #region --- VFX HELPERS ---
    private void UpdateWeaponVfx(Vector3 origin, Vector3 endPoint)
    {
        Weapon weapon = characterShooter.weapons[characterShooter.currentWeaponId];
        RectTransform rectTransform = weapon.WeaponAimVfx.GetComponent<RectTransform>();

        Vector3 localStart = rectTransform.InverseTransformPoint(origin);
        Vector3 localEnd = rectTransform.InverseTransformPoint(endPoint);
        float localHeight = Mathf.Abs(localEnd.y - localStart.y);

        Vector2 size = rectTransform.sizeDelta;
        size.y = localHeight;
        rectTransform.sizeDelta = size;
    }

    private void UpdateAbilityVfx(Vector3 endPoint)
    {
        Ability ability = characterShooter.ability;
        ability.AbilityAimVfx.transform.position = endPoint;
    }
    #endregion

    #region --- FIRE HELPERS ---
    private void ShootInDirection(Vector3 aimDir)
    {
        GetComponent<PlayerCharacterController>().HandleShooting(aimDir);
    }

    private void StartAbilitySpawn(Vector3 startPos)
    {
        GetComponent<PlayerCharacterController>().HandleAbility(startPos);
    }

    private void RotateToDirection(Vector3 aimDir)
    {
        if (aimDir.magnitude > 0.1f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(aimDir, Vector3.up);
            Quaternion targetRotation = Quaternion.Euler(0f, lookRotation.eulerAngles.y, 0f);
            aimPivot.rotation = Quaternion.Slerp(aimPivot.rotation, targetRotation, 10f * Time.deltaTime);
        }
    }

    private void AutoAim()
    {
        Collider[] opponents = Physics.OverlapSphere(transform.position, autoAimRange, oppLayer);

        if (opponents.Length == 0)
        {
            ShootInDirection(aimFollow.position);
            return;
        }

        Transform closestOpponent = null;
        float closestDistance = Mathf.Infinity;

        foreach (var opp in opponents)
        {
            float distance = Vector3.Distance(transform.position, opp.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestOpponent = opp.transform;
            }
        }

        if (closestOpponent != null)
            ShootInDirection(closestOpponent.position);
    }
    #endregion

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, autoAimRange);
    }
}
