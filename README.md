# Jolly64Emu

A work-in-progress **Commodore 64 emulator** written in **C# / .NET 10** with a **WPF** desktop interface.

Jolly64Emu is an experimental emulator project focused on building the C64 hardware components in C# and gradually improving compatibility with real software. It is **not yet a fully compatible or cycle-accurate emulator**.

## Current features

- MOS 6510/6502 CPU emulation and C64 memory bus, including ROM/RAM and I/O mapping.
- VIC-II video emulation with character and bitmap display modes, raster-register handling, and initial sprite rendering.
- CIA emulation for keyboard input, timers and interrupt-related behaviour (still under development).
- Keyboard matrix input and initial joystick port 2 support.
- Initial three-voice SID sound synthesis with triangle, sawtooth, pulse and noise waveforms, basic envelopes and NAudio playback.
- **PRG loading**, with automatic BASIC `RUN` support for suitable programs.
- **T64 loading**, extracting programs from tape archives without emulating a physical datasette.
- **D64 loading** via directory selection and direct PRG extraction (in the latest D64 update; no 1541 drive emulation).
- WPF display with a live assembly/disassembly panel.

## Project status

**Early development / experimental.** The emulator can boot into Commodore 64 BASIC and execute some software, but game compatibility is incomplete.

Known limitations include:

- **Beamrider** reaches the sector introduction but does not yet advance into gameplay. The cause has not been isolated; joystick input, CIA interrupts and VIC-II timing are areas to investigate.
- SID sound generation is implemented at a basic level, but **audible game sound has not yet been confirmed**. SID filters and other hardware details remain incomplete.
- VIC-II rendering and timing are not cycle-accurate. Some games may show incorrect graphics or fail while waiting for raster/interrupt events.
- D64 and T64 loaders extract files directly; they do not emulate a 1541 disk drive, datasette, custom fast loaders or subsequent disk/tape requests from running games.
- Keyboard and joystick mappings may still need refinement.

## Requirements

- Windows
- .NET 10 SDK
- Visual Studio with the .NET desktop development workload (recommended)
- NAudio 2.2.1 (restored automatically through NuGet)
- Appropriate C64 ROM images for your own lawful use

## Build and run

1. Clone the repository.
2. Open `Jolly64Emu.csproj` in Visual Studio, or run:

   ```powershell
   dotnet restore
   dotnet build
   dotnet run
   ```

3. Start the emulator and use the **File** menu to load a supported program or archive.

The project currently references embedded ROM files at:

```text
Core/ROMS/Basic.ROM
Core/ROMS/Char.ROM
Core/ROMS/Kernal.ROM
```

**ROM distribution:** Check the licensing and redistribution rights for these ROMs before pushing to a public repository. If you do not have permission to redistribute them, exclude them from Git and document how users should supply their own copies.

## Loading software

| Format | Support | Notes |
| --- | --- | --- |
| `.prg` | Initial support | Loads program data into C64 memory; BASIC auto-run where applicable |
| `.t64` | Initial support | Reads tape archive entries and extracts a selected program |
| `.d64` | Initial support | Reads a disk directory and extracts a selected PRG; **not** drive emulation |

> The D64 loader was supplied as a separate update after the latest full project archive. Ensure those changes have been merged before expecting **Load D64** to appear in the File menu.

## Code layout

```text
Core/
  Audio/        NAudio output
  Bus/          Memory and I/O bus
  CIA6526/      CIA devices
  CPU/          6502/6510 execution
  Keyboard/     Keyboard and joystick input
  ROMS/         C64 ROM resources
  SID6581/      SID synthesis
  VIC/          VIC-II graphics
  C64.cs        Emulator hardware integration
  PrgLoader.cs  PRG loading
  T64Loader.cs  T64 loading
  D64Loader.cs  D64 loading (latest update)
ViewModels/      WPF application state and emulation loop
MainWindow.xaml  Main emulator window
```

## Planned work

- Diagnose Beamrider's sector-intro stall using a targeted CPU and I/O trace.
- Verify end-to-end SID register writes, sample generation and speaker output.
- Improve VIC-II raster timing, interrupts, sprite behaviour and display accuracy.
- Improve CIA timer/interrupt accuracy and keyboard/joystick compatibility.
- Expand disk and tape support, potentially including true 1541 drive emulation.
- Add regression tests using small diagnostic programs and ROMs.

## Disclaimer

Jolly64Emu is an independent hobby/learning project and is not affiliated with Commodore or the rights holders of any commercial games or ROMs. No game software is required to be included in this repository.
