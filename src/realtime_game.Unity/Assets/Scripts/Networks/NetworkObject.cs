using DG.Tweening;
using System;
using UnityEngine;

public class NetworkObject : MonoBehaviour {
    private GameManager gameManager;
    private RoomModel roomModel;
    [HideInInspector] public Guid myObjectId;

    [HideInInspector] public bool sendDestroyMessage;
    private float updateTransformTime;

    private void Awake() {
        sendDestroyMessage = true;
        updateTransformTime = 0;
    }

    private async void Start() {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        roomModel = gameManager.GetComponent<RoomModel>();

        if(myObjectId != Guid.Empty) {
            return;
        }

        Debug.Log("オブジェクトを作成");
        myObjectId = await roomModel.CreateObjectAsync(this.gameObject.transform.position, this.gameObject.transform.rotation, this.gameObject);

        if(myObjectId == Guid.Empty) {
            enabled = false;
            Debug.Log(this.gameObject.name + "をサーバー上で作成出来ませんでした。");
        }
    }

    private async void Update() {
        if (!gameManager.isJoined ||
            myObjectId == Guid.Empty) {
            return;
        }

        updateTransformTime += Time.deltaTime;

        if (updateTransformTime >= 0.1f) {
            updateTransformTime = 0;
            await roomModel.UpdateObjectTransformAsync(myObjectId, this.gameObject.transform.position, this.gameObject.transform.rotation);
        }
    }

    private async void OnDestroy() {
        DOTween.Kill(this.gameObject);
        if (sendDestroyMessage) {
            Debug.Log("削除");
            gameManager.RemoveObjectList(myObjectId);
            await roomModel.DestroyObjectAsync(myObjectId);
        }
    }

    private void OnTriggerEnter(Collider other) {
        
    }

    private void OnTriggerExit(Collider other) {
        
    }
}
