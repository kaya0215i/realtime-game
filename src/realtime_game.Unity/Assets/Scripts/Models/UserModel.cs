using Cysharp.Threading.Tasks;
using Grpc.Core;
using MagicOnion;
using MagicOnion.Client;
using realtime_game.Shared.Interfaces.Services;
using realtime_game.Shared.Models.Entities;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class UserModel : BaseModel {
    public int UserId { private set; get; } // 登録ユーザーID

    private GrpcChannelx channel;
    private IUserService client;

    // シングルトンにする
    private static UserModel instance;

    private static bool isQuitting;
    private static bool isShuttingDown;

    public static UserModel Instance {
        get {
            // アプリ終了/破棄中は新規生成しない
            if (isQuitting || isShuttingDown) {
                return null;
            }

            if (instance == null) {
                GameObject obj = new GameObject("UserModel");
                instance = obj.AddComponent<UserModel>();

                DontDestroyOnLoad(obj);
            }
            return instance;
        }
    }

    private void OnDestroy() {
        if (instance == this) {
            isShuttingDown = true;
            instance = null;
        }
    }

    private void OnApplicationQuit() {
        isQuitting = true;
    }

    /// <summary>
    /// MagicOnion接続処理
    /// </summary>
    public async UniTask CreateUserModel() {
        channel = GrpcChannelx.ForAddress(ServerURL);
        client = MagicOnionClient.Create<IUserService>(channel);
    }

    /// <summary>
    /// ユーザーを登録するAPI
    /// </summary>
    public async UniTask<bool> RegistUserAsync(string loginId, string password, string displayName) {
        try {
            // 登録成功
            UserId = await client.RegistUserAsync(loginId, HashPassword(password), displayName);
            return true;
        } catch (RpcException e) {
            // 登録失敗
            Debug.LogException(e);
            return false;
        }
    }

    /// <summary>
    /// ユーザーログインAPI
    /// </summary>
    public async UniTask<bool> LoginUserAsync(string loginId, string password, bool isHashed = false) {
        // ハッシュ化されていなかったらする
        string HashedPassword = password;
        if(isHashed == false) {
            HashedPassword = HashPassword(password);
        }

        int result = await client.LoginUserAsync(loginId, HashedPassword);

        if(result != -1) {
            UserId = result;
            return true;
        }

        return false;
    }

    /// <summary>
    /// ユーザーのフレンドを取得
    /// </summary>
    public async UniTask<List<User>> GetFriendInfoAsync() {
        return await client.GetFriendInfoAsync(UserId);
    }

    /// <summary>
    /// ユーザーの届いたフレンドリクエスト情報を取得
    /// </summary>
    public async UniTask<List<User>> GetFriendRequestInfoAsync() {
        return await client.GetFriendRequestInfoAsync(UserId);
    }

    /// <summary>
    /// ユーザーが送信したフレンドリクエスト情報を取得
    /// </summary>
    public async UniTask<List<User>> GetSendFriendRequestInfoAsync() {
        return await client.GetSendFriendRequestInfoAsync(UserId);
    }

    /// <summary>
    /// フレンドリクエストを送信
    /// </summary>
    public async UniTask SendFriendRequestAsync(int recipientUserId) {
        try {
            // 送信成功
            await client.SendFriendRequestAsync(UserId, recipientUserId);
        }
        catch (RpcException e) {
            // 送信失敗
            Debug.LogException(e);
        }
        
    }

    /// <summary>
    /// フレンドリクエストを承認
    /// </summary>
    public async UniTask AcceptFriendRequestAsync(int senderUserId) {
        try {
            // 承認成功
            await client.AcceptFriendRequestAsync(UserId, senderUserId);
        }
        catch (RpcException e) {
            // 承認失敗
            Debug.LogException(e);
        }
    }

    /// <summary>
    /// フレンドリクエストを拒否
    /// </summary>
    public async UniTask RefusalFriendRequestAsync(int senderUserId) {
        try {
            // 拒否成功
            await client.RefusalFriendRequestAsync(UserId, senderUserId);
        }
        catch (RpcException e) {
            // 拒否失敗
            Debug.LogException(e);
        }
    }

    /// <summary>
    /// id指定でユーザー情報を取得する
    /// </summary>
    public async UniTask<User> GetUserByIdAsync(int id) {
        return await client.GetUserByIdAsync(id);
    }

    /// <summary>
    /// displayNameに含まれる文字列でユーザー情報を取得するAPI
    /// </summary>
    public async UniTask<List<User>> GetUserByDisplayNameAsync(string findName) {
        return await client.GetUserByDisplayNameAsync(findName);
    }

    /// <summary>
    /// ユーザー一覧を取得する
    /// </summary>
    public async UniTask<User[]> GetAllUsersAsync() {
        return await client.GetAllUsersAsync();
    }

    /// <summary>
    /// id指定で表示名を更新する
    /// </summary>
    public async UniTask<bool> UpdateUserAsync(int id, string name) {
        try {
            // 成功
            await client.UpdateUserAsync(id, name);
            return true;
        }
        catch (RpcException e) {
            // 失敗
            Debug.LogException(e);
            return false;
        }
    }

    /// <summary>
    /// ハッシュ化する
    /// </summary>
    public string HashPassword(string password) {
        using (SHA256 sha256 = SHA256.Create()) {
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++) {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }
}
