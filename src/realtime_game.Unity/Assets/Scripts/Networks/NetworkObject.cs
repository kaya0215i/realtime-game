using DG.Tweening;
using System;
using Unity.Mathematics;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Linq;
using realtime_game.Shared;

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

    // このオブジェクトのデータID
    [SerializeField] public int myObjectDataId;

    // このオブジェクトのデータ
    private ObjectData myObjectData;

    // このオブジェクトの親Transformの名前
    [SerializeField] public string parentTransformName;

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

        myObjectData = GameManager.objectDataSO.objectDataList.FirstOrDefault(_ => _.id == this.myObjectDataId);
        // オブジェとが見つからなければreturn
        if(myObjectData == null) {
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

        myObjectId = await RoomModel.Instance.CreateObjectAsync(myObjectData.id, this.gameObject.transform.position, this.gameObject.transform.rotation, updateType);

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

        if(updateType == UpdateObjectTypes.None) {
            return;
        }

        if(updateType == UpdateObjectTypes.Interactor &&
           !isInteracting) {
            return;
        }

        updateTransformTime += Time.deltaTime;

        if (updateTransformTime >= 0.1f) {
            updateTransformTime = 0;
            await RoomModel.Instance.UpdateObjectTransformAsync(myObjectId, this.gameObject.transform.position, this.gameObject.transform.rotation);
        }
    }

    private async void OnDestroy() {
        if (RoomModel.Instance == null ||
           !GameManager.isJoined ||
           myObjectData == null) {
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
    /// <returns></returns>
    public bool IsCreater() {
        if (!GameManager.isJoined ||
            !isCreater) {
            return false;
        }

        return true;
    }

    /// <summary>
    /// オブジェクトをインタラクト可能であればする
    /// </summary>
    /// <returns></returns>
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
