# Respondeo.Maui

The Android app: a .NET MAUI Blazor Hybrid host (`net11.0-android`, package id `faith.respondeo.app`) that renders the
shared `Respondeo.UI` pages inside a `BlazorWebView`. All content is bundled, so the app works offline.

`MauiProgram` supplies native adapters and package transport, then calls the shared `AddRespondeoApplication` registration.
Both hosts embed `Respondeo.UI/Configuration/appsettings.json`; missing or invalid required settings fail startup.
See [deployment configuration](../../docs/deployment.md#feature-flags) for the shared baseline and override precedence.

The authored native shell is `index.template.html`. Both hosts compose shared startup blocks from `build/ShellFragments.xml` at build time;
`wwwroot/index.html` is generated under `obj` and packaged at the unchanged WebView host-page path.
See [shared HTML startup](../../docs/deployment.md#shared-html-startup) before editing startup scripts, links, or error markup.

## Android presentation

The page's safe-area gutters stay black with light system-bar icons; only the inner grid and WebView follow the paper theme.
Native route changes briefly hide the scrolling page shell, including its masthead and footer, while asynchronous WebView interop
applies the destination's scroll position. Internal link clicks and history Back/Forward start the mask synchronously in JavaScript,
before routing can shorten the document and expose the masthead. The router's interop call also covers programmatic navigation.
The mask preserves layout for measurement and waits for content/remembered-position scrolling to settle. A three-second timeout
reveals the page and logs a warning if navigation fails or never lands. Native pages skip the shared entry fade/rise, so they appear
fully drawn at their landing position; the reel's independent card transition is unchanged.
The website retains its synchronous, unmasked navigation and page-entry fades.

## Building and installing

Requires the `maui-android` workload (`dotnet workload install maui-android`) and a device or emulator.

```powershell
dotnet build src/Respondeo.Maui -f net11.0-android -c Release -t:Install
```

Trimming is disabled because content loaders deserialize DTOs through reflection. ReadyToRun compilation is also disabled,
preserving the previous non-AOT build policy. Changes to these settings require published-APK verification.

## Releases

App-relevant pushes to `master`, or a manual `workflow_dispatch` with `release_apk` enabled, run `android-build` in
[`deploy.yml`](../../.github/workflows/deploy.yml). Public APKs require all four signing secrets:
`ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASSWORD`, `ANDROID_KEY_ALIAS`, and `ANDROID_KEY_PASSWORD`.
Missing credentials fail the build; public releases never fall back to debug signing. Keep the same signing key across releases
so updates can replace installed versions without losing reader data.

The shared publish action checks the signed APK's archive, signature, ZIP alignment, shell references, shared asset hashes, and bundled manifest targets.
It also tests native content transport against the final APK's `assets/wwwroot/` entries and runs the focused API 36 emulator smoke
before uploading it with `SHA256SUMS`. The signing keystore is removed before device tooling starts; the tested APK is not rebuilt or resigned.
Native plain-name CSS/JS packaging is intentional; browser fingerprint mappings and native paper-color parity are checked separately.
See [packaging contracts](../../docs/deployment.md#packaging-contracts) for commands, host exceptions, and the limits of ZIP-backed tests.
The `android-release` job waits for shared checks, Android verification, and applicable browser acceptance tests.
It downloads the verified artifact, checks its checksum, and publishes `respondeo-$VERSION.apk` without rebuilding or re-signing it.
Release notes list commits since the previous release tag (`v$VERSION`), with a compare link; the first release lists the last 30 commits.

PRs use the same publish/verification action without release secrets. Their debug-signed APKs are uploaded as `android-validation`
with `respondeo-validation-$VERSION.apk` filenames. They are for testing only and cannot update a release-signed installation.
See [native test commands](../../tests/Respondeo.AcceptanceTests/README.md#native-lifecycle-and-upgrade) to run the same APK checks locally
or explicitly test a prior same-key APK upgrade. Emulator results do not close physical-device flicker, icon-contrast, or stored-progress checks;
use the [release checklist](../../docs/deployment.md#physical-device-release-checklist) as well.

## In-app APK updates

Release-key builds enable `EnableApkUpdates=true`. Development and PR builds do not check for public updates.
On startup and foreground activation, the app checks the latest GitHub Release's `android-update.json` over HTTPS.
Automatic successful checks are limited to once every six hours per process; failed checks wait at least 15 minutes. Automatic prompts offer a version only once per process.
The quick menu's **Check for updates** action bypasses both limits, so a declined or failed update can be retried immediately.
Manual checks report when the app is up to date, offline, or unable to retrieve update information. The action is hidden in browser and update-disabled builds.
No network connection is required to keep using the app, and declining an update never blocks reading.

The release job generates the manifest from the verified APK: package id, numeric Android version code, display version,
immutable release download URL, byte length, and SHA-256. The current version convention is `1.0.<versionCode>`.
Downloads show progress and can be canceled. Partial or corrupt downloads are deleted. Only the dedicated update cache is shared with Android's installer.
The APK must have the expected package/version and the same signing certificate set as the installed app; Android performs final signature verification and asks for installation consent.
On Android 8+, first-time users must allow installs from Respondeo in system settings. The pending handoff survives process recreation and asks again before opening the installer.
Updates replace the installed app without clearing its data. Signing-key rotation is not supported by this flow; retain the existing release key.

The first APK containing this feature must still be installed manually. It can then offer later releases; older APKs cannot acquire update checking on their own.
Automatic check failures are logged under `Respondeo.Update` in Android logcat without interrupting reading; failures after accepting a download also show an alert.

Verify the real update flow on a dedicated Android device before release: decline, cancel a download, deny/allow install permission,
return from settings, cancel the installer, then complete a same-key, higher-version update and confirm reading preferences and progress remain intact.
The deterministic parser/download tests do not replace these Android installer checks.

## Icon

The app icon (`Resources/AppIcon`) matches the site favicon.
