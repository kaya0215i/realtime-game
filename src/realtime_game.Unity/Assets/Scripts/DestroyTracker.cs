using System;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

public static class DestroyTracker {
    public static void Destroy(UnityEngine.Object obj, string reason = null,
        [CallerFilePath] string file = "",
        [CallerMemberName] string member = "",
        [CallerLineNumber] int line = 0) {
        if (obj == null) return;

        var fileName = Path.GetFileName(file);
        var stack = Environment.StackTrace;

        Debug.Log($"[DestroyTracker] Destroy‘ÎÛ: {obj} | ——R: {reason ?? "(–¢w’è)"} | ŒÄ‚Ño‚µŒ³: {member} ({fileName}:{line})\nStack:\n{stack}");

        UnityEngine.Object.Destroy(obj);
    }
}
