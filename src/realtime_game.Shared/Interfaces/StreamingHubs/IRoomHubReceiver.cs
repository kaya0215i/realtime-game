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


        /*
         * 
         * ロビー内の通知
         * 
         */


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
        /// チームメンバーにロードアウト変更通知
        /// </summary>
        public void OnChangeLoadout(Guid connectionId, LoadoutData loadoutData);

        /// <summary>
        /// 準備状態通知
        /// </summary>
        public void OnIsReadyStatus(Guid connectionId, bool IsReady);

        /// <summary>
        /// ロビーに帰ってきたとき通知
        /// </summary>
        public void OnReturnedLobyRoom(Guid connectionId);

        /// <summary>
        /// マッチング通知
        /// </summary>
        public void OnMatchingRoom(string roomName);

        /// <summary>
        /// ゲームシーンに移動した通知
        /// </summary>
        public void OnGoGameRoom(Guid connectionId);


        /*
         * 
         * インゲーム内の通知
         * 
         */


        /// <summary>
        /// ユーザーの入室通知
        /// </summary>
        public void OnJoinRoom(JoinedUser user);

        /// <summary>
        /// ユーザーの退室通知
        /// </summary>
        public void OnLeaveRoom(Guid connectionId, int joinOrder);


        /// <summary>
        /// ゲームスタート通知
        /// </summary>
        public void OnGameStart();

        /// <summary>
        /// ゲーム終了通知
        /// </summary>
        public void OnGameEnd();


        /// <summary>
        /// ゲームタイマーの更新通知
        /// </summary>
        public void OnUpdateGameTimer(float timer);


        /// <summary>
        /// プレイヤーのリスポーン通知
        /// </summary>
        public void OnReSpownPlayer(Guid connectionId);

        /// <summary>
        /// プレイヤー死亡通知
        /// </summary>
        public void OnDeathPlayer(Guid connectionId, Guid killerPlayerConnectionId, int deathCauseNum);

        /// <summary>
        /// プレイヤーのヒットパーセント通知
        /// </summary>
        public void OnHitPercent(Guid connectionId, float value);

        /// <summary>
        /// ユーザーのTransform通知
        /// </summary>
        public void OnUpdateUserTransform(Guid connectionId, Vector3 pos, Quaternion rotate, Quaternion cameraRotate);

        /// <summary>
        /// オブジェクトの作成通知
        /// </summary>
        public void OnCreateObject(Guid connectionId, Guid objectId, string objectName, Vector3 pos, Quaternion rotate, int updateTypeNum);

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

        /// <summary>
        /// アニメーション通知(Trigger)
        /// </summary>
        public void OnAnimationTrigger(Guid connectionId, string animName);

        /// <summary>
        /// アニメーション通知(State)
        /// </summary>
        public void OnAnimationState(Guid connectionId, int state);
    }
}
