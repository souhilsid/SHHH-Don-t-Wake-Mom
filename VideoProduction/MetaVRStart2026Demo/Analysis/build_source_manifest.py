import subprocess, json, csv, hashlib, os
FP = r"C:/Users/aisci/AppData/Local/Microsoft/WinGet/Packages/Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe/ffmpeg-9.0.2-full_build/bin/ffprobe.exe"
D = "C:/Users/aisci/Downloads/"; R = D + "SHHH Game/SHHH/"
files = [("F01", D+"com.oculus.vrshell-20261107-124508-0.mp4", "early alpha; '(1).mp4' and '(1) (1).mp4' copies are byte-identical", "not used"),
 ("F02", D+"com.oculus.vrshell-20261107-201409-0.mp4", "early alpha", "not used"), ("F03", D+"com.oculus.vrshell-20261107-202049-0.mp4", "early alpha menu only", "not used"),
 ("F04", D+"com.oculus.vrshell-20261111-170037-0.mp4", "c13, night lighting", "used"), ("F05", D+"com.oculus.vrshell-20261112-170302-0.mp4", "c15 practice", "used"),
 ("F06", D+"com.oculus.vrshell-20261115-170859-0.mp4", "c20/21; identical copy in Remotion/Raw/Records", "used"),
 ("F07", D+"com.oculus.vrshell-20261117-122141-0.mp4", "c21 solo 10 min; identical copy in Remotion/Raw/Records", "used"),
 ("F09", R+"Remotion/Raw/Records-20261117T150733Z-1-001/Records/com.oculus.vrshell-20261117-174620-0.mp4", "c23 (recorded 14:46 UTC, c23 built 13:32 UTC): online 2-human match + solo match", "PRIMARY"),
 ("X01", R+"QA_Evidence/finger_gun/finger_gun_test_unity.mp4", "Unity desktop simulated hands 1280x720 10 fps", "not used (quality)"),
 ("X02", R+"SHHH_ArtProduction_V5/Evidence/Runtime/Living_To_Bedroom_Route_Desktop.mp4", "Unity desktop route 960x540 8 fps", "not used (quality)"),
 ("X03", D+"SHHH_HERO_ANIMATION_TEST_V3.mp4", "pre-production animation test, not gameplay", "excluded")]
with open("FOOTAGE_SOURCE_MANIFEST.csv", "w", newline="", encoding="utf-8") as fh:
    w = csv.writer(fh); w.writerow(["id", "path", "size_bytes", "md5_12", "duration_s", "codec", "width", "height", "avg_fps", "audio", "notes", "status"])
    for i, p, n, s in files:
        j = json.loads(subprocess.check_output([FP, "-v", "error", "-show_format", "-show_streams", "-of", "json", p]))
        v = [x for x in j["streams"] if x["codec_type"] == "video"][0]; a = [x for x in j["streams"] if x["codec_type"] == "audio"]
        num, den = map(int, v["avg_frame_rate"].split("/"))
        h = hashlib.md5(); f = open(p, "rb")
        for chunk in iter(lambda: f.read(1 << 22), b""): h.update(chunk)
        w.writerow([i, p, j["format"]["size"], h.hexdigest()[:12], round(float(j["format"]["duration"]), 2), v["codec_name"], v["width"], v["height"], round(num / den, 2),
                    f"{a[0]['codec_name']} {a[0]['sample_rate']} {a[0]['channels']}ch" if a else "none", n, s])
print("ok")
