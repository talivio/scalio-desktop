# Scalio

Free, open-source AI image upscaling for Windows, macOS and Linux. Scalio enlarges photos and
illustrations x2 or x4 with [Real-ESRGAN](https://github.com/xinntao/Real-ESRGAN)
running locally on your own GPU — no account, no subscription, and no image ever
leaves your computer.

- x2 / x4 upscaling, or a target width in pixels
- Photo and drawing/logo models
- Batch processing for whole folders
- Ready-made crops: hero banner, card, Instagram, Open Graph
- Before/after comparison slider
- English, Turkish and Estonian interface

Download it from **https://scalio.talivio.com**.

| System | Package | Requirements |
|--------|---------|--------------|
| Windows 10/11 (x64) | `Scalio-x.y.z-setup.exe` | Vulkan-capable GPU, WebView2 runtime |
| macOS 12+ (Apple Silicon and Intel) | `Scalio-x.y.z-macos.dmg` (universal) | nothing extra — Metal via MoltenVK is built into the engine |
| Linux x86_64 (Ubuntu 22.04+, Debian 12+, Fedora 36+) | `scalio_x.y.z_amd64.deb`, `Scalio-x.y.z-x86_64.AppImage` | Vulkan driver, WebKitGTK 4.1 (the .deb installs both) |

Without a working GPU / Vulkan driver the engine cannot run (it has no CPU
mode); Scalio says so instead of failing silently. `Scalio --selftest` prints
every link of the chain and really runs the engine once.

## Layout

| Project | |
|---------|--|
| `src/Scalio.Core` | engine runner, queue, pipeline — any OS |
| `src/Scalio.Shell` | the HTML interface (`ui/`) and the interface↔engine bridge, shared by both shells |
| `src/Scalio.App` | Windows shell: WPF + WebView2 |
| `src/Scalio.Desktop` | macOS / Linux shell: [Photino.NET](https://www.tryphotino.io) (WKWebView / WebKitGTK) |

## Build

`Scalio.Core` builds and tests on any OS: `dotnet test tests/Scalio.Core.Tests`.
Engine binaries are not stored in this repository: `tools/fetch-engine.ps1` /
`tools/fetch-engine.sh` download the official Real-ESRGAN ncnn-vulkan release
and verify its SHA-256.

**Windows** (.NET 8 SDK, WebView2 runtime):

```powershell
dotnet build Scalio.sln
./tools/fetch-engine.ps1 -Destination src/Scalio.App/bin/Debug/net8.0-windows/engine
dotnet run --project src/Scalio.App
./publish.ps1   # installer; SCALIO_SIGN_THUMBPRINT signs it
```

**macOS / Linux** (.NET 8 SDK):

```bash
./tools/fetch-engine.sh macos /tmp/scalio-engine   # or: linux
SCALIO_ENGINE_PATH=/tmp/scalio-engine dotnet run --project src/Scalio.Desktop
```

```bash
./tools/package-macos.sh   # universal Scalio.app + .dmg
./tools/package-linux.sh   # .deb + AppImage (on Linux)
```

### macOS signing and notarization

Without signing the app is ad-hoc signed: it runs on the Mac that built it, but
Gatekeeper on other Macs refuses to open it until the user allows it in System
Settings → Privacy & Security. Release builds must be signed and notarized.
CI never signs; signed builds are made on the Mac that holds the certificate.

One-time setup (Apple Developer account holder):

1. developer.apple.com → Certificates → **Developer ID Application**
   (not "Apple Distribution", which only works for the App Store). Create it
   from a CSR made in Keychain Access on the build Mac and install the `.cer`
   there; `security find-identity -v -p codesigning` must list it.
2. App Store Connect → Users and Access → Integrations → **App Store Connect API**:
   create a key with the *Developer* role and download the `.p8` (once).
3. Store the notarization credentials in the keychain under a profile name:

   ```bash
   xcrun notarytool store-credentials scalio-notary --key <path/to/AuthKey.p8> --key-id <KEY ID> --issuer <ISSUER ID>
   ```

Every release:

```bash
SCALIO_MAC_SIGN_IDENTITY="Developer ID Application: <name> (<team id>)" \
SCALIO_NOTARY_PROFILE=scalio-notary \
./tools/package-macos.sh
spctl -a -vv -t exec dist/macos/Scalio.app   # must say "accepted, source=Notarized Developer ID"
```

The script signs inside-out with the hardened runtime
(`packaging/macos/Scalio.entitlements`: JIT only), submits the dmg to Apple,
waits for the verdict and staples the ticket. The bundle identifier is
`com.talivio.scalio`; Developer ID distribution does not need an App ID.

On Linux, the NuGet build of `Photino.Native.so` requires glibc 2.38 (Ubuntu
24.04+). The release workflow builds the same version from source on Ubuntu
22.04 and passes it as `PHOTINO_NATIVE_SO`, so the packages also run on 22.04.

## Network use

The only request Scalio makes is an anonymous update check to
`https://scalio.talivio.com/api/version` (override with `SCALIO_RELAY_URL`).
Images are never uploaded.

## Licenses

Scalio is released under the [MIT License](LICENSE). The bundled engine is
Real-ESRGAN / ncnn-vulkan under the BSD 3-Clause license; see
[`engine-licenses/`](engine-licenses).
