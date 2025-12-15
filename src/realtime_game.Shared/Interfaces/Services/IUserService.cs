using MagicOnion;
using realtime_game.Shared.Models.Entities;
using System;
using System.Collections.Generic;

namespace realtime_game.Shared.Interfaces.Services {
    public interface IUserService: IService<IUserService> {
        /// <summary>
        /// ユーザーを登録するAPI
        /// </summary>
        UnaryResult<int> RegistUserAsync(string login_id, string hashedPassword, string displayName);

        /// <summary>
        /// ユーザーログインAPI
        /// </summary>
        UnaryResult<int> LoginUserAsync(string login_id, string hashedPassword);

        /// <summary>
        /// ユーザーのフレンド情報を取得するAPI
        /// </summary>
        UnaryResult<List<User>> GetFriendInfoAsync(int userId);

        /// <summary>
        /// ユーザーの届いたフレンドリクエスト情報を取得するAPI
        /// </summary>
        UnaryResult<List<User>> GetFriendRequestInfoAsync(int userId);

        /// <summary>
        /// ユーザーが送信したフレンドリクエスト情報を取得するAPI
        /// </summary>
        UnaryResult<List<User>> GetSendFriendRequestInfoAsync(int userId);

        /// <summary>
        /// フレンドリクエストを送信するAPI
        /// </summary>
        UnaryResult SendFriendRequestAsync(int userId, int recipientUserId);

        /// <summary>
        /// フレンドリクエストを承認するAPI
        /// </summary>
        UnaryResult AcceptFriendRequestAsync(int userId, int senderUserId);

        /// <summary>
        /// フレンドリクエストを拒否するAPI
        /// </summary>
        UnaryResult RefusalFriendRequestAsync(int userId, int senderUserId);

        /// <summary>
        /// id指定でユーザー情報を取得するAPI
        /// </summary>
        UnaryResult<User> GetUserByIdAsync(int id);

        /// <summary>
        /// displayNameに含まれる文字列でユーザー情報を取得するAPI
        /// </summary>
        UnaryResult<List<User>> GetUserByDisplayNameAsync(string findName);

        /// <summary>
        /// ユーザー一覧を取得するAPI
        /// </summary>
        UnaryResult<User[]> GetAllUsersAsync();

        /// <summary>
        /// id指定でユーザー名を更新するAPI
        /// </summary>
        UnaryResult<bool> UpdateUserAsync(int id, string name);
    }

}
