using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;

public class AnimatronicMovement : MonoBehaviour
{
    public float sensX;
    public float sensY;
    float xRotation;
    float yRotation;
    public Transform cameraTransform;
    public float playerAcceleration;
    public float playerMaxVelocity;
    private Rigidbody playerRigidbody;
    Vector3 moveVec;
    private bool isAccelerating = false;

    void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();
    }
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        cameraTransform.transform.localRotation = Quaternion.Euler(25, 0, 0);
    }
    void Update()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensY;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        //Vector3 moveVec = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        //cameraTransform.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
        cameraTransform.transform.Rotate(Vector3.up * mouseX);
        if (Math.Abs(Input.GetAxisRaw("Vertical")) > 0.1f)
        {
            isAccelerating = true;
        }
        else if (Math.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f)
        {
            isAccelerating = true;
        }
        else { isAccelerating = false; }



    }

    void FixedUpdate()
    {
        if (isAccelerating)
        {
            if (Input.GetAxisRaw("Vertical") > 0)
            {
                playerRigidbody.AddForce(playerAcceleration * transform.forward);
            }
            if (Input.GetAxisRaw("Vertical") < 0)
            {
                playerRigidbody.AddForce(playerAcceleration * -transform.forward);
            }
            if (Input.GetAxisRaw("Horizontal") > 0)
            {
                playerRigidbody.AddForce(playerAcceleration * transform.right);
            }
            if (Input.GetAxisRaw("Horizontal") < 0)
            {
                playerRigidbody.AddForce(playerAcceleration * -transform.right);
            }
            playerRigidbody.linearVelocity = Vector3.ClampMagnitude(playerRigidbody.linearVelocity, playerMaxVelocity);
        }
    }
}
