# OilSkinBoo

OilSkinBoo makes agent skin look wet in Zenless Zone Zero. NPCs, bangboo, and enemies stay matte.

中文说明：[README.zh-CN.md](README.zh-CN.md)

The shine comes from OilSkin 2. OilSkinBoo does not include that mod. Keep one oil folder enabled, `油光皮肤OilSkin2`. Leave the older oil folder disabled.

## Install

1. Install **ZZMI** with XXMI Launcher or FlairX. Use the one you already have.
2. Install **油光皮肤 OilSkin 2**: [GameBanana 741702](https://gamebanana.com/mods/741702). The folder contains `Matcap_ZZZ.ini` and an `Oil` folder.
3. Put `OilSkinBoo` in `ZZMI\Mods` for the English panel, or `油光布` for the Chinese panel. Put it beside the oil folder, not inside it. Enable only one.
4. Start the game and press **F10**.

FlairX turns a mod off by adding `DISABLED_` to the front of the folder name. A `.bin` file next to an `.hlsl` file is a cache the game writes. It is not part of the download.

The first F10 after a shader change can take several seconds. Later F10 reloads are fast.

## How to use

Press **Ctrl+Alt+F7** to open the panel. Drag it anywhere. Click X, or press the same key, to close it.

**Face** and **Body** turn gloss on for agents. With both on, agents shine and everyone else stays matte. Face starts on. Body starts off.

**Style** walks 0 through 7. 0 is off. 7 is the wet-drop style. One press moves one style.

**Stronger** and **Weaker** move the level by 1. The level runs from 0 to 10.

**Save** stores the style and level for the agent on screen. Changes after Save stay on that agent. Changes before Save change the shared default. **Reset** drops that agent's save and returns them to the default.

Change a key in FlairX by pressing the new key, then press F10. The panel shows the new name.

## Keys

If green text is covering the screen, press **Numpad 0** until it disappears. That text is ZZMI's hunting display.

| Action | Key | Starts as |
| --- | --- | --- |
| Open or close the panel | Ctrl+Alt+F7 | Closed |
| Face | Ctrl+Alt+F8 | On |
| Body | Ctrl+Alt+F9 | Off |
| Style | Numpad * | 0 through 7 |
| Stronger | Numpad + | One level |
| Weaker | Numpad − | One level |

The oil mod's own numpad keys stay off, so an OilSkinBoo key on numpad *, +, or − moves once.

## Limits

A game update can change a character mesh. That character stays matte until OilSkinBoo gets the new hash.

A body hash OilSkinBoo does not list stays matte. The rest of the game is unchanged.

Rina, Lighter, Caesar, Soldier 0, and Vivian keep the hair matte and the legs glossy. Ellen is glossy on the whole mesh, including the hair.

## Credits

- Oil shader and textures: [油光皮肤 OilSkin](https://gamebanana.com/mods/741702), a ZZZ port of [KatTDev's Wuthering Waves matcap](https://gamebanana.com/mods/733773). Not included here.
- Mesh hash list: [ZZ-Model-Importer-Assets](https://github.com/leotorrez/ZZ-Model-Importer-Assets)
