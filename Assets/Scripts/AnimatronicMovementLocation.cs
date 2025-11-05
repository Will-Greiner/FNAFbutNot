using System;
using System.Collections;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class AnimatronicMovementLocation : MonoBehaviour
{
    //this will move the empty of the camera and player
    //this does not include rotation just location

    [SerializeField] float maxSpeed;
    [SerializeField] float acceleration;
    //float momentumForward;
    //float momentumSideways;
    Vector2 playerTranslate;
    public GameObject CameraOrientation;
    //float directionY;
    [SerializeField] float turningSpeed;
    [SerializeField] Animator animator;

    public bool isAccelerating = false;

    private Rigidbody playerRigidbody;
    public float playerAcceleration;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        playerRigidbody = GetComponent<Rigidbody>();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created


    void Start()
    {
        GameObject CameraOrientation = GameObject.FindGameObjectWithTag("CameraArm");
    }

    // Update is called once per frame
    void Update()
    {
        //float CameraYRotation = CameraOrientation.transform.eulerAngles.y;

        //Vector3 Direction = CameraOrientation.transform.forward;

        if (Math.Abs(Input.GetAxisRaw("Vertical")) > 0.1f)
        {
            isAccelerating = true;
        }
        else if (Math.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f)
        {
            isAccelerating = true;
        }
        else
        {
            isAccelerating = false;

        } 
    }
    private void FixedUpdate()
    {
        if (isAccelerating)
        {
            if (Input.GetAxisRaw("Vertical") > 0)
            {
                playerRigidbody.AddForce(playerAcceleration * CameraOrientation.transform.forward);
            }
            if (Input.GetAxisRaw("Vertical") < 0)
            {
                playerRigidbody.AddForce(playerAcceleration * -CameraOrientation.transform.forward);
            }
            if (Input.GetAxisRaw("Horizontal") > 0)
            {
                playerRigidbody.AddForce(playerAcceleration * CameraOrientation.transform.right);
            }
            if (Input.GetAxisRaw("Horizontal") < 0)
            {
                playerRigidbody.AddForce(playerAcceleration * -CameraOrientation.transform.right);
            }
            playerRigidbody.linearVelocity = Vector3.ClampMagnitude(playerRigidbody.linearVelocity, maxSpeed);
        }
    }
}
           
   