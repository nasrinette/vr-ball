using UnityEngine;

public class MySelect : MonoBehaviour
{
    public OVRInput.Controller controller;

    [Header("Raycast Settings")]
    [SerializeField] private float rayMaxDistance = 10f;
    [SerializeField] private Color rayColor = Color.white;
    [SerializeField] private Color rayHitColor = Color.yellow;

    [Header("Visual Feedback")]
    [SerializeField] private Color selectedColor = Color.yellow;

    [Header("Debug")]
    [SerializeField] private bool isInCollider;
    [SerializeField] private bool isSelected;

    private float triggerValue;
    private GameObject selectedObj;

    // Track overlaps separately so exiting one doesn't cancel the other
    private bool inBoardCollider;
    private bool inPlayerCollider;

    // Track what we grabbed to handle release correctly
    private bool grabbedPlayer;
    private bool grabbedViaRaycast;
    private Vector3 grabOffset;
    private float lockedY; // Ball Y position locked during raycast grab

    // Color restore
    private Color originalColor;
    private Renderer selectedRenderer;

    // Raycast selection
    private GameObject rayTarget;
    private LineRenderer lineRenderer;
    private bool rayHitting;

    void Start()
    {
        // Create a LineRenderer for the ray visual
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.005f;
        lineRenderer.endWidth = 0.005f;
        lineRenderer.positionCount = 2;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = rayColor;
        lineRenderer.endColor = rayColor;
    }

    void Update()
    {
        triggerValue = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, controller);

        // --- Raycast logic (only when not already holding something) ---
        UpdateRaycast();

        // --- Grab / Release ---
        if (isInCollider || rayTarget != null)
        {
            if (!isSelected && triggerValue > 0.95f)
            {
                Grab();
            }
            else if (isSelected && triggerValue < 0.95f)
            {
                Release();
            }
        }
    }

    private void UpdateRaycast()
    {
        if (isSelected)
        {
            // Hide ray while holding something
            lineRenderer.enabled = false;
            return;
        }

        // Don't show ray when hand is directly touching something
        if (inPlayerCollider || inBoardCollider)
        {
            lineRenderer.enabled = false;
            rayTarget = null;
            rayHitting = false;
            return;
        }

        lineRenderer.enabled = true;

        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;
        rayTarget = null;
        rayHitting = false;

        if (Physics.Raycast(ray, out hit, rayMaxDistance))
        {
            // Draw ray to hit point
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, hit.point);

            string hitName = hit.collider.gameObject.name;
            if (hitName == "Player")
            {
                rayTarget = hit.collider.gameObject;
                rayHitting = true;
                lineRenderer.startColor = rayHitColor;
                lineRenderer.endColor = rayHitColor;
            }
            else
            {
                lineRenderer.startColor = rayColor;
                lineRenderer.endColor = rayColor;
            }
        }
        else
        {
            // Draw ray to max distance
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, transform.position + transform.forward * rayMaxDistance);
            lineRenderer.startColor = rayColor;
            lineRenderer.endColor = rayColor;
        }
    }

    private void Grab()
    {
        // Determine what to grab: direct touch takes priority, then raycast
        GameObject target;
        bool viaRaycast;
        if (isInCollider && selectedObj != null)
        {
            target = selectedObj;
            viaRaycast = false;
        }
        else if (rayTarget != null)
        {
            target = rayTarget;
            viaRaycast = true;
        }
        else
            return;

        isSelected = true;
        selectedObj = target;
        grabbedPlayer = (target.name == "Player");
        grabbedViaRaycast = viaRaycast;

        Rigidbody rb = target.GetComponent<Rigidbody>();

        if (grabbedPlayer)
        {
            // Keep ball dynamic so it still collides with floor/walls
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            grabOffset = Quaternion.Inverse(transform.rotation) * (target.transform.position - transform.position);

            // Lock Y for raycast grabs so ball stays on the board
            if (grabbedViaRaycast)
            {
                lockedY = target.transform.position.y;
            }

            // Yellow highlight
            selectedRenderer = target.GetComponent<Renderer>();
            if (selectedRenderer != null)
            {
                originalColor = selectedRenderer.material.color;
                selectedRenderer.material.color = selectedColor;
            }
        }
        else
        {
            // Board: make kinematic and parent to hand (original behavior)
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            target.transform.parent = this.transform;
        }
    }

    private void Release()
    {
        if (selectedObj == null) return;

        isSelected = false;

        if (!grabbedPlayer)
        {
            // remove parent, adjust all physics back
            selectedObj.transform.parent = null;
        }

        // velocity and angular velocity have to use tracked value from OVRInput
        Rigidbody rb = selectedObj.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearVelocity = OVRInput.GetLocalControllerVelocity(controller);
        rb.angularVelocity = OVRInput.GetLocalControllerAngularVelocity(controller);

        // Restore ball color
        if (grabbedPlayer && selectedRenderer != null)
        {
            selectedRenderer.material.color = originalColor;
            selectedRenderer = null;
        }

        grabbedPlayer = false;
    }

    void FixedUpdate()
    {
        // Drive the Player ball toward hand via velocity — physics handles collisions
        if (isSelected && grabbedPlayer && selectedObj != null)
        {
            Rigidbody rb = selectedObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 targetPos = transform.position + transform.rotation * grabOffset;

                if (grabbedViaRaycast)
                {
                    // Constrain to board surface: only move in XZ, lock Y
                    targetPos.y = lockedY;
                }

                Vector3 delta = targetPos - rb.position;
                rb.linearVelocity = delta / Time.fixedDeltaTime;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (isSelected) return; // Don't switch targets while holding something

        if (other.gameObject.name == "Player")
        {
            // Player ball takes priority over the board
            inPlayerCollider = true;
            isInCollider = true;
            selectedObj = other.gameObject;
        }
        else if (other.gameObject.name == "roll-a-ball")
        {
            inBoardCollider = true;
            isInCollider = true;
            // Only target the board if we're not already touching the Player ball
            if (!inPlayerCollider)
            {
                selectedObj = other.gameObject;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (isSelected) return; // Don't clear state while holding something

        if (other.gameObject.name == "Player")
        {
            inPlayerCollider = false;
            // Fall back to board if hand is still inside it
            if (inBoardCollider)
            {
                selectedObj = GameObject.Find("roll-a-ball");
            }
            else
            {
                isInCollider = false;
                selectedObj = null;
            }
        }
        else if (other.gameObject.name == "roll-a-ball")
        {
            inBoardCollider = false;
            if (!inPlayerCollider)
            {
                isInCollider = false;
                selectedObj = null;
            }
        }
    }
}
