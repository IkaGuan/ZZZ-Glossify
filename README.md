# Glossy Skin Patcher v1.0

**Glossy Skin Base** glosses stock faces and bodies. **Glossy Skin Patcher** is only for a skin mod the base does not cover.

Use the base mod first. If a character already looks glossy, leave that skin mod alone. Use the patcher when a skin mod replaces the body mesh and the gloss stops on the parts that mod draws. Texture swaps that keep the original body draw still get gloss from the base, and do not need a patch.

---

## Requirements

1. **ZZMI (3DMigoto for Zenless Zone Zero)** — via XXMI Launcher or FlairX Mod Manager.
2. **Shared OilSkin Engine** — [GameBanana ZZZ OilSkin](https://gamebanana.com/mods/741702), installed and enabled (`Matcap_ZZZ.ini` + `Oil\` textures).
3. **Face engine** — `OilSkinFaceEngine.ini` and `OilSkinFaceEngine\FaceOil.hlsl` from this repo, in that same folder. Glossy Skin Base and the patcher call `CommandList\OilSkinFaceEngine\DrawOriginal`.
   - *Tip for FlairX Mod Manager users:* Place these folders inside `Mods\Bangboo Skins\` so FlairX keeps them enabled when switching character outfits.

The baseline is the engine already enabled as `油光皮肤OilSkin`, not `DISABLED_油光皮肤OilSkin2` and not a fresh unzip of `zzzoilskin_e8613.zip`. Those two author copies have the same body shine shader and the same `CommandList\KatFXMatcap\OilSkin` / `Clear` lists. They do not include the face engine, and both bind **Numpad \*** to the style cycle, including an off step. That key is the patcher's body toggle. The baseline keeps style on **Shift + Numpad \*** and cycles styles 1 through 6, so the oil does not turn off. The author's later package only adds a seventh WetDrop style on top of that same shader.

---

## Installation & Quick Start

1. **Ensure the OilSkin Engine is enabled** inside `ZZMI\Mods\` (e.g. `Mods\Bangboo Skins\油光皮肤OilSkin\`).
2. **Install Glossy Skin Base** into `ZZMI\Mods` (or `Mods\Bangboo Skins\`). Face gloss starts on (`Ctrl+Alt+F8`). Body gloss starts off (`Ctrl+Alt+F9`). Press **F10** and check the character.
3. **Run `Glossy Skin Patcher.exe` only for a mod the base does not gloss** *(the exe can sit anywhere on your PC)*:
   - **Mods Folder:** Paste that mod's folder, a character folder, or your `ZZMI\Mods` library, then press `Enter`. Or click **Choose Folder**.
   - **Backup Path:** Backups go to `%LOCALAPPDATA%\OilSkinPatcher\Backups` unless you click **Change...**. **Open Folder** shows them. **Restore Backups** puts the selected mods back.
   - **Gloss Toggles:** Keep the default shortcuts, or click a box and press a new combo.
   - **Select & Patch:** Leave checked only the green **Patch** rows that still need gloss, then click **Patch**. Skip a mod that already looks glossy.
4. **Reload in-game** with **F10**.

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
| **Toggle Glossy Skin Base face** | `Ctrl+Alt+F8` | `GlossySkinBase\GlossySkinBase.ini` | Starts **On**. Stock faces. |
| **Toggle Glossy Skin Base body** | `Ctrl+Alt+F9` | `GlossySkinBase\GlossySkinBase.ini` | Starts **Off**. Stock bodies. |

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

## When to use the patcher

| What you see | What to do |
|--------------|------------|
| Stock character, or a skin mod, already has the gloss you want | Leave it. The base mod is doing the work. |
| A skin mod replaced the body, legs, or outfit, and those parts stayed matte | Patch that one mod. The patcher puts the oil on the draws that mod owns. |
| You want the mod back to its original files | Use **Restore Backups** and check that mod. |

Patching a whole library is no longer the normal way to use this tool.

## Backups & Restore

Before modifying a skin the first time, the patcher backs up only the `.ini` file(s) being edited (skipping heavy `.dds`/`.buf` assets) into `<Backup Path>\<ModName>\`, along with a `backup-map.json` reference file. Each mod keeps that one backup. Patching the same mod again leaves it in place. Copies named `<ModName> (2)` and `<ModName> (3)` from older versions are removed, so the original backup is the one that stays.

**To restore mods:**
1. Click **Restore Backups** next to **Backup Path**.
2. Check the mods to put back. **Select All** checks every mod that has a backup. **Clear** unchecks them.
3. Confirm. The patcher puts each mod's original `.ini` files back, deletes `oilskin.json` and `OilSkinSwitch.ini` from those mod folders, and deletes every backup for those mods, including any `(2)` or `(3)` copy.
4. Press **F10** in-game.

FlairX renames a mod folder when you enable or disable it (`DISABLED_` on the front). Restore still finds that mod if the folder was renamed after the patch. If both the enabled and disabled copies exist, it restores the one that still has the oil files.

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
