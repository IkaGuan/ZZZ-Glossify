# Glossy Skin Patcher for ZZMI

Glossy Skin adds adjustable shine to character bodies and faces in Zenless Zone Zero. This Windows patcher connects compatible skins to the separately installed OilSkin engine. Existing files and ZZMI commands use the OilSkin name for compatibility.

## Build

On Windows with the .NET Framework C# compiler installed, open PowerShell in the repository folder and run:

```powershell
.\build.ps1
```

The script embeds `controller.ini` and produces `bin\OilSkinPatcher.exe`.

## Use

1. Start the EXE and select one skin, a folder of test copies, or your ZZMI Mods folder. A copy of one of your own skins is a good first test.
2. Review the proposed body and face coverage, then click **Apply**. The patcher backs up edited files and records the integration in `oilskin.json`. Scan the folder again to check its status.
3. To see the effect, enable the patched skin and the shared OilSkin engine in ZZMI and reload. **Numpad \*** toggles gloss. **Alt + Numpad \*** toggles face gloss.

## Known interaction

On Grace Howard's Bondage & Reverse Bunny skin, the face can appear black when the skin's face modification and face gloss are used together. The observed workaround is to switch **face gloss off**, enable the skin's own face modification, then switch **face gloss on**.

## Credits

The effect originated with [neerajksah's Wuthering Waves mod](https://gamebanana.com/mods/733773) and was adapted for [Zenless Zone Zero](https://gamebanana.com/mods/741702). This repository contains the patcher. Install the compatible shared engine with face support separately.
