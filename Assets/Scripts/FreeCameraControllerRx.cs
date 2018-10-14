using UniRx;
using UniRx.Triggers;
using UnityEngine;

public class FreeCameraControllerRx : MonoBehaviour
{
    [Header("Speed Settings")]
    public float speed = 10f;
    public float fastSpeed = 80f;
    public float rollSpeed = 80f;
    public float fastRollSpeed = 150f;
    public Vector2 mouseSensitivity = new Vector2(100f, 100f);

    [Header("Key Bindings")]
    public string moveRightAxis = "Horizontal";
    public string moveForwardAxis = "Vertical";
    public KeyCode moveUpKey = KeyCode.Space;
    public KeyCode moveDownKey = KeyCode.C;
    public string lookRightAxis = "Mouse X";
    public string lookUpAxis = "Mouse Y";
    public KeyCode rollLeft = KeyCode.Q;
    public KeyCode rollRight = KeyCode.E;
    public KeyCode fastKey = KeyCode.LeftShift;
    public KeyCode fastLockKey = KeyCode.CapsLock;
    public KeyCode haltKey = KeyCode.Escape;

    private float currentMoveSpeed;
    private float currentRollSpeed;

    private ReactiveProperty<bool> FastLock = new ReactiveProperty<bool>(false);

    private void Start()
    {
        IObservable<Unit> keyDownStream = KeyDownStream();

        // Modifiers
        keyDownStream
            .Where(_ => Input.GetKeyDown(haltKey))
            .Subscribe(_ => Debug.Break());
        keyDownStream
            .Where(_ => Input.GetKeyDown(fastLockKey))
            .Subscribe(_ => FastLock.Value = !FastLock.Value);
        FastKeyStream()
            .CombineLatest(FastLock, (fast, fastLock) => fast ^ fastLock)
            .Subscribe(fast =>
            {
                currentMoveSpeed = fast ? fastSpeed : speed;
                currentRollSpeed = fast ? fastRollSpeed : rollSpeed;
            });

        // Mouse look and roll rotation
        this.UpdateAsObservable()
            .Select(_ => GetLookRotation())
            .Where(v => !Mathf.Approximately(v.sqrMagnitude, 0))
            .Subscribe(look =>
            {
                transform.rotation *= Quaternion.Euler(Vector3.Scale(look, new Vector3(mouseSensitivity.y, mouseSensitivity.x, currentRollSpeed) * Time.deltaTime));
            });

        // Movement
        KeyPressStream()
            .Select(_ => GetMovement())
            .Where(v => !Mathf.Approximately(v.sqrMagnitude, 0))
            .Subscribe(movement =>
            {
                transform.Translate(movement * Time.deltaTime * currentMoveSpeed);
            });
    }

    /// <summary>
    /// Streams its value on the first frame when the fastKey is changed
    /// </summary>
    /// <returns></returns>
    private IObservable<bool> FastKeyStream()
    {
        return this.UpdateAsObservable()
            .Select(_ => Input.GetKey(fastKey))
            .StartWith(false)
            .DistinctUntilChanged();
    }

    /// <summary>
    /// Streams the first frame when any keyboard or mouse button is pressed
    /// </summary>
    /// <returns></returns>
    private IObservable<Unit> KeyDownStream()
    {
        return this.UpdateAsObservable().Where(_ => Input.anyKeyDown);
    }

    /// <summary>
    /// Streams every frame while any keyboard or mouse button is held down
    /// </summary>
    /// <returns></returns>
    private IObservable<Unit> KeyPressStream()
    {
        return this.UpdateAsObservable().Where(_ => Input.anyKey);
    }

    /// <summary>
    /// Get the look rotation vector for this frame from mouse/keyboard input
    /// </summary>
    /// <returns></returns>
    private Vector3 GetLookRotation()
    {
        float yaw = Input.GetAxisRaw(lookRightAxis);
        float pitch = -1 * Input.GetAxisRaw(lookUpAxis);
        float roll = ((Input.GetKey(rollLeft) ? 1 : 0) + (Input.GetKey(rollRight) ? -1 : 0));
        return new Vector3(pitch, yaw, roll);
    }

    /// <summary>
    /// Get the movement vector for this frame from keyboard input
    /// </summary>
    /// <returns></returns>
    private Vector3 GetMovement()
    {
        float up = ((Input.GetKey(moveUpKey) ? 1 : 0) + (Input.GetKey(moveDownKey) ? -1 : 0));
        float right = Input.GetAxisRaw(moveRightAxis);
        float forward = Input.GetAxisRaw(moveForwardAxis);
        return new Vector3(right, up, forward).normalized;
    }
}
