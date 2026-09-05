using UnityEngine;

/// <summary>
/// Dynamically rotates text/billboards to align perfectly flat with the active Camera view.
/// This acts as a futuristic holographic HUD that is perfectly readable from any spectator angle.
/// </summary>
public class FaceCamera : MonoBehaviour
{
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera != null)
        {
            // Match the view rotation of the camera exactly to keep the text perfectly upright and readable
            transform.rotation = mainCamera.transform.rotation;
        }
    }
}
