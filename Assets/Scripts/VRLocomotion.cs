using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class VRLocomotion : MonoBehaviour
{
    [Header("Smooth Locomotion")]
    [SerializeField] private float moveSpeed = 2.0f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Snap Turn")]
    [SerializeField] private float snapTurnAngle = 45f;

    [Header("Teleportation")]
    [SerializeField] private OVRInput.Button teleportButton = OVRInput.Button.Two;
    [SerializeField] private Transform rightHandAnchor;
    [SerializeField] private float teleportArcVelocity = 6f;
    [SerializeField] private float teleportArcGravity = -9.81f;
    [SerializeField] private int arcSegments = 30;
    [SerializeField] private float arcSegmentLength = 0.3f;
    [SerializeField] private LayerMask floorLayer;

    [Header("Teleport Visuals")]
    [SerializeField] private Color arcValidColor = Color.green;
    [SerializeField] private Color arcInvalidColor = Color.red;
    [SerializeField] private float arcLineWidth = 0.01f;

    private CharacterController controller;
    private OVRCameraRig cameraRig;
    private Transform centerEyeAnchor;

    private bool snapTurnReady = true;

    private LineRenderer teleportArc;
    private GameObject teleportReticle;
    private bool isTeleportAiming;
    private bool teleportValid;
    private Vector3 teleportTarget;

    private float verticalVelocity;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        cameraRig = GetComponent<OVRCameraRig>();
    }

    void Start()
    {
        centerEyeAnchor = cameraRig.centerEyeAnchor;

        if (rightHandAnchor == null)
            rightHandAnchor = cameraRig.rightHandAnchor;

        CreateTeleportVisuals();
    }

    void Update()
    {
        HandleSnapTurn();
        HandleTeleport();
        HandleSmoothMovement();
    }

    private void HandleSmoothMovement()
    {
        if (isTeleportAiming) return;

        Vector2 input = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);

        Vector3 forward = centerEyeAnchor.forward;
        forward.y = 0f;
        forward.Normalize();
        Vector3 right = centerEyeAnchor.right;
        right.y = 0f;
        right.Normalize();

        Vector3 moveDir = (forward * input.y + right * input.x) * moveSpeed;

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -0.5f;
        else
            verticalVelocity += gravity * Time.deltaTime;

        moveDir.y = verticalVelocity;

        controller.Move(moveDir * Time.deltaTime);
    }

    private void HandleSnapTurn()
    {
        if (isTeleportAiming) return;

        bool leftPressed = OVRInput.Get(OVRInput.Button.SecondaryThumbstickLeft);
        bool rightPressed = OVRInput.Get(OVRInput.Button.SecondaryThumbstickRight);

        if (leftPressed && snapTurnReady)
        {
            transform.RotateAround(centerEyeAnchor.position, Vector3.up, -snapTurnAngle);
            snapTurnReady = false;
        }
        else if (rightPressed && snapTurnReady)
        {
            transform.RotateAround(centerEyeAnchor.position, Vector3.up, snapTurnAngle);
            snapTurnReady = false;
        }
        else if (!leftPressed && !rightPressed)
        {
            snapTurnReady = true;
        }
    }

    private void HandleTeleport()
    {
        bool buttonHeld = OVRInput.Get(teleportButton);
        bool buttonUp = OVRInput.GetUp(teleportButton);

        if (buttonHeld)
        {
            isTeleportAiming = true;
            UpdateTeleportArc();
        }
        else if (isTeleportAiming)
        {
            if (teleportValid)
                ExecuteTeleport();

            isTeleportAiming = false;
            teleportArc.enabled = false;
            teleportReticle.SetActive(false);
        }
    }

    private void UpdateTeleportArc()
    {
        teleportArc.enabled = true;

        Vector3 startPos = rightHandAnchor.position;
        Vector3 velocity = rightHandAnchor.forward * teleportArcVelocity;

        List<Vector3> arcPoints = new List<Vector3>();
        arcPoints.Add(startPos);

        teleportValid = false;
        Vector3 currentPos = startPos;
        Vector3 currentVel = velocity;

        for (int i = 0; i < arcSegments; i++)
        {
            float dt = arcSegmentLength / Mathf.Max(currentVel.magnitude, 0.01f);
            Vector3 nextPos = currentPos + currentVel * dt;
            currentVel += Vector3.up * (teleportArcGravity * dt);

            Vector3 segDir = nextPos - currentPos;
            float segLen = segDir.magnitude;

            if (Physics.Raycast(currentPos, segDir.normalized, out RaycastHit hit, segLen, floorLayer))
            {
                arcPoints.Add(hit.point);
                teleportTarget = hit.point;
                teleportValid = true;
                break;
            }

            arcPoints.Add(nextPos);
            currentPos = nextPos;
        }

        teleportArc.positionCount = arcPoints.Count;
        teleportArc.SetPositions(arcPoints.ToArray());

        Color c = teleportValid ? arcValidColor : arcInvalidColor;
        teleportArc.startColor = c;
        teleportArc.endColor = c;

        if (teleportValid)
        {
            teleportReticle.SetActive(true);
            teleportReticle.transform.position = teleportTarget + Vector3.up * 0.01f;
        }
        else
        {
            teleportReticle.SetActive(false);
        }
    }

    private void ExecuteTeleport()
    {
        Vector3 headOffset = centerEyeAnchor.position - transform.position;
        headOffset.y = 0f;

        Vector3 newRigPos = teleportTarget - headOffset;
        newRigPos.y = teleportTarget.y;

        controller.enabled = false;
        transform.position = newRigPos;
        controller.enabled = true;

        verticalVelocity = 0f;
    }

    private void CreateTeleportVisuals()
    {
        GameObject arcObj = new GameObject("TeleportArc");
        arcObj.transform.SetParent(transform, false);
        teleportArc = arcObj.AddComponent<LineRenderer>();
        teleportArc.startWidth = arcLineWidth;
        teleportArc.endWidth = arcLineWidth;
        teleportArc.material = new Material(Shader.Find("Sprites/Default"));
        teleportArc.enabled = false;

        teleportReticle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        teleportReticle.name = "TeleportReticle";
        teleportReticle.transform.localScale = new Vector3(0.3f, 0.005f, 0.3f);
        Destroy(teleportReticle.GetComponent<Collider>());
        teleportReticle.GetComponent<Renderer>().material = new Material(Shader.Find("Sprites/Default"));
        teleportReticle.GetComponent<Renderer>().material.color = arcValidColor;
        teleportReticle.SetActive(false);
    }
}
