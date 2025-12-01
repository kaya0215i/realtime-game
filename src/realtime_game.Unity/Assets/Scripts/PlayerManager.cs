using Unity.Cinemachine;
using UnityEngine;

public class PlayerManager : MonoBehaviour {
    private Rigidbody myRb;

    // プレイヤーのTransform
    [SerializeField] private Transform _head;
    [SerializeField] private Transform _body;

    // プレイヤーパラメータ
    [Header("プレイヤーパラメータ")]
    // 移動速度
    [SerializeField] private float moveSpeed;
    //ジャンプ量
    [SerializeField] private float jumpValue;
    // カメラ感度
    [SerializeField] private float _sensX = 5f, _sensY = 5f;

    // プレイヤーのシネマシンカメラ
    [SerializeField] public CinemachineCamera cinemachineCamera;

    // プレイヤーフォローカメラ
    private CinemachineFollow cinemachineFollow;

    // プレイヤーカメラモードの列挙型
    private enum PLAYER_CAMERA_MODE {
        None,
        FPS,
        TPS,
    }

    // プレイヤーカメラモード
    [SerializeField] private PLAYER_CAMERA_MODE playerCameraMode;

    // 移動量
    private float horizontal, vertical;
    // 視点の移動量
    private float _yRotation, _xRotation;


    private void Awake() {
        
    }

    private void Start() {
        myRb = GetComponent<Rigidbody>();
        cinemachineFollow = cinemachineCamera.GetComponent<CinemachineFollow>();

        switch (playerCameraMode) {
            case PLAYER_CAMERA_MODE.FPS:
                cinemachineFollow.TrackerSettings.RotationDamping = new Vector3(0, 0, 0);
                cinemachineFollow.TrackerSettings.PositionDamping = new Vector3(0, 0, 0);
                cinemachineFollow.FollowOffset = new Vector3(0, 0, 0.1f);
                break;

            case PLAYER_CAMERA_MODE.TPS:
                cinemachineFollow.TrackerSettings.RotationDamping = new Vector3(0.2f, 0.2f, 0.2f);
                cinemachineFollow.TrackerSettings.PositionDamping = new Vector3(0.3f, 0.3f, 0.3f);
                cinemachineFollow.FollowOffset = new Vector3(0, 1, -4);
                break;
        }
    }

    private void FixedUpdate() {
        Movement();
    }

    private void Update() {
        Look();

        //IsField();

        if (Input.GetKeyDown(KeyCode.Space)) {
            Jump();
        }
    }

    /// <summary>
    /// 移動処理
    /// </summary>
    private void Movement() {
        horizontal = Input.GetAxis("Horizontal") * Time.fixedDeltaTime * moveSpeed;
        vertical = Input.GetAxis("Vertical") * Time.fixedDeltaTime * moveSpeed;

        this.transform.Translate(horizontal, 0, vertical);
    }

    /// <summary>
    /// 視点処理
    /// </summary>
    private void Look() {
        Vector2 mouseInput = new Vector2(Input.GetAxis("Mouse X") * _sensX,
            Input.GetAxis("Mouse Y") * _sensY);

        _xRotation -= mouseInput.y;
        _yRotation += mouseInput.x;
        _yRotation %= 360; // 絶対値が大きくなりすぎないように

        // 上下の視点移動量をClamp
        _xRotation = Mathf.Clamp(_xRotation, -70, 70);

        // 頭、体の向きの適用
        if (_head != null) {
            _head.transform.localRotation = Quaternion.Euler(_xRotation, 0, 0);
        }
        if (_body != null) {
            _body.transform.localRotation = Quaternion.Euler(0, _yRotation, 0);
        }
    }

    /// <summary>
    /// ジャンプ処理
    /// </summary>
    private void Jump() {
        if (IsField()) {
            myRb.AddForce(Vector3.up * jumpValue, ForceMode.Impulse);
        }
    }

    /// <summary>
    /// 地面についているか
    /// </summary>
    /// <returns>地面についていればTrue, ついていなければFalseを返す</returns>
    private bool IsField() {
        foreach (RaycastHit rayHit in Physics.RaycastAll(transform.position - new Vector3(0, 0.7f, 0), Vector3.down, 0.3f)) {
            if (rayHit.collider.gameObject.CompareTag("Field")) {
                return true;
            }
        }
        return false;
    }
}
