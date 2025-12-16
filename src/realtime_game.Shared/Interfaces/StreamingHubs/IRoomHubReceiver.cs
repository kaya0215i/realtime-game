using realtime_game.Shared.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace realtime_game.Shared.Interfaces.StreamingHubs {
    /// <summary>
    /// サーバーからクライアントへの通知関連
    /// </summary>
    public interface IRoomHubReceiver {
        // [クライアントに実装]
        // [サーバーから呼び出す]

        /// <summary>
        /// ユーザーの入室通知
        /// </summary>
        public void OnJoinRoom(JoinedUser user);

        /// <summary>
        /// ユーザーの退室通知
        /// </summary>
        public void OnLeaveRoom(Guid connectionId, int joinOrder);

        /// <summary>
        /// ロビールームの入室通知
        /// </summary>
        public void OnJoinLoby(JoinedUser user);

        /// <summary>
        /// ロビールームの退室通知
        /// </summary>
        public void OnLeaveLoby(Guid connectionId, int joinOrder);

        /// <summary>
        /// チームの参加通知
        /// </summary>
        public void OnJoinTeam(JoinedUser user);

        /// <summary>
        /// チームの退出通知
        /// </summary>
        public void OnLeaveTeam(Guid connectionId);

        /// <summary>
        /// チームに招待通知
        /// </summary>
        public void OnInviteTeam(Guid teamId, User senderUser);

        /// <summary>
        /// 準備状態通知
        /// </summary>
        public void OnIsReadyStatus(Guid connectionId, bool IsReady);

        /// <summary>
        /// マッチング通知
        /// </summary>
        public void OnMatchingRoom(string roomName);

        /// <summary>
        /// ユーザーのTransform通知
        /// </summary>
        public void OnUpdateUserTransform(Guid connectionId, Vector3 pos, Quaternion rotate, Quaternion cameraRotate);

        /// <summary>
        /// オブジェクトの作成通知
        /// </summary>
        public void OnCreateObject(Guid connectionId, Guid objectId, int objectDataId, Vector3 pos, Quaternion rotate, int updateTypeNum);

        /// <summary>
        /// オブジェクトのInteracterをfalseにする通知
        /// </summary>
        public void OnFalseObjectInteracting(Guid objectId);

        /// <summary>
        /// オブジェクトの破棄通知
        /// </summary>
        public void OnDestroyObject(Guid objectId);

        /// <summary>
        /// オブジェクトのTransform通知
        /// </summary>
        public void OnUpdateObjectTransform(Guid objectId, Vector3 pos, Quaternion rotate);
    }
}
