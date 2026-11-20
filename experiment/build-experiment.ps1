# Builds the experiment mods from the mesh catalogue and a read-only scan of installed skins.
# Does not write into Glossify or into the Mods folder.
$ErrorActionPreference = 'Stop'
$utf8 = New-Object System.Text.UTF8Encoding $false
$root = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path -LiteralPath (Join-Path $root 'Glossify.cs'))) { $root = $PSScriptRoot }
$repo = $root
$hashDir = Join-Path $env:TEMP 'zzallhash2'
$mods = 'C:\Users\Ika\Desktop\zzz\FlairX Mod Manager\app\XXMI\ZZMI\Mods'
$groups = Join-Path $repo 'experiment'
$parts = Join-Path $repo 'experiment'
$lower = Join-Path $repo 'experiment'
foreach ($d in @($groups, $parts, $lower)) { New-Item -ItemType Directory -Force -Path $d | Out-Null }

function Write-Utf8([string]$path, [string]$text) {
    [IO.File]::WriteAllText($path, $text.Replace("`r`n", "`n").Replace("`n", "`r`n"), $utf8)
}

function Classify([string]$name) {
    if ($name -match '^(Legs|LowerBody)$') { return 'lower' }
    if ($name -match '^(Arms|BothArms|LeftArm|RightArm|Arm)$') { return 'arms' }
    if ($name -match '^(Body|Torso|UpperBody|NoCoatTorso)$') { return 'upper' }
    return $null
}

$byHash = @{}
$partOf = @{}
if (Test-Path -LiteralPath $hashDir) {
    Get-ChildItem -LiteralPath $hashDir -Filter *.json | ForEach-Object {
        $who = $_.BaseName
        $comps = Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json
        foreach ($c in $comps) {
            $ib = "$($c.ib)".ToLower()
            if ($ib -notmatch '^[0-9a-f]{8}$') { continue }
            if (-not $byHash.ContainsKey($ib)) { $byHash[$ib] = New-Object System.Collections.Generic.List[string] }
            [void]$byHash[$ib].Add("$who $($c.component_name)")
            $bucket = Classify $c.component_name
            if ($bucket) {
                if (-not $partOf.ContainsKey($ib)) { $partOf[$ib] = @{ bucket = $bucket; who = $who; part = $c.component_name; indexes = @($c.object_indexes) } }
            }
        }
    }
}

$baseIni = Join-Path $repo 'Glossify\GlossifyBase.ini'
$baseText = [IO.File]::ReadAllText($baseIni)
$inBody = $false
$comment = ''
foreach ($line in ($baseText -split "`r?`n")) {
    if ($line -match 'Stock body, legs, torso') { $inBody = $true; continue }
    if (-not $inBody) { continue }
    if ($line -match '^\s*;\s*(.+)$') { $comment = $Matches[1].Trim(); continue }
    if ($line -match '^\s*hash\s*=\s*([0-9a-fA-F]{8})\s*$') {
        $ib = $Matches[1].ToLower()
        $bucket = $null
        if ($comment -match 'NoCoatTorso|Torso|UpperBody|\bBody\b') { $bucket = 'upper' }
        if ($comment -match '\bLegs\b|LowerBody') { $bucket = 'lower' }
        if ($comment -match '\bArms?\b|BothArms|LeftArm|RightArm') { $bucket = 'arms' }
        if ($bucket -and -not $partOf.ContainsKey($ib)) {
            $partOf[$ib] = @{ bucket = $bucket; who = $comment; part = $bucket; indexes = @(0) }
        }
        if (-not $byHash.ContainsKey($ib)) { $byHash[$ib] = New-Object System.Collections.Generic.List[string]; [void]$byHash[$ib].Add($comment) }
    }
}

# Every catalogue and stock-body hash is an agent mesh. World oil must skip these.
$agentIds = New-Object System.Collections.Generic.List[string]
$seenAgent = @{}
foreach ($id in ($byHash.Keys | Sort-Object)) {
    if ($seenAgent.ContainsKey($id)) { continue }
    $seenAgent[$id] = $true
    [void]$agentIds.Add($id)
}
# Face hashes from the base mod, so a shared skin shader does not oil agent faces either.
foreach ($m in [regex]::Matches($baseText, '(?m)^hash = ([0-9a-fA-F]{8})\s*$')) {
    $id = $m.Groups[1].Value.ToLower()
    if ($seenAgent.ContainsKey($id)) { continue }
    $seenAgent[$id] = $true
    [void]$agentIds.Add($id)
}

$isAgent = New-Object System.Text.StringBuilder
[void]$isAgent.AppendLine('[CommandListIsAgent]')
[void]$isAgent.AppendLine('$agent = 0')
foreach ($id in $agentIds) {
    [void]$isAgent.AppendLine("if ib == $id")
    [void]$isAgent.AppendLine('    $agent = 1')
    [void]$isAgent.AppendLine('endif')
}

$groupIni = @"
; Experiment: agents and the rest of the cast use different draws.
; Safe beside Glossify. Toggles start off. The placeholder hashes match nothing.
; Replace a hash only after hunting the pixel shader on that kind of model.
; The Parts panel turns NPC, Enemy, and Bangboo on. Ctrl+Alt+F6 opens that panel.
; Agents stay on Glossify's own list. This file only adds the other three.
; A story NPC that reuses an agent mesh is an agent. The index hash is the same.
namespace = ExperimentGroups

[Constants]
global `$agent = 0
global persist `$npc = 0
global persist `$enemy = 0
global persist `$bangboo = 0

[CommandListWorldOil]
; Leave agent draws alone. Clearing t125 here can strip oil Glossify just bound.
if `$agent == 0
    if `$\KatFXMatcap\oilSkin == 0
        `$\KatFXMatcap\oilSkin = 2
    endif
    run = CommandList\KatFXMatcap\OilSkin
endif

; Paste the hunted pixel shader hash over the placeholder. Each slot has its own
; placeholder so the three sections do not fight. If two groups share a shader,
; one section will oil both, and the index list is what keeps agents out.
; NPC and enemy bodies usually need a normal target. Bangboo often do not.
[ShaderOverrideExpNpc]
hash = fffffff1
if `$npc == 1 && @o0 && @o3
    run = CommandListIsAgent
    run = CommandListWorldOil
endif

[ShaderOverrideExpEnemy]
hash = fffffff2
if `$enemy == 1 && @o0 && @o3
    run = CommandListIsAgent
    run = CommandListWorldOil
endif

[ShaderOverrideExpBangboo]
hash = fffffff3
if `$bangboo == 1 && @o0
    run = CommandListIsAgent
    run = CommandListWorldOil
endif

"@
Write-Utf8 (Join-Path $groups 'GroupSplit.ini') ($groupIni + $isAgent.ToString())

function New-PartBlock([string]$hash, [string]$bucket, [string]$label) {
    $var = switch ($bucket) { 'lower' { 'lower' } 'arms' { 'arms' } default { 'upper' } }
    @"
; $label
[TextureOverrideExp_$($bucket)_$hash]
hash = $hash
match_priority = -2
if @o0 && @o3 && @oD
    if `$$var == 1
        run = CommandListExpBind
    endif
endif
if @o0 && @o3 && @oD
    post run = CommandListExpClear
endif

"@
}

$partBody = New-Object System.Text.StringBuilder
[void]$partBody.AppendLine(@'
; Experiment: stock upper body, lower body, and arms as separate switches.
; match_priority -2 loses to Glossify (-1) and to skin mods (0).
; With Glossify enabled this file does nothing. Disable Glossify to test it.
; The Parts panel turns these on. Ctrl+Alt+F6 opens that panel.
namespace = ExperimentParts

[Constants]
global persist $arms = 0
global persist $upper = 0
global persist $lower = 0

[CommandListExpBind]
if $\KatFXMatcap\oilSkin == 0
    $\KatFXMatcap\oilSkin = 2
endif
run = CommandList\KatFXMatcap\OilSkin

[CommandListExpClear]
ps-t125 = null

'@)
$counts = @{ upper = 0; lower = 0; arms = 0 }
foreach ($id in ($partOf.Keys | Sort-Object)) {
    $p = $partOf[$id]
    $counts[$p.bucket]++
    [void]$partBody.Append((New-PartBlock $id $p.bucket ("{0} {1}" -f $p.who, $p.part)))
}
Write-Utf8 (Join-Path $parts 'PartToggles.ini') $partBody.ToString()

$sub = New-Object System.Text.StringBuilder
[void]$sub.AppendLine(@'
; Not loaded. Rename to Submesh.ini only while Glossify is disabled.
; A non-zero first index is one piece of a stock mesh. Index 0 stays on the part switch.
; Priority -1 beats the part file's -2 for that piece only, and still loses to a skin mod.
namespace = ExperimentParts

'@)
foreach ($id in ($partOf.Keys | Sort-Object)) {
    $p = $partOf[$id]
    $indexes = @($p.indexes | Where-Object { $_ -ne $null -and "$_" -ne '' } | Select-Object -Unique)
    foreach ($idx in $indexes) {
        if ([int]$idx -eq 0) { continue }
        $var = switch ($p.bucket) { 'lower' { 'lower' } 'arms' { 'arms' } default { 'upper' } }
        [void]$sub.AppendLine(("; {0} {1} first index {2}" -f $p.who, $p.part, $idx))
        [void]$sub.AppendLine("[TextureOverrideExpSub_${id}_$idx]")
        [void]$sub.AppendLine("hash = $id")
        [void]$sub.AppendLine("match_first_index = $idx")
        [void]$sub.AppendLine('match_priority = -1')
        [void]$sub.AppendLine('if @o0 && @o3 && @oD')
        [void]$sub.AppendLine("    if `$$var == 1")
        [void]$sub.AppendLine('        run = CommandListExpBind')
        [void]$sub.AppendLine('    endif')
        [void]$sub.AppendLine('endif')
        [void]$sub.AppendLine('if @o0 && @o3 && @oD')
        [void]$sub.AppendLine('    post run = CommandListExpClear')
        [void]$sub.AppendLine('endif')
        [void]$sub.AppendLine('')
    }
}
Write-Utf8 (Join-Path $parts 'Submesh.optional.txt') $sub.ToString()

# Read-only scan of installed skins. The patcher oils a whole section or skips it.
$bodyToFace = @{}
$cs = [IO.File]::ReadAllText((Join-Path $repo 'Glossify.cs'))
foreach ($m in [regex]::Matches($cs, '\{"([0-9a-fA-F]{8})","([0-9a-fA-F]{8})"\}')) {
    $bodyToFace[$m.Groups[1].Value.ToLower()] = $m.Groups[2].Value.ToLower()
}
$bodyName = [regex]'^(?:TextureOverride|CommandList)(?:\w*(?:Body(?:Upper|Lower)?|NoCoatTorso|Torso|LowerBody|UpperBody|Legs|LeftArm|RightArm|BothArms|shenti)(?:IB|[A-Z]\d*|\d+)?|[^]]*(?:_IB_[a-f0-9]{8}_shenti_Component\d+|[a-f0-9]{8}Part\d+))$'
$lowerComment = [regex]'(?i)\b(legs?|lower\s*half|lowerbody|thighs?|hips?)\b'
$skinComment = [regex]'(?i)\b(body|torso|arms?|chest|skin|legs?|thighs?|lower)\b'
$hairComment = [regex]'(?i)\b(hair|bangs?|ahoge)\b'
$lowerInHair = New-Object System.Collections.Generic.List[string]
$skinInHair = New-Object System.Collections.Generic.List[string]
$unnamedPart = New-Object System.Collections.Generic.List[string]
$mixed = New-Object System.Collections.Generic.List[string]
$scanned = 0
$enabledHairLower = 0
$enabledUnnamedDraw = 0
$enabledMixed = 0
$enabledUnnamedSamples = New-Object System.Collections.Generic.List[string]
$section = ''
$hash = ''
$note = ''
$hasDrawHook = $false
$hasThis = $false
$disabled = $false
$rel = ''
$drawNotes = New-Object System.Collections.Generic.List[object]
function Flush-Section {
    if (-not $script:section) { return }
    $excluded = $script:section -match 'hair|weapon|shadow|outline|wing|mask|bangboo|custom'
    $namedBody = $script:bodyName.IsMatch($script:section)
    $catalogBody = $script:hash -and $script:bodyToFace.ContainsKey($script:hash) -and $script:hasDrawHook -and -not $script:hasThis
    $isBody = (-not $excluded) -and ($namedBody -or $catalogBody)
    $tag = $(if ($script:disabled) { 'disabled' } else { 'enabled' })
    foreach ($d in $script:drawNotes) {
        if ($script:section -match 'hair' -and $script:lowerComment.IsMatch($d.note)) {
            [void]$script:lowerInHair.Add("$tag`t$($script:rel)`t$($script:section)`t$($d.line)`t$($d.note)`t$($d.draw)")
        } elseif ($script:section -match 'hair' -and $script:skinComment.IsMatch($d.note) -and -not $script:hairComment.IsMatch($d.note)) {
            [void]$script:skinInHair.Add("$tag`t$($script:rel)`t$($script:section)`t$($d.line)`t$($d.note)`t$($d.draw)")
        }
    }
    if ($isBody) {
        $hasHairDraw = $false
        $hasSkinDraw = $false
        foreach ($d in $script:drawNotes) {
            if ($script:hairComment.IsMatch($d.note)) { $hasHairDraw = $true }
            if ($script:skinComment.IsMatch($d.note)) { $hasSkinDraw = $true }
        }
        if ($hasHairDraw -and $hasSkinDraw) {
            [void]$script:mixed.Add("$tag`t$($script:rel)`t$($script:section)`thash=$($script:hash)")
        }
    }
    if ($script:hash -and $script:partOf.ContainsKey($script:hash) -and -not $isBody) {
        $bucket = $script:partOf[$script:hash].bucket
        [void]$script:unnamedPart.Add("$tag`t$bucket`t$($script:rel)`t$($script:section)`thash=$($script:hash)")
    }
    $drawCount = $script:drawNotes.Count
    $probe = $script:section -match 'CheckHash|Active\.Mark|Marker'
    if (-not $script:disabled -and $drawCount -gt 0) {
        if ($script:section -match 'hair') {
            foreach ($d in $script:drawNotes) {
                if ($script:lowerComment.IsMatch([string]$d.note)) { $script:enabledHairLower++ }
            }
        }
        if ($hasHairDraw -and $hasSkinDraw) { $script:enabledMixed++ }
        if ($script:hash -and $script:partOf.ContainsKey($script:hash) -and -not $isBody -and -not $probe) {
            $script:enabledUnnamedDraw++
            if ($script:enabledUnnamedSamples.Count -lt 25) {
                $bucket = $script:partOf[$script:hash].bucket
                [void]$script:enabledUnnamedSamples.Add("$bucket`t$($script:rel)`t$($script:section)`thash=$($script:hash)`tdraws=$drawCount")
            }
        }
    }
}
if (Test-Path -LiteralPath $mods) {
    foreach ($file in (Get-ChildItem -LiteralPath $mods -Recurse -Filter *.ini -File)) {
        $full = $file.FullName
        if ($full -match 'Glossify|OilSkin|油光皮肤|experiment') { continue }
        if ($file.Length -gt 3MB) { continue }
        $scanned++
        $bytes = [IO.File]::ReadAllBytes($full)
        if ($bytes.Length -ge 2 -and $bytes[0] -eq 255 -and $bytes[1] -eq 254) { $text = [Text.Encoding]::Unicode.GetString($bytes) }
        else { $text = [Text.Encoding]::UTF8.GetString($bytes) }
        $rel = $full.Substring($mods.Length).TrimStart('\')
        $disabled = $rel -match 'DISABLED'
        $lines = $text -split "`r?`n"
        $section = ''
        $hash = ''
        $note = ''
        $hasDrawHook = $false
        $hasThis = $false
        $drawNotes = New-Object System.Collections.Generic.List[object]
        for ($i = 0; $i -lt $lines.Length; $i++) {
            $line = $lines[$i]
            if ($line -match '^\s*\[([^\]]+)\]') {
                Flush-Section
                $section = $Matches[1]
                $hash = ''
                $note = ''
                $hasDrawHook = $false
                $hasThis = $false
                $drawNotes = New-Object System.Collections.Generic.List[object]
                continue
            }
            if ($line -match '^\s*;\s*(.*)$') { $note = $Matches[1].Trim(); continue }
            if ($line -match '^\s*hash\s*=\s*([0-9a-fA-F]{8})\b') { $hash = $Matches[1].ToLower(); continue }
            if ($line -match '^\s*(?:ib|match_first_index|handling)\s*=') { $hasDrawHook = $true; continue }
            if ($line -match '^\s*this\s*=') { $hasThis = $true; continue }
            if ($line -match '^\s*drawindexed\s*=\s*[^;\r]+' -or $line -match '^\s*draw\s*=\s*\d+') {
                $draw = ($line.Trim())
                [void]$drawNotes.Add([pscustomobject]@{ line = $i + 1; note = $note; draw = $draw })
            }
        }
        Flush-Section
    }
}

$report = New-Object System.Text.StringBuilder
[void]$report.AppendLine("scanned_ini=$scanned")
[void]$report.AppendLine("agent_hashes=$($agentIds.Count)")
[void]$report.AppendLine("stock_upper=$($counts.upper) stock_lower=$($counts.lower) stock_arms=$($counts.arms)")
[void]$report.AppendLine("lower_draws_inside_hair_sections=$($lowerInHair.Count)")
[void]$report.AppendLine("other_skin_draws_inside_hair_sections=$($skinInHair.Count)")
[void]$report.AppendLine("part_hash_sections_the_patcher_skips=$($unnamedPart.Count)")
[void]$report.AppendLine("body_sections_that_also_comment_hair=$($mixed.Count)")
[void]$report.AppendLine("enabled_lower_draws_inside_hair=$enabledHairLower")
[void]$report.AppendLine("enabled_part_sections_with_draws_the_patcher_skips=$enabledUnnamedDraw")
[void]$report.AppendLine("enabled_body_sections_that_also_comment_hair=$enabledMixed")
[void]$report.AppendLine('')
[void]$report.AppendLine('== enabled part sections with draws the patcher skips (first 25) ==')
$enabledUnnamedSamples | ForEach-Object { [void]$report.AppendLine($_) }
[void]$report.AppendLine('')
[void]$report.AppendLine('== lower draws inside a hair section (first 40) ==')
$lowerInHair | Select-Object -First 40 | ForEach-Object { [void]$report.AppendLine($_) }
[void]$report.AppendLine('')
[void]$report.AppendLine('== other skin draws inside a hair section (first 20) ==')
$skinInHair | Select-Object -First 20 | ForEach-Object { [void]$report.AppendLine($_) }
[void]$report.AppendLine('')
[void]$report.AppendLine('== known part hash, section name is not a body (first 40) ==')
$unnamedPart | Select-Object -First 40 | ForEach-Object { [void]$report.AppendLine($_) }
[void]$report.AppendLine('')
[void]$report.AppendLine('== body section whose comments include both hair and skin (first 20) ==')
$mixed | Select-Object -First 20 | ForEach-Object { [void]$report.AppendLine($_) }
Write-Utf8 (Join-Path $lower 'report.txt') $report.ToString()

Copy-Item -LiteralPath (Join-Path $repo 'Glossify\GlossifyFaceEngine\FaceOil.hlsl') -Destination (Join-Path $lower 'LowerRedraw.hlsl') -Force
$hlsl = [IO.File]::ReadAllText((Join-Path $lower 'LowerRedraw.hlsl'))
$hlsl = $hlsl.Replace('Experimental face-only sheen. Reuses the native vertex shader and geometry.', 'Copy of the face sheen, used to redraw one index range of a shared mesh.')
Write-Utf8 (Join-Path $lower 'LowerRedraw.hlsl') $hlsl
Write-Output "agents=$($agentIds.Count) upper=$($counts.upper) lower=$($counts.lower) arms=$($counts.arms)"
Write-Output "scan=$scanned hairLower=$($lowerInHair.Count) hairSkin=$($skinInHair.Count) unnamed=$($unnamedPart.Count) mixed=$($mixed.Count)"
