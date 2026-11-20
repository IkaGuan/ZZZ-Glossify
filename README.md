# Glossify

Glossify makes skin look wet and shiny in Zenless Zone Zero.

中文说明：[README.zh-CN.md](README.zh-CN.md)

The shine comes from the oil mod. Glossify does not include that mod.

## Before you start

1. Install **ZZMI** with XXMI Launcher or FlairX. Use the one you already have.
2. Install **油光皮肤 OilSkin**: [GameBanana 741702](https://gamebanana.com/mods/741702).

<<<<<<< HEAD
Use the oil mod whose style key is **Shift+Numpad \***. It cycles styles 1 through 6. Keep one copy. A later package uses plain **Numpad \*** for style, and Glossify uses that key for patched body.
=======
1. **ZZMI** for Zenless Zone Zero. XXMI Launcher and FlairX Mod Manager both provide it. Use the one you already play with.
2. **油光皮肤 OilSkin**, the shared oil engine: [GameBanana 741702](https://gamebanana.com/mods/741702). Use the folder that contains `Matcap_ZZZ.ini` and the `Oil` textures. Glossify does not ship those files.
3. Use the engine whose style key is **Shift + Numpad \***, cycling styles 1 through 6. Leave any second copy of that engine out of the mods folder. Later package [GameBanana 741702](https://gamebanana.com/mods/741702) binds plain **Numpad \*** to style, and that key is also Glossify's patched-skin body toggle. You can change the keybind
>>>>>>> 9b14b396601274112a6192991bb753a60e68310a

## Install

Press **F10** in game after you add or change a folder. F10 reloads mods.

### XXMI Launcher

1. Put the oil folder in `ZZMI\Mods`. It contains `Matcap_ZZZ.ini` and an `Oil` folder.
2. Put the `Glossify` folder in `ZZMI\Mods`, next to the oil folder.
3. Start the game and press **F10**.

A folder in `Mods` loads by itself.

### FlairX

1. Enable the oil mod, and leave it enabled.
2. Put `Glossify` in your mods library and enable it. `Mods\Bangboo Skins` is a fine place.
3. Press **F10** in game.

Leave the Glossify folder name unchanged. FlairX turns a mod off by adding `DISABLED_` to the front of the folder name.

Put Glossify beside the oil folder. An oil update replaces the oil folder and removes anything stored inside it.

The download is the `Glossify` folder. `Glossify Patcher.exe` can sit anywhere on your PC. It does not go in `Mods`. You need it only when a skin replaced the body and that body stayed matte.

After F10, face gloss is on. Body gloss stays off until you press **Ctrl+Alt+F9**.

A `.bin` file next to an `.hlsl` file is a cache the game writes. It is not part of the download.

## How to use

Press **Ctrl+Alt+F7** to open the UI. Drag it anywhere. Click X, or press the same key, to close it.

**Stock face** and **stock body** shine the game's original models. Your agent, NPCs, and enemies use those models, so these two switches shine all of them.

**Patched body** and **patched face** shine a skin you patched. They work while that skin is on screen. The UI then says **PATCHED** and shows those switches. On any other model it says **STOCK** and hides them.

| You want | Set this |
| --- | --- |
| My agent shiny. NPCs and enemies matte. | Wear a patched skin. Patched body on. Patched face on. Stock face off. Stock body off. |
| Everyone shiny. | Stock face on. Stock body on. A replaced body stays matte until you patch that skin. |
| This patched skin keeps its own look. | Set the style and strength, then click **SAVE LOOK** while that skin is on screen. |
| All gloss off. | Turn off stock face, stock body, patched body, and patched face. |

Leave the Glossify folder loaded. Closing that folder also turns off gloss on patched skins.

The oil keys change the stock look. **Shift+Numpad \*** picks the style. **Numpad +** and **Numpad −** change the strength. **SAVE LOOK** stores that look on the patched skin. Click it again to store a new one.

With stock body on, a patched skin's body also shines while it is on screen. With stock face on, a stock face that the skin still uses also shines.

Hair stays matte. Anyone wearing that same patched model shines too.

The patched shortcuts are printed on the UI. Until you change them, patched body is **Numpad \*** and patched face is **Alt+Numpad \***.

## Keys

If green text is covering the screen, press **Numpad 0** until it disappears. That text is ZZMI's hunting display. While it is up, Numpad \*, +, and − do not change the gloss.

| Action | Key | Starts as |
| --- | --- | --- |
| Open or close the UI | Ctrl+Alt+F7 | Closed |
| Stock face | Ctrl+Alt+F8 | On |
| Stock body | Ctrl+Alt+F9 | Off |
| Patched face | Printed on the UI | Alt+Numpad \* |
| Patched body | Printed on the UI | Numpad \* |
| Style | Shift+Numpad \* | 1 through 6 |
| Stronger | Numpad + | 0, 2, 4, 6, 8, 10 |
| Weaker | Numpad − | 10 down to 0 |

The strength number runs from 0 to 10. Numpad + moves two steps, so it skips the odd numbers.

Each switch is remembered. If a key does nothing the first time, press F10 and try again.

To change a patched key, open Glossify Patcher, click **Mod Body** or **Mod Face**, press the new shortcut, then click **Apply Keybinds** and press F10. The UI shows the new shortcut. Esc cancels a key. **Reset** restores Numpad \* and Alt+Numpad \*.

## A skin stayed matte

If the character is already shiny, leave that skin alone. A skin that only swaps textures still gets stock gloss.

Patch a skin when it replaces the body, legs, or outfit and those parts stay matte. Patch one skin at a time.

1. Run `Glossify Patcher.exe`. If Windows says it protected your PC, click **More info**, then **Run anyway**. You can also build it with `.\build.ps1`.
2. Paste a skin folder, a character folder, or your `ZZMI\Mods` folder into **Mods Folder**, then press Enter. Or click **Choose Folder**.
3. Leave checked only the green **Patch** rows. Click **Patch**.
4. Press F10.

| You see | Do this |
| --- | --- |
| Already shiny | Leave it. |
| A replaced body, legs, or outfit stayed matte | Patch that skin. |
| NPCs or enemies turned shiny | Turn stock face and stock body off. Patch only the skins that should shine. |
| You want the old files back | **Restore Backups**, check that skin, confirm, then F10. |

The patcher skips hair, earrings, and blend passes. It treats a Head section as the face only when that model is the face. A skin patched before this fix needs a restore, then a new patch.

Backups are the edited `.ini` files. They go to `%LOCALAPPDATA%\OilSkinPatcher\Backups` unless you click **Change...**. Each skin keeps one backup. **Open Folder** shows them. Restore puts the original ini files back and removes `oilskin.json` and `OilSkinSwitch.ini`. If FlairX added `DISABLED_` to the folder name, restore still finds the skin.

| Colour | Status | What to do |
| --- | --- | --- |
| Green | Patch | Ready. Checked by default. |
| Grey | Up to Date | Already patched with this version. |
| Grey | Skipped - Built-in Matcap | This skin already has its own oil shader. Use the author's keys. |
| Grey | Skipped - No Draws | Nothing to gloss, such as a weapon-only or hair-only folder. |
| Orange | Manual Integration | Has an `OilSkinSwitch.ini` this tool did not write. Left alone. |
| Orange | Needs Review | Read the note after the dash. |
| Red | Broken (FlairX) | Marked broken in FlairX. Skipped. |

## Limits

A game update can change a character's model. That character's gloss turns off until Glossify is updated.

**Seth.** No face gloss and no body gloss.

**Roxy.** Face gloss works. Body gloss does not.

**Legs drawn with the hair.** Some agents draw the bare legs and the hair together. Glossify skips that pass, so the hair and those legs stay matte. Stock body can still shine the legs when they are the original model. A skin that replaced that model keeps the legs matte.

## Build

Windows .NET Framework 4.x is enough. Visual Studio is not required.

```powershell
.\build.ps1
```

That writes `bin\Glossify Patcher.exe`.

## Credits

- Oil shader and textures: [油光皮肤 OilSkin](https://gamebanana.com/mods/741702), a ZZZ port of [KatTDev's Wuthering Waves matcap](https://gamebanana.com/mods/733773). Not included here.
- Mesh hash list: [ZZ-Model-Importer-Assets](https://github.com/leotorrez/ZZ-Model-Importer-Assets)
