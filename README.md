# Balajong

## How to Balajong!

Welcome to **Balajong** a fast-paced, procedural roguelike where you build decks, unlock synergies, and outsmart your opponents.

### Core Gameplay
- **Play tiles** from your hand to perform attacks, defenses, and special effects.
- **Manage your resources** (such as energy or mana) to maximize your turn efficiency.
- **Adapt your strategy** based on the procedurally generated enemy requirements of each match.

### How to Win
- Build your score to beat the enemy's threshold before they reduce your health to **0**.
- Use **combos**, **timing**, and **deck synergies** to gain the upper hand.

---

## Procedural Systems

Balajong uses procedural generation to keep every play session fresh and engaging.

- **Procedural map + encounters (node-map):**
  - Builds a layered node-based encounter map from `MapConfig`.
  - `NodeMap` selects nodes, saves `MapRunState`, assigns encounters to `EnemyManager`, and transitions to the encounter scene.
  - `GameManager` resolves battles and advances the run.

- **Procedural enemy stats, names, and descriptions:**
  - Generates enemy health, names, and descriptions based on depth/type.
  - Uses thresholds and lists from `EnemyInformationGrammer`.
  - Stores results in `EnemyManager` for `GameManager` and UI to use.

- **Procedural sound tool:**
  - Creates ambient and “racking” sounds using Perlin noise for volume control.
  - Adds random delays/noise to make sound effects feel less repetitive.

- **Generic loot chest system (demo-style):**
  - Uses CSV tables to map rarity tiers to items.
  - Picks loot via weighted randomness configured in the inspector.
  - Runs in a standalone scene via `LootChest` (not hooked into main loop).

- **Joker loot table + rule-mod system:**
  - Generates jokers from a CSV-defined, rarity-weighted pool.
  - Stores activation counts in `JokerManager`.
  - `GameManager` and `DeckManager` query it to modify rules (hand size, discard behavior, rewards).

- **Consumable loot + tile effects:**
  - Generates consumables via CSV rarity tiers.
  - Applies effects using a multi-phase system driven by item properties (e.g., `equationType`).
  - `ConsumableManager` + UI triggers coordinate with `GameManager` during effect resolution.

- **Procedural tile generation + deck randomization:**
  - Assigns tile identities and deck order using weighted random generation and shuffling.
  - Managed by `TileGenerator`, `RandomTileGen`, and `DeckManager`.
  - Gameplay uses the generated deck state for draws and other tile actions.

- **GameManager as procedural orchestrator:**
  - Coordinates gameplay phases and integrates outputs from enemy, joker, consumable, and deck systems.
  - Manages encounter outcomes and transitions via map state systems.

---

## Credits

**Balajong** was developed as a game project in the CMPM147 course at UCSC with the following procedural tools:

- [Jeremy Miller Background Generator](https://github.com/jeremymiller99/NoiseBGShader)
- [Michael Tang Loot Chest Generator](https://github.com/mtang44/CMPM147-Loot-Table-Generator)
- [Charles Lesser Audio Generator](https://github.com/chlesser/Audio-Stream-Generator)

**Inspirations**

- [Balatro](https://store.steampowered.com/app/2379780/Balatro/)
- [Mahjong Scoring](https://docs.google.com/document/d/1NBE6n6PjTUZTOovkKI3vujHYXhTNVJRMZgKoo4tElM4/edit?tab=t.0)

**Assets**

- [Riichi Mahjong Sets](https://sketchfab.com/3d-models/riichi-mahjong-839fb524dab9479f87995f17c072b151)
- [Mixed Shader Pack](https://assetstore.unity.com/packages/vfx/shaders/mixedshaderpack-127277)
- [Character Effects Shader](https://assetstore.unity.com/packages/vfx/shaders/character-effects-shaders-304307)
- [Plasma Shader](https://assetstore.unity.com/packages/vfx/shaders/plasma-shader-328840)

---

## Contributors

This project is built by the following contributors:

- [Charles Lesser](https://github.com/chlesser)
- [Michael Tang](https://github.com/mtang44)
- [Andrew Degan](https://github.com/adegan1)
- [Jenalee Nguyen](https://github.com/jnguy405)

