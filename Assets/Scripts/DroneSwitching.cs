using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class DroneSwitching : MonoBehaviour
{
    public TextMeshProUGUI currentCamera;
    [SerializeField] RenderTexture[] CamViews;
    public RawImage povCam;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void swapCam(int renderTextureIndex)
    {
        string cameraTitle = "";
        switch (renderTextureIndex) {
            case 0: cameraTitle = "Supply Closet";
                break;
            case 1:
                cameraTitle = "Backstage";
                break;
            case 2:
                cameraTitle = "Kitchen";
                break;
            case 3:
                cameraTitle = "Restrooms";
                break;
            case 4:
                cameraTitle = "L Hall";
                break;
            case 5:
                cameraTitle = "Marionette Room";
                break;
            case 6:
                cameraTitle = "Stage";
                break;
            case 7:
                cameraTitle = "Dining Area";
                break;
            case 8:
                cameraTitle = "Pirate Cove";
                break;
            case 9:
                cameraTitle = "West Hall";
                break;
            case 10:
                cameraTitle = "West Hall Corner";
                break;
            case 11:
                cameraTitle = "East Hall";
                break;
            case 12:
                cameraTitle = "East Hall Corner";
                break;
        }

        if (renderTextureIndex == -1)
        {
            povCam.gameObject.SetActive(false);
            currentCamera.SetText("");
        }
        else
        {
            currentCamera.SetText(cameraTitle);
            povCam.gameObject.SetActive(true);
            povCam.texture = CamViews[renderTextureIndex];
        }
    }
}
