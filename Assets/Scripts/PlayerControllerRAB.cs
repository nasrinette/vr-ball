using UnityEngine;
// using UnityEngine.InputSystem;
using TMPro;

public class PlayerControllerRAB : MonoBehaviour
{
    private Rigidbody rb;
    private int count;
    private float movementX;
    private float movementY;
    private bool isPlaying = false;

    public float speed = 0;
    public TextMeshProUGUI countText;
    public GameObject homeUI;
    public GameObject wonUI;
    public GameObject lostUI;




    void Start()
    {
        rb = GetComponent<Rigidbody>();
        count = 0;
        SetCountText();

        homeUI.SetActive(true);
        wonUI.SetActive(false);
        lostUI.SetActive(false);
    }

    public void Play()
    {
        isPlaying = true;
        homeUI.SetActive(false);
        wonUI.SetActive(false);
        lostUI.SetActive(false);
    }

    private void FixedUpdate()
    {
        if (!isPlaying) return;

        Vector3 movement = new Vector3(movementX, 0.0f, movementY);
        rb.AddForce(movement * speed);
    }

    void OnTriggerEnter(Collider other) 
    {
        if (other.gameObject.CompareTag("PickUp")) 
        {
            other.gameObject.SetActive(false); // set unactive
            count = count+1;
            SetCountText();
        }
    }


    //  void OnMove (InputValue movementValue)
    // {
    //     Vector2 movementVector = movementValue.Get<Vector2>(); 
        
    //     movementX = movementVector.x; 
    //     movementY = movementVector.y; 
    // }

    void SetCountText()
    {
        countText.text = "Count: " + count.ToString();
        if (count >= 11)
        {
            isPlaying = false;
            wonUI.SetActive(true);
            Destroy(GameObject.FindGameObjectWithTag("Enemy"));
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            isPlaying = false;
            lostUI.SetActive(true);
            Destroy(gameObject);
        }
    }
}
