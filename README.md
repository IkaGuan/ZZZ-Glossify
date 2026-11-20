# Glossify

Gloss for stock faces and bodies in Zenless Zone Zero, plus a patcher for skin mods that replace the mesh.

中文说明：[README.zh-CN.md](README.zh-CN.md)

Glossify does not include the oil shader or its textures. Those stay in the author's oil mod.

## Dependencies

Install and enable these before Glossify. Order matters.

1. **ZZMI** for Zenless Zone Zero, through XXMI Launcher or FlairX Mod Manager.
2. **油光皮肤 OilSkin**, the shared oil engine: [GameBanana 741702](https://gamebanana.com/mods/741702). Enable the folder that contains `Matcap_ZZZ.ini` and the `Oil` textures. Glossify does not ship those files.
3. Use the engine whose style key is **Shift + Numpad \***, cycling styles 1 through 6. Do not enable a second copy of that engine. The author's later package binds plain **Numpad \*** to style, and that key is also Glossify's patched-mod body toggle.

## Installation

Enable two folders. Leave both enabled. Press **F10** in game.

| Folder | Where it comes from | What it does |
| --- | --- | --- |
| `Glossify` | This download. Put it in `ZZMI\Mods`, or in `Mods\Bangboo Skins` if you use FlairX. | Stock faces and bodies, the face engine, the panel, and the keys for patched skins. One folder. |
| `油光皮肤 OilSkin` | [GameBanana 741702](https://gamebanana.com/mods/741702) | The oil shader and textures. Not included here. |

Do not copy Glossify into the oil folder. The next oil update would delete it.

`Glossify` contains `GlossifyBase.ini`, `FaceFallback.hlsl`, `GlossifyFaceEngine.ini`, `GlossifyFaceEngine\FaceOil.hlsl`, `GlossifyToggles.ini`, `GlossifyToggles.hlsl`, `Font.dds`, `GlossifySkinToggles.ini`, and `mod.json`.

`Glossify Patcher.exe` is only for a skin mod the stock hooks do not cover. It can sit anywhere on the PC. It does not go inside `Mods`.

After F10, look at a stock character. Face gloss should already be on. Body gloss is off until you press **Ctrl+Alt+F9**.

Shader files named `.bin` next to an `.hlsl` are a cache the game writes. They are not part of the download.

## Keys

Turn hunting mode off first. If green hunting text is on screen, press **Numpad 0**. Hunting captures Numpad \*, +, and −.

| Action | Key | Starts |
| --- | --- | --- |
| Show or hide the panel | Ctrl+Alt+F7 | Hidden. Drag the panel anywhere. Click X, or press the same key, to close it. |
| Stock face | Ctrl+Alt+F8 | On |
| Stock body | Ctrl+Alt+F9 | Off |
| Patched mod face | Alt+Numpad \* | On |
| Patched mod body | Numpad \* | On |
| Oil style | Shift+Numpad \* | Style 1 through 6, on the oil engine |
| Stronger | Numpad + | The panel shows 0, 2, 4, 6, 8, 10 |
| Weaker | Numpad − | The panel shows 10 down to 0, one step at a time |

The panel's strength number is the engine's 0.0–1.0 shine, shown as 0 through 10. Numpad + uses the engine's 0.2 steps, so it skips the odd numbers.

The choice for each toggle is saved. If a key seems to do nothing the first time, press F10 once and try the key again.

To change the patched-mod keys, open Glossify Patcher, click **Body Gloss** or **Face Gloss**, press the new combo, then click **Apply Keybinds** and press F10. Already patched skins pick up the new keys. Esc cancels a key. **Reset** restores Numpad \* and Alt+Numpad \*.

## When a skin mod stays matte

Use the base first. If the character is already glossy, leave that skin mod alone. Texture swaps that keep the original body draw get gloss from the base.

Use the patcher only when a mod replaces the body, legs, or outfit and those parts stay matte. Patch that one mod. Do not patch the whole library.

The patcher hooks a face section that draws with a number, with `drawindexed = auto`, or with `draw =`. It also hooks a face section that only swaps the face texture and never draws, including sections named `脸`. A section named Head is hooked only when that mesh is the character's face. Hair pieces, earrings, and blend-shape passes stay skipped. A mod patched before this face fix keeps the old result until you restore that mod and patch it again.

## Only the agents you patch

Patch each skin mod on its own if you want that agent glossy and NPCs matte. Stock face and stock body follow the original meshes, so they also gloss some shop NPCs. An unpatched mod that still uses a stock face or body glosses every NPC who draws that mesh. Leave the `Glossify` folder enabled and turn the stock toggles off:

1. Press **Ctrl+Alt+F8** until the stock face is off.
2. Press **Ctrl+Alt+F9** until the stock body is off.
3. Patch only the agent skins that should shine. **Numpad \*** is the patched body. **Alt+Numpad \*** is the patched face. **Ctrl+Alt+F9** also glosses that patched mod's body while it is on screen. **SAVE LOOK** on the panel stores the current style and strength on that mod. After that, the oil keys change the stock look only. Press **SAVE LOOK** again to copy the stock look onto the mod.

Do not disable the `Glossify` folder to hide NPC gloss. That also turns off the face engine and the patched-mod keys, and patched faces go matte.

A patched mesh still shines on anyone who draws that mesh. With stock face off, the patched face key glosses the face while a patched mod is on screen, including a stock face that mod still uses. Hair stays matte on purpose. The panel says PATCHED while that mod is drawing, and STOCK on any other model. The patched body and face rows show up only for PATCHED.

1. Run `Glossify Patcher.exe`. Windows SmartScreen may say "Windows protected your PC" the first time, because the exe is unsigned. **More info**, then **Run anyway**. You can also compile it yourself with `.\build.ps1`.
2. **Mods Folder:** paste a mod folder, a character folder, or the `ZZMI\Mods` library, then Enter. Or click **Choose Folder**.
3. Leave checked only the green **Patch** rows that still need gloss. Click **Patch**.
4. Press F10.

| What you see | What to do |
| --- | --- |
| Stock character, or a skin, already glossy | Leave it. |
| A replaced body, legs, or outfit stayed matte | Patch that mod. |
| NPCs turned glossy | Turn stock face and stock body off, and patch only the mods that should shine. |
| You want the original files back | **Restore Backups**, check that mod, confirm, then F10. |

Backups are the edited `.ini` files only. They go to `%LOCALAPPDATA%\OilSkinPatcher\Backups` unless you click **Change...**. Each mod keeps one backup. **Open Folder** shows them. Restore puts the original ini files back, removes `oilskin.json` and `OilSkinSwitch.ini` from that mod, and deletes the backup. FlairX adds `DISABLED_` to a folder name when you disable a mod. Restore still finds the mod after that rename.

| Colour | Status | What to do |
| --- | --- | --- |
| Green | Patch | Ready. Checked by default. |
| Grey | Up to Date | Already patched with this version. |
| Grey | Skipped - Built-in Matcap | The skin author already has an oil shader. Leave it on their keys. |
| Grey | Skipped - No Draws | Nothing to gloss, such as a weapon-only or hair-only folder. |
| Orange | Manual Integration | Has an `OilSkinSwitch.ini` this tool did not write. Left alone. |
| Orange | Needs Review | Read the note after the dash. |
| Red | Broken (FlairX) | Marked broken in FlairX. Skipped. |

## Limits

Hashes are exact. A game update that changes a mesh turns that character's gloss off until the list is refreshed.

**Seth.** No face gloss and no body gloss.

**Roxy.** Face gloss works. Body gloss does not. Her body hash `d5e1e7ff` is not in this mod.

**Lower body that is drawn with the hair.** On some agents the bare legs and the hair are one draw. Glossify skips that draw so the hair stays matte, and the legs in that same draw stay matte too. Seen on Alex, Ellen, Caesar, Soldier 0, and Vivian. Try base body: press **Ctrl+Alt+F9**. If those legs are still the stock mesh, the base body hook can gloss them without glossing the hair draw. If a skin mod replaced that mesh, the base cannot reach it, and patching the mod still skips the combined hair draw.

**Lighter.** The lower body does not gloss.

**Ye Shunguang.** The stock body and stock legs can gloss. A skin that replaces the legs, including Monikirakira, keeps those replaced legs matte until that mod is patched.

On Grace Howard's Bondage & Reverse Bunny skin, the face can turn black if that skin's own face toggle and face gloss are both on. Turn face gloss off, switch the skin's face, then turn face gloss back on.

## Build

Windows .NET Framework 4.x is enough. Visual Studio is not required.

```powershell
.\build.ps1
```

That writes `bin\Glossify Patcher.exe`.

## Credits

- Oil shader and textures: [油光皮肤 OilSkin](https://gamebanana.com/mods/741702), a ZZZ port of [KatTDev's Wuthering Waves matcap](https://gamebanana.com/mods/733773). Not included here.
- Mesh hash list: [ZZ-Model-Importer-Assets](https://github.com/leotorrez/ZZ-Model-Importer-Assets)
