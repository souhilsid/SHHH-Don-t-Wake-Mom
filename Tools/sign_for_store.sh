#!/bin/bash
# Re-sign an APK with the key the Meta Horizon store app already uses (the Android DEBUG key of this PC, cert SHA-256 63955fbe...7a4d).
# Builds 0.1.1/0.1.2 (codes 2-3, Nov 5) were uploaded as Unity development builds signed with ~/.android/debug.keystore;
# Meta only accepts updates signed with the same certificate. Backup of that keystore: Secrets/store-signing-debug.keystore.
# Usage: bash Tools/sign_for_store.sh Builds/Quest2_3_3S/SHHH_alpha_X_cNN.apk   -> writes ..._storekey.apk next to it (v2 scheme, like Unity)
set -e
AND="/c/Program Files/Unity/Hub/Editor/6000.0.62f1/Editor/Data/PlaybackEngines/AndroidPlayer"
J="$AND/OpenJDK/bin/java.exe"; JAR="$(cygpath -w "$AND/SDK/build-tools/36.0.0/lib/apksigner.jar")"
KS="$(dirname "$0")/../Secrets/store-signing-debug.keystore"
IN="$1"; OUT="${IN%.apk}_storekey.apk"
"$J" -jar "$JAR" sign --ks "$(cygpath -w "$KS")" --ks-key-alias androiddebugkey --ks-pass pass:android --key-pass pass:android \
  --v1-signing-enabled false --v2-signing-enabled true --v3-signing-enabled false --v4-signing-enabled false --out "$OUT" "$IN"
"$J" -jar "$JAR" verify --print-certs "$OUT" | grep "certificate SHA-256"
sha256sum "$OUT"
