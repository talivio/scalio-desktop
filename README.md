# Scalio

Free, open-source AI image upscaling for Windows. Scalio enlarges photos and
illustrations x2 or x4 with [Real-ESRGAN](https://github.com/xinntao/Real-ESRGAN)
running locally on your own GPU — no account, no subscription, and no image ever
leaves your computer.

- x2 / x4 upscaling, or a target width in pixels
- Photo and drawing/logo models
- Batch processing for whole folders
- Ready-made crops: hero banner, card, Instagram, Open Graph
- Before/after comparison slider
- English, Turkish and Estonian interface

Download the installer from **https://scalio.talivio.com**.

## Build

Requirements: Windows 10/11, .NET 8 SDK, WebView2 runtime, a Vulkan-capable GPU.

```powershell
dotnet build Scalio.sln
dotnet test Scalio.sln
./tools/fetch-engine.ps1 -Destination src/Scalio.App/bin/Debug/net8.0-windows/engine
dotnet run --project src/Scalio.App
```

`tools/fetch-engine.ps1` downloads the official Real-ESRGAN ncnn-vulkan release
and verifies its SHA-256; engine binaries are not stored in this repository.
`publish.ps1` builds the self-contained app and the Inno Setup installer
(`SCALIO_SIGN_THUMBPRINT` signs it when a code-signing certificate is installed).

`Scalio.Core` (engine runner, queue, pipeline) builds and tests on any OS:
`dotnet test tests/Scalio.Core.Tests`.

## Network use

The only request Scalio makes is an anonymous update check to
`https://scalio.talivio.com/api/version` (override with `SCALIO_RELAY_URL`).
Images are never uploaded.

## Licenses

Scalio is released under the [MIT License](LICENSE). The bundled engine is
Real-ESRGAN / ncnn-vulkan under the BSD 3-Clause license; see
[`engine-licenses/`](engine-licenses).
