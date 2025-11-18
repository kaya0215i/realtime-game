using MagicOnion.Server.Hubs;
using realtime_game.Server.Models.Contexts;
using realtime_game.Shared.Models.Entities;
using realtime_game.Shared.Interfaces.StreamingHubs;

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
            this.roomContext.RoomUserDataList[ConnectionId] = roomUserData;

            // 自分以外のルーム参加者全員に、ユーザーの入室通知を送信
            this.roomContext.Group.Except([this.ConnectionId]).OnJoin(joinedUser);

            // 入室リクエストをしたユーザーに、参加者の情報をリストで返す
            return this.roomContext.RoomUserDataList.Select(
                f => f.Value.JoinedUser).ToArray();
        }

        // 退出処理
        public Task LeaveAsync() {
            // 退出したことを全メンバーに通知
            this.roomContext.Group.All.OnLeave(this.ConnectionId);

            // ルーム内のメンバーから自分を削除
            this.roomContext.Group.Remove(this.ConnectionId);

            // ルームデータから退出したユーザーを削除
            this.roomContext.RoomUserDataList.Remove(this.ConnectionId);

            // ルーム内にユーザーが一人もいなかったらルームを削除
            if(this.roomContext.RoomUserDataList.Count == 0) {
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
            return default;
        }

        // 接続ID取得
        public Task<Guid> GetConnectionId() {
            return Task.FromResult<Guid>(this.ConnectionId);
        }
    }
}
