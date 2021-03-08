using UnityEngine;
using static UnityEngine.Vector3;

sealed class CameraController : MonoBehaviour
{
    #region Parameters
    [SerializeField] private float cameraSpeed = 2.0f;

    public static CameraController Instance { get; private set; }
    #endregion

    #region MonoBehaviour API
    private void Awake()
    {
        #region Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
        #endregion
    }

    private void Update()
    {
        MoveCamera();
    }
    #endregion

    #region Custom methods
    private void MoveCamera()
    {
        transform.position = Lerp(
            transform.position,
            PlayerController.Instance.transform.position,
            cameraSpeed * Time.deltaTime);
    }
    #endregion
}
