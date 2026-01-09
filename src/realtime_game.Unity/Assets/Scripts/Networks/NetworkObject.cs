using DG.Tweening;
using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Linq;

public class NetworkObject : MonoBehaviour {
    public GameManager GameManager { private set; get; }
    private Rigidbody myRb;

    // このオブジェクトのID
    [NonSerialized] public Guid myObjectId;

    // このオブジェクトの作成者のコネクションID
    [NonSerialized] public Guid createrConnectionId;

    // 削除通知を送るかどうか
    [NonSerialized] public bool sendDestroyMessage;

    // 経過時間タイマー
    private float updateTransformTime;

    // このオブジェクトのデータ
    private GameObject myObject;

    // このオブジェクトの親Transformの名前
    [SerializeField] public string parentTransformName;

    // Transform情報を送る間隔
    private const float SEND_TRANSFORM_INTERVAL = 0.1f;

    // 更新タイプの列挙型
    public enum UpdateObjectTypes {
        None = 0,
        Creater,
        Interactor,
    }
    // 更新タイプ
    public UpdateObjectTypes updateType = UpdateObjectTypes.None;

    public bool isInteracting { get; private set; }
    public bool isCreater { get; private set; }

    private void Awake() {
        sendDestroyMessage = true;
        isInteracting = false;
        isCreater = false;
        updateTransformTime = 0;
    }

    private async void Start() {
        GameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        myRb = this.GetComponent<Rigidbody>();

        myObject = GameManager.objectDataSO.objectDataList.FirstOrDefault(_ => this.gameObject.name.StartsWith(_.gameObject.name));
        // オブジェとが見つからなければreturn
        if(myObject == null) {
            Debug.Log($"{this.gameObject.name} : オブジェクトがSOから見つかりませんでした");
            Destroy(this.gameObject);
            return;
        }

        // 作成者でなければreturn
        if (myObjectId != Guid.Empty) {
            return;
        }

        createrConnectionId = GameManager.mySelf.ConnectionId;

        Debug.Log("オブジェクトを作成");

        isCreater = true;

        switch (updateType) {
            case UpdateObjectTypes.Creater:
            case UpdateObjectTypes.Interactor:
                isInteracting = true;
                break;
        }

        myObjectId = await RoomModel.Instance.CreateObjectAsync(myObject.name, this.gameObject.transform.position, this.gameObject.transform.rotation, updateType);

        if(myObjectId == Guid.Empty) {
            enabled = false;
            Debug.Log(this.gameObject.name + "をサーバー上で作成出来ませんでした。");
            return;
        }

        if(this != null) {
            GameManager.AddObjectList(myObjectId, this.gameObject);
        }
    }

    private async void Update() {
        if (!GameManager.isJoined ||
            myObjectId == Guid.Empty) {
            return;
        }

        if (updateType == UpdateObjectTypes.None) {
            return;
        }
        else if (updateType == UpdateObjectTypes.Creater &&
                 !IsCreater()) {
            return;
        }
        else if (updateType == UpdateObjectTypes.Interactor &&
                 !isInteracting) {
            return;
        }

        updateTransformTime += Time.deltaTime;

        if (updateTransformTime >= SEND_TRANSFORM_INTERVAL) {
            updateTransformTime = 0;
            await RoomModel.Instance.UpdateObjectTransformAsync(myObjectId, this.gameObject.transform.position, this.gameObject.transform.rotation);
        }
    }

    private async void OnDestroy() {
        if (RoomModel.Instance == null ||
            GameManager == null ||
           !GameManager.isJoined ||
           myObject == null) {
            return;
        }

        DOTween.Kill(this.gameObject);
        if (sendDestroyMessage) {
            Debug.Log("削除");
            GameManager.RemoveObjectList(myObjectId);
            await RoomModel.Instance.DestroyObjectAsync(myObjectId);
        }
    }

    /// <summary>
    /// 作成者かどうか
    /// </summary>
    public bool IsCreater() {
        if (!GameManager.isJoined ||
            !isCreater) {
            return false;
        }

        return true;
    }

    /// <summary>
    /// オブジェクトをインタラクト可能であればする
    /// </summary>v
    public async UniTask<bool> InteractObject() {
        if(RoomModel.Instance == null ||
           !GameManager.isJoined) {
            return false;
        }


        bool isInteract = await RoomModel.Instance.InteractObjectAsync(myObjectId);

        if (isInteract) {
            Debug.Log("インタラクト成功");
            isInteracting = true;
        }
        else {
            Debug.Log("インタラクト失敗");
        }

        return isInteract;
    }

    /// <summary>
    /// オブジェクトを手放す
    /// </summary>
    public async void DisInteractObject() {
        if (RoomModel.Instance == null ||
           !GameManager.isJoined) {
            return;
        }

        myRb.linearVelocity = Vector3.zero;
        await RoomModel.Instance.DisInteractObjectAsync(myObjectId);
    }

    /// <summary>
    /// interactingをfalseにする
    /// </summary>
    public void FalseIsInteracting() {
        Debug.Log("Interacterが他のプレイヤーに変わった");
        isInteracting = false;
    }
}
