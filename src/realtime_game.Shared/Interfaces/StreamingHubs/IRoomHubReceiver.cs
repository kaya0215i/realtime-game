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

        // ユーザーの退出通知
        public void OnLeave(Guid connectionId);

        // ユーザーのTransform通知
        public void OnUpdateTransform(Guid connectionId, Vector3 pos, Quaternion rotate);

        // オブジェクトの作成通知
        public void OnCreateObject(Guid connectionId, Guid objectId, Vector3 pos, Quaternion rotate);

        // オブジェクトの破棄通知
        public void OnDestroyObject(Guid connectionId, Guid objectId);

        // オブジェクトのTransform通知
        public void OnUpdateObjectTransform(Guid connectionId, Guid objectId, Vector3 pos, Quaternion rotate);
    }
}
