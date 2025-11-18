using Cysharp.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using MagicOnion;
using MagicOnion.Client;
using realtime_game.Shared.Interfaces.Services;
using realtime_game.Shared.Models.Entities;
using UnityEngine;

public class UserModel : BaseModel {
    private int userId; // 登録ユーザーID
    // ユーザーを登録するAPI
    public async UniTask<bool> RegistUserAsync(string name) {
        var channel = GrpcChannelx.ForAddress(ServerURL);
        var client = MagicOnionClient.Create<IUserService>(channel);

        try {
            // 登録成功
            userId = await client.RegistUserAsync(name);
            return true;
        } catch (RpcException e) {
            // 登録失敗
            Debug.Log(e);
            return false;
        }
    }

    // id指定でユーザー情報を取得する
    public async UniTask<User> GetUserAsync(int id) {
        var channel = GrpcChannelx.ForAddress(ServerURL);
        var client = MagicOnionClient.Create<IUserService>(channel);

        return await client.GetUserAsync(id);
    }

    // ユーザー一覧を取得する
    public async UniTask<User[]> GetAllUsersAsync() {
        var channel = GrpcChannelx.ForAddress(ServerURL);
        var client = MagicOnionClient.Create<IUserService>(channel);

        return await client.GetAllUsersAsync();
    }

    // id指定でユーザー名を更新する
    public async UniTask<bool> UpdateUserAsync(int id, string name) {
        var channel = GrpcChannelx.ForAddress(ServerURL);
        var client = MagicOnionClient.Create<IUserService>(channel);

        try {
            // 成功
            await client.UpdateUserAsync(id, name);
            return true;
        }
        catch (RpcException e) {
            // 失敗
            Debug.Log(e);
            return false;
        }
    }
}
