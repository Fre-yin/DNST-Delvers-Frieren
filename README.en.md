# Dungeon Settlers Delvers: Frieren

[Deutsch](README.md) | **English**

Frieren is an optional character pack for Dungeon Settlers Delvers. You can recruit her as a unique elven mage in Custom Expedition or through the guild. She has her own biography and portraits, the legendary Elven Archmage background, and the Booklover trait. Her story follows her search for rare spells into the awakened dungeon core.

**Version 0.3.0 requires Delvers Core 0.3.0 with API 1.3.0.** Install Core first and use the same loader for both mods. Download `DelversFrieren-0.3.0-MelonLoader.zip` or `DelversFrieren-0.3.0-BepInEx.zip` from [Releases](../../releases), depending on your loader. GitHub's automatically generated source archives cannot be installed as mods.

## Frieren in the game

Frieren can appear in Custom Expedition and the guild pool. The guild charges technique books instead of gold. Her price is one book for each started 150 gold of the usual guild price, with a minimum of one book. Core prevents rerolls from creating her more than once as a unique candidate. Once you have recruited her in a campaign, the game remembers this even if she dies and does not generate another Frieren.

The legendary Elven Archmage background replaces her ordinary Mage background. It lets her use fire, water, and nature magic with the appropriate staffs. It raises her maximum energy by 50 and her cooldown reduction by 20, while reducing her physical damage by 40. Her main skill trees are Fire, Water, and Nature Magic. Her secondary trees are Burst, Tide, and Harmony. She uses the game's existing spells; the package contains no copied spell art.

Frieren starts between levels 1 and 8, depending on the candidate level. Her base attributes are Intelligence 17, Willpower 16, Strength 11, Constitution 12, Agility 12, and Perception 13. Intelligence and Willpower have Genius talents, Strength has Poor, and the remaining attributes have Moderate. Higher starting levels use fixed rolls through the game's normal level progression. Her clothing, headgear, and fire staff improve across three equipment tiers. The game then calculates her guild price.

Booklover is an ordinary individual trait. When a character with this trait uses a technique book from their inventory, the book is consumed. They receive two main skill points, two secondary skill points, and four extra mood points for 20 minutes. Characters from any of the seven original races can also receive Booklover through their usual trait pools. It gives no passive skill point or sleep bonus.

The package includes four PNG files: two portraits and the images for Elven Archmage and Booklover. Frieren's character in the game world uses art from your installed game. That art and the working files are not included. New candidates receive her biography immediately. On older saves, Frieren fills in a blank biography but leaves any custom text intact.

## Requirements and installation

1. You need Windows x64, Dungeon Settlers DS_B.0.4.23 with the verified signature of Steam builds 25269660 or 25284551, and Delvers Core 0.3.0 with API 1.3.0.
2. Install MelonLoader 0.7.3 or BepInEx 6 for Unity IL2CPP x64. Core and Frieren must use the same loader. Do not install both loaders in the same game.
3. If you used an older version of Frieren, remove `DungeonSettlersFrierenPortrait.dll` from `Mods/`, `UserLibs/`, or `BepInEx/plugins/`.
4. Back up your saves and extract the ZIP for your loader into the game folder. With MelonLoader, the adapter goes in `Mods/`, the Frieren DLL in `UserLibs/`, and the images in `Mods/DungeonSettlersDelvers/Frieren/`. With BepInEx, both DLLs go in `BepInEx/plugins/DungeonSettlersDelvers/`, with the images in its `Frieren/` subfolder.

Extended Hotbar 1.0.1 is optional. Normal campaigns loaded in game with Core, Frieren, and this Hotbar version under both loaders. This does not confirm compatibility with every other combination of mods.

## Testing and rights

The separate source package built against Core for MelonLoader and BepInEx with no warnings or errors. It passed 31 checks of Frieren's rules for each loader. Her biography was checked in the recruitment window under MelonLoader. A normal campaign also loaded under BepInEx, where an older blank Frieren biography was filled in. Quickload and further cases involving older saves have not yet been fully checked in game.

Original code and documentation are MIT licensed. Frieren, Dungeon Settlers, characters, designs, game art used as a reference, and trademarks are excluded. See [LICENSING.txt](LICENSING.txt) and [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md). This is an unofficial community project and is not made by CanOpener. For source build instructions, see [BUILDING.en.md](BUILDING.en.md).
