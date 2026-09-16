using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class ClickDragCam : MonoBehaviour
{
    private Camera mainCamera;
    private Vector3 dragOrigin;
    private InputSystem_Actions inputActions;
    private InputAction scrollAction;
    private InputAction middleMouseButtonAction;

    private InputAction wasdMovementAction;
    private float pendingScrollDelta;
    [SerializeField] float scrollSpeed;
    [SerializeField] float minCamSize;
    [SerializeField] float maxCamSize;
    [SerializeField] float camMoveSpeed;

    void Awake()
    {
        mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
        {
            Debug.LogError("Main camera not found.");
        }

        inputActions = new InputSystem_Actions();
        scrollAction = inputActions.Player.Scroll;
        middleMouseButtonAction = inputActions.Player.MiddleMouseButton;
        wasdMovementAction = inputActions.Player.CameraMovement;
    }

    void OnEnable()
    {
        scrollAction.performed += OnScrollPerformed;
        inputActions?.Player.Enable();
    }

    void OnDisable()
    {
        scrollAction.performed -= OnScrollPerformed;
        inputActions?.Player.Disable();
    }

    void OnDestroy()
    {
        inputActions?.Dispose();
    }

    void Update()
    {
        if (mainCamera == null)
        {
            return;
        }

        float scroll = pendingScrollDelta * scrollSpeed;
        pendingScrollDelta = 0f; //reset the pending delta
        if (Mathf.Abs(scroll) > 0.01f)
        {
            mainCamera.orthographicSize -= scroll;
            mainCamera.orthographicSize = Mathf.Clamp(mainCamera.orthographicSize, minCamSize, maxCamSize);
        }

        if (Mouse.current == null)
        {
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        if (middleMouseButtonAction.WasPressedThisFrame())
        {
            dragOrigin = mainCamera.ScreenToWorldPoint(mousePosition);
        }

        if (middleMouseButtonAction.IsPressed())
        {
            Vector3 difference = dragOrigin - mainCamera.ScreenToWorldPoint(mousePosition);
            mainCamera.transform.position += difference;
        }

        if(wasdMovementAction.ReadValue<Vector2>().magnitude != 0f)
        {
            mainCamera.transform.position += (Vector3)wasdMovementAction.ReadValue<Vector2>().normalized * camMoveSpeed * Time.deltaTime;
        }
    }

    private void OnScrollPerformed(InputAction.CallbackContext context)
    {
        float deltaY = context.ReadValue<Vector2>().y;
        pendingScrollDelta += deltaY;
    }

    public void PlaceCamera(Vector2 position)
    {
        //reset the camera's position (to the squad leader's transform, once that system exists)
        mainCamera.transform.position = new Vector3(position.x, position.y, mainCamera.transform.position.z); //z-coordinate never changes
    }
}
