using System;
using UnityEngine;

public class AppManager : MonoBehaviour {
    private void Awake() {
        // フレームレートを60に固定
        Application.targetFrameRate = 60;

        // Randomの設定
        DateTime dt = DateTime.Now;
        int iDate = int.Parse(dt.ToString("MMddHHmmss"));
        UnityEngine.Random.InitState(iDate);
    }
}
