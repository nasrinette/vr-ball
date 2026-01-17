using UnityEngine;

public class MySelect : MonoBehaviour
{
    public OVRInput.Controller controller;

    private float triggerValue;

    [SerializeField] private bool isInCollider;
    [SerializeField] private bool isSelected;
    private GameObject selectedObj;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // call indextrigger value from controller
        // so primary will map to right hand when inspector is RTouch in Unity
        triggerValue = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, controller);
        if (isInCollider)
        {
            if (!isSelected && triggerValue > 0.95f)
            {
                isSelected = true;
                selectedObj.transform.parent = this.transform;
                // make roll-a-ball as the child or handAnchor
                Rigidbody rb = selectedObj.GetComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            } 
            else  if (isSelected && triggerValue < 0.95f)
            {
                // select and release the trigger
                isSelected = false;
                selectedObj.transform.parent = null;
                // remove parent, adjust all physics back
                // velocity and angular velocity have to use trakced value from OVRInput
                Rigidbody rb = selectedObj.GetComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.linearVelocity = OVRInput.GetLocalControllerVelocity(controller);
                rb.angularVelocity = OVRInput.GetLocalControllerAngularVelocity(controller);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "roll-a-ball")
        {
            isInCollider = true;
            selectedObj = other.gameObject;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.name == "roll-a-ball")
        {
            isInCollider = false;
            selectedObj = null;
        }
    }
}
