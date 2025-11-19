using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class UICanvasCameraBinder : MonoBehaviour
{
    private Canvas _canvas;

    private void Awake()
    {
        _canvas = GetComponent<Canvas>();
    }

    private void LateUpdate()
    {
        if (UICameraManager.CurrentCamera != null)
            _canvas.worldCamera = UICameraManager.CurrentCamera;
    }
}
