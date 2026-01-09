using UnityEngine;

public static class LayerMaskExtensions {
    /// <summary>
    /// LayerMaskに指定したレイヤーが含まれているかどうか
    /// </summary>
    public static bool Contains(this LayerMask self, int layerId) {
        return ((1 << layerId) & self) != 0;
    }

    public static bool Contains(this LayerMask self, string layerName) {
        return self.Contains(LayerMask.NameToLayer(layerName));
    }

    /// <summary>
    /// LayerMaskに指定したレイヤーを追加
    /// </summary>
    public static LayerMask Add(this LayerMask self, LayerMask layerId) {
        return self | (1 << layerId);
    }

    public static LayerMask Add(this LayerMask self, string layerName) {
        return self.Add(LayerMask.NameToLayer(layerName));
    }

    /// <summary>
    /// LayerMaskに指定したレイヤーを追加/削除の切り替え
    /// </summary>
    public static LayerMask Toggle(this LayerMask self, LayerMask layerId) {
        return self ^ (1 << layerId);
    }

    public static LayerMask Toggle(this LayerMask self, string layerName) {
        return self.Toggle(LayerMask.NameToLayer(layerName));
    }

    /// <summary>
    /// LayerMaskに指定したレイヤーを削除
    /// </summary>
    public static LayerMask Remove(this LayerMask self, LayerMask layerId) {
        return self & ~(1 << layerId);
    }

    public static LayerMask Remove(this LayerMask self, string layerName) {
        return self.Remove(LayerMask.NameToLayer(layerName));
    }
}