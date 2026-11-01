# Game Design

## Pitch

A cosy 3D platformer. You are a young witch with a tiny hut, a cat and a head full of ambition. Explore the island, gather what you need, come home to brew, inscribe and build, and fill your home with magical friends.

## Core loop

1. **Explore** the island by running, jumping and flying. Each area has things to gather and a challenge to reach.
2. **Gather** ingredients (potions), glyphs (spells) and materials (building).
3. **Return home** to brew at the alchemy bench, combine glyphs in the spellbook and build with spells.
4. **Expand** the home plot at the hearth once you have the materials and companions, unlocking new build pieces.
5. Use new potions and spells to reach new places, and repeat.

## The island

| Area | Where | What's there |
| ---- | ----- | ------------ |
| Home meadow | Centre | Starter hut, alchemy bench, hearth, crates to levitate, herb planters |
| Forest | North east | Lavender, nettle, wild berries, fallen timber, glowcaps, bounce mushrooms, Bramble the hedgehog |
| Pond | West | Spring water, reeds, clay, glowcaps, lily pads, a stump climb to the **Vita** shrine, Pip the frog |
| Hills | South | Stone, Moon Dew silverbells, moonpetals on the tallest hill, the start of a hidden path |
| Crystal cave | South east | Crystal, ember moss, the **Nihil** shrine, Ember the fox spirit |
| Sky spire | East | A spiral climb of platforms, a moving platform and a bounce mushroom up to the **Motus** shrine and the Featherfall recipe |
| Sky island | Above the spire | Reached by riding the updraft beside the spire summit. **Lux** shrine, feather down, moonpetals, Hoot the owl |
| Secret isle | South, high up | Reached by a hidden path that only appears near a Wisp or while Glimmering. Moonpetals and crystal |

## First five minutes

Sable guides a new witch through the core loop, one suggestion at a time, shown on the HUD with a beacon where it helps:

1. Walk and look around.
2. Inscribe Terra + Forma in the spellbook to discover Conjure.
3. Ready Conjure with 1.
4. Build something on the plot.
5. Brew a Swiftness Brew at the bench (the starting satchel has everything).
6. Bring Pip three Glowcaps.

Each step checks game state, so doing things early or out of order still counts. It can be skipped from the pause menu.

## Day, night and weather

A day lasts about fourteen minutes: dawn at 06:00, dusk at 20:00. The HUD shows the day, time and weather.

* Nights are moonlit and blue, with fireflies, crickets and softer music.
* Silverbells (Moon Dew) and moonpetals are closed buds by day and only open at night.
* Companions sleep at night. Hoot the owl is nocturnal and sleeps by day instead.
* Any bed lets you sleep until morning, or nap until evening. Sleeping saves.
* Every few hours the weather may change: clear, cloudy or rain. Rain greys the sky, brings the fog in, hushes the birds and makes herbs regrow twice as fast.

## Sound

All sound is synthesised at startup (`Audio/SoundSynth`): footsteps, jumps and landings, pickups, learning chimes, building pops, spell shimmer, gust whooshes, bounce boings, the bellows, grinding, a bubbling cauldron that gets louder with the heat, Sable's purr, broom wind and birdsong, plus a gentle music loop. Music and sound can be toggled in the pause menu.

## Movement

* Camera relative running with walk, run and sprint speeds.
* Jumps are buffered and forgiving (coyote time). Holding jump goes higher, tapping hops. One air jump.
* Bounce mushrooms launch you and refresh your air jump. Moving platforms carry you.
* **Broom** (F): fly where the camera looks, Space to climb, Ctrl to dive, Shift to boost. Flight uses a charge that refills on the ground. The broom skims the land: above its hover ceiling it glides down, so the high places still need platforming. Updrafts lift you past the ceiling and refill the broom.
* Falling into the sea puts you back on the last safe ground.

## Alchemy

The bench is a hands-on minigame inspired by Kingdom Come: Deliverance II.

* Pick a recipe in the recipe book; it lists the steps in order.
* **Pour** a base (Spring Water or Moon Dew).
* **Add** herbs whole, or put them in the **mortar** and **grind** (G) before adding them.
* **Turn the hourglass** to boil for one turn. The fire cools constantly; pump the **bellows** (Space) to keep it in the green boiling band. Too cool and the turn doesn't count; too hot and it scorches.
* **Bottle** when done.

The brew is judged against every recipe: following steps exactly gives a perfect brew (three bottles), small mistakes give fewer, worse bottles, and anything else is Murky Sludge. Brewing an unknown recipe correctly by accident (or from a companion's gossip) adds it to your book.

| Potion | Effect | Source of recipe |
| ------ | ------ | ---------------- |
| Swiftness Brew | Run faster | Known |
| Leaping Tonic | Jump higher, extra air jump | Known |
| Featherfall Elixir | Drift down slowly | Scroll on the spire summit, or Bramble |
| Glimmer Draught | Glow and reveal hidden paths | Pip |
| Skyward Draught | Tireless broom | Hoot |

## Spells

Glyphs come from shrines out in the world. In the spellbook (B) you place up to three glyphs and inscribe them; the right set discovers a spell. Wrong sets fizzle with a hint about how close you were, and undiscovered spells show as riddles. Equip spells to slots 1-4.

| Spell | Glyphs | Use |
| ----- | ------ | --- |
| Conjure | Terra + Forma | Build floors, walls, doorways, windows, fences, roofs, stairs and furniture on the home plot, snapped to a grid |
| Levitate | Aer + Forma | Pick up furniture and crates, steer them, rotate with Q/R, push and pull with the wheel, set them down |
| Unmake | Nihil + Forma | Remove something you built and get the materials back |
| Gust | Aer + Motus | A wind dash: long leap on the ground, one air dash per jump, a burst on the broom |
| Bloom | Vita + Terra | Grow enchanted soil, such as the herb planters at home |
| Wisp | Lux + Vita | A light that follows you and reveals hidden paths |

You start knowing Forma, Terra and Aer, so Conjure and Levitate are available from the first visit to the spellbook.

## Home

The plot starts 8 cells (16m) across with a starter hut. Walls snap to cell edges, floors and roofs to cells, and pieces stack in storeys of 2.5m, so stairs and upper floors work. Furniture sits freely on a finer grid.

| Level | Plot | Needs |
| ----- | ---- | ----- |
| 1 | 8 × 8 cells | - |
| 2 | 12 × 12 | 12 timber, 10 stone, 2 crystal, 1 companion. Unlocks stone walls, slate roofs, stairs, bookshelf, potion shelf, armchair |
| 3 | 16 × 16 | 20 timber, 16 stone, 6 crystal, 6 clay, 3 companions. Unlocks the glowcap lamp |

## Companions

**Sable** the cat is with you from the start: she follows at your heel, sits when you stop, rides your broom and can be petted.

Others are met out in the world and ask for something before they move in. At home they wander the plot; visit them to chat (a little friendship) or give gifts they like (more), and at friendship milestones they share recipes and secrets.

| Companion | Found | Wants | Shares |
| --------- | ----- | ----- | ------ |
| Pip the Frog | Pond | 3 Glowcaps | Glimmer Draught |
| Bramble the Hedgehog | Forest | 4 Wild Berries | Featherfall Elixir |
| Hoot the Owl | Sky island | A Swiftness Brew | Skyward Draught |
| Ember the Fox Spirit | Crystal cave | A Glimmer Draught | A hint about Wisp |

Ember needs a potion only Pip can teach you, and Hoot needs you to brew, so the companions pull you through the other systems.

## Ideas for later

* Day and night, with Moon Dew and moonpetals only at night
* Companion requests and home "comfort" that grows with furniture near them
* Decorating with paint and fabric colours
* More islands reached by broom from the sky island
* Rigged character models and animation to replace the primitive placeholders
