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

        // ユーザーの入室通知
        public void OnJoin(JoinedUser user);

        // ユーザーの退室通知
        public void OnLeave(Guid connectionId, int joinOrder);

        // ロビールームの入室通知
        public void OnJoinLoby(JoinedUser user);

        // ロビールームの退室通知
        public void OnLeaveLoby(Guid connectionId, int joinOrder);

        // チームの参加通知
        public void OnJoinTeam(JoinedUser user);

        // チームの退出通知
        public void OnLeaveTeam(Guid connectionId);

        // ユーザーのTransform通知
        public void OnUpdateUserTransform(Guid connectionId, Vector3 pos, Quaternion rotate, Quaternion cameraRotate);

        // オブジェクトの作成通知
        public void OnCreateObject(Guid connectionId, Guid objectId, int objectDataId, Vector3 pos, Quaternion rotate, int updateTypeNum);

        // オブジェクトのInteracterをfalseにする通知
        public void OnFalseObjectInteracting(Guid objectId);

        // オブジェクトの破棄通知
        public void OnDestroyObject(Guid objectId);

        // オブジェクトのTransform通知
        public void OnUpdateObjectTransform(Guid objectId, Vector3 pos, Quaternion rotate);
    }
}
