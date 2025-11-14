using Cysharp.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using MagicOnion;
using MagicOnion.Client;
using realtime_game.Shared.Interfaces.Services;
using UnityEngine;

public class UserModel : BaseModel {
    private int userId; // ìoò^ÉÜÅ[ÉUÅ[ID
    public async UniTask<bool> RegistUserAsync(string name) {
        var channel = GrpcChannelx.ForAddress(ServerURL);
        var client = MagicOnionClient.Create<IUserService>(channel);

        try {
            // ìoò^ê¨å˜
            userId = await client.RegistUserAsync(name);
            return true;
        } catch (RpcException e) {
            // ìoò^é∏îs
            Debug.Log(e);
            return false;
        }
    }
}
