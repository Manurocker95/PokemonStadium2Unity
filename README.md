# Pokémon Stadium (N64) — Model, Animation, Texture and Material Animation Reverse Engineering Notes

> **Scope:** Pokémon Stadium (Nintendo 64), based on the working Unity importer supplied with this write-up.
>
> This document is intended as a reverse-engineering reference for developers implementing their own parser/exporter. It describes what the current parser has actually established from the ROM and FRAGMENT resources. Where behavior is an importer policy rather than a proven game-format rule, it is called out explicitly.

## 1. High-level picture

Pokémon Stadium's Pokémon model pipeline is much more self-contained than Pokémon Stadium 2's battle-animation pipeline.

For Stadium 1, the working path is essentially:

```text
ROM
 └─ Pokémon model archive @ 0x00920000
     └─ archive entry
         └─ decompress (PERS-SZP/Yay0 when needed)
             └─ FRAGMENT module
                 ├─ species ID
                 ├─ model/layout command stream
                 │   ├─ skeleton
                 │   ├─ materials
                 │   ├─ textures/TLUTs
                 │   └─ display lists / geometry
                 ├─ skeletal animation pointer list
                 └─ auxiliary/material animation pointer list
```

The important contrast with Stadium 2 is that the Stadium 1 importer does **not** need a second battle-animation archive to obtain the skeletal clips. The FRAGMENT root itself exposes both the skeletal-animation list and the auxiliary/material-animation list.

There is, however, separate battle data in the ROM that is useful for determining which auxiliary/material animation is associated with each skeletal animation.

---

## 2. ROM normalization and revision assumptions

The importer accepts the three common N64 byte orders and normalizes them to big-endian ROM order.

Recognized magic values:

```text
0x80371240  z64 / native big-endian
0x37804012  v64 / byte-swapped
0x40123780  n64 / word-swapped
```

The implementation was developed against the USA ROM and expects MD5:

```text
ed1378bc12115f71209a77844965ba50
```

A different MD5 is currently treated as a warning, but all fixed ROM offsets documented below should be considered revision-specific until verified on another build.

---

## 3. Pokémon model archive

The Pokémon model archive begins at:

```text
ROM 0x00920000
```

The archive header contains the entry count at:

```text
archive + 0x0C
```

Entries begin at:

```text
archive + 0x10
```

with a stride of:

```text
0x10 bytes
```

The working parser currently uses the first two words of each descriptor:

```c
struct ArchiveEntry {
    u32 relativeOffset;
    u32 compressedSize;
    // remaining descriptor fields not required by the current parser
};
```

The payload is therefore read from:

```text
0x00920000 + relativeOffset
```

for `compressedSize` bytes.

### 3.1 Compression

An entry may be:

* raw/uncompressed;
* `Yay0`;
* wrapped as `PERS-SZP`, with the actual Yay0 stream beginning at the offset stored at `+0x08`.

The current decompression logic is:

```text
PERS-SZP -> read inner-stream offset -> Yay0 decode
Yay0     -> Yay0 decode
otherwise -> use payload directly
```

This means archive extraction and FRAGMENT parsing should be kept as separate stages. Do not assume every archive entry begins directly with `FRAGMENT`.

---

## 4. FRAGMENT modules

After decompression, Pokémon resources are FRAGMENT modules.

The parser verifies:

```text
offset +0x08: "FRAGMENT"
```

Pointers inside the module are virtual addresses relative to:

```text
FRAGMENT base = 0x8FF00000
```

Thus:

```text
localOffset = pointer - 0x8FF00000
```

with a zero pointer treated as null.

### 4.1 Finding the root

The root is not read from a hardcoded local offset.

The current parser scans approximately `0x20..0x80` for a MIPS `LUI` followed by an `ADDIU` using the same register. Combining their immediates produces a virtual address, which is converted from `0x8FF00000` into a FRAGMENT-local offset.

Conceptually:

```text
LUI   rX, hi(root)
ADDIU rX, rX, lo(root)
```

This has proven considerably safer than assuming the root structure is always at a fixed location.

---

## 5. Root structure

The fields used by the working parser are:

```text
root + 0x00 : u16 species ID

root + 0x08 : pointer -> null-terminated pointer list of model/layout streams
root + 0x0C : pointer -> null-terminated pointer list of skeletal animations
root + 0x10 : pointer -> null-terminated pointer list of auxiliary/material animations
```

The pointer lists terminate when a null pointer is encountered.

Only the first layout stream is required by the current model parser.

This root layout is one of the most useful observations in Stadium 1: **geometry, skeleton, skeletal animations and auxiliary animations can all be reached from the same FRAGMENT.**

---

## 6. Layout command stream

The model/layout is not simply a conventional array of bones and meshes. It is a command stream/tree interpreted recursively.

The current parser maintains:

* a bone stack;
* current texture;
* current TLUT;
* current material display list;
* current texture-animation channel;
* N64 tile state;
* a 64-entry vertex cache.

Several commands are especially important.

### 6.1 Structural commands

Observed/used commands include:

```text
0x00 / 0x03 : recurse into child stream
0x02        : jump/continue at another stream
0x05        : push current bone
0x06        : pop bone
```

The exact semantic names are less important than preserving the hierarchy while walking the stream.

### 6.2 Model header — command 0x17

Command `0x17` exposes texture and TLUT tables.

Relevant fields:

```text
+0x02 : s16 textureCount
+0x04 : s16 tlutCount
+0x08 : ptr textureTable
+0x0C : ptr tlutTable
```

Texture records have a stride of `0x0C`:

```text
+0x00 : u8  format
+0x01 : u8  size
+0x02 : s16 width
+0x04 : u16 height
+0x08 : ptr texel data
```

TLUT records also use `0x0C`:

```text
+0x02 : u16 count
+0x04 : ptr palette data
+0x08 : ptr palette display list
```

The TLUT display list may override the palette data pointer/count, so following it is important for CI textures.

### 6.3 Root scale — command 0x1C

The parser reads three signed 16.16 fixed-point values:

```text
+0x04 : X
+0x08 : Y
+0x0C : Z
```

and stores them as the model root scale.

### 6.4 Bone — command 0x1D

The working bone structure is:

```text
+0x01 : u8  boneId
+0x02 : u8  flags
+0x03 : s8  animation channel
+0x04 : s16 translation X
+0x06 : s16 translation Y
+0x08 : s16 translation Z
+0x0A : s16 rotation X
+0x0C : s16 rotation Y
+0x0E : s16 rotation Z
+0x10 : s32 scale X, 16.16
+0x14 : s32 scale Y, 16.16
+0x18 : s32 scale Z, 16.16
```

The parent is the bone currently at the top of the parser stack.

### 6.5 The critical animation-binding field: `Bone.Channel`

Do **not** assume skeletal animation channel `N` belongs to bone `N`.

Each bone explicitly contains:

```text
s8 Channel
```

at bone command `+0x03`.

A negative value means that the bone has no sampled animation track and should remain at its bind transform.

For animated bones, this `Channel` value is the index used by the animation decoder.

This became an important reference when later reverse engineering Stadium 2.

### 6.6 Material/texture state — command 0x23

The current parser extracts:

```text
+0x02 : texture-animation channel
+0x04 : material display-list pointer
+0x08 : texture index
+0x0A : TLUT index
```

It then follows the material display list to recover palette and N64 tile addressing state.

That texture-animation channel is what later connects a primitive to an `AuxAnimationData.Channels[]` entry.

---

## 7. N64 display lists and geometry

Geometry is reconstructed by interpreting the display lists referenced from the layout.

The parser currently handles the subset required by the Pokémon models, including nested display lists, vertex loading, triangle emission and relevant render/tile state.

### 7.1 Vertex load

For the observed vertex format, each vertex occupies `0x10` bytes:

```text
+0x00 : s16 position X
+0x02 : s16 position Y
+0x04 : s16 position Z

+0x08 : s16 texture S
+0x0A : s16 texture T

+0x0C..0x0F : normal+alpha OR RGBA
```

Texture coordinates are interpreted as:

```text
S / 32.0
T / 32.0
```

Whether bytes `0x0C..0x0F` are normals or vertex colors depends on the N64 lighting geometry mode.

With lighting enabled:

```text
s8 nx
s8 ny
s8 nz
u8 alpha
```

Without lighting:

```text
u8 red
u8 green
u8 blue
u8 alpha
```

Each loaded vertex is associated with the bone active when its display list is processed.

### 7.2 Tile state

`G_SETTILE`-style state is used to recover:

* palette index;
* mirror S/T;
* clamp S/T.

Preserving this matters. A texture that visually looks "wrong" is not necessarily decoded incorrectly; incorrect mirror/clamp semantics can produce the same symptom.

For old Unity versions without independent mirror wrap support, the importer may bake mirrored copies. That is an exporter workaround, not part of the Stadium file format.

---

## 8. Skeletal animation structure

A skeletal animation header is currently interpreted as:

```text
+0x00 : u8  flags
+0x06 : u16 loopStart
+0x08 : u16 channelCount
+0x0A : u16 frameCount

+0x0C : ptr channel descriptor table
+0x10 : ptr scale data
+0x14 : ptr rotation data
+0x18 : ptr translation data
```

Each low-level channel descriptor is `0x0A` bytes:

```text
+0x00 : u8  number of scale values/keys
+0x01 : u8  number of rotation values/keys
+0x02 : u8  number of translation values/keys
+0x03 : u8  interpolation flags
+0x04 : u16 scale offset/index
+0x06 : u16 rotation offset/index
+0x08 : u16 translation offset/index
```

### 8.1 Three descriptors per logical animation channel

A bone's `Channel` is converted to:

```text
baseIndex = Bone.Channel * 3
```

and the decoder reads three consecutive descriptors:

```text
baseIndex + 0
baseIndex + 1
baseIndex + 2
```

for X/Y/Z.

Each of those descriptors independently describes scale, rotation and translation data for its axis.

Therefore the skeletal binding is:

```text
Bone
 └─ Bone.Channel
     └─ channel * 3
         ├─ X descriptor
         ├─ Y descriptor
         └─ Z descriptor
```

rather than bone index -> track index.

---

## 9. Animation sampling modes

The animation flags select between at least two encoding families in the working decoder.

### 9.1 Hermite/keyframe mode

When:

```text
flags & 0x08
```

is set, scale/rotation/translation may be represented by keyframes.

A key contains:

```text
s16 frame
s16 value
s16 inTangent
[s16 outTangent]
```

The interpolation flag for each component determines whether a separate output tangent is present.

The current mapping is:

```text
interpolation & 0x01 : translation wide key
interpolation & 0x02 : rotation wide key
interpolation & 0x04 : scale wide key
```

The parser evaluates these using Hermite interpolation.

Observed unit conversions in this path:

```text
rotation value / 10.0 -> degrees
scale value / 100.0
```

Rotation is then converted into Stadium's 16-bit angular unit representation.

### 9.2 Packed/sample mode

Without the Hermite flag, components can be:

* absent -> use bind-pose component;
* constant -> value stored directly in the descriptor;
* sampled -> values read from component data arrays.

Observed packed widths include:

```text
translation : 12-bit or 16-bit depending on flags
rotation    : 12-bit packed samples, promoted to 16-bit angular units
scale       : signed 16-bit samples / 1000
```

A useful implementation rule is to preserve the bind transform as the fallback before sampling each channel. Zero is **not** the correct fallback for a missing component.

---

## 10. Rotation representation

Bones and decoded tracks retain rotation in a 16-bit turn-based unit:

```text
65536 units = 360 degrees
```

For Hermite rotations, the stored key value is interpreted as tenths of a degree before conversion.

When exporting to another engine, convert deliberately and consistently. Small errors here can manifest as jitter, wrap discontinuities or bones taking the long rotational path.

---

## 11. Auxiliary / material animations

Stadium 1 contains a separate auxiliary animation list in the FRAGMENT root.

The parsed header is:

```text
+0x00 : u8  flags
+0x06 : u16 loopStart
+0x08 : u16 channelCount
+0x0A : u16 frameCount
+0x0C : ptr channel table
+0x10 : ptr data
```

Each auxiliary channel descriptor is four bytes:

```text
+0x00 : u16 count
+0x02 : u16 baseIndex
```

The data itself is byte-sized.

For each frame:

```text
value = data[baseIndex + min(frame, count - 1)]
```

If `count == 0`, the parser emits `-1`.

These values act as texture/material selections for primitives whose layout command specified a matching `TextureAnimation` channel.

This is how animated eyes, mouths and similar material changes are represented in the working parser.

---

## 12. Associating skeletal and material animations

Although both animation lists live in the model FRAGMENT, the preferred skeletal-animation -> auxiliary-animation pairing is obtained from battle data in the ROM.

Relevant fixed locations in the USA ROM:

```text
Main ROM code/data base : 0x00001000
Main VRAM base          : 0x80000400
Pointer table VRAM      : 0x80075BD0
Battle data base        : 0x0070D3A0
```

The pointer table contains one 32-bit entry per species.

For species `1..151`:

```text
pointerOffset =
    MainRomOffset
  + (PointerTableVram - MainVram)
  + (species - 1) * 4
```

The importer masks the pointer to 24 bits:

```text
relative = pointer & 0x00FFFFFF
battleTable = 0x0070D3A0 + relative
```

The per-Pokémon battle table inspected by the importer is:

```text
stride/size = 0xB90
entry size  = 0x10
```

For the material-animation association, the first two bytes are used:

```text
entry + 0x00 : skeletal animation index
entry + 0x01 : auxiliary animation index, 0xFF = none
```

The current importer counts how often each `(animationIndex, auxIndex)` pair occurs and assigns the most frequently referenced auxiliary animation to that skeletal animation.

That is an important distinction:

**the FRAGMENT tells us what animations exist; battle data tells us which material animation is normally paired with a skeletal animation in battle contexts.**

The "most frequent pair" selection is an importer heuristic built on those observed references. A stricter game reimplementation may want to preserve the complete battle table rather than collapse it to one preferred Aux index.

---

## 13. Material animation export strategies

The reverse-engineered data can be represented in an engine in at least two ways.

The supplied Unity importer supports both:

### Animation-curve approach

Create object-reference keys in each animation clip that swap materials/textures at the required frames.

This maps naturally onto the decoded `AuxAnimationData`.

### Callback/event approach

Generate animation events and let a runtime texture swapper select from prebuilt texture sets.

This can be useful on constrained targets or older Unity versions.

These are exporter/runtime choices, **not two different Stadium formats**.

---

## 14. Shiny Pokémon

Shiny export should be treated separately from the core model parser.

The model FRAGMENT gives you the normal texture/TLUT resources. The supplied project additionally contains `PS3DS_PokemonStadiumShinyData.cs`, which implements game-specific shiny recoloring/LUT knowledge used by the exporter.

Important distinction:

```text
FRAGMENT parsing       = model format reverse engineering
shiny reconstruction   = color/LUT interpretation layered on top
```

Do not assume a second complete shiny model is stored beside every normal model.

The current exporter creates shiny texture/material assets and can create a shiny companion prefab while reusing the same underlying geometry/skeleton/animation structure.

---

## 15. HUE / trainer color variants

Stadium's trainer-dependent color variants are also an additional layer over the normal model.

The importer supports:

* trainer-data-derived HUE;
* random HUE;
* manual HUE.

The resulting variant should reuse the normal:

* geometry;
* skeleton;
* skeletal animation timing;

while replacing the relevant textures/materials.

### 15.1 Critical implementation detail: animated materials

A common exporter bug is:

1. create a variant prefab;
2. replace its static renderer materials;
3. leave animation clips pointing to the **normal** materials.

The model then looks correct until an eye/mouth/material animation plays, at which point the normal texture reappears.

The correct engine-side order is:

```text
1. create/copy variant texture and material assets
2. clone/remap material-animation clips or texture-swap sets
3. make those animation references point to variant materials/textures
4. assign the variant's static renderer materials
5. save the variant prefab
```

This is not a new ROM-format rule; it is a consequence of correctly preserving the references encoded by Stadium's auxiliary animation channels.

---

## 16. Recommended parser architecture

A clean implementation can be split into these stages:

```text
RomReader
 ├─ normalize N64 byte order
 ├─ locate fixed archive for known revision
 └─ extract archive entries

ArchiveDecoder
 ├─ raw
 ├─ PERS-SZP
 └─ Yay0

FragmentReader
 ├─ virtual pointer -> local pointer
 ├─ locate root through MIPS bootstrap
 └─ primitive endian reads

FragmentParser
 ├─ root metadata
 ├─ layout command walker
 ├─ skeleton
 ├─ texture/TLUT records
 ├─ N64 display lists
 ├─ skeletal animations
 └─ auxiliary animations

BattleDataParser
 └─ skeletal animation <-> auxiliary animation usage

EngineExporter
 ├─ meshes
 ├─ skeleton
 ├─ textures/materials
 ├─ skeletal clips
 ├─ material animation
 ├─ shiny assets
 └─ HUE variants
```

Keeping the engine exporter separate from the ROM/parser code makes debugging substantially easier.

---

## 17. Common reverse-engineering traps

### Trap 1 — binding animations by bone index

Wrong:

```text
bone 0 -> animation channel 0
bone 1 -> animation channel 1
...
```

Correct:

```text
bone -> Bone.Channel -> channel descriptors
```

Bones with `Channel < 0` are not sampled.

### Trap 2 — assuming the FRAGMENT root is at a fixed local offset

The working parser derives it from the MIPS bootstrap code.

### Trap 3 — ignoring bind-pose fallback

Missing animation components retain their bind value. Treating them as zero corrupts poses.

### Trap 4 — decoding CI textures without TLUT display-list state

The TLUT record's display list can resolve/override palette information.

### Trap 5 — ignoring mirror/clamp tile state

Texture decode can be correct while UV presentation is wrong.

### Trap 6 — treating material animation as skeletal animation

They are separate data structures:

```text
AnimationData    -> bone S/R/T
AuxAnimationData -> material/texture selection channels
```

### Trap 7 — replacing variant materials but not animated references

This produces normal eyes/mouths on an otherwise correctly HUE-shifted Pokémon.

### Trap 8 — assuming all archive entries are uncompressed

Check `PERS-SZP` and `Yay0`.

---

## 18. What is strongly established by the working implementation

The supplied importer provides working evidence for:

* Pokémon model archive at `0x00920000` in the targeted USA ROM;
* archive count at `+0x0C`;
* `0x10`-byte archive descriptors;
* PERS-SZP/Yay0 decompression;
* FRAGMENT virtual base `0x8FF00000`;
* MIPS-derived FRAGMENT root;
* root species ID;
* root pointer lists for layouts, skeletal animations and auxiliary animations;
* command-stream skeleton construction;
* explicit `Bone.Channel` animation binding;
* texture and TLUT tables;
* N64 display-list geometry reconstruction;
* palette, mirror and clamp state;
* skeletal S/R/T animation decoding;
* Hermite and packed/sample animation paths;
* auxiliary/material animation decoding;
* battle-data-assisted skeletal/Aux pairing;
* normal, shiny and HUE-variant export on top of the parsed model.

---

## 19. What should still be treated cautiously

The current importer is sufficient to reconstruct the models, but not every byte has been semantically named.

In particular:

* unused fields in the `0x10` archive descriptors are not documented here;
* many layout command IDs are only implemented to the extent needed by the models;
* not every N64 display-list opcode is required or interpreted;
* several animation flag bits are understood operationally rather than through original symbolic names;
* collapsing battle references to a single "preferred AuxAnimation" is an importer convenience;
* shiny/HUE behavior includes exporter/game-behavior knowledge beyond the raw FRAGMENT parser;
* fixed ROM offsets are confirmed for the targeted USA revision and should not be assumed universal.

For decompilation-quality work, preserve unknown fields and raw records instead of discarding them merely because an asset exporter does not need them.

---

## 20. Stadium 1 vs Stadium 2: the key lesson

Stadium 1 ended up being extremely useful as a reference for Stadium 2 because much of the conceptual model survives:

```text
FRAGMENT
Bone.Channel
S/R/T animation descriptors
Aux/material animation channels
N64 material/display-list state
```

But the major storage difference discovered during the Stadium 2 work is:

```text
Stadium 1:
    model FRAGMENT -> skeletal animation list directly

Stadium 2:
    battle model archive entry N
        +
    separate battle-animation archive entry N
        -> animation bank
```

That difference is why a Stadium 1-derived decoder could be fundamentally correct while a Stadium 2 importer still appeared to have "no animations": the missing problem was initially **resource location and association**, not the S/R/T decoder itself.

Once the Stadium 2 animation bank was found, Stadium 1's channel semantics became the guide that made the battle animations usable.

---

## 21. Minimal pseudocode

A simplified Stadium 1 Pokémon import looks like:

```c
rom = normalize_n64_rom(file);

archive = rom + 0x00920000;
count = be32(archive + 0x0C);

entry = archive + 0x10 + index * 0x10;
payload = archive + be32(entry + 0x00);
size    = be32(entry + 0x04);

fragmentBytes = decompress_if_needed(payload, size);
fragment = FragmentReader(fragmentBytes, 0x8FF00000);

root = find_root_from_mips_bootstrap(fragment);

model.species = be16(root + 0x00);

layouts    = read_ptr_list(ptr(root + 0x08));
animations = read_ptr_list(ptr(root + 0x0C));
auxAnims   = read_ptr_list(ptr(root + 0x10));

walk_layout(layouts[0], model);

for each animation:
    for each bone:
        if bone.channel >= 0:
            sample animation channel bone.channel into S/R/T track;

for each aux animation:
    decode byte-valued material channels;

read battle table for model.species;
associate skeletal animation indices with aux animation indices;
```

That is the shortest useful mental model of the format currently implemented.

---

## 22. Source basis for this write-up

This document was derived from the supplied working Stadium 1 implementation:

```text
PS3DS_PokemonStadiumModelImporter.cs
PS3DS_PokemonStadiumShinyData.cs
```

It intentionally documents the behavior and structures supported by that implementation rather than inventing names for unknown fields.

The most reusable reverse-engineering discoveries are the FRAGMENT root structure, `Bone.Channel` binding, animation descriptor decoding, auxiliary material-animation representation, and the separate battle-data association step.
