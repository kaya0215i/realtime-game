using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// シリアル化可能なDictionaryクラス。
/// </summary>
/// <typeparam name="TKey">辞書のキーの型。</typeparam>
/// <typeparam name="TValue">辞書の値の型。</typeparam>
[Serializable]
public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver {
    // 再帰的な呼び出しを防止するためのフラグ
    [NonSerialized]
    private bool isUpdating = false;

    /// <summary>
    /// シリアル化対象のキーと値のペアのリスト。
    /// Unityのシリアル化システムで辞書データをシリアル化するために使用します。
    /// </summary>
    [SerializeField]
    private List<SerializableKeyValuePair<TKey, TValue>> keyValuePairs = new List<SerializableKeyValuePair<TKey, TValue>>();

    private void UpdateKeyValuePairs() {
        keyValuePairs.Clear();
        foreach (var kvp in this) {
            keyValuePairs.Add(new SerializableKeyValuePair<TKey, TValue>(kvp.Key, kvp.Value));
        }
    }


    /// <summary>
    /// Unityによるシリアル化前に呼び出され、辞書データをリストに変換します。
    /// </summary>
    public void OnBeforeSerialize() {
        if (isUpdating) {
            return; // フラグが立っている場合、再帰的な呼び出しを防止
        }

        UpdateKeyValuePairs();
    }

    /// <summary>
    /// Unityによるシリアル化後に呼び出され、リストデータを辞書に変換します。
    /// 重複するキーが存在する場合は、キーが整数型、列挙型、または文字列型であればキーを調整して追加します。
    /// </summary>
    public void OnAfterDeserialize() {
        if (isUpdating)
            return; // フラグが立っている場合、再帰的な呼び出しを防止

        isUpdating = true; // フラグを立てて再帰的な呼び出しを防止

        this.Clear();

        foreach (var kvp in keyValuePairs) {
            TKey uniqueKey = kvp.Key;

            if (this.ContainsKey(uniqueKey)) {
                if (TryGetUniqueKey(uniqueKey, out TKey newUniqueKey)) {
                    uniqueKey = newUniqueKey;
                    Debug.LogWarning($"Duplicate key '{kvp.Key}' found. Changed to unique key '{uniqueKey}'.");
                }
                else {
                    Debug.LogWarning($"Cannot resolve duplicate key '{kvp.Key}'. Skipping entry.");
                    continue; // ユニークなキーが取得できなかった場合、エントリーをスキップ
                }
            }

            this.Add(uniqueKey, kvp.Value);
        }

        UpdateKeyValuePairs();

        isUpdating = false; // フラグを解除
    }

    /// <summary>
    /// ユニークなキーを生成します。キーが整数型、列挙型、または文字列型の場合にのみ機能します。
    /// </summary>
    /// <param name="originalKey">元のキー。</param>
    /// <param name="uniqueKey">生成されたユニークなキー。</param>
    /// <returns>ユニークなキーを生成できた場合は true、できなかった場合は false。</returns>
    private bool TryGetUniqueKey(TKey originalKey, out TKey uniqueKey) {
        uniqueKey = default;
        bool success = false;

        Type keyType = typeof(TKey);

        if (keyType.IsEnum) {
            // 列挙型の場合の処理
            Array enumValues = Enum.GetValues(keyType);
            int currentIndex = Array.IndexOf(enumValues, originalKey);
            if (currentIndex < 0) {
                Debug.LogWarning($"Key '{originalKey}' is not a valid enum value.");
                return false;
            }

            // セグメント1: originalKeyの次のインデックスから最後まで
            for (int i = currentIndex + 1; i < enumValues.Length; i++) {
                TKey enumValue = (TKey)enumValues.GetValue(i);
                if (!this.ContainsKey(enumValue)) {
                    uniqueKey = enumValue;
                    return true;
                }
            }

            // セグメント2: 先頭からoriginalKeyのインデックスまで
            for (int i = 0; i < currentIndex; i++) {
                TKey enumValue = (TKey)enumValues.GetValue(i);
                if (!this.ContainsKey(enumValue)) {
                    uniqueKey = enumValue;
                    return true;
                }
            }

            // すべての列挙型の値が使用されている場合
            Debug.LogWarning($"All enum values for key '{keyType}' are already used. Cannot add duplicate key '{originalKey}'.");
        }
        else if (IsIntegralType(keyType)) {
            // 整数型の場合の処理
            success = TryIncrementIntegralKey(originalKey, keyType, out uniqueKey);
            if (!success) {
                Debug.LogWarning($"Cannot generate a unique key for '{originalKey}'.");
            }
        }
        else if (typeof(string) == keyType) {
            // 文字列型の場合の処理
            success = TryGenerateUniqueStringKey(originalKey as string, out string newStringKey);
            if (success) {
                uniqueKey = (TKey)(object)newStringKey;
            }
            else {
                Debug.LogWarning($"Cannot generate a unique string key for '{originalKey}'.");
            }
        }
        else {
            Debug.LogWarning($"Duplicate key '{originalKey}' found, but key type '{keyType}' is not supported for automatic key generation. Skipping entry.");
        }

        return success;
    }

    /// <summary>
    /// 指定された型が整数型かどうかを判定します。
    /// </summary>
    /// <param name="type">判定する型。</param>
    /// <returns>整数型の場合は true、そうでない場合は false。</returns>
    private bool IsIntegralType(Type type) {
        return type == typeof(byte) ||
               type == typeof(sbyte) ||
               type == typeof(short) ||
               type == typeof(ushort) ||
               type == typeof(int) ||
               type == typeof(uint) ||
               type == typeof(long) ||
               type == typeof(ulong);
    }

    /// <summary>
    /// 整数型キーのユニークなキーを生成します。
    /// </summary>
    /// <param name="originalKey">元のキー。</param>
    /// <param name="keyType">キーの型。</param>
    /// <param name="uniqueKey">生成されたユニークなキー。</param>
    /// <returns>ユニークなキーを生成できた場合は true、できなかった場合は false。</returns>
    private bool TryIncrementIntegralKey(TKey originalKey, Type keyType, out TKey uniqueKey) {
        uniqueKey = default;
        try {
            if (keyType == typeof(byte)) {
                byte key = (byte)(object)originalKey;
                byte newKey = (byte)(key + 1);
                while (this.ContainsKey((TKey)(object)newKey)) {
                    if (newKey == byte.MaxValue) {
                        Debug.LogError("Exceeded maximum byte value while trying to find a unique key.");
                        return false;
                    }
                    newKey++;
                }
                uniqueKey = (TKey)(object)newKey;
                return true;
            }
            else if (keyType == typeof(sbyte)) {
                sbyte key = (sbyte)(object)originalKey;
                sbyte newKey = (sbyte)(key + 1);
                while (this.ContainsKey((TKey)(object)newKey)) {
                    if (newKey == sbyte.MaxValue) {
                        Debug.LogError("Exceeded maximum sbyte value while trying to find a unique key.");
                        return false;
                    }
                    newKey++;
                }
                uniqueKey = (TKey)(object)newKey;
                return true;
            }
            else if (keyType == typeof(short)) {
                short key = (short)(object)originalKey;
                short newKey = (short)(key + 1);
                while (this.ContainsKey((TKey)(object)newKey)) {
                    if (newKey == short.MaxValue) {
                        Debug.LogError("Exceeded maximum short value while trying to find a unique key.");
                        return false;
                    }
                    newKey++;
                }
                uniqueKey = (TKey)(object)newKey;
                return true;
            }
            else if (keyType == typeof(ushort)) {
                ushort key = (ushort)(object)originalKey;
                ushort newKey = (ushort)(key + 1);
                while (this.ContainsKey((TKey)(object)newKey)) {
                    if (newKey == ushort.MaxValue) {
                        Debug.LogError("Exceeded maximum ushort value while trying to find a unique key.");
                        return false;
                    }
                    newKey++;
                }
                uniqueKey = (TKey)(object)newKey;
                return true;
            }
            else if (keyType == typeof(int)) {
                int key = (int)(object)originalKey;
                int newKey = key + 1;
                while (this.ContainsKey((TKey)(object)newKey)) {
                    if (newKey == int.MaxValue) {
                        Debug.LogError("Exceeded maximum int value while trying to find a unique key.");
                        return false;
                    }
                    newKey++;
                }
                uniqueKey = (TKey)(object)newKey;
                return true;
            }
            else if (keyType == typeof(uint)) {
                uint key = (uint)(object)originalKey;
                uint newKey = key + 1;
                while (this.ContainsKey((TKey)(object)newKey)) {
                    if (newKey == uint.MaxValue) {
                        Debug.LogError("Exceeded maximum uint value while trying to find a unique key.");
                        return false;
                    }
                    newKey++;
                }
                uniqueKey = (TKey)(object)newKey;
                return true;
            }
            else if (keyType == typeof(long)) {
                long key = (long)(object)originalKey;
                long newKey = key + 1;
                while (this.ContainsKey((TKey)(object)newKey)) {
                    if (newKey == long.MaxValue) {
                        Debug.LogError("Exceeded maximum long value while trying to find a unique key.");
                        return false;
                    }
                    newKey++;
                }
                uniqueKey = (TKey)(object)newKey;
                return true;
            }
            else if (keyType == typeof(ulong)) {
                ulong key = (ulong)(object)originalKey;
                ulong newKey = key + 1;
                while (this.ContainsKey((TKey)(object)newKey)) {
                    if (newKey == ulong.MaxValue) {
                        Debug.LogError("Exceeded maximum ulong value while trying to find a unique key.");
                        return false;
                    }
                    newKey++;
                }
                uniqueKey = (TKey)(object)newKey;
                return true;
            }
            else {
                Debug.LogWarning($"Integral key type '{keyType}' is not supported for unique key generation.");
                return false;
            }
        }
        catch (Exception ex) {
            Debug.LogError($"Error while generating unique integral key: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 文字列型キーのユニークなキーを生成します。
    /// 「a」から「z」まで試し、「z」の次は「aa」とし、以降必要に応じて一文字ずつ追加されます。
    /// </summary>
    /// <param name="originalKey">元のキー。</param>
    /// <param name="newStringKey">生成されたユニークな文字列キー。</param>
    /// <returns>ユニークなキーを生成できた場合は true、できなかった場合は false。</returns>
    private bool TryGenerateUniqueStringKey(string originalKey, out string newStringKey) {
        newStringKey = null;

        if (originalKey == null) {
            Debug.LogWarning("Original key is null. Cannot generate a unique string key.");
            return false;
        }

        // 定義済みの最大長を設定（必要に応じて変更可能）
        const int maxLength = 10;

        // 関数内で使用するローカル変数
        int length = 1;

        while (length <= maxLength) {
            IEnumerable<string> candidates = GenerateStringCombinations(length);
            foreach (var c in candidates) {
                if (!this.ContainsKey((TKey)(object)c)) {
                    newStringKey = c;
                    return true;
                }
            }
            length++;
        }

        Debug.LogWarning($"Exceeded maximum string length ({maxLength}) while trying to find a unique string key.");
        return false;
    }

    /// <summary>
    /// 指定された長さのアルファベット小文字の組み合わせを生成します。
    /// </summary>
    /// <param name="length">生成する文字列の長さ。</param>
    /// <returns>指定された長さの文字列の列挙。</returns>
    private IEnumerable<string> GenerateStringCombinations(int length) {
        if (length == 1) {
            for (char c = 'a'; c <= 'z'; c++) {
                yield return c.ToString();
            }
        }
        else {
            foreach (var prefix in GenerateStringCombinations(length - 1)) {
                for (char c = 'a'; c <= 'z'; c++) {
                    yield return prefix + c;
                }
            }
        }
    }

    /// <summary>
    /// シリアル化用のキーと値のペアのリストを取得します。
    /// Unityのインスペクタ上で辞書データを表示・編集するために使用されます。
    /// </summary>
    public List<SerializableKeyValuePair<TKey, TValue>> SerializableKeyValuePairs => keyValuePairs;
}

/// <summary>
/// シリアル化可能なキーと値のペアを表すクラス。
/// </summary>
/// <typeparam name="TKey">キーの型。</typeparam>
/// <typeparam name="TValue">値の型。</typeparam>
[Serializable]
public class SerializableKeyValuePair<TKey, TValue> {
    /// <summary>
    /// キー。
    /// </summary>
    public TKey Key;

    /// <summary>
    /// 値。
    /// </summary>
    public TValue Value;

    /// <summary>
    /// デフォルトコンストラクタ。
    /// </summary>
    public SerializableKeyValuePair() {
        Key = default;   // TKeyのデフォルト値
        Value = default; // TValueのデフォルト値
    }

    /// <summary>
    /// キーと値を指定して新しいインスタンスを作成します。
    /// </summary>
    /// <param name="key">キー。</param>
    /// <param name="value">値。</param>
    public SerializableKeyValuePair(TKey key, TValue value) {
        Key = key;
        Value = value;
    }
}