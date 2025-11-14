using MagicOnion;
using realtime_game.Shared.Models.Entities;
using System;

namespace realtime_game.Shared.Interfaces.Services {
    public interface IUserService: IService<IUserService> {
        /// <summary>
        /// ユーザーを登録するAPI
        /// </summary>
        /// <param name="name">プレイヤー名</param>
        /// <returns></returns>
        UnaryResult<int> RegistUserAsync(string name);

        /// <summary>
        /// id指定でユーザー情報を取得するAPI
        /// </summary>
        /// <param name="id">プレイヤーid</param>
        /// <returns></returns>
        UnaryResult<User> GetUserAsync(int id);

        /// <summary>
        /// ユーザー一覧を取得するAPI
        /// </summary>
        /// <returns></returns>
        UnaryResult<User[]> GetAllUsersAsync();

        /// <summary>
        /// id指定でユーザー名を更新するAPI
        /// </summary>
        /// <param name="id">プレイヤーid</param>
        /// <returns></returns>
        UnaryResult<bool> UpdateUserAsync(int id, string name);
    }

}
