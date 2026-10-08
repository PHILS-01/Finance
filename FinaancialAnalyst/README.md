# FinLite
- `android/`  Kotlin APK (no dependencies)
- `windows/`  single-file C# WinForms exe

## Build with GitHub Actions
1. Create a GitHub repo and push this folder to `main`.
2. Open the Actions tab, run "Build FinLite" (or push any commit).
3. Download `FinLite-android-apk` and `FinLite-windows-exe` from the run's Artifacts.
4. For a public download page: `git tag v1.0 && git push origin v1.0` publishes both files to a GitHub Release.
