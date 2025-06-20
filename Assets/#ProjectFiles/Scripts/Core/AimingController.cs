using Unity.Netcode;
using UnityEngine;

public class AimingController : NetworkBehaviour
{
    [Header("Aim Settings")]
    public Transform aimPivot;// This will be rotated
    public Transform aimFollow;
    public LayerMask oppLayer;
    public LayerMask groundLayer;
    public LayerMask hitMask;
    public float autoAimRange = 10f;
    public float aimDeadzone = 0.2f;      // How much stick/mouse movement needed to be considered aiming
    public bool joyStick; // Is the player using a joystick?
    PlayerInputHandler playerInputHandler;
    CharacterShooter characterShooter;

    private Camera mainCamera;

    [SerializeField] private GameObject aimIndicator;

    private void Start()
    {
        mainCamera = Camera.main;
        playerInputHandler = GameObject.Find("PlayerInputHandler").GetComponent<PlayerInputHandler>();

        characterShooter = GetComponent<CharacterShooter>();

        hitMask = oppLayer + groundLayer;
    }

    bool startedAiming;
    private void Update()
    {
        aimIndicator.SetActive(false);

        if (!IsOwner || GameManager.Instance.isGameOver) return;

        HandleAimingInput();

        aimPivot.position = new(transform.position.x, aimPivot.position.y, transform.position.z);
    }

    Vector3 aimInput;
    private void HandleAimingInput()
    {
        if (!characterShooter.CanShoot) return; // Don't aim if we can't shoot

        bool isAiming = playerInputHandler.IsAimingInput;

        joyStick = playerInputHandler.playerInput.currentControlScheme == "Gamepad";

        aimIndicator.SetActive(isAiming);

        if (!joyStick && !playerInputHandler.IsMobile)
        {
            Vector3 mouseScreenPos = Input.mousePosition;
            Ray ray = mainCamera.ScreenPointToRay(mouseScreenPos);
            if (Physics.Raycast(ray, out var hitInfo, Mathf.Infinity, groundLayer))
            {
                Vector3 inputDir = hitInfo.point - transform.position;
                inputDir.y = 0;
                inputDir.Normalize();
                aimFollow.position = aimPivot.position + (inputDir * characterShooter.maxDistance);
            }
        }
        else
        {
            float horizontal = playerInputHandler.AimDirectionInput.x;
            float vertical = playerInputHandler.AimDirectionInput.y;
            Vector3 inputDir = new(horizontal, 0f, vertical);
            inputDir.Normalize();
            if (inputDir.magnitude > 0.1f)
                aimFollow.position = aimPivot.position + (inputDir * characterShooter.maxDistance);
        }

        if (isAiming)
        {
            aimInput = aimFollow.position - aimPivot.position;
            aimInput.y = 0f;

            CalculateAim();

            if (!startedAiming) startedAiming = true;
        }

        if (aimInput.magnitude > aimDeadzone && aimPivot != null)
            RotateToDirection(aimInput.normalized);

        if (startedAiming && !isAiming) FireWeapon();
    }

    void CalculateAim()
    {
        Vector3 origin = aimPivot.position;
        Vector3 direction = aimInput;
        Vector3 endPoint;

        Ray ray = new(origin, direction);
        float radius = 1f;

        if (Physics.SphereCast(ray, radius, out RaycastHit hit, characterShooter.maxDistance, hitMask))
            endPoint = new Vector3(hit.point.x, origin.y, hit.point.z);
        else endPoint = aimFollow.position;

        // Draw the shot
        int weaponId = characterShooter.currentWeaponId;
        Weapon weapon = characterShooter.weapons[weaponId];
        RectTransform rectTransform = weapon.WeaponAimVfx.GetComponent<RectTransform>();

        // Convert the distance from world space to local canvas units
        Vector3 localStart = rectTransform.InverseTransformPoint(origin);
        Vector3 localEnd = rectTransform.InverseTransformPoint(endPoint);
        float localHeight = Mathf.Abs(localEnd.y - localStart.y);

        // Update the RectTransform height (Y axis only)
        Vector2 size = rectTransform.sizeDelta;
        size.y = localHeight;
        rectTransform.sizeDelta = size;
    }

    private void FireWeapon()
    {
        ShootInDirection(aimFollow.position);
        startedAiming = false;
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

        if (opponents.Length > 0)
        {
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
            {
                ShootInDirection(closestOpponent.position);
            }
        }
        else
        {
            ShootInDirection(aimFollow.position);
        }
    }

    void ShootInDirection(Vector3 aimDir)
    {
        GetComponent<PlayerCharacterController>().HandleShooting(aimDir);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, autoAimRange);
    }
}