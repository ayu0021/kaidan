using UnityEngine;

/// <summary>
/// WASD 指到哪，箭頭就朝哪；保持平躺（鎖 X/Z 只轉 Y）
/// 並可指定「哪個 local 軸」要面向移動方向（例如用 -Z 當前方）。
/// </summary>
public class BPlayerArrowFacing : MonoBehaviour
{
    public enum FacingAxis
    {
        PositiveZ,
        NegativeZ,
        PositiveX,
        NegativeX
    }

    [Header("Target")]
    [Tooltip("要轉向的箭頭物件。不填就用自己")]
    public Transform arrow;

    [Header("Input")]
    public KeyCode up = KeyCode.W;
    public KeyCode down = KeyCode.S;
    public KeyCode left = KeyCode.A;
    public KeyCode right = KeyCode.D;

    [Header("Direction Space")]
    [Tooltip("勾選：W=相機前方。不勾：W=世界 +Z")]
    public bool cameraRelative = false;

    [Tooltip("相機參考（不填會用 Camera.main）")]
    public Transform cameraTransform;

    [Header("Which local axis faces move direction")]
    [Tooltip("想要 Z 軸反邊永遠面向移動方向 => 選 NegativeZ")]
    public FacingAxis facingAxis = FacingAxis.NegativeZ;

    [Header("Rotation")]
    [Tooltip("旋轉平滑速度（越大越快）。設很大≈瞬轉")]
    public float rotateSpeed = 18f;

    [Tooltip("額外的 Y 角度微調（方向差一點點再用）")]
    public float yawOffset = 0f;

    [Header("Keep Flat")]
    [Tooltip("鎖住 X/Z，只讓 Y 旋轉（平躺貼地）")]
    public bool keepFlat = true;

    [Tooltip("keepFlat=true 時：使用啟動時箭頭的 X/Z 當作固定平躺角度")]
    public bool useInitialXZAsFlat = true;

    [Tooltip("若不使用初始角度：手動指定固定的 X/Z（例如 X=90, Z=0）")]
    public Vector2 fixedXZ = new Vector2(90f, 0f);

    float _flatX, _flatZ;

    void Reset()
    {
        arrow = transform;
    }

    void Awake()
    {
        if (arrow == null) arrow = transform;
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;

        if (useInitialXZAsFlat)
        {
            Vector3 e = arrow.eulerAngles;
            _flatX = e.x;
            _flatZ = e.z;
        }
        else
        {
            _flatX = fixedXZ.x;
            _flatZ = fixedXZ.y;
        }
    }

    void Update()
    {
        Vector2 input = ReadWASD();
        if (input.sqrMagnitude < 0.0001f) return;

        Vector3 dir = GetDirection(input);
        if (dir.sqrMagnitude < 0.0001f) return;

        // 世界方向 -> 水平角（Y 軸）
        float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;

        // 讓指定的「local 軸」對準移動方向
        yaw += AxisYawOffset(facingAxis);

        // 你的額外微調
        yaw += yawOffset;

        Quaternion target = keepFlat
            ? Quaternion.Euler(_flatX, yaw, _flatZ)
            : Quaternion.Euler(0f, yaw, 0f);

        arrow.rotation = Quaternion.Slerp(
            arrow.rotation,
            target,
            1f - Mathf.Exp(-rotateSpeed * Time.deltaTime)
        );
    }

    static float AxisYawOffset(FacingAxis axis)
    {
        // yaw 0° 時，物件的 +Z 會朝世界 +Z。
        // 如果你要 -Z 朝世界 +Z，就要加 180°。
        return axis switch
        {
            FacingAxis.PositiveZ => 0f,
            FacingAxis.NegativeZ => 180f,
            FacingAxis.PositiveX => 90f,
            FacingAxis.NegativeX => -90f,
            _ => 0f
        };
    }

    Vector3 GetDirection(Vector2 input)
    {
        if (cameraRelative && cameraTransform != null)
        {
            Vector3 f = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            Vector3 r = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            return (f * input.y + r * input.x);
        }

        // 世界座標：W=+Z、D=+X
        return new Vector3(input.x, 0f, input.y);
    }

    Vector2 ReadWASD()
    {
        float x = 0f, y = 0f;
        if (Input.GetKey(left))  x -= 1f;
        if (Input.GetKey(right)) x += 1f;
        if (Input.GetKey(down))  y -= 1f;
        if (Input.GetKey(up))    y += 1f;

        Vector2 v = new Vector2(x, y);
        if (v.sqrMagnitude > 1f) v.Normalize();
        return v;
    }
}


