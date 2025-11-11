
using UnityEngine;

public class AnimatronicMovementRotation : MonoBehaviour
{
    [SerializeField] GameObject CameraArm;
    [SerializeField] float turningSpeed;
    [SerializeField] float yDirection;
    [SerializeField] float targetAngle;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameObject CameraArm = GameObject.FindGameObjectWithTag("CameraArm");
        yDirection = transform.eulerAngles.y;
    }

    // Update is called once per frame
    void Update()
    {
        
        if (Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f || Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f)
        {
            if (Input.GetAxisRaw("Horizontal") < -0.1f)
            {
                targetAngle = CameraArm.transform.eulerAngles.y - 90;
            }
            if (Input.GetAxisRaw("Horizontal") > 0.1f)
            {
                targetAngle = CameraArm.transform.eulerAngles.y + 90; 
            }

            if (Input.GetAxisRaw("Vertical") > 0.1f)
            {
                targetAngle = CameraArm.transform.eulerAngles.y;
            }
            if (Input.GetAxisRaw("Vertical") < -0.1f)
            {
                targetAngle = CameraArm.transform.eulerAngles.y + 180;
            }

            yDirection = Mathf.MoveTowards(yDirection, targetAngle, turningSpeed * Time.deltaTime);

            transform.rotation = Quaternion.Euler(0, yDirection, 0);
            
        }
        else
        {
            targetAngle = CameraArm.transform.eulerAngles.y;
        }
    }
}
