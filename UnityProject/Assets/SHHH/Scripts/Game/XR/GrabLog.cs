using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// On-device grab diagnostics (Quest run 2026-11-11: pinch grabs failed on the mug, coasters and the practice book with no data).
    /// Every pinch start / grab / miss is written to persistentDataPath/shhh_grablog.txt (last 400 lines), readable with
    /// `adb pull /sdcard/Android/data/&lt;package&gt;/files/shhh_grablog.txt`. Also mirrored to the Unity log.
    /// </summary>
    public static class GrabLog
    {
        static readonly List<string> _lines = new List<string>();
        static float _flushAt;
        static string PathOf => System.IO.Path.Combine(Application.persistentDataPath, "shhh_grablog.txt");

        public static void Add(string line)
        {
            var l = $"{Time.realtimeSinceStartup:0.00} {line}";
            _lines.Add(l); if (_lines.Count > 400) _lines.RemoveRange(0, _lines.Count - 400);
            Debug.Log("[SHHH][grablog] " + l);
            if (Time.realtimeSinceStartup > _flushAt) Flush();
        }

        public static void Flush()
        {
            _flushAt = Time.realtimeSinceStartup + 3f;
            try { File.WriteAllLines(PathOf, _lines); } catch { /* storage not available: the Unity log still has it */ }
        }
    }
}
