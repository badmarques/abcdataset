using UnityEngine;

public class VirtualTrackball : MonoBehaviour
{
    public Vector3 targetPosition = Vector3.zero;
    [Range(0.1f, 10f)]
    [Tooltip("How sensitive the mouse drag to camera rotation")]
    public float mouseRotateSpeed = 5.0f;
    [Tooltip("Smaller positive value means smoother rotation, 1 means no smooth apply")]
    public float slerpValue = 0.95f;

    private Quaternion cameraRot; // store the quaternion after the slerp operation
    private float distanceBetweenCameraAndTarget;

    private float minXRotAngle = -80; // min angle around x axis
    private float maxXRotAngle = 80;  // max angle around x axis

    // Mouse rotation related
    private float rotX; // around x
    private float rotY; // around y

    // Start is called before the first frame update
    void Start()
    {
        distanceBetweenCameraAndTarget = Vector3.Distance(transform.position, targetPosition);
        cameraRot = transform.rotation;
        rotX = transform.localEulerAngles.x;
        rotY = transform.localEulerAngles.y;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            rotX += -Input.GetAxis("Mouse Y") * mouseRotateSpeed; // around X
            rotY += Input.GetAxis("Mouse X") * mouseRotateSpeed;
        }

        if (rotX < minXRotAngle)
        {
            rotX = minXRotAngle;
        }
        else if (rotX > maxXRotAngle)
        {
            rotX = maxXRotAngle;
        }
    }

    private void LateUpdate()
    {
        Vector3 dir = new Vector3(0, 0, -distanceBetweenCameraAndTarget); //assign value to the distance between the camera and the target

        // value equal to the delta change of our mouse or touch position
        Quaternion newQ = Quaternion.Euler(rotX, rotY, 0); //We are setting the rotation around X, Y, Z axis respectively
        cameraRot = Quaternion.Slerp(cameraRot, newQ, slerpValue);  //let cameraRot value gradually reach newQ which corresponds to our touch
        transform.position = targetPosition + cameraRot * dir;
        transform.LookAt(targetPosition);
    }

    public void SetCamPos()
    {
        transform.position = new Vector3(0, 0, -distanceBetweenCameraAndTarget);
    }
}