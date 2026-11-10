# Respondeo.Maui

The Android app: a .NET MAUI Blazor Hybrid host (`net10.0-android`, package id `faith.respondeo.app`) that renders the
shared `Respondeo.UI` pages inside a `BlazorWebView`. All content is bundled, so the app works offline.

## Android presentation

The page's safe-area gutters stay black with light system-bar icons; only the inner grid and WebView follow the paper theme.
Native route changes briefly hide the content region while asynchronous WebView interop applies the destination's scroll position.
The mask preserves layout for measurement and waits for content/remembered-position scrolling to settle. A three-second timeout
reveals content and logs a warning if navigation fails or never lands. The website retains its synchronous, unmasked navigation.

## Building and installing

Requires the `maui-android` workload (`dotnet workload install maui-android`) and a device or emulator.

```powershell
dotnet build src/Respondeo.Maui -f net10.0-android -c Release -t:Install
```

Trimming and AOT are disabled, including in Release builds, because content loaders deserialize DTOs through reflection.
Re-enabling them requires separate compatibility work and published-APK verification.

## Releases

App-relevant pushes to `master`, or a manual `workflow_dispatch` with `release_apk` enabled, run `android-build` in
[`deploy.yml`](../../.github/workflows/deploy.yml). Public APKs require all four signing secrets:
`ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASSWORD`, `ANDROID_KEY_ALIAS`, and `ANDROID_KEY_PASSWORD`.
Missing credentials fail the build; public releases never fall back to debug signing. Keep the same signing key across releases
so updates can replace installed versions without losing reader data.

The shared publish action checks the signed APK's archive, signature, and ZIP alignment, then uploads it with `SHA256SUMS`.
The `android-release` job waits for shared checks, Android verification, and applicable browser acceptance tests.
It downloads the verified artifact, checks its checksum, and publishes `respondeo-$VERSION.apk` without rebuilding or re-signing it.
Release notes list commits since the previous release tag (`v$VERSION`), with a compare link; the first release lists the last 30 commits.

PRs use the same publish/verification action without release secrets. Their debug-signed APKs are uploaded as `android-validation`
with `respondeo-validation-$VERSION.apk` filenames. They are for testing only and cannot update a release-signed installation.
Device lifecycle, visual rendering, and upgrade persistence still require the focused Android checks described in the streamlining plan.

## Icon

The app icon (`Resources/AppIcon`) matches the site favicon.
