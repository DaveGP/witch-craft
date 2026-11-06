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

Arriving in one of these places names it briefly in the bottom-left corner.

| Area | Where | What's there |
| ---- | ----- | ------------ |
| Home meadow | Centre | Starter hut, alchemy bench, hearth, crates to levitate, herb planters |
| Forest | North east | Lavender, nettle, wild berries, fallen timber, glowcaps, bounce mushrooms, Bramble the hedgehog |
| Pond | West | Spring water, reeds, clay, glowcaps, lily pads, a stump climb to the **Vita** shrine, Pip the frog |
| Hills | South | Stone, Moon Dew silverbells, moonpetals on the tallest hill, the start of a hidden path |
| Crystal cave | South east | A rocky mound with a jagged tunnel of stalactites. Crystal, ember moss, the **Nihil** shrine, Ember the fox spirit |
| Sky spire | East | A spiral climb of platforms, a moving platform and a bounce mushroom up to the **Motus** shrine and the Featherfall recipe |
| Sky island | Above the spire | Reached by riding the updraft beside the spire summit. **Lux** shrine, feather down, moonpetals, Hoot the owl |
| Sea stacks | North of the main island | Rocky stacks for a broom to hop and recharge on, with a signpost on the north beach |
| Tidewater Isle | Across the northern sea | A sandy beach island with a grassy rise and dunes. Kelp, seashells, giant clams with pearls, driftwood, palms, **Tilly the Turtle**; stepping rocks out to the **Aqua** shrine on the eastern reef; a geyser updraft rising to Starfall Ridge |
| Starfall Ridge | High above Tidewater | Night-only stardust, **Nova the Star Sprite**; the **Astra** shrine on an isle too high for the broom (Bubble up to it); the Comet Nook, only reachable with Blink |
| Secret isle | South, high up | Reached by a hidden path that only appears near a Wisp or while Glimmering. Moonpetals and crystal |

## Your witch

Starting an empty slot asks for your witch's name first. The field comes filled with a suggestion, and "Suggest another" picks a new one, so a gamepad never has to type. Names are up to 16 characters; markup and braces are stripped. Saves from before names existed ask once, on Continue. The name shows on the save slot and the HUD ("Hazel's home"), companions use it once they've moved in, and the hearth's "Change my name" renames her.

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
* Every few hours the weather may change: clear, cloudy or rain. Rain greys the sky, brings the fog in, hushes the birds and makes herbs regrow twice as fast. Rain stops on roofs, walls and the ground, so it's dry under any roof you build.

## Sound

All sound is synthesised at startup (`Audio/SoundSynth`): footsteps, jumps and landings, pickups, learning chimes, building pops, spell shimmer, gust whooshes, bounce boings, the bellows, grinding, a bubbling cauldron that gets louder with the heat, Sable's purr, broom wind and birdsong, plus a gentle music loop. Music and sound can be toggled in the pause menu.

## Movement

* Camera relative running with walk, run and sprint speeds. Q and R turn the view for playing without a mouse, unless a spell is using them (Conjure, or something held with Levitate).
* Jumps are buffered and forgiving (coyote time). Holding jump goes higher, tapping hops. One air jump.
* Bounce mushrooms launch you and refresh your air jump. Moving platforms carry you.
* **Broom** (F): fly level where the camera faces (it holds its height), Space to climb, Ctrl to dive, Shift to boost; tilting the view well up or down also climbs or dives. Flight uses a charge that refills on the ground. The broom skims the land and the sea: climbing slows over the last few metres to a gentle stop at its 7 m hover ceiling (the broom shimmers, and the first time a hint says updrafts can carry you higher); above the ceiling it eases back down, and flying off something high becomes a long glide, so the high places still need platforming. Updrafts lift you past the ceiling and refill the broom.
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
| Seafoam Draught | Walk on water | Tilly |

## Spells

Glyphs come from shrines out in the world. In the spellbook (B) you place up to three glyphs and inscribe them; the right set discovers a spell. Wrong sets fizzle with a hint about how close you were, and undiscovered spells show as riddles. Equip spells to slots 1-4.

| Spell | Glyphs | Use |
| ----- | ------ | --- |
| Conjure | Terra + Forma | Build floors, walls, doorways, windows, fences, roofs, stairs and furniture on the home plot, snapped to a grid |
| Levitate | Aer + Forma | Pick up furniture and crates, steer them, rotate with Q/R, push and pull with the wheel, set them down |
| Unmake | Nihil + Forma | Remove something you built and get the materials back (half for the starter hut, which was a gift) |
| Gust | Aer + Motus | A wind dash: long leap on the ground, one air dash per jump, a burst on the broom |
| Bloom | Vita + Terra | Grow enchanted soil, such as the herb planters at home |
| Wisp | Lux + Vita | A light that follows you and reveals hidden paths |
| Bubble | Aqua + Aer | Float upwards for a few seconds, steering as you rise. Once per landing; jump to pop |
| Rain Call | Aqua + Vita | Call a shower (or clear one). Herbs regrow twice as fast in the rain |
| Blink | Astra + Motus | Vanish and reappear 7m ahead, up hillsides but stopping at walls. Once per jump |

You start knowing Forma, Terra and Aer, so Conjure and Levitate are available from the first visit to the spellbook.

## Home

The plot starts 8 cells (16m) across with a starter hut. Walls snap to cell edges, floors and roofs to cells, and pieces stack in storeys of 2.5m, so stairs and upper floors work. Furniture sits freely on a finer grid.

**Conjuring.** Right-click opens the build catalogue: every piece with its cost (red when you're short), its comfort, and the home level it unlocks at, plus ten paint colours. New pieces are conjured in the chosen colour; the catalogue's paintbrush repaints anything already built, for free. Z undoes the last piece conjured with a full refund, and middle-click (or V) copies a built piece's type, colour and rotation, which makes building runs of matching walls quick.

**Comfort.** Furniture (and windows and roofs, a little) add comfort, shown on the HUD and at the hearth. Each morning every resident grows fonder: +1, plus 1 for every 10 comfort (up to +3), plus 1 if their favourite piece is in the home. A comfortable home (12+) also makes chats worth more, and gifts are worth more as comfort rises. Favourites: Pip the herb planter, Bramble the rug, Hoot the bookshelf, Ember the crystal lamp.

| Level | Plot | Needs |
| ----- | ---- | ----- |
| 1 | 8 × 8 cells | - |
| 2 | 12 × 12 | 12 timber, 10 stone, 2 crystal, 1 companion. Unlocks stone walls, slate roofs, stairs, potion shelf, armchair |
| 3 | 16 × 16 | 20 timber, 16 stone, 6 crystal, 6 clay, 3 companions. Unlocks the glowcap lamp |

## Companions

**Sable** the cat is with you from the start: she follows at your heel, sits when you stop, rides your broom and can be petted.

Others are met out in the world and ask for something before they move in. At home they wander the plot; visit them to chat (a little friendship) or give gifts they like (more), and at friendship milestones they share recipes and secrets.

### Earning a companion

Companions are being moved from a single gift to a chain of quests, so the loop becomes: explore, find a book, find its author, earn them through several quests, and they move in. Bramble works this way now; the rest are planned below and still ask for their old gift.

* **A book leads to them.** They can't be found until their storybook has been read; a note in the back says where to look.
* **Each step is asked in person.** Doing something early still counts, but it has to be handed in, so each step means a trip back to them. Steps show on the HUD and in the journal (J), which hides what's still to come.
* **Bramble's chain:** read *Eye of Newt* (western shore) → find her spectacles in the shade beyond the Old Oak → conjure a Woven Rug at home → shift the boulder from her burrow with Levitate (Aer + Forma) → brew a Leaping Tonic and bring her one → climb the Old Oak for the acorn in its nest.
* **The Old Oak** is the first platforming trial. A bounce mushroom starts it, branches spiral up the trunk, one branch glides, and one 3.9 m gap is beyond a double jump (about 3 m) without a Leaping Tonic. The branches are too tangled to fly through. Lanterns mark checkpoints. A fall leaves you on the ground, free to walk away (to brew a tonic, say); the lantern at the foot of the oak will lift you back to the last one you landed on if you ask it.

| Companion | Book | Find | Build | Spell challenge | Potion | Trial | Status |
| --------- | ---- | ---- | ----- | --------------- | ------ | ----- | ------ |
| Pip (short, teaches the system) | The Silver Spellbook | 3 Glowcaps | Herb planter | - | - | - | Planned |
| Bramble | Eye of Newt | Her spectacles | Woven Rug | Levitate a boulder from her door | Leaping Tonic | The Old Oak | **Done** |
| Ember | The Lantern Under the Hill | Her lantern | Crystal lamp | Wisp, to reveal a hidden way | Glimmer Draught | A climb up a dark cave shaft | Planned |
| Hoot | The Owl Who Counted Stars (moved somewhere earlier) | A star chart page | Rooftop perch (new) | Gust, against the wind | Swiftness Brew | The spire's upper route | Planned |
| Tilly | Bubble Witch | Her shell keepsake and pearls | Washtub (new) | Bubble or Rain Call | Skyward Draught | Timed hops across the sea stacks | Planned |
| Nova | The Claw of the Phoenix | Stardust, at night | Telescope | Blink | A brew to be chosen | Night-only constellation platforms | Planned |

Still to do alongside these: slower gathering (fewer nodes, longer respawns, dearer home levels, rarer materials further out).

| Companion | Found | Wants | Shares |
| --------- | ----- | ----- | ------ |
| Pip the Frog | Pond | 3 Glowcaps | Glimmer Draught |
| Bramble the Hedgehog | Forest, once *Eye of Newt* is read | Her quest chain, below | Featherfall Elixir |
| Hoot the Owl | Sky island | A Swiftness Brew | Skyward Draught |
| Ember the Fox Spirit | Crystal cave | A Glimmer Draught | A hint about Wisp |
| Tilly the Turtle | Tidewater Isle | 3 Pearls | Seafoam Draught |
| Nova the Star Sprite (nocturnal) | Starfall Ridge | 4 Stardust | A hint about Blink |

Ember needs a potion only Pip can teach you, and Hoot needs you to brew, so the companions pull you through the other systems.

## Storybooks

Six short stories float open at waist height around the world, one in each area, so exploring pays off with something to read as well as something to gather. Picking one up opens it straight away (two pages at a time; the buttons, arrow keys or A and D turn the pages) and sends it home. No book sits beside a glyph shrine; each is a find of its own. Every book found also stands on top of each bookshelf at home in its cover colour, in a slot of its own, so the shelf fills up as you explore. Any bookshelf at home is your bookcase: it lists every book found, ready to read again, and gives a hint for each one still missing. Every home has a bookshelf, and older saves are given one in the hut. The bookshelf can be built from level 1, so an unmade one can go straight back up.

| Book | Author | Found |
| ---- | ------ | ----- |
| The Silver Spellbook | Hazel Thistlewick | On the summit of the tallest southern hill |
| Eye of Newt | Bramble Underleaf | The western shore, where the grass meets the sand |
| Bubble Witch | Tilly | On top of the middle sea stack, halfway to Tidewater |
| The Claw of the Phoenix | Anonymous | The hidden isle floating above the southern hills |
| The Owl Who Counted Stars | Hoot | The Comet Nook past Starfall Ridge (Blink only) |
| The Lantern Under the Hill | Ember | The hollow behind the southernmost hill |

## Ideas for later

* Day and night, with Moon Dew and moonpetals only at night
* Companion requests and home "comfort" that grows with furniture near them
* Decorating with paint and fabric colours
* More islands reached by broom from the sky island
* Rigged character models and animation to replace the primitive placeholders
