using MagicOnion.Server.Hubs;
using realtime_game.Server.Models.Contexts;
using realtime_game.Shared.Models.Entities;
using realtime_game.Shared.Interfaces.StreamingHubs;
using UnityEngine;


namespace realtime_game.Server.StreamingHubs {
    public class RoomHub(RoomContextRepository roomContextRepository) :
        StreamingHubBase<IRoomHub, IRoomHubReceiver>, IRoomHub {
        private RoomContextRepository roomContextRepos;
        private RoomContext roomContext;

        // ルームに接続
        public async Task<JoinedUser[]> JoinAsync(string roomName, int userId) {
            // 同時に生成しない用に排他制御
            lock (roomContextRepos) {
                // 指定の名前のルームがあるかどうかを確認
                this.roomContext = roomContextRepos.GetContext(roomName);
                if(this.roomContext == null) {
                    // なかったら生成

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("CreateRoom : ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(roomName);

                    this.roomContext = roomContextRepos.CreateContext(roomName);
                }
            }

            // ルームに参加 ＆ ルームを保持
            this.roomContext.Group.Add(this.ConnectionId, Client);

            // DBからユーザー情報取得
            GameDbContext context = new GameDbContext();
            User user = context.Users.Where(user => user.Id == userId).First();

            // 入室済みユーザーのデータを作成
            var joinedUser = new JoinedUser();
            joinedUser.ConnectionId = this.ConnectionId;
            joinedUser.UserData = user;

            // ルームコンテキストにユーザー情報を登録
            var roomUserData = new RoomUserData() { JoinedUser = joinedUser };
            this.roomContext.RoomUserDataList[this.ConnectionId] = roomUserData;

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write("JoinRoom : ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(roomName + 
                ", ID : " + roomUserData.JoinedUser.UserData.Id + 
                ", Player : " + roomUserData.JoinedUser.UserData.Name + 
                ", ConnectionID : " + roomUserData.JoinedUser.ConnectionId);

            // 自分以外のルーム参加者全員に、ユーザーの入室通知を送信
            this.roomContext.Group.Except([this.ConnectionId]).OnJoin(joinedUser);

            // 入室リクエストをしたユーザーに、参加者の情報をリストで返す
            return this.roomContext.RoomUserDataList.Select(
                f => f.Value.JoinedUser).ToArray();
        }

        // 退出処理
        public Task LeaveAsync() {
            if (roomContext == null ||
                roomContext.RoomUserDataList[this.ConnectionId] == null) {
                return Task.CompletedTask;
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("LeaveRoom : ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(roomContext.Name + 
                ", ID : " + roomContext.RoomUserDataList[this.ConnectionId].JoinedUser.UserData.Id + 
                ", Player : " + roomContext.RoomUserDataList[this.ConnectionId].JoinedUser.UserData.Name + 
                ", ConnectionID : " + this.ConnectionId);

            // 退出したことを全メンバーに通知
            this.roomContext.Group.All.OnLeave(this.ConnectionId);

            // ルーム内のメンバーから自分を削除
            this.roomContext.Group.Remove(this.ConnectionId);

            // ルームデータから退出したユーザーを削除
            this.roomContext.RoomUserDataList.Remove(this.ConnectionId);

            // ルーム内にユーザーが一人もいなかったらルームを削除
            if(this.roomContext.RoomUserDataList.Count == 0) {

                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("DeleteRoom : ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(roomContext.Name);

                roomContextRepos.RemoveContext(roomContext.Name);
            }

            return Task.CompletedTask;
        }

        // 接続時の処理
        protected override ValueTask OnConnected() {
            roomContextRepos = roomContextRepository;
            return default;
        }

        // 切断時の処理
        protected override ValueTask OnDisconnected() {
            LeaveAsync();
            return CompletedTask;
        }

        // 接続ID取得
        public Task<Guid> GetConnectionId() {
            return Task.FromResult<Guid>(this.ConnectionId);
        }

        // Transform更新
        public Task UpdateTransformAsync(Vector3 pos, Quaternion rotate) {
            // 位置情報を記録
            this.roomContext.RoomUserDataList[this.ConnectionId].pos = pos;
            // 回転情報を記録
            this.roomContext.RoomUserDataList[this.ConnectionId].rotate = rotate;

            // Transform情報を自分以外のメンバーに通知
            this.roomContext.Group.Except([this.ConnectionId]).OnUpdateTransform(this.ConnectionId, pos, rotate);

            return Task.CompletedTask;
        }

        // オブジェクトの作成
        public async Task<Guid> CreateObjectAsync(Vector3 pos, Quaternion rotate) {
            // ルームコンテキストにオブジェクト情報を登録
            Guid objectId = Guid.NewGuid();

            var roomObjectData = new RoomObjectData();
            this.roomContext.RoomObjectDataList[objectId] = roomObjectData;

            // 自分以外のルーム参加者全員に、オブジェクトの作成通知を送信
            this.roomContext.Group.Except([this.ConnectionId]).OnCreateObject(this.ConnectionId, objectId, pos, rotate);

            return objectId;
        }

        // オブジェクトの破棄
        public Task DestroyObjectAsync(Guid objectId) {
            // 破棄したことを全メンバーに通知
            this.roomContext.Group.All.OnDestroyObject(this.ConnectionId, objectId);

            // ルームデータから破棄したオブジェクトを削除
            this.roomContext.RoomObjectDataList.Remove(objectId);

            return Task.CompletedTask;
        }

        // オブジェクトのTransform更新
        public Task UpdateObjectTransformAsync(Guid objectId, Vector3 pos, Quaternion rotate) {
            // 位置情報を記録
            this.roomContext.RoomObjectDataList[objectId].pos = pos;
            // 回転情報を記録
            this.roomContext.RoomObjectDataList[objectId].rotate = rotate;

            // オブジェクトのTransform情報を自分以外のメンバーに通知
            this.roomContext.Group.Except([this.ConnectionId]).OnUpdateObjectTransform(this.ConnectionId, objectId, pos, rotate);

            return Task.CompletedTask;
        }
    }
}
