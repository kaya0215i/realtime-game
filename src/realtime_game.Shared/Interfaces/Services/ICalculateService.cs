using System;
using MagicOnion;

/// <summary>
/// はじめてのRPCサービス
/// </summary>
namespace realtime_game.Shared.Interfaces.Services {
    public interface ICalculateService : IService<ICalculateService> {
        // ここにどのようなAPIを作るのか、関数形式で定義を作成する

        /// <summary>
        /// 乗算処理を行う
        /// </summary>
        /// <param name="x">かける数1つ目</param>
        /// <param name="y">かける数2つ目</param>
        /// <returns>xとyの乗算値</returns>
        UnaryResult<int> MulAsync(int x, int y);

        /// <summary>
        /// 受け取った配列の値の合計を返す
        /// </summary>
        /// <param name="numList">たす値の配列</param>
        /// <returns>numListの値の合計値</returns>
        UnaryResult<int> SumAllAsync(int[] numList);

        /// <summary>
        /// x + y を[0]に、x - y を[1]に、x * y を[2]に、x / y を[3]に入れて配列で返す
        /// </summary>
        /// <param name="x">計算する値1つ目</param>
        /// <param name="y">計算する値2つ目</param>
        /// <returns>xとyの計算値</returns>
        UnaryResult<int[]> CalcForOperationAsync(int x, int y);

        /// <summary>
        /// 小数の値3つをフィールドに持つNumberクラスを渡して、3つの値の合計値を返す
        /// </summary>
        /// <param name="numData">小数の値3つをフィールドに持つNumberクラス</param>
        /// <returns>numDataの値の合計値</returns>
        UnaryResult<float> SumAllNumberAsync(Number numData);
    }
}
