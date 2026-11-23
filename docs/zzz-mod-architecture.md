# ZZZ mod architecture

Read this before building, patching, or debugging a Zenless Zone Zero mod. The installed game stack and the Phoenix packages are the source for how a mod loads. OilSkinBoo is a layer on top of that, not a second way to replace a mesh. The product name is OilSkinBoo. The namespaces stay `Glossify*`.

Public guides (LeoTools ZZMI package, [INI basics](https://litho9.github.io/ini/ini-basics.html), [texture slots](https://litho9.github.io/ini/beyond-texture-slots.html)) describe the same loader. When a guide's slot table disagrees with the installed `Core\ZZMI` files, trust the install. Guides that call `ps-t0` the diffuse map are wrong for this game.

## Where things live

| Piece | Path |
| --- | --- |
| Live mods | `C:\Users\Ika\Desktop\zzz\FlairX Mod Manager\app\XXMI\ZZMI\Mods` |
| Game | `C:\Program Files\HoYoPlay\games\ZenlessZoneZero Game` |
| Loader | `ZZMI\d3dx.ini` includes `Core\ZZMI\main.ini`, then `include_recursive = Mods` |
| Core | `ZZMI\Core\ZZMI\` — ZZMI 1.5.2 (`$version = 1.52`) |
| Working character mods | `Mods\Phoenix Reffaella\` |
| Oil engine | `Mods\Bangboo Skins\油光皮肤OilSkin2\Matcap_ZZZ.ini` |
| OilSkinBoo source | this repo. There is no `Glossify\` folder. |
| Persisted variables | `ZZMI\d3dx_user.ini` |

FlairX groups mods into character folders (`Phoenix Reffaella`, `Ellen Joe`, …). ZZMI does not care about those folders. It loads every `.ini` under `Mods`, including nested ones. A folder whose name starts with `DISABLED_` is off. Leave it off. Most Phoenix packages are disabled on purpose. Disabled does not mean broken. `PhoenixHalfNudeModRenKamui` is the enabled mesh mod. `DISABLED_Phoenix -yyzj-` is the clean split into body, face, and hair. Use both as structure, not as outfits to copy.

`mod.json` is manager metadata (`author`, `version`, `syncMethod`). It does not decide what loads. `syncMethod: classic` is the usual skin. OilSkinBoo uses `syncMethod: namespace` so FlairX can map ini files to namespaces. An ini still loads if it is missing from that list.

The repo packs all declare the same six namespaces: `GlossifyBase`, `GlossifyFaceEngine`, `GlossifyMemory`, `GlossifySkinToggles`, `GlossifyToggles`, `zGlossifyLegMask`. Enable one. A second pack fights over those variables. OilSkin 2 stays enabled beside that one pack. It is the engine, not a sixth pack.

| Folder | What it is |
| --- | --- |
| `OilSkinBoo` | English. Agents. No `WorldBeforeDraw`. |
| `油光布` | Chinese of that pack. |
| `OilSkinBoo-Glossify` | English. Also runs `CommandListWorldBeforeDraw`. |
| `油光布-全油化版` | Chinese of that pack. |
| `OilSkinBoo-Skin` | Agent-only, with the skin-mask path. No `WorldBeforeDraw`. |

Installed copies live under `Mods\Bangboo Skins\`. On 22 Nov 2026 the enabled pair was `OilSkinBoo` and `油光皮肤OilSkin2`. The other packs were `DISABLED_`. List the directory before editing. A `DISABLED_` copy is often behind the repo. Change the repo folder and the live folder that is actually enabled.

`hotkeys.json` is the manager's key page. In-game keys are `[Key…]` sections. Descriptions must be the section name with `Key` removed: Menu, Face, Body, Style, Stronger, Weaker, Click. FlairX matches on the description. A custom description is kept and the English section name is added, so the manager shows two lists. The in-game language is the shader. `Sid()` only recognizes `[KeyStyle]`, `[KeyStronger]`, `[KeyWeaker]`, `[KeyMenu]`, `[KeyFace]`, and `[KeyBody]`. Do not rename those sections. Finder log: `FlairX Mod Manager\app\Settings\HotkeyFinder.log`.

`DrawStr` holds 16 characters, four `int4`s. A longer title is a second call, placed at the `Advance()` width of the first string plus a gap. A fixed x of 176 drew "-Glossify" through "OilSkinBoo" and the screen read "OilSkinBodlossify". The English font accepts codes 32–126. The Chinese font rejects `idx >= 128` (`idx = ch - 32`), so codes above 159 do not draw. 油, 光, 布 are 127, 128, 129. Do not invent a glyph id. 全, 化, and 版 are not in the atlas. The folder name can still be `油光布-全油化版`. FlairX shows that name.

F10 reloads ini text, and hlsl whose beside-file `.bin` was removed. It does not drop a ShaderRegex already in the driver cache. Adding or removing a ShaderRegex needs a full game restart. Leave the `.bin` files unless the hlsl beside them changed. Deleting them forces a compile that freezes the game and drives commit up. Ctrl+Alt+F10 wipes persisted values. A `.bin` is a cache the game wrote. It is not source.

`d3dx.ini` ships `cache_shaders = 0` and `hunting = 0`. OilSkin 2 sets `cache_shaders = 1` later, and the later assignment wins. A mod that sets `cache_shaders = 0` after that compiles for about 6.8 seconds on every reload. Leave hunting off. Green hunting text means Numpad 0 must be pressed before Numpad * changes the oil style.

PowerShell is the shell on this machine. Python is not on PATH. From a PowerShell function, `return ,$list` keeps a one-item list as a list. A bare `return $list` unwraps it.

## How an ini becomes a draw

```
d3dx.ini
  Core\ZZMI\main.ini          ShaderRegex on every vs_4/vs_5 runs CommandListSkin
  Libraries\SlotFix           named maps → the slot this shader actually uses
  Mods\**\*.ini               character mods, oil, Glossify, UI
```

Each included ini has a namespace. A file that sets `namespace = GlossifyBase` owns that name. A file that does not is namespaced from its path: `Mods\<relative path>\<file.ini>`. Two files can both use `$active` and not clash. Reach another file with `$\Namespace\var`, `CommandList\Namespace\Name`, or `Resource\ZZMI\Diffuse`.

`[Present]` sections run in ini filename order, after the frame's draws. ZZZ can Present more than once in a frame. The second Present sees no new draws. Do not clear a latched flag at the end of Present and expect the next Present in the same frame to still see it.

`global persist` is written to `d3dx_user.ini` when a hotkey or a command list changes it. The file header says so. F10 reloads ini text. It does not reread `d3dx_user.ini`. A hash does not fit. Persist values are floats with a 24-bit mantissa, so an 8-digit hash stored there is corrupted. Compare a hash with `if ib == 9040e2c4` if you must. Do not save it in a persist variable. Do not use an ib/vb equality test in PRE to decide that a mesh was replaced.

On a raw hash, one TextureOverride wins. The highest `match_priority` wins. A section that sets `match_priority` is visited before a section that leaves it unset. Glossify's body sections set `-1`, so they run before a skin that leaves the priority at the default, and they still lose the implicit winner to that skin. `checktextureoverride` is the other path: CommandListSkin uses it, and it runs every matching section. That is why a stock-body hook can bind oil and the skin can still draw its own mesh.

## The frame

`[ShaderRegexEnableTextureOverrides]` on `vs_4_0 vs_4_1 vs_5_0 vs_5_1` runs `CommandListSkin` for every vertex shader.

`CommandListSkin` (`Core\ZZMI\main.ini`) only works while `$costume_mods` is on. With the installed `$is_legacy = 1`:

| `DRAW_TYPE` | What Skin checks |
| --- | --- |
| 1 or 3 | `vb0`, `vb2` |
| 2 or 4 | `vb0`, `vb1`, `vb2`, `ib`, then SlotFix cleanup |

`$check_textures == 2` also checks `ps-t0`. `x140 = 0` resets the engine's texture marker after the checks.

So a position-like pass never sees the texcoord or the index buffer. The blend buffer is on every pass. That is why the blend section is where a mesh mod swaps the position buffer and sets `$active`.

| Buffer | Slot | Stride | File |
| --- | --- | --- | --- |
| Position | `vb0` | 40 | `*Position.buf` |
| Texcoord | `vb1` | 24 | `*Texcoord.buf` |
| Blend | `vb2` | 32 | `*Blend.buf` |
| Index | `ib` | — | `*.ib` |

## Textures

Do not assign `ps-t0`–`ps-t7` for a character map. The same map sits in a different slot for the body shader, the face shader, the night variant, and the censor variant. `ps-t0` is the shadow map. Overriding it breaks the lighting.

A mod fills the named slots and lets SlotFix place them:

```
Resource\ZZMI\Diffuse = ref ResourceBodyDiffuse
Resource\ZZMI\NormalMap = ref ResourceBodyNormal
Resource\ZZMI\LightMap = ref ResourceBodyLight
Resource\ZZMI\MaterialMap = ref ResourceBodyMaterial
run = CommandList\ZZMI\SetTextures
```

`CommandListSetTextures` (`Libraries\SlotFix\Resources\Resources.ini`) runs SlotFix only when `ps-t3` is bound and `DRAW_TYPE` is 2 or 4, then clears the named slots. The names it understands are Diffuse, NormalMap, LightMap, MaterialMap, GlowMap, GlowGradient, and WengineFx. Aliases `ResourceD`, `ResourceN`, `ResourceL`, `ResourceM`, `ResourceG`, `ResourceW` exist for older inis. `CommandListSkinTexture` (ps-t1 through ps-t10) is the older path. New work uses `CommandList\ZZMI\SetTextures`.

ZZMI also ships a flat normal and an empty glow. A lot of normal maps share one hash (`ebac056e` at 2k, `798adba3` at 1k). Replacing that hash by name replaces every normal in the scene. Leave the flat-normal override in core alone.

## A character mod

Phoenix is the complete shape. There are two legal mods.

**Mesh replace.** New position, blend, texcoord, and index buffer. Body and hair do this. `PhoenixHalfNudeModRenKamui\Phoenix.ini` is the short version.

**Texture swap.** Same mesh, new Diffuse (and sometimes the other maps). Faces usually do this. `DISABLED_Phoenix -yyzj-\face\face.ini` matches Phoenix face hash `d58b6350`, swaps `Resource\ZZMI\Diffuse`, and calls SetTextures. It does not ship a new IB.

Split a character across files when the parts are independent. yyzj uses `body\mod.ini`, `face\face.ini`, and `hair_weapon\hair_weapon.ini`. Each file gets its own namespace from its path, so each can have `$active0` and its own keys. Hair (`toufa`) is its own position, blend, texcoord, and IB. A dumped "hair" component often contains bare legs. Those triangles belong to that vertex buffer only.

### Mesh replace, in order

1. **Vertex limit raise**, on the original position hash, when the new mesh has more vertices than the game allocated.

```
[TextureOverridePhoenixBodyVertexLimitRaise]
hash = 4ac4d074
override_vertex_count = 61759
override_byte_stride = 40
```

Hair raises also set `uav_byte_stride = 4`.

2. **Blend section.** This hash is visited on every draw type. Swap `vb2`. On `DRAW_TYPE == 1`, also swap `vb0`, draw the whole new vertex count, and mark the character present.

```
[TextureOverridePhoenixBodyBlend]
hash = 0fc8027f
handling = skip
vb2 = ResourcePhoenixBodyBlend
if DRAW_TYPE == 1
    vb0 = ResourcePhoenixBodyPosition
    draw = 61759, 0
    $active = 1
endif
```

3. **Texcoord section.** `vb1 = ResourcePhoenixBodyTexcoord` on the texcoord hash. No draw.

4. **Index buffer.** One hash for the whole component (`9040e2c4` for this Phoenix body). A section with `handling = skip` and no draw kills the stock draw. Further sections share that hash and pick a stock draw call with `match_first_index`. Each one binds its own IB and its own maps, then draws slices.

```
[TextureOverridePhoenixBodyA]
hash = 9040e2c4
match_first_index = 0
ib = ResourcePhoenixBodyAIB
Resource\ZZMI\Diffuse = ref ResourcePhoenixBodyADiffuse
Resource\ZZMI\NormalMap = ref ResourcePhoenixBodyANormalMap
Resource\ZZMI\LightMap = ref ResourcePhoenixBodyALightMap
Resource\ZZMI\MaterialMap = ref ResourcePhoenixBodyAMaterialMap
run = CommandList\ZZMI\SetTextures
drawindexed = 8484, 0, 0
if $bottom == 0
    drawindexed = 2436, 8484, 0
endif
```

`drawindexed = count, firstIndex, 0`. The count is the number of indices, not triangles. The third value stays 0. `match_first_index` is how several stock draw calls that share one IB stay separate (`0`, then `41253`, then `41955` on this body).

Resources:

```
[ResourcePhoenixBodyPosition]
type = Buffer
stride = 40
filename = PhoenixBodyPosition.buf
```

Textures are `[ResourcePhoenixBodyADiffuse]` with `filename = PhoenixBodyADiffuse.dds`. No `type` line.

### Keys

`$active` is 1 only during that character's draw. `[Present]` does `post $active = 0`, so the next frame starts dark. Keys use `condition = $active == 1` and `type = cycle`, and the cycled variables are `global persist`, so the outfit survives F10.

```
[KeyBottom]
condition = $active == 1
key = no_modifiers 6
type = cycle
$bottom = 0,1,2
```

One key may cycle several flags together. Monsoon's Phoenix toggle does this: one press moves body, stockings, and shoes as a set. A part that should vanish is a `drawindexed` that you skip. Do not toggle by deleting the IB section.

yyzj puts the face cycle on Shift+Up, the body cycles on their own keys, and the hair cycle in `hair_weapon.ini`. Copy that split. Do not put face, body, and hair draws in one section.

## Oil

Oil is [GameBanana 741702](https://gamebanana.com/mods/741702), namespace `KatFXMatcap`. The loaded copy is `Mods\Bangboo Skins\油光皮肤OilSkin2`. `DISABLED_油光皮肤OilSkin` must stay disabled. Two inis with that namespace fight over the same variables and the same `t120` declaration.

Do not edit `Matcap_ZZZ.ini`. `[Rendering] cache_shaders = 1` stays.

The installed keys, which Glossify depends on:

| Key | Variable | Values |
| --- | --- | --- |
| Numpad * | `$oilSkin` | 0 through 7. 0 is off. 7 is the wet-drop style |
| Numpad + | `$oilStrength` | 0.0, 0.2, 0.4, 0.6, 0.8, 1.0 |
| Numpad − | `$oilStrength` | 1.0 down to 0.0 by 0.1 |

Shift+Numpad * is Glossify's patched-body key. Plain Numpad * is the oil style key. If green hunting text is on screen, Numpad 0 dismisses it before Numpad * will change the style.

Glossify's own key sections are what FlairX lists on the mod page. The pencil records a key press and writes the `key =` line. Players never type a key code. The panel reads that line and prints it, so F10 picks up a new bind. A name with no special case is still shown. Do not build the name with a local array in the shader. That compile takes the game down. Glossify sets `$oilKeysEnabled = 0`. The oil numpad keys would stack on Glossify and step twice. Stronger and weaker each move strength by 0.1.

`CommandListOilSkin` binds `ps-t125` to the style texture, nulls `ps-t126`, copies `$oilStrength` into `$matcap1_opacity`, and runs `SetParams`. `SetParams` writes `y121 = $oilStrength`. The pixel shader reads IniParams through `t120` and samples `t125`. It does not read `$oilSkin`. Changing `$oilSkin` after `SetParams` does not change the draw that already bound `t125`. Put the old values back before the next mesh calls OilSkin.

`[Present]` runs `CommandListClear`, which nulls `ps-t124` through `ps-t127`. Oil cannot leak into the next frame. A leak is inside one frame. Null `t125` after the draw that should shine, not before it. Do not unbind a pile of unrelated slots in the middle of a mesh. That blacks the next mesh.

The shader regex targets the character pixel shader (`ps_4_0` / `ps_5_0`, four outputs, normal built by `dp3` / `rsq` / `mul`). It writes `o0` only. `o3` is the world-normal G-buffer. Never write `o3`. There are two patterns: `Matcap` for the linear interpolators, `MatcapB` for the `sample` interpolators. The camera basis is `cb0[55]`. The in-scene variant uses `cb0[197]`. The regex also declares `t120` and `t124`–`t127`. A second regex that declares `t120` fails the compile. Delete the extra declaration, not the oil one.

The header inside `Matcap_ZZZ.ini` shows the old hook, `run = OilSkin` then `drawindexed` then `run = Clear`. Glossify does not use the Clear-per-draw form. Present already clears. Glossify restores the knobs and nulls `t125` so the next mesh in the same frame stays on the stock knobs.

## Glossify

Glossify decides which draws receive oil, and it remembers a look. It does not ship a mesh. Stock face and stock body shine the game's meshes, so they also shine NPCs and enemies. A skin that only swaps textures is still those meshes, so stock gloss reaches it. A skin that replaces the body stays matte until its draws are listed in `GlossifyBase.ini`.

`OilSkinBoo-Skin` is the agent-only copy of Standard. It has the same face and body toggles, memory, and panel. It does not run `WorldBeforeDraw`, so NPCs, Bangboo, enemies, and scene objects stay matte. Ellen's hair draw stays matte. A body draw still includes the clothes in that mesh until the section binds a skin mask: `ps-t126` to a texture that is white on skin and black on cloth, and `$skin_mask = 1` before `CommandListBaseBody`. OilSkin writes `z122 = 0`, which ignores `t126`. BaseBody sets `z122 = 1` only when `$skin_mask` is 1. Do not add `WorldBeforeDraw` to this folder. Do not edit `Matcap_ZZZ.ini`.

| File | Namespace | Job |
| --- | --- | --- |
| `GlossifyBase.ini` | GlossifyBase | Stock face and stock body, by hash |
| `GlossifyFaceEngine.ini` | GlossifyFaceEngine | Face sheen |
| `GlossifyMemory.ini` | GlossifyMemory | Per-agent saved style and strength. Generated |
| `GlossifyToggles.ini` | GlossifyToggles | On-screen panel |
| `GlossifySkinToggles.ini` | GlossifySkinToggles | Publishes the patched skin's saved look to the panel |
| `zzLegMask.ini` | zGlossifyLegMask | Dormant. Leave it |

### Stock path

Stock body sections use `match_priority = -1` and call `CommandListBaseBody`. Stock face sections use `match_priority = 19500` and, when the face outputs are bound (`@o0`, `@o3`, `@oD`), call `CommandList\GlossifyFaceEngine\DrawOriginal`. A marker section at `-9500` only plants `filter_index = 743221`.

`$face_enabled` starts at 1 (Ctrl+Alt+F8). `$body_enabled` starts at 0 (Ctrl+Alt+F9). Both are persist.

`CommandListBaseBody` applies the saved look, calls `CommandList\KatFXMatcap\OilSkin`, then `CommandList\GlossifyMemory\RestoreSaved` before the draw returns. `t125` and `y121` stay bound for this draw. The `$` knobs are already back, so the next mesh's OilSkin reads the stock knobs.

### Face sheen

The face is not a body oil pass. `FaceOil.hlsl` samples a copy of the `o3` normal and adds a screen-space sheen. The first ~60 frames of a face warm up. Do not remove the warmup. Do not switch the face to `CommandListOilSkin`. That path froze the machine. `ResourceFaceNormals` needs `max_copies_per_frame = 32` (the Sigrid fix). Do not unbind every render target in the face pass.

### Per-agent memory

`GlossifyMemory.ini` is generated. Do not hand-edit it. The slot id is the face hash, declared as its own variables. Body hashes of that agent call the same slot. Phoenix face `d58b6350` shares a slot with body `9040e2c4`. A body-only mesh with no catalogue face (Pyrois) has its own slot.

The texture override that matched the mesh sets the agent id. `CommandListApplySaved` uses that id. It does not re-check the index buffer. A saved mesh binds its style and strength. An unsaved mesh binds the shared default. `RestoreSaved` then puts the knobs back to the published look, so the next mesh does not inherit the previous agent. The first catalogued mesh of the frame accepts a knob change, Save, or Clear. A key is a change from that published look. Switching agents is not a key, so the new agent does not copy the previous look into the default. A saved agent keeps the next key press continuing from that agent's style. Clear drops that agent's slot and the draw uses the shared default again.

`GlossifyMemory.ini` sorts before `GlossifyToggles.ini`. The panel click is handled in the Toggles Present, after Memory's Present, so the slot is written on the next frame. The click lights the SAVED label immediately. The label on the click frame is not proof the slot was written.

### Patched skins

A skin that replaces the body stays matte until its draws are listed in `GlossifyBase.ini`.

- Body, legs, torso, arms: oil before the `drawindexed`, then clear it.
- Face: only when that hash is a known face. A section named Head is not enough.
- Skip hair, earrings, and blend passes.
- Skip a mod that already calls `KatFXMatcap` itself.
- Set `z83 = 1` and `$\GlossifyFaceEngine\fHASH = 1` for the hooked draw.

`x83` means this draw is a mod body. `y83` means this draw is a mod face. `z83` means this draw came from a patched skin.

`BodyStart` oils when `x83 == 1` or stock body is on. Otherwise it nulls `t125`. `BodyEnd` nulls `t125`. `FaceDraw` runs the face engine only when `y83 == 1`. Stock face sections still match the real face hash, so Base Face alone glosses a patched face that kept the stock face mesh.

The skin's controller runs in the skin's ini, which is later than GlossifyBase. On a patched body it applies that skin's saved style after Glossify's stock pass. That is intentional. A patched look does not follow the stock knobs. SAVE LOOK stores `$oilSkin` and `$oilStrength` into the skin's persist variables. Style 0 is stored as 2.

All four switches off (stock face, stock body, patched body, patched face) leaves a patched skin matte. Closing the OilSkinBoo pack also removes gloss from patched skins, because the command lists it injected disappear with the folder.

### Hair and legs

Hair stays matte. Ellen is the exception already in `GlossifyBase.ini`: her hair draw is a full oil pass, with no height cut, because that is the look that was asked for. Do not put a height mask back on her. Do not draw her stock leg IB into the mod vertex buffer.

For Rina, Lighter, Caesar, Soldier 0, and Vivian the stock hair draw stays matte, and a second draw uses the stock leg IB (`*LegIB.ib`, `match_priority = -1`, `match_first_index = 0`) so the bare legs inside the hair component shine. Those index lists are valid only for the stock vertex buffer. A skin that replaces the hair mesh must not receive them. Drawing them into a mod VB scrambles the UVs.

`zzLegMask.ini` was the height-mask attempt. It is dormant (`x123` stays 0). Do not delete it until asked, and do not turn it back on as the product path. Shader-regex group order is the full namespaced section name. `zGlossifyLegMask` sorts after `KatFXMatcap`, which is why the mask regex can see the oil sample. `GlossifyBase` sorts before `KatFXMatcap`.

## What not to build

- A character map bound with `ps-t3 = ResourceDiffuse` or any other hardcoded slot.
- A second `namespace = KatFXMatcap`, or any edit to `Matcap_ZZZ.ini`.
- Glossify stored inside the oil folder. An oil update deletes that folder.
- A hash saved in `global persist`.
- A stock leg IB drawn with a modded hair VB.
- A face hooked through `CommandListOilSkin`, or a face sheen with the warmup removed.
- A write to `o3`, or an override of `ps-t0`.
- A `Present` that clears `show_saved` or a save request unconditionally. The frame can Present again.
- A new hash table copied into a third ini. Stock hashes live in `GlossifyBase.ini`. The save slots live in `GlossifyMemory.ini`. Edit that file in place.
- UI, camera, and letterbox mods (the `UI` folder, TTLib) treated as character meshes. They are a different system.
- A second OilSkinBoo pack enabled beside another. They share namespaces.
- `cache_shaders = 0` in a mod that loads after OilSkin 2.
- A ShaderRegex with a shader model and no pattern. It matches every shader of that model. An empty vertex-shader regex was tried and rejected.
- `CommandListNpcFace` or `CommandListWorldFace`. They were added and removed. NPC faces do not sample `t125`, so the oil seal never sees them.
- The Upper / Lower / Arms part picker. That experiment is aborted. Most agents do not draw those parts as separate meshes, and the picker crashed on agent swap.
- A title placed at a fixed x, or a glyph id that is not already in that pack's font.
- A `hotkeys.json` description other than Menu, Face, Body, Style, Stronger, Weaker, or Click.

## When a game update breaks a character

The mesh hashes change. The architecture does not. Hunt the new position, blend, texcoord, and IB hashes, then update the stock tables in `GlossifyBase.ini` and the matching slot in `GlossifyMemory.ini`. A known hash still glosses when a skin wins the implicit draw, because `checktextureoverride` runs every match. A hash that is not listed stays matte.

After any ini change, F10 in game. After a ShaderRegex is added or removed, restart the game.

## When the game closes on this PC

This machine has 16 GB of RAM and `C:\pagefile.sys` locked at 14000 MB, initial and maximum. The commit limit is about 29 GB. The GPU is an RTX 3080 Ti plus an AMD Radeon iGPU. WMI reports about 4 GB for the 3080 Ti. That field is wrong.

A black screen here comes from the commit limit.

1. Application event 1002, AppHang, means Windows closed `ZenlessZoneZero.exe` because it had stopped responding.
2. `dwm.exe` faulting with `0xc00001ad` in `udwm.dll` or `dwmcore.dll` is `STATUS_COMMITMENT_LIMIT`. The paging file could not back the allocation. The desktop goes black, then Windows closes the game.
3. System event 2004 from `Microsoft-Windows-Resource-Exhaustion-Detector` names the top consumers. The game is repeatedly 9–15 GB and has reached about 21 GB. Read the byte count in that event. A process virtual size near 50 GB is reserved address space, not usage.
4. WER "BlueScreen" at the next login often attaches old minidumps. Check `C:\Windows\Minidump` write times. `111226-9656-01.dmp` is 12 Nov 2026, bugcheck `0x3B`. `102526-10687-01.dmp` is 25 Oct 2026, bugcheck `0xEF`. Kernel-Power 41 is an unclean reboot. Kernel-Power 109 with reason Kernel API is a requested reboot. No new dump means this episode was a hang.
5. `amdxx64.dll` faults in a small process such as `OpenWith.exe`, and Paint dying with `8007000e`, during event 2004, come from the iGPU running out of memory.

OilSkinBoo and OilSkin 2 are small files. Face normal copies are capped at 32. Their cost is shader work on the draws they hook. The gigabytes are enabled costume textures. On 22 Nov 2026 the heaviest were Velina OG & Bunny (about 990 MB of DDS), Ye Shunguang Flowing Night (about 936 MB), Nicole yyzj (about 664 MB), Yanagi Miko (about 404 MB), and Remielle White Ultimate (about 376 MB), with about 4 GB of DDS and 568 inis enabled in total. Re-measure after the mod list changes.

Do not report that the mods have no performance problem because the game has no application fault. On this PC the failure is the commit limit. Do not delete `.bin` caches to clear it. F10 cannot revive a process Windows already closed. ZZMI does not write a useful log for this failure. Use the Windows event log.
