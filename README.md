# Glossy Skin Patcher v1.0

**Glossy Skin Patcher** is a lightweight Windows utility for **Zenless Zone Zero (ZZMI)** that automatically adds adjustable glossy / oily shine to character **bodies** and **faces** across your skin mods.

It connects compatible character mods to the shared **OilSkin** matcap & face-sheen engine (`KatFXMatcap` + `OilSkinFaceEngine`), supports separate in-game toggles for Body and Face gloss with customizable keybinds, and backs up modified `.ini` files automatically.

---

## Requirements

1. **ZZMI (3DMigoto for Zenless Zone Zero)** — via XXMI Launcher or FlairX Mod Manager.
2. **Shared OilSkin Engine (with Face Engine support)** — installed and enabled in your `ZZMI\Mods` folder (`Matcap_ZZZ.ini` + `Oil\` textures + `OilSkinFaceEngine.ini` + `OilSkinFaceEngine\FaceOil.hlsl`).
   - *Tip for FlairX Mod Manager users:* Place global engine folders inside `Mods\Bangboo Skins\` so FlairX keeps them enabled when switching character outfits.

---

## Installation & Quick Start

1. **Ensure the OilSkin Engine is enabled** inside `ZZMI\Mods\` (e.g. `Mods\Bangboo Skins\OilSkin\`).
2. *(Optional)* **Stock Base Faces:** If you also want face gloss on unpatched/default character outfits, copy the included `GlossySkinBaseFaces` folder into `ZZMI\Mods\` (or `Mods\Bangboo Skins\`).
3. **Run `Glossy Skin Patcher.exe`** *(can be placed anywhere on your PC)*:
   - **Mods Folder:** Paste a folder path (and press `Enter`) or click **Choose Folder**. You can select a single skin folder, a character category folder (e.g. `Ellen Joe`), or your entire `ZZMI\Mods` library.
   - **Backup Path:** Check where backups are saved (`%LOCALAPPDATA%\OilSkinPatcher\Backups` by default), click **Change...** to pick another location, or click **Open Folder** to view backups.
   - **Gloss Toggles:** Keep the default shortcuts or click either box (`Body Gloss` / `Face Gloss`) and press any key combo to customize them.
   - **Select & Patch:** Check/uncheck which green **Patch** skins you want to update, then click **Patch**.
4. **Reload in-game:** Launch Zenless Zone Zero (or press **F10** if already in-game) to load the changes.

> **Note on Windows SmartScreen:** Because this is a standalone unsigned `.exe`, Windows may show a *"Windows protected your PC"* prompt on first launch (`More info` → `Run anyway`). You can also inspect `OilSkin.cs` and compile the `.exe` yourself in seconds using `.\build.ps1`.

---

## Controls & Keybinds

Body Gloss and Face Gloss are **completely independent**—you can turn off face gloss while keeping body gloss on, or turn off body gloss while keeping face gloss on.

| Action | Default Shortcut | Where Configured | Notes |
|--------|------------------|------------------|-------|
| **Toggle Body Gloss** | `Numpad *` | Patcher UI (**Body Gloss**) | Cycles body gloss On / Off (`$\OilSkinToggles\body_enabled`) |
| **Toggle Face Gloss** | `Alt + Numpad *` | Patcher UI (**Face Gloss**) | Cycles face gloss On / Off (`$\OilSkinToggles\face_enabled`) |
| **Cycle Gloss Style** | `Shift + Numpad *` | OilSkin Engine (`Matcap_ZZZ.ini`) | Cycles between 6 matcap/wet presets |
| **Increase Shine Strength** | `Numpad +` | OilSkin Engine (`Matcap_ZZZ.ini`) | Steps shine intensity up (`0.0` → `1.0`) |
| **Decrease Shine Strength** | `Numpad -` | OilSkin Engine (`Matcap_ZZZ.ini`) | Steps shine intensity down (`1.0` → `0.0`) |
| **Toggle Stock Base Faces** *(optional)* | `Ctrl + Alt + Numpad *` | `GlossySkinBaseFaces\BaseFaces.ini` | Starts **Off** by default; for unpatched native faces |

> **Tip:** Make sure ZZMI hunting mode is turned **off** (press `Numpad 0` if green hunting text is on screen), otherwise `Numpad *` / `+` / `-` are captured by 3DMigoto's shader hunter.

### Changing Keybinds Later (No Re-Patching Needed)
Individual skin mods do **not** have hotkeys injected into them (keeping FlairX Mod Manager hotkey cards clean). Instead, all patched skins read from a single shared `GlossySkinToggles.ini` file automatically installed into your `ZZMI\Mods` folder (`Mods\Bangboo Skins\GlossySkinToggles\` if `Bangboo Skins` exists, or `Mods\GlossySkinToggles\`).

To change your Body or Face toggle keys later (for example, if using a TKL/60% keyboard without a Numpad):
1. Open `Glossy Skin Patcher.exe`.
2. Click the **Body Gloss** or **Face Gloss** box and press your desired key combination (`Esc` cancels; **Reset** restores defaults).
3. Click **Apply Keybinds**, then press **F10** in-game. Every already-patched skin immediately uses the new shortcuts.

---

## Status Labels Explained

| Colour | Status | What It Means & What To Do |
|--------|--------|----------------------------|
| Green | **Patch - Body + Face / Body / Face** | Ready to patch (or ready to upgrade from an older patcher version). Checked by default. |
| Grey | **Up to Date** | Already patched with the current controller; no changes needed. |
| Grey | **Skipped - Built-in Matcap** | **Already has custom matcap/oil shaders built in by the skin author** (common on custom Latex, Metal, or KatFXMatcap skins that bind their own `ps-t125` matcaps and have their own oil hotkey). **Do not force-patch**—leave it as-is and use the mod author's own built-in toggle keys so its custom materials aren't overwritten. |
| Grey | **Skipped - No Draws** | No compatible character body/face `drawindexed` calls found (e.g. weapon-only, hair-only, UI, or empty folders). |
| Orange | **Manual Integration** | Contains a custom `OilSkinSwitch.ini` written outside this tool. Left untouched for safety. |
| Orange | **Needs Review** | Unexpected file state or partially edited hooks—check the message shown after the dash. |
| Red | **Broken (FlairX)** | Marked as broken in FlairX Mod Manager (`mod.json` or `ZZ-BrokenMods.json`) and skipped automatically. |

---

## Backups & How to Restore (Unpatch)

Before modifying any skin, the patcher backs up only the `.ini` file(s) being edited (skipping heavy `.dds`/`.buf` assets) into `<Backup Path>\<ModName>\`, along with a `backup-map.json` reference file. Re-patching a mod preserves your clean original backup `<ModName>` and creates `<ModName> (2)`, `<ModName> (3)`, etc.

**To restore a mod to its original unpatched state:**
1. Click **Open Folder** next to **Backup Path** in the patcher and open `<ModName>` *(choose the base folder without `(2)` for the clean pre-patch original)*.
2. Copy the `.ini` file(s) inside *(ignore `backup-map.json`)* back into the skin mod's folder, overwriting the patched `.ini` file(s).
3. Delete `OilSkinSwitch.ini` and `oilskin.json` from the skin mod's root folder, then press **F10** in-game.

---

## Supported Mod Layouts

Supports deep directory structures (including paths exceeding Windows' legacy 260-character `MAX_PATH` limit), both **female and male** playable characters, `Other Misc` / NPC mods, and upgrades from older patcher versions:

- **Standard ZZMI exports:** `TextureOverride*Body*`, `TextureOverride*Face*`
- **Native Face auto-detection:** Automatically pairs body hashes to native face hashes (`BodyToFace` catalogue + local IB scan) when a skin uses the character's default face mesh
- **DaisyMeow & LOD exports:** `Head` sections and `TextureOverride_LOD0.<hash>` sections
- **Chinese-named exports (yyzj):** `shenti` (body), `lian` (face), `_IB_<hash>_shenti_Component<N>`
- **Part-numbered & split meshes:** `TextureOverride<hash>Part<N>`, `Torso` / `Legs` splits
- **Animated & multi-toggle mods:** Local `CommandList` draw chains
- **Grace Howard & Nicole Demara:** Dedicated handling for `Gracebody` / `_IB_Grace Howard_` and texture-only face overrides

Sections containing `hair`, `weapon`, `shadow`, `outline`, `wing`, `mask`, `bangboo`, or `custom` are automatically excluded.

### Known Interaction
- On Grace Howard's *Bondage & Reverse Bunny* skin, the face may appear black if the skin's own face toggle and face gloss are enabled simultaneously. Workaround: toggle **Face Gloss off**, switch the skin's face toggle, then toggle **Face Gloss back on**.

---

## Build from Source

No Visual Studio installation or external NuGet packages are required—only Windows' built-in `.NET Framework 4.x` C# compiler:

```powershell
.\build.ps1
```

This embeds `controller.ini` into `OilSkin.cs` and outputs `bin\Glossy Skin Patcher.exe`.

---

## Credits

- Original dual-layer matcap shader concept: [neerajksah / KatTDev's Wuthering Waves mod](https://gamebanana.com/mods/733773)
- Zenless Zone Zero OilSkin adaptation: [GameBanana ZZZ OilSkin](https://gamebanana.com/mods/741702)
- Character mesh hash catalogue: [ZZ-Model-Importer-Assets](https://github.com/leotorrez/ZZ-Model-Importer-Assets)
