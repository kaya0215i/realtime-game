using MagicOnion;
using MagicOnion.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using realtime_game.Server.Models.Contexts;
using realtime_game.Shared.Interfaces.Services;
using realtime_game.Shared.Models.Entities;
using System;
using System.Text;

namespace realtime_game.Server.Services {
    public class UserService: ServiceBase<IUserService>, IUserService {
        private readonly GameDbContext _context;

        // 排他制御用
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        // DI
        public UserService(GameDbContext context) {
            _context = context;
        }

        /// <summary>
        /// ユーザーを登録するAPI
        /// </summary>
        public async UnaryResult<int> RegistUserAsync(string loginId, string hashedPassword, string displayName) {
            await _semaphore.WaitAsync(); // 入室

            try {
                // バリデーションチェック(ログインID登録済みかどうか)
                if (await _context.Users.AnyAsync(u => u.Login_Id == loginId)) {
                    throw new ReturnStatusException(Grpc.Core.StatusCode.InvalidArgument, "");
                }

                // テーブルにレコードを追加
                User user = new User();
                user.Login_Id = loginId;
                user.Password = hashedPassword;
                user.Display_Name = displayName;
                user.Created_at = DateTime.Now;
                user.Updated_at = DateTime.Now;
                _context.Users.Add(user);

                await _context.SaveChangesAsync();

                return user.Id;
            }
            finally {
                _semaphore.Release(); // 退室
            }
        }

        /// <summary>
        /// ユーザーログインAPI
        /// </summary>
        public async UnaryResult<int> LoginUserAsync(string loginId, string hashedPassword) {
            // ログインしようとしているユーザーがいるかまたパスワードがあっているか
            var user = await _context.Users.FirstOrDefaultAsync(user => user.Login_Id == loginId && user.Password == hashedPassword);

            return user?.Id ?? -1;

        }

        /// <summary>
        /// ユーザーのフレンドを取得するAPI
        /// </summary>
        public async UnaryResult<List<User>> GetFriendInfoAsync(int userId) {
            // IDを取得
            List<int> friendIds = await _context.Friends
                .Where(friend => friend.User_Id == userId || friend.Sender_User_Id == userId)
                .Select(friend => friend.User_Id == userId ? friend.Sender_User_Id : friend.User_Id) // ユーザーIDが自分のIDだったらそのIDを、違ったら送った側のIDをとる
                .Distinct()
                .ToListAsync();

            // 取得したIDをもとにUserを探して返す
            return await _context.Users
                .Where(user=> friendIds.Contains(user.Id))
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// ユーザーの届いたフレンドリクエスト情報を取得するAPI
        /// </summary>
        public async UnaryResult<List<User>> GetFriendRequestInfoAsync(int userId) {
            return await _context.FriendRequests
                .Where(request => request.User_Id == userId && request.Status == "pending") // フレンドリクエストテーブルから自分のユーザーIDでステータスが保留のものをとってくる
                .Join(_context.Users, request => request.Sender_User_Id, user => user.Id, (request, user) => user) // ユーザーテーブルと結合し送った人のUser情報をとってくる
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// ユーザーが送信したフレンドリクエスト情報を取得するAPI
        /// </summary>
        public async UnaryResult<List<User>> GetSendFriendRequestInfoAsync(int userId) {
            return await _context.FriendRequests
                .Where(request => request.Sender_User_Id == userId && request.Status == "pending") // フレンドリクエストテーブルから自分のユーザーIDでステータスが保留のものをとってくる
                .Join(_context.Users, request => request.User_Id, user => user.Id, (request, user) => user) // ユーザーテーブルと結合し送った人のUser情報をとってくる
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// フレンドリクエストを送信するAPI
        /// </summary>
        public async UnaryResult SendFriendRequestAsync(int userId, int recipientUserId) {
            // 送信先のユーザーIDが存在するか
            if (!await _context.Users.AnyAsync(user => user.Id == recipientUserId)) {
                throw new ReturnStatusException(Grpc.Core.StatusCode.NotFound, "");
            }

            // テーブルにレコードを追加
            FriendRequest friendRequest = new FriendRequest();
            friendRequest.User_Id = recipientUserId;
            friendRequest.Sender_User_Id = userId;
            friendRequest.Created_at = DateTime.Now;
            friendRequest.Updated_at = DateTime.Now;
            _context.FriendRequests.Add(friendRequest);

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// フレンドリクエストを承認するAPI
        /// </summary>
        public async UnaryResult AcceptFriendRequestAsync(int userId, int senderUserId) {
            // リクエストが存在するか
            var request = await _context.FriendRequests.FirstOrDefaultAsync(request =>
                request.User_Id == userId &&
                request.Sender_User_Id == senderUserId &&
                request.Status == "pending");

            if (request == null) {
                throw new ReturnStatusException(Grpc.Core.StatusCode.NotFound, "");
            }

            // リクエストのステータスを許可にする
            request.Status = "accepted";
            request.Updated_at = DateTime.Now;

            // フレンドテーブルに追加
            Friend friend = new Friend();
            friend.User_Id = userId;
            friend.Sender_User_Id = senderUserId;
            friend.Created_at = DateTime.Now;
            friend.Updated_at = DateTime.Now;
            _context.Friends.Add(friend);

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// フレンドリクエストを拒否するAPI
        /// </summary>
        public async UnaryResult RefusalFriendRequestAsync(int userId, int senderUserId) {
            // リクエストが存在するか
            var request = await _context.FriendRequests.FirstOrDefaultAsync(request =>
                request.User_Id == userId &&
                request.Sender_User_Id == senderUserId &&
                request.Status == "pending");

            if (request == null) {
                throw new ReturnStatusException(Grpc.Core.StatusCode.NotFound, "");
            }

            // リクエストのステータスを拒否にする
            request.Status = "refusal";
            request.Updated_at = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// id指定でユーザー情報を取得するAPI
        /// </summary>
        public async UnaryResult<User> GetUserByIdAsync(int id) {
            // ユーザーを検索
            var user = await _context.Users.FirstOrDefaultAsync(user => user.Id == id);

            if (user == null) {
                throw new ReturnStatusException(Grpc.Core.StatusCode.NotFound, "");
            }

            return user;
        }

        /// <summary>
        /// displayNameに含まれる文字列でユーザー情報を取得するAPI
        /// </summary>
        public async UnaryResult<List<User>> GetUserByDisplayNameAsync(string findName) {
            return await _context.Users.Where(user => user.Display_Name.Contains(findName, StringComparison.OrdinalIgnoreCase))
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// ユーザー一覧を取得するAPI
        /// </summary>
        public async UnaryResult<User[]> GetAllUsersAsync() {
            // ユーザー一覧取得
            var users = await _context.Users
                .AsNoTracking()
                .ToArrayAsync();

            if (users.Length == 0) {
                throw new ReturnStatusException(Grpc.Core.StatusCode.InvalidArgument, "");
            }

            return users;

        }

        /// <summary>
        /// id指定で表示名を更新するAPI
        /// </summary>
        public async UnaryResult<bool> UpdateUserAsync(int id, string displayName) {
            // ユーザーが存在するか
            var user = await _context.Users.FirstOrDefaultAsync(user => user.Id == id);

            if (user == null) {
                throw new ReturnStatusException(Grpc.Core.StatusCode.NotFound, "");
            }

            user.Display_Name = displayName;
            user.Updated_at = DateTime.Now;

            await _context.SaveChangesAsync();

            return true;
        }
    }
}
