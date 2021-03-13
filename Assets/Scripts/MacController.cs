using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using UnityEngine.SceneManagement;

//For moving Mac going down the slide etc

public class MacController : MonoBehaviour
{
    [SerializeField] private float speed = 3;
    [SerializeField] private GameObject gameCon, canvasCon;

    private WorldMover gameScript;

    private Rigidbody rb;
    private bool isGrounded = false;
    private Vector3 startingPos;

    private void Awake()
    {
        isGrounded = false;
        startingPos = transform.position;
        rb = GetComponent<Rigidbody>();
        if (gameCon != null)
            gameScript = gameCon.GetComponent<WorldMover>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Used to reset the character position 
    public void ResetCharacter()
    {
        gameObject.transform.position = startingPos;
        gameScript.ResetWorld();
        rb.velocity = Vector3.zero;
      
        isGrounded = true;
        //gameCon.GetComponent<MacController>().isGrounded = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag == "Floor")
        {
            scoreCounter++;
            updateScore();
            //Debug.Log("Ice cream hit");
            isGrounded = true;
        }
    }

    //for when user falls off the side of the slide
    private void OnTriggerExit(Collider collider)
    {
        if (collider.gameObject.tag == "Killbox")
        {
            Debug.Log("User fell off the slide");
            ResetCharacter();
        }
    }

    //hits ice cream or poop
    private void OnTriggerEnter(Collider other)
    {
        //scoreCounter--;
        scoreCounter = 0;
        updateScore();
        //Debug.Log("Poop hit");
        //ResetCharacter();
        Start();
    }

    // Update is called once per frame
    void Update()
    {
        if (isGrounded)
        {
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
                rb.AddForce(Vector3.right * speed, ForceMode.Impulse);
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
                rb.AddForce(Vector3.left * speed, ForceMode.Impulse);
        }
    }

    private GameObject txtBox;
    private static int scoreCounter = 0;
    private void updateScore()
    {
        txtBox = GameObject.Find("scoreCountTxt");
        //Debug.Log("Tag: " + txtBox.tag);

        //Debug.Log("Text: " + txtBox.GetComponent<Text>().text);

        //need to fix to get the correct text
        txtBox.GetComponent<Text>().text = scoreCounter.ToString();
    }
}
