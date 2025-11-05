using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEditor.Build.Content;
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
        if (renderTextureIndex == -1)
        {
            povCam.gameObject.SetActive(false);
            currentCamera.SetText("");
        }
        else
        {
            currentCamera.SetText("Camera " + (renderTextureIndex + 1));
            povCam.gameObject.SetActive(true);
            povCam.texture = CamViews[renderTextureIndex];
        }
    }
}
