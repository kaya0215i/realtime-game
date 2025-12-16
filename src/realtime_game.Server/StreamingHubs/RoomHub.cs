using MagicOnion;
using MagicOnion.Server.Hubs;
using Microsoft.EntityFrameworkCore;
using realtime_game.Server.Models.Contexts;
using realtime_game.Shared.Interfaces.StreamingHubs;
using realtime_game.Shared.Models.Entities;
using System.Collections.Generic;
using UnityEngine;


namespace realtime_game.Server.StreamingHubs {
    public class RoomHub : StreamingHubBase<IRoomHub, IRoomHubReceiver>, IRoomHub {
        private readonly RoomContextRepository _roomContextRepository;
        private readonly GameDbContext _dbContext;

        private RoomContext? _roomContext;

        public RoomHub(RoomContextRepository roomContextRepository, GameDbContext dbContext) {
            _roomContextRepository = roomContextRepository;
            _dbContext = dbContext;
        }

        /// <summary>
        /// コンテキストを取得
        /// </summary>
        private void GetContext(string roomName) {
            // コンテキストを取得
            this._roomContext = this._roomContextRepository.GetContext(roomName);
            if (_roomContext == null ||
                !_roomContext.RoomUserDataList.ContainsKey(this.ConnectionId)) {
                throw new ReturnStatusException(Grpc.Core.StatusCode.NotFound, "Context Not Found.");
            }
        }

        /// <summary>
        /// ルームに接続
        /// </summary>
        public async Task<JoinedUser[]> JoinRoomAsync(string roomName, int userId) {
            // 同時に生成しない用に排他制御
            lock (_roomContextRepository) {
                // 指定の名前のルームがあるかどうかを確認
                this._roomContext = _roomContextRepository.GetContext(roomName);
                if (this._roomContext == null) {
                    // なかったら生成

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("CreateRoom : ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(roomName);

                    this._roomContext = _roomContextRepository.CreateContext(roomName);
                }
            }

            // ルームに参加 ＆ ルームを保持
            this._roomContext.Group.Add(this.ConnectionId, Client);

            // DBからユーザー情報取得
            User user = await _dbContext.Users.FirstAsync(user => user.Id == userId);

            // 入室済みユーザーのデータを作成
            var joinedUser = new JoinedUser();
            joinedUser.ConnectionId = this.ConnectionId;
            joinedUser.UserData = user;

            // 同時に作成しない用に排他制御
            lock (_roomContext) {
                joinedUser.JoinOrder = this._roomContext.RoomUserDataList.Count + 1;

                // ルーム内に同じユーザーIDの人がいたら
                foreach(var roomUser in _roomContext.RoomUserDataList.Values) {
                    if (roomUser.JoinedUser.UserData.Id == userId) {
                        // ルーム内のメンバーから自分を削除
                        this._roomContext.Group.Remove(this.ConnectionId);
                        JoinedUser[] nonUser = Array.Empty<JoinedUser>();
                        return nonUser;
                    }
                }

                // ルームコンテキストにユーザー情報を登録
                var roomUserData = new RoomUserData() { JoinedUser = joinedUser };
                this._roomContext.RoomUserDataList[this.ConnectionId] = roomUserData;

                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Write("JoinRoom : ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(roomName +
                    ", ID : " + roomUserData.JoinedUser.UserData.Id +
                    ", Player : " + roomUserData.JoinedUser.UserData.Display_Name +
                    ", ConnectionID : " + roomUserData.JoinedUser.ConnectionId +
                    ", JoinOrder : " + roomUserData.JoinedUser.JoinOrder);
            }

            // 自分以外のルーム参加者全員に、ユーザーの入室通知を送信
            this._roomContext.Group.Except([this.ConnectionId]).OnJoinRoom(joinedUser);

            // 入室リクエストをしたユーザーに、参加者の情報をリストで返す
            return this._roomContext.RoomUserDataList.Select(
                f => f.Value.JoinedUser).ToArray();
        }

        /// <summary>
        /// 退出処理
        /// </summary>
        public Task LeaveRoomAsync(string roomName) {
            // コンテキストを取得
            GetContext(roomName);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("LeaveRoom : ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(_roomContext.Name +
                ", ID : " + _roomContext.RoomUserDataList[this.ConnectionId].JoinedUser.UserData.Id +
                ", Player : " + _roomContext.RoomUserDataList[this.ConnectionId].JoinedUser.UserData.Display_Name +
                ", ConnectionID : " + this.ConnectionId +
                ", JoinOrder : " + _roomContext.RoomUserDataList[this.ConnectionId].JoinedUser.JoinOrder);

            // 退出したことを全メンバーに通知
            int LeaveJoinOrder = _roomContext.RoomUserDataList[this.ConnectionId].JoinedUser.JoinOrder;
            this._roomContext.Group.All.OnLeaveRoom(this.ConnectionId, LeaveJoinOrder);

            // ルーム内のメンバーから自分を削除
            this._roomContext.Group.Remove(this.ConnectionId);

            // 参加順番を繰り下げ
            foreach (RoomUserData roomUserData in _roomContext.RoomUserDataList.Values) {
                if (roomUserData.JoinedUser.JoinOrder > LeaveJoinOrder) {
                    roomUserData.JoinedUser.JoinOrder -= 1;
                }
            }

            // ルームデータから退出したユーザーを削除
            this._roomContext.RoomUserDataList.Remove(this.ConnectionId);

            // ルーム内にユーザーが一人もいなかったらルームを削除
            if (this._roomContext.RoomUserDataList.Count == 0) {

                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("DeleteRoom : ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(_roomContext.Name);

                _roomContextRepository.RemoveContext(_roomContext.Name);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// ロビールームの入室
        /// </summary>
        public async Task<JoinedUser[]> JoinLobyAsync(int userId) {
            // 同時に生成しない用に排他制御
            lock (_roomContextRepository) {
                // 指定の名前のルームがあるかどうかを確認
                this._roomContext = _roomContextRepository.GetContext("Loby");
                if (this._roomContext == null) {
                    // なかったら生成

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("CreateLoby");
                    Console.ForegroundColor = ConsoleColor.White;

                    this._roomContext = _roomContextRepository.CreateContext("Loby");
                }
            }

            // ルームに参加 ＆ ルームを保持
            this._roomContext.Group.Add(this.ConnectionId, Client);

            // DBからユーザー情報取得
            User user = await _dbContext.Users.FirstAsync(user => user.Id == userId);

            // 入室済みユーザーのデータを作成
            var joinedUser = new JoinedUser();
            joinedUser.ConnectionId = this.ConnectionId;
            joinedUser.UserData = user;

            // 同時に作成しない用に排他制御
            lock (_roomContext) {
                joinedUser.JoinOrder = this._roomContext.RoomUserDataList.Count + 1;

                // ルーム内に同じユーザーIDの人がいたら
                foreach (var item in _roomContext.RoomUserDataList.Values) {
                    if (item.JoinedUser.UserData.Id == userId) {
                        // ルーム内のメンバーから自分を削除
                        this._roomContext.Group.Remove(this.ConnectionId);
                        JoinedUser[] nonUser = Array.Empty<JoinedUser>();
                        return nonUser;
                    }
                }

                // ルームコンテキストにユーザー情報を登録
                var roomUserData = new RoomUserData() { JoinedUser = joinedUser };
                this._roomContext.RoomUserDataList[this.ConnectionId] = roomUserData;

                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Write("JoinLoby : ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(
                    "ID : " + roomUserData.JoinedUser.UserData.Id +
                    ", Player : " + roomUserData.JoinedUser.UserData.Display_Name +
                    ", ConnectionID : " + roomUserData.JoinedUser.ConnectionId +
                    ", JoinOrder : " + roomUserData.JoinedUser.JoinOrder);
            }

            // 自分以外のルーム参加者全員に、ユーザーの入室通知を送信
            this._roomContext.Group.Except([this.ConnectionId]).OnJoinLoby(joinedUser);

            // 入室リクエストをしたユーザーに、参加者の情報をリストで返す
            return this._roomContext.RoomUserDataList.Select(
                f => f.Value.JoinedUser).ToArray();
        }

        /// <summary>
        /// ロビールームの退室
        /// </summary>
        public Task LeaveLobyAsync() {
            // コンテキストを取得
            GetContext("Loby");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("LeaveLoby : ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(
                "ID : " + _roomContext.RoomUserDataList[this.ConnectionId].JoinedUser.UserData.Id +
                ", Player : " + _roomContext.RoomUserDataList[this.ConnectionId].JoinedUser.UserData.Display_Name +
                ", ConnectionID : " + this.ConnectionId +
                ", JoinOrder : " + _roomContext.RoomUserDataList[this.ConnectionId].JoinedUser.JoinOrder);

            // 退出したことを全メンバーに通知
            int LeaveJoinOrder = _roomContext.RoomUserDataList[this.ConnectionId].JoinedUser.JoinOrder;
            this._roomContext.Group.All.OnLeaveLoby(this.ConnectionId, LeaveJoinOrder);

            // ルーム内のメンバーから自分を削除
            this._roomContext.Group.Remove(this.ConnectionId);

            // 参加順番を繰り下げ
            foreach (RoomUserData roomUserData in _roomContext.RoomUserDataList.Values) {
                if (roomUserData.JoinedUser.JoinOrder > LeaveJoinOrder) {
                    roomUserData.JoinedUser.JoinOrder -= 1;
                }
            }

            // ルームデータから退出したユーザーを削除
            this._roomContext.RoomUserDataList.Remove(this.ConnectionId);

            // ルーム内にユーザーが一人もいなかったらルームを削除
            if (this._roomContext.RoomUserDataList.Count == 0) {

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("DeleteLoby");
                Console.ForegroundColor = ConsoleColor.White;

                _roomContextRepository.RemoveContext(_roomContext.Name);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 切断時の処理
        /// </summary>
        protected override ValueTask OnDisconnected() {
            foreach (var item in this._roomContextRepository.GetAllContext()) {
                this._roomContext = item.Value;

                if (item.Key == "Loby") {
                    // ロビーから退室しチームを抜ける
                    LeaveTeamAsync();
                    LeaveLobyAsync();
                }
                else {
                    // ルームから退室
                    LeaveRoomAsync(item.Key);
                }
            }

            return CompletedTask;
        }

        /// <summary>
        /// チームを作成
        /// </summary>
        public async Task<Guid> CreateTeamAndJoinAsync() {
            // コンテキストを取得
            GetContext("Loby");

            // もうチームに入っていたら何もしない
            Guid findTeamId = this._roomContext.TeamUserDataList.FirstOrDefault(a=> a.Value.ContainsKey(this.ConnectionId)).Key;
            if(findTeamId != Guid.Empty) {
                return Guid.Empty;
            }

            // チーム作成
            Guid teamId = Guid.NewGuid();

            Dictionary<Guid, RoomUserData> addUserData = new Dictionary<Guid, RoomUserData>();
            addUserData[this.ConnectionId] = this._roomContext.RoomUserDataList[this.ConnectionId];

            this._roomContext.TeamUserDataList[teamId] = addUserData;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("[CreateTeam] : ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(
                "TeamID : " + teamId +
                ", ID : " + this._roomContext.RoomUserDataList[this.ConnectionId].JoinedUser.UserData.Id +
                ", Player : " + this._roomContext.RoomUserDataList[this.ConnectionId].JoinedUser.UserData.Display_Name +
                ", ConnectionID : " + this.ConnectionId);

            return teamId;
        }

        /// <summary>
        /// チームに参加
        /// </summary>
        public async Task<JoinedUser[]> JoinTeamAsync(Guid targetTeamId) {
            // コンテキストを取得
            GetContext("Loby");

            // 人数が5人以上だったらなにもしない
            if ( this._roomContext.TeamUserDataList[targetTeamId].Count() >= 5) {
                JoinedUser[] nonUser = Array.Empty<JoinedUser>();
                return nonUser;
            }

            // もうチームに入っていたら何もしない
            Guid findTeamId = this._roomContext.TeamUserDataList.FirstOrDefault(a => a.Value.ContainsKey(this.ConnectionId)).Key;
            if (findTeamId != Guid.Empty) {
                JoinedUser[] nonUser = Array.Empty<JoinedUser>();
                return nonUser;
            }

            // チームリストに追加
            this._roomContext.TeamUserDataList[targetTeamId][this.ConnectionId] = this._roomContext.RoomUserDataList[this.ConnectionId];

            // チームメンバーに参加通知を送る
            foreach(Guid connectionId in this._roomContext.TeamUserDataList[targetTeamId].Keys) {
                this._roomContext.Group.Only([connectionId]).OnJoinTeam(this._roomContext.RoomUserDataList[this.ConnectionId].JoinedUser);
            }

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write("[JoinTeam] : ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(
                "TeamID : " + targetTeamId +
                ", ID : " + this._roomContext.RoomUserDataList[this.ConnectionId].JoinedUser.UserData.Id +
                ", Player : " + this._roomContext.RoomUserDataList[this.ConnectionId].JoinedUser.UserData.Display_Name +
                ", ConnectionID : " + this._roomContext.RoomUserDataList[this.ConnectionId].JoinedUser.ConnectionId);

            // チームに参加リクエストをしたユーザーに、参加者の情報をリストで返す
            return this._roomContext.TeamUserDataList[targetTeamId].Select( _ => _.Value.JoinedUser).ToArray();
        }

        /// <summary>
        /// チームを抜ける
        /// </summary>
        public Task LeaveTeamAsync() {
            // コンテキストを取得
            GetContext("Loby");

            // チームに入っていなかったら何もしない
            var findTeam = this._roomContext.TeamUserDataList.FirstOrDefault(a => a.Value.ContainsKey(this.ConnectionId));
            if (findTeam.Key == Guid.Empty) {
                return Task.CompletedTask;
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("[LeaveTeam] : ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(
                "TeamID : " + findTeam.Key +
                ", ID : " + this._roomContext.RoomUserDataList[this.ConnectionId].JoinedUser.UserData.Id +
                ", Player : " + this._roomContext.RoomUserDataList[this.ConnectionId].JoinedUser.UserData.Display_Name +
                ", ConnectionID : " + this.ConnectionId);

            // チームから自分を削除
            findTeam.Value.Remove(this.ConnectionId);

            // チームにプレイヤーがいたら退出通知を送る
            if (findTeam.Value.Count() >= 1) {
                foreach (Guid connectionId in findTeam.Value.Keys) {
                    this._roomContext.Group.Only([connectionId]).OnLeaveTeam(this.ConnectionId);
                }
            }
            else {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("[DeleteTeam] : ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(findTeam.Key);

                // プレイヤーがいないのでチームを削除
                this._roomContext.TeamUserDataList.Remove(findTeam.Key);
            }

            return Task.CompletedTask;
        }
        
        /// <summary>
        /// チームにフレンドを招待
        /// </summary>
        public async Task InviteTeamFriendAsync(Guid targetConnectionId, Guid teamId) {
            // コンテキストを取得
            GetContext("Loby");

            // 送り先のコネクションIDが存在するか
            if (!this._roomContext.RoomUserDataList.ContainsKey(targetConnectionId)) {
                return;
            }

            // ターゲットユーザーに招待を通知
            this._roomContext.Group.Only([targetConnectionId]).OnInviteTeam(teamId, this._roomContext.RoomUserDataList[this.ConnectionId].JoinedUser.UserData);
        }

        /// <summary>
        /// 準備状態を通知
        /// </summary>
        public async Task SendIsReadyStatusAsync(bool isReady) {
            // コンテキストを取得
            GetContext("Loby");

            // チームメンバー取得
            var teamMember = this._roomContext.TeamUserDataList.FirstOrDefault(tm => tm.Value.ContainsKey(this.ConnectionId));

            // チームメンバーに準備完了状態を通知
            this._roomContext.Group.Only(teamMember.Value.Keys).OnIsReadyStatus(this.ConnectionId, isReady);
        }

        /// <summary>
        /// マッチングする　ルームを探してなかったら作る
        /// </summary>
        public async Task StartMatchingAsync() {
            // コンテキストを取得
            GetContext("Loby");

            // チームメンバーもマッチングさせる
            var teamMember = this._roomContext.TeamUserDataList.FirstOrDefault(tm => tm.Value.ContainsKey(this.ConnectionId));

            // ルーム検索
            string roomName = "";
            foreach (var roomContext in this._roomContextRepository.GetAllContext()) {
                // ロビーは無視
                if (roomContext.Value.Name == "Loby") {
                    continue;
                }

                // チーム全員が入れるか確認
                if (10 - roomContext.Value.RoomUserDataList.Count() >= teamMember.Value.Count()) {
                    // 入るルームの名前を取得
                    roomName = roomContext.Value.Name;
                    break;
                }
            }

            // チームに合ったルームがなかったらルーム名を作成
            if(roomName == "") {
                roomName = Guid.NewGuid().ToString();
            }

            // チームメンバーにマッチング通知を送信
            this._roomContext.Group.Only(teamMember.Value.Keys).OnMatchingRoom(roomName);
        }

        /// <summary>
        /// 接続ID取得
        /// </summary>
        public Task<Guid> GetConnectionId() {
            return Task.FromResult<Guid>(this.ConnectionId);
        }

        /// <summary>
        /// Transform更新
        /// </summary>
        public Task UpdateUserTransformAsync(Vector3 pos, Quaternion rotate, Quaternion cameraRotate) {
            // 位置情報を記録
            this._roomContext.RoomUserDataList[this.ConnectionId].pos = pos;
            // 回転情報を記録
            this._roomContext.RoomUserDataList[this.ConnectionId].rotate = rotate;

            // Transform情報を自分以外のメンバーに通知
            this._roomContext.Group.Except([this.ConnectionId]).OnUpdateUserTransform(this.ConnectionId, pos, rotate, cameraRotate);

            return Task.CompletedTask;
        }

        /// <summary>
        /// オブジェクトの作成
        /// </summary>
        public async Task<Guid> CreateObjectAsync(int objectDataId, Vector3 pos, Quaternion rotate, int updateTypeNum) {
            if(this._roomContext == null) {
                return Guid.Empty;
            }

            // ルームコンテキストにオブジェクト情報を登録
            Guid objectId = Guid.NewGuid();

            var roomObjectData = new RoomObjectData();
            roomObjectData.pos = pos;
            roomObjectData.rotate = rotate;
            switch (updateTypeNum) {
                case 0:
                    roomObjectData.InteractingUser = Guid.Empty;
                    roomObjectData.IsInteracting = false;
                    break;

                case 1:
                    roomObjectData.InteractingUser = this.ConnectionId;
                    roomObjectData.IsInteracting = true;
                    break;
                case 2:
                    roomObjectData.InteractingUser = this.ConnectionId;
                    roomObjectData.IsInteracting = false;
                    break;
            }
            
            this._roomContext.RoomObjectDataList[objectId] = roomObjectData;

            // 自分以外のルーム参加者全員に、オブジェクトの作成通知を送信
            this._roomContext.Group.Except([this.ConnectionId]).OnCreateObject(this.ConnectionId, objectId, objectDataId, pos, rotate, updateTypeNum);

            return objectId;
        }

        /// <summary>
        /// オブジェクトの破棄
        /// </summary>
        public Task DestroyObjectAsync(Guid objectId) {
            // オブジェクトが存在していなかったら終了
            if (!this._roomContext.RoomObjectDataList.ContainsKey(objectId)) {
                return Task.CompletedTask;
            }

            // 破棄したことを自分以外のルーム参加者全員に通知
            this._roomContext.Group.Except([this.ConnectionId]).OnDestroyObject(objectId);

            // ルームデータから破棄したオブジェクトを削除
            this._roomContext.RoomObjectDataList.Remove(objectId);

            return Task.CompletedTask;
        }

        /// <summary>
        /// オブジェクトのTransform更新
        /// </summary>
        public Task UpdateObjectTransformAsync(Guid objectId, Vector3 pos, Quaternion rotate) {
            // オブジェクトが存在していなかったら終了
            if (!this._roomContext.RoomObjectDataList.ContainsKey(objectId)) {
                return Task.CompletedTask;
            }

            // 位置情報を記録
            this._roomContext.RoomObjectDataList[objectId].pos = pos;
            // 回転情報を記録
            this._roomContext.RoomObjectDataList[objectId].rotate = rotate;

            // オブジェクトのTransform情報を自分以外のメンバーに通知
            this._roomContext.Group.Except([this.ConnectionId]).OnUpdateObjectTransform(objectId, pos, rotate);

            return Task.CompletedTask;
        }

        /// <summary>
        /// オブジェクトがインタラクト可能であればする
        /// </summary>
        public Task<bool> InteractObjectAsync(Guid objectId) {
            // オブジェクトが存在していなかったら終了
            if (!this._roomContext.RoomObjectDataList.ContainsKey(objectId)) {
                return Task.FromResult<bool>(false);
            }

            if (this._roomContext.RoomObjectDataList[objectId].IsInteracting) {
                return Task.FromResult<bool>(false);
            }

            // オブジェクトのIsInteractingをfalseにするように通知
            if(this._roomContext.RoomObjectDataList[objectId].InteractingUser != this.ConnectionId) {
                this._roomContext.Group.Only([this._roomContext.RoomObjectDataList[objectId].InteractingUser]).OnFalseObjectInteracting(objectId);
            }

            this._roomContext.RoomObjectDataList[objectId].IsInteracting = true;
            this._roomContext.RoomObjectDataList[objectId].InteractingUser = this.ConnectionId;

            return Task.FromResult<bool>(true);
        }

        /// <summary>
        /// オブジェクトを手放す
        /// </summary>
        public Task DisInteractObjectAsync(Guid objectId) {
            // オブジェクトが存在していなかったら終了
            if (!this._roomContext.RoomObjectDataList.ContainsKey(objectId)) {
                return Task.CompletedTask;
            }

            if (!this._roomContext.RoomObjectDataList[objectId].IsInteracting ||
                this._roomContext.RoomObjectDataList[objectId].InteractingUser != this.ConnectionId) {
                return Task.CompletedTask;
            }

            this._roomContext.RoomObjectDataList[objectId].IsInteracting = false;

            return Task.CompletedTask;
        }
    }
}
