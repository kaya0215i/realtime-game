using MagicOnion;
using MagicOnion.Server;
using realtime_game.Shared.Interfaces.Services;
using System;

public class CalclateService : ServiceBase<ICalculateService>, ICalculateService {
    // 乗算API 二つの整数を引数で受け取り乗算値を返す
    public async UnaryResult<int> MulAsync(int x, int y) {
        Console.WriteLine("Received:" + x + ", " + y);
        return x * y;
    }

    // 受け取った配列の値の合計を返す
    public async UnaryResult<int> SumAllAsync(int[] numList) {
        int result = 0;

        foreach (int num in numList) {
            result += num;
        }

        Console.Write("Received:");
        for (int i = 0; i < numList.Length; i++) {
            if (i == 0) {
                Console.Write(numList[i]);
            }
            else {
                Console.Write(", " + numList[i]);
            }
            
        }
        Console.WriteLine();

        return result;
    }

    // x + y を[0]に、x - y を[1]に、x * y を[2]に、x / y を[3]に入れて配列で返す
    public async UnaryResult<int[]> CalcForOperationAsync(int x, int y) {
        int[] result = [x + y, x - y, x * y, x / y];
        Console.WriteLine("Received:" + x + ", " + y);
        return result;
    }

    // 小数の値3つをフィールドに持つNumberクラスを渡して、3つの値の合計値を返す
    public async UnaryResult<float> SumAllNumberAsync(Number numData) {
        float result = numData.x + numData.y + numData.z;
        Console.WriteLine("Received:" + numData.x  + ", " + numData.y + ", " + numData.y);
        return result;
    }
}
