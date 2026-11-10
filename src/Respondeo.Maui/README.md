# Respondeo.Maui

The Android app: a .NET MAUI Blazor Hybrid host (`net10.0-android`, package id `faith.respondeo.app`) that renders the
shared `Respondeo.UI` pages inside a `BlazorWebView`. All content is bundled, so the app works offline.

## Building and installing

Requires the `maui-android` workload (`dotnet workload install maui-android`) and a device or emulator.

```powershell
dotnet build src/Respondeo.Maui -f net10.0-android -c Release -t:Install
```

Release builds are trimmed; if a type is only reached by reflection, keep it from being trimmed rather than disabling trimming.

## Releases

The `android-release` job in [`deploy.yml`](../../.github/workflows/deploy.yml) publishes an APK on a manual
`workflow_dispatch` with `release_apk` enabled. It signs with the `ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASSWORD`,
`ANDROID_KEY_ALIAS`, and `ANDROID_KEY_PASSWORD` secrets when present (otherwise a debug key) and uploads
`respondeo-$VERSION.apk` to a GitHub Release. Release notes list the commit messages since the previous release tag
(the job tags each release `v$VERSION` itself, so no manual tagging is needed), with a full-changelog compare link; the first
release falls back to the last 30 commits.

## Icon

The app icon (`Resources/AppIcon`) matches the site favicon.
