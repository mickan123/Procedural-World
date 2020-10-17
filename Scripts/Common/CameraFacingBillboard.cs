using UnityEngine;
 
public class CameraFacingBillboard : MonoBehaviour
{
    public Camera m_Camera;

    public void SetCamera(Camera camera) {
        this.m_Camera = camera;
    }
 
    void Update()
    {
        if (m_Camera != null) {
            transform.LookAt(transform.position + m_Camera.transform.rotation * Vector3.forward,
                m_Camera.transform.rotation * Vector3.up);
        }
    }
}
