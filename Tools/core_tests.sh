#!/bin/bash
# V10: run the Core rules tests on the PC WITHOUT a .NET SDK, using the Unity Editor's bundled .NET 6 runtime + Roslyn.
# Compiles Core/SHHH.Core.Tests/*.cs + UnityProject/Assets/SHHH/Scripts/Core/**/*.cs (the exact files Unity compiles) into
# Core/SHHH.Core.Tests/bin/pc/SHHH.Core.Tests.dll and runs it. Extra args are passed to the test program (e.g. --coverage ...).
set -e
cd "$(dirname "$0")/.."
U="${UNITY_DATA:-/c/Program Files/Unity/Hub/Editor/6000.0.62f1/Editor/Data}"
DOTNET="$U/NetCoreRuntime/dotnet.exe"; CSC="$U/DotNetSdkRoslyn/csc.dll"
FX=$(ls -d "$U/NetCoreRuntime/shared/Microsoft.NETCore.App/"* | tail -1)
FXV=$(basename "$FX")
OUT=Core/SHHH.Core.Tests/bin/pc; mkdir -p "$OUT"
RSP="$OUT/csc.rsp"; : > "$RSP"
for f in "$FX"/*.dll; do case "$(basename "$f")" in api-ms-*|clr*|coreclr*|host*|Microsoft.DiaSymReader*|*Native*|mscordaccore*|mscordbi*|mscorrc*|ucrtbase*|msquic*|Microsoft.VisualBasic*|netstandard.dll) ;; System.*|Microsoft.*|mscorlib.dll|WindowsBase.dll) echo "-r:\"$(cygpath -w "$f")\"" >> "$RSP";; esac; done
echo "-r:\"$(cygpath -w "$FX/netstandard.dll")\"" >> "$RSP"
{ echo "-nologo -langversion:9.0 -nullable:disable -optimize+ -target:exe -nowarn:CS1701,CS1702,CS0618,CS0162,CS0219,CS0414,CS0649,CS0169"
  echo "-out:\"$(cygpath -w "$OUT/SHHH.Core.Tests.dll")\""
  for f in Core/SHHH.Core.Tests/*.cs; do echo "\"$(cygpath -w "$f")\""; done
  find UnityProject/Assets/SHHH/Scripts/Core -name '*.cs' | while read -r f; do echo "\"$(cygpath -w "$f")\""; done; } >> "$RSP"
"$DOTNET" "$CSC" "@$(cygpath -w "$RSP")"
cat > "$OUT/SHHH.Core.Tests.runtimeconfig.json" <<JSON
{ "runtimeOptions": { "tfm": "net6.0", "framework": { "name": "Microsoft.NETCore.App", "version": "$FXV" } } }
JSON
cd Core/SHHH.Core.Tests && "$DOTNET" "bin/pc/SHHH.Core.Tests.dll" "$@"
