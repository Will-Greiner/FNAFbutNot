using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class AnimatronicMovementRotation : MonoBehaviour
{
    [SerializeField] GameObject CameraArm;
    [SerializeField] float turningSpeed;
    [SerializeField] float yDirection;
    //[SerializeField] float rotThreshold;
    //[SerializeField] float rotAngle;
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
        //AHHHHHHHH

       
        //Current behavior, when w is pressed character will face in correct direction(facing away from the camera)
        //when no button is pressed character is faced towards the camera
        //when any other button is pressed the character will face towards the camera.
        
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
            //conditional statement if the distance between target angle and current location is less then 180  then turn right
            //else turn left to reach the negative 
            //if (transform.eulerAngles.y - targetAngle < 180)
            //{
            //    yDirection = Mathf.MoveTowards(yDirection, targetAngle, turningSpeed * Time.deltaTime);
            //}
            //else
            //{
            //    yDirection = Mathf.MoveTowards(yDirection, targetAngle -360, turningSpeed * Time.deltaTime);
            //}

            //yDirection = Mathf.MoveTowards(yDirection, targetAngle, turningSpeed * Time.deltaTime);

            float angelDiff = targetAngle - transform.eulerAngles.y;
            if (angelDiff >= 0)
            {
                //rotate right
                yDirection = Mathf.MoveTowards(yDirection, targetAngle, turningSpeed * Time.deltaTime);

            }
            else
            {
                //rotate left
                yDirection = Mathf.MoveTowards(yDirection, targetAngle - 180, turningSpeed * Time.deltaTime);
            }
                transform.rotation = Quaternion.Euler(0, yDirection, 0);
            
        }
        else
        {
            targetAngle = CameraArm.transform.eulerAngles.y;
        }
            //transform.rotation = Quaternion.Euler(0, targetAngle, 0);

        ////Ok I think i want to put this on the empty for the player character
        ////if left rotate to the left relative to the player camera
        ////if right rotate right relative to the player camera
        //float rotAngle = Quaternion.Angle(transform.rotation, CameraArm.transform.rotation);

        //if (Input.GetAxisRaw("Horizontal") > 0 && rotAngle > rotThreshold)
        //{
            
        //    transform.rotation = Quaternion.Euler(Vector3.up * turningSpeed * Time.deltaTime);
        //    //RotateRight(CameraArm.transform.eulerAngles.y + 90);
        //    //rotate right until
        //    //if (transform.eulerAngles.y < CameraArm.transform.eulerAngles.y) { 
        //    //    yDirection = Mathf.MoveTowards(yDirection, CameraArm.transform.eulerAngles.y + 90, turningSpeed);
        //    //    //yDirection = Mathf.Clamp(yDirection, 0, CameraArm.transform.eulerAngles.y + 90);
        //    //}
        //}
        //else if(Input.GetAxisRaw("Horizontal") > 0 && rotAngle < rotThreshold)
        //{
        //    transform.Rotate(Vector3.up, -turningSpeed * Time.deltaTime);
        //}
        //else if (Input.GetAxisRaw("Horizontal") < 0)
        //{
        //    //RotateLeft(CameraArm.transform.eulerAngles.y - 90);
        //    yDirection = Mathf.MoveTowards(yDirection, CameraArm.transform.eulerAngles.y - 90, turningSpeed);
        //    //yDirection = Mathf.Clamp(yDirection, CameraArm.transform.eulerAngles.y - 90, 0);

        //}
        //if(Input.GetAxisRaw("Vertical") > 0)
        //{
        //    yDirection = Mathf.MoveTowards(yDirection, CameraArm.transform.eulerAngles.y, turningSpeed);
        //}
        //else if(Input.GetAxisRaw("Vertical") < 0)
        //{
        //    yDirection = Mathf.MoveTowards(yDirection, CameraArm.transform.eulerAngles.y + 180, turningSpeed);
        //}
        ////transform.Rotate((Vector3.up * yDirection * Time.deltaTime));

        ////compare euler angles 
        
    }
    
    
    
    void RotateRight(float maxRotation)
    {
        transform.Rotate(Vector3.up * turningSpeed * Time.deltaTime);
        //while (transform.eulerAngles.y < maxRotation)
        //{
        //    transform.Rotate(Vector3.up * turningSpeed * Time.deltaTime);
        //}
    }
    void RotateLeft(float minRotation)
    {
        transform.Rotate(Vector3.up * turningSpeed * Time.deltaTime);
        //while (transform.eulerAngles.y > minRotation)
        //{
        //    transform.Rotate(Vector3.up * turningSpeed * Time.deltaTime);
        //}
    }
}
