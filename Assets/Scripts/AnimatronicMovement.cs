using UnityEngine;


public class AnimatronicMovement : MonoBehaviour
{
    [SerializeField] float maxSpeed;
    [SerializeField] float acceleration;
    float momentum;
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        //basically we want to head in the direction that the camera is facing when moving forward
        //wsad increases momentum in any direction and changes the direction the character is facing relative to the camera
        //can do this by adding the input value of a and d to the rotation of the player

        transform.Translate(Vector3.forward * momentum * Time.deltaTime);
        if (Input.GetAxis("Vertical") > 0.1f)
        {
            momentum = Mathf.MoveTowards(momentum, maxSpeed, acceleration * Time.deltaTime);
        }
        if (Input.GetAxis("Vertical") < -0.1f)
        {
            momentum = Mathf.MoveTowards(momentum, -maxSpeed, acceleration * Time.deltaTime);
        }
        else
        {
            momentum = Mathf.MoveTowards(momentum, 0, acceleration * Time.deltaTime * 1 / 2);
        }

        if (Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f)
        {
            transform.Rotate(Vector3.up * Input.GetAxis("Horizontal") * 200 * Time.deltaTime);
        }
    }
}
