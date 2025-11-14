using Cysharp.Threading.Tasks;
using Grpc.Net.Client;
using UnityEngine;
using MagicOnion;
using MagicOnion.Client;
using realtime_game.Shared.Interfaces.Services;
using NUnit.Framework;

public class CalculateModel : MonoBehaviour {
    const string ServerURL = "http://localhost:5244";
    async void Start() {
        int result1 = await Mul(100, 323);
        Debug.Log(result1);

        int[] numList = new int[4];
        numList[0] = 1;
        numList[1] = 2;
        numList[2] = 3;
        numList[3] = 4;

        int result2 = await SumAll(numList);
        Debug.Log(result2);

        int[] result3 = await CalcForOperation(6, 2);
        foreach (int num in result3) {
            Debug.Log(num);
        }

        Number number = new Number();
        number.x = 5.5f;
        number.y = 4.4f;
        number.z = 3.3f;

        float result4 = await SumAllNumber(number);
        Debug.Log(result4);
    }

    public async UniTask<int> Mul(int x, int y) {
        var channel = GrpcChannelx.ForAddress(ServerURL);
        var client = MagicOnionClient.Create<ICalculateService>(channel);
        var result = await client.MulAsync(x, y);
        return result;
    }

    public async UniTask<int> SumAll(int[] numList) {
        var channel = GrpcChannelx.ForAddress(ServerURL);
        var client = MagicOnionClient.Create<ICalculateService>(channel);
        var result = await client.SumAllAsync(numList);
        return result;
    }

    public async UniTask<int[]> CalcForOperation(int x, int y) {
        var channel = GrpcChannelx.ForAddress(ServerURL);
        var client = MagicOnionClient.Create<ICalculateService>(channel);
        var result = await client.CalcForOperationAsync(x, y);
        return result;
    }

    public async UniTask<float> SumAllNumber(Number numData) {
        var channel = GrpcChannelx.ForAddress(ServerURL);
        var client = MagicOnionClient.Create<ICalculateService>(channel);
        var result = await client.SumAllNumberAsync(numData);
        return result;
    }
}
