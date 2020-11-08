using System.Collections.Specialized;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform lookFrom;
    public float followDistance;
    public float mouseSensitivity = 1.0f;

    private float yaw;
    private float pitch;
    private float roll;
    private float x;
    private float y;
    private float z;

    void OnEnable()
    {
        SetFromTransform(transform);
    }

    public void SetFromTransform(Transform t)
    {
        pitch = t.eulerAngles.x;
        yaw = t.eulerAngles.y;
        roll = t.eulerAngles.z;
        x = t.position.x;
        y = t.position.y;
        z = t.position.z;
    }

    public void SetPosition(Vector3 position)
    {
        x = position.x;
        y = position.y;
        z = position.z;
    }

    public void UpdateTransform(Transform t)
    {
        t.eulerAngles = new Vector3(pitch, yaw, roll);
        t.position = new Vector3(x, y, z);
    }

    public void Move()
    {
        Vector3 cameraPos = new Vector3(0.0f, 0.0f, -1.0f);
        lookFrom.transform.eulerAngles = new Vector3(pitch, yaw, roll);
        SetPosition(lookFrom.TransformPoint(cameraPos * followDistance));
        UpdateTransform(transform);
    }

    public void Look(Vector2 mouseMovement)
    {
        yaw += mouseMovement.x * mouseSensitivity;
        pitch -= mouseMovement.y * mouseSensitivity;

        pitch = Mathf.Clamp(pitch, -90.0f, 90.0f);
        yaw = Mathf.Repeat(yaw, 360.0f);

        Move();
    }

    void Update()
    {

    }
}