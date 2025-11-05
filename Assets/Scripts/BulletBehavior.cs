using UnityEngine;

public class BulletBehavior : MonoBehaviour
{

    [SerializeField] float speed;
    [SerializeField] float bulletLife;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        Destroy(gameObject, bulletLife);
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.CompareTag("Animatronic"))
        {
            this.enabled = false;
            Destroy(gameObject);
        }
    }
}