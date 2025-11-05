using UnityEngine;
using UnityEngine.SceneManagement;
public class GameManager : MonoBehaviour
{
    [SerializeField] int botCount = 5;
    public int animatronicCount = 0; //starts at zero, increment when players join

    public bool securityGuardAlive = true;
    // Player1, Player2, Player3, and Player4 respectively
    public bool[] aliveAnimatronics = { true, true, true, true };
    public int[] animatronicStun = { 0, 0, 0, 0 };
    public int[] animatronicHealth = { 3, 3, 3, 3 };
    public bool[] aliveSecurityBots = { };
    public bool gunArrived = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for(int i = 0; i < botCount; i++)
        {
//            aliveSecurityBots[i] = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (animatronicCount == 0 || !securityGuardAlive) { gameOver(); }
        if (gunArrived)
        {
            //releases control of the security main player. Shuts off the bots, and enters the endgame.
        }
    }

    void gameOver()
    {
        //display gameOver UI
        //should display something cool hopefully
        //when continue is clicked, load the main menu/staging scene
        //SceneManager.LoadScene("YourSceneName"); 
    }
}
