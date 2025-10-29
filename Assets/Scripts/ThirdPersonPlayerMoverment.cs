
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;

public class ThirdPersonPlayerMoverment : MonoBehaviour
{
    Vector2 mouseInput;
    [SerializeField] float sens;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        mouseInput.x = Input.GetAxis("Mouse X") * sens * Time.deltaTime;
        mouseInput.y = Input.GetAxis("Mouse Y") * sens * Time.deltaTime;
       
        mouseInput.y = Mathf.Clamp(mouseInput.y, -70f, 70f);
  
        transform.eulerAngles += new Vector3(-mouseInput.y, mouseInput.x, 0);

    }
}
