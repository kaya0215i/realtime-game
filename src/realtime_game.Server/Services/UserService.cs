using MagicOnion;
using MagicOnion.Server;
using realtime_game.Server.Models.Contexts;
using realtime_game.Shared.Interfaces.Services;
using realtime_game.Shared.Models.Entities;
using System;

namespace realtime_game.Server.Services {
    public class UserService: ServiceBase<IUserService>, IUserService {
        // ユーザーを登録するAPI
        public async UnaryResult<int> RegistUserAsync(string name) {
            using var context = new GameDbContext();

            // バリデーションチェック(名前登録済みかどうか)
            if(context.Users.Count() > 0 &&
               context.Users.Where(user => user.Name == name).Count() > 0) {
               throw new ReturnStatusException(Grpc.Core.StatusCode.InvalidArgument, "");
            }

            // テーブルにレコードを追加
            User user = new User();
            user.Name = name;
            user.Token = Guid.NewGuid().ToString();
            user.Created_at = DateTime.Now;
            user.Updated_at = DateTime.Now;
            context.Users.Add(user);
            await context.SaveChangesAsync();

            return user.Id;
        }

        // id指定でユーザー情報を取得するAPI
        public async UnaryResult<User> GetUserAsync(int id) {
            using var context = new GameDbContext();

            // ユーザーを検索
            if (context.Users.Count() > 0 &&
                context.Users.Where(user => user.Id == id).Count() <= 0) {
                throw new ReturnStatusException(Grpc.Core.StatusCode.InvalidArgument, "");
            }

            User user = context.Users.First(user => user.Id == id);

            return user;
        }

        // ユーザー一覧を取得するAPI
        public async UnaryResult<User[]> GetAllUsersAsync() {
           using var context = new GameDbContext();

            // ユーザー一覧取得
            if (context.Users.Count() <= 0) {
                throw new ReturnStatusException(Grpc.Core.StatusCode.InvalidArgument, "");
            }

            User[] users = context.Users.ToArray();
            return users;

        }

        // id指定でユーザー名を更新するAPI
        public async UnaryResult<bool> UpdateUserAsync(int id, string name) {
            using var context = new GameDbContext();

            // ユーザーが存在するか
            if (context.Users.Count() > 0 &&
               context.Users.Where(user => user.Id == id).Count() > 0) {
                throw new ReturnStatusException(Grpc.Core.StatusCode.InvalidArgument, "");
            }

            User user = context.Users.First(user => user.Id == id);
            user.Name = name;
            await context.SaveChangesAsync();

            return true;
        }
    }
}
