using UnityEngine;
using System.Collections;
using UnityEngine.UIElements;
//using System.Numerics;

public class ShotgunOLD : MonoBehaviour
{
    private GameManager gameManager;
    bool canShoot = true;
    [SerializeField] GameObject bullet;
    [SerializeField] int bulletCount;

    public Transform firePoint;
    public Rotate fireRotation;
    [SerializeField] float spreadY;
    [SerializeField] float spreadZ;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameManager = FindAnyObjectByType<GameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Fire1") && canShoot) { SpawnBullet(); }
        if (gameManager.gunArrived)
        {
            //if (Input.GetButtonDown("Fire1") && canShoot) { SpawnBullet(); }
        }
    }

    private void SpawnBullet()
    {
        for(int i = 0; i < bulletCount; i++)
        {
            Vector3 currentRotation = firePoint.eulerAngles;
            Quaternion newRotation = Quaternion.Euler(Random.Range(currentRotation.z - spreadZ, currentRotation.z + spreadZ), Random.Range(currentRotation.y - spreadY, currentRotation.y + spreadY), Random.Range(currentRotation.z - spreadZ, currentRotation.z + spreadZ));
            Instantiate(bullet, firePoint.position, newRotation);
        }
        StartCoroutine(Cooldown());
    }

    private IEnumerator Cooldown()
    {
        canShoot = false;
        yield return new WaitForSeconds(1f);
        canShoot = true;
    }
}
