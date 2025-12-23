using Newtonsoft.Json;
using System;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour {
    // データをセーブする
    public void SaveData(string loginId, string hashedPassword, bool autoLogin) {
        var settings = new JsonSerializerSettings {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        SaveData saveData = new SaveData();
        saveData.LoginId = loginId;
        saveData.HashedPassword = hashedPassword;
        saveData.AutoLogin = autoLogin;

        string json = JsonConvert.SerializeObject(saveData, settings);
        var writer = new StreamWriter(Application.persistentDataPath + "/user.json");
        writer.Write(json);
        writer.Flush();
        writer.Close();
    }

    // データをロードする
    public SaveData LoadData() {
        if (!ExistsData()) {
            return null;
        }
        var reader = new StreamReader(Application.persistentDataPath + "/user.json");
        string json = reader.ReadToEnd();
        reader.Close();
        try {
            SaveData saveData = JsonConvert.DeserializeObject<SaveData>(json);

            return saveData;
        }
        catch (Exception e) {
            Debug.LogException(e);
            return null;
        }
    }

    // データがあるか
    public bool ExistsData() {
        if (!File.Exists(Application.persistentDataPath + "/user.json")) {
            return false;
        }
        return true;
    }

    // データを削除する
    public void DeleteData() {
        File.Delete(Application.persistentDataPath + "/user.json");
    }
}
