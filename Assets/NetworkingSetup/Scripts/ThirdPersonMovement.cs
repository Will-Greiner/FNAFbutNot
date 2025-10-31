using UnityEngine;
using Unity.Netcode;

public class ThirdPersonMoverment : NetworkBehaviour
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
        if (IsClient)
        {
            mouseInput.x = Input.GetAxis("Mouse X") * sens * Time.deltaTime;
            mouseInput.y = Input.GetAxis("Mouse Y") * sens * Time.deltaTime;

            mouseInput.y = Mathf.Clamp(mouseInput.y, -70f, 70f);

            transform.eulerAngles += new Vector3(-mouseInput.y, mouseInput.x, 0);
        }
    }
}

