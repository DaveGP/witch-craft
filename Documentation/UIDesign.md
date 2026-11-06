# Witch Craft UI design

The interface borrows from objects the witch might keep at home. Open books have a visible fold and layered paper edges. Tools sit on wood or leather, held in place by small brass fittings. A few inked stars and ruled lines carry the motif between screens. The island stays visible around menus and through the middle of the alchemy bench.

## Palette

| Token | Hex | Use |
| --- | --- | --- |
| Parchment | `#F5E7CA` | Pages and writing surfaces |
| Page edge | `#D9BB8B` | Stacked paper and ruled details |
| Ink | `#34251F` | Main text on light surfaces |
| Soft ink | `#60483B` | Captions on parchment |
| Leather | `#422B39` | Book covers and bag flap |
| Walnut | `#51372B` | Shelf and workbench trim |
| Night ink | `#2A2028` | HUD and dark control faces |
| Linen | `#FFF2DC` | Main text on dark surfaces |
| Soft linen | `#DBC7B1` | Secondary text on dark surfaces |
| Brass | `#C18A49` | Borders, clasps and meter frames |
| Focus gold | `#FFE2A1` | Focus ring paired with a dark outer edge |
| Moss | `#315344` | Selected and positive states |
| Brick | `#813D3D` | Danger and destructive actions |
| Sky magic | `#B8D6F2` | Broom charge and small magical accents |
| Scrim | `#1B151C` | Translucent background dimming |

Use content colours for small swatches, book ribbons, potion and spell marks. Keep the associated text in a contrast-safe theme colour. Aim for at least 4.5:1 on readable text, including HUD text against the effective backplate on bright sky and at night.

## Typography and spacing

Alegreya gives headings a literary serif. Alegreya Sans Regular is the body and control face. Both permit commercial use under SIL OFL 1.1. Use official TTFs and retain each upstream licence with the imported files. Theme font slots load from `Unity/Assets/Resources/UI/Fonts/` and fall back to `LegacyRuntime.ttf` when files are unavailable. Check the existing runes, arrows, checkmark and punctuation, with per-label fallback where glyph coverage requires it.

- Display 88, screen heading 40, section heading 28, reading text 26, body 22, controls 20, secondary 18 and metadata 16 at the 1920 × 1080 reference size.
- Use 4, 8, 12, 16, 24, 32 and 48 reference units for gaps and insets. Controls start at 44 reference units high and can grow for wrapped labels.
- Measure dynamic text and use existing scrolling areas for long lists. Story spreads keep their existing page numbering and navigation. Give titles and folios dedicated space, then check each page at its actual text width.
- Keep HUD edges inset and cap centred menu width and height. Fit against both viewport dimensions, allow the title and bench layouts to compact when needed, and leave the additional scenery visible on ultrawide.

[Alegreya licence](https://github.com/google/fonts/blob/main/ofl/alegreya/OFL.txt) · [Alegreya Sans licence](https://github.com/google/fonts/blob/main/ofl/alegreyasans/OFL.txt)

## Shared components

Named surfaces cover parchment, book cover, leather, wood, dark plaque and inset. Named controls cover primary, secondary, danger, selected and swatch. Use original small nine-sliced sprites for frames, paper edges, controls and bar tracks, sharing a compact sprite atlas and the stock uGUI material. Use a shared soft offset shadow on main surfaces and fold shading baked into the artwork. Let Unity create asset metadata and verify the sprite borders. Runtime sprites with explicit borders are the fallback if Unity cannot import the art.

Normal buttons use a quiet border, hover lifts the edge, pressed darkens the face, and selected state uses a moss inset and brass mark. Disabled faces are muted. Keyboard and gamepad focus adds a distinct double outline on buttons, sliders and the name field. Decorative graphics are non-raycastable. Keep the button's legacy Text discoverable and keep functional focus separate from persistent selection such as the current recipe.

Menus open in about 150 ms and close in about 100 ms. Small opacity and offset changes use unscaled time. Close animation is presentation only. Logical closure, input mode restoration and callbacks still happen immediately. Any outgoing visuals are non-interactive and are dropped on a screen handoff or when reopening. Button feedback is about 75 ms. HUD popups fade and rise gently within their existing lifetimes.

## Screens and commit order

All script paths below are under `Unity/Assets/Scripts/UserInterface/`. Each row is a separate implementation commit after approval.

| Stage | Files | Treatment |
| --- | --- | --- |
| Theme and factory | `UIFactory.cs`, `UIRoot.cs`, `Views/View.cs`, new `UITheme.cs` and focused presentation helpers, `Unity/Assets/Resources/UI/` | Shared fonts, sizes, spacing, sprites, states, responsive bounds and transitions. Keep existing factory signatures available while views migrate. |
| HUD | `Views/HudView.cs` | Four small spell symbols in a single row at the lower right, mapped to slots 1–4 from left to right. Mark the readied spell with a gold pip. Keep the interaction prompt centred and give it and the quest card matching translucent backplates. Give the prompt a keycap separated from the action by a middle dot. Keep quest, broom, location and status details at the edges. |
| Spellbook | `Views/SpellbookView.cs` | A leather-bound grimoire with a shaded fold, stacked pages, stamped glyph tiles and a ruled list of spells opposite. |
| Story reader | `Views/BookView.cs` | An open storybook with cream paper, restrained ink flourishes, a ribbon tinted per book and clear page numbers at the foot. |
| Satchel | `Views/SatchelView.cs` | A leather flap with stitched edges and a brass clasp above pockets with cream inventory tags. Keep quantities, descriptions and Drink easy to scan. |
| Alchemy | `Views/AlchemyView.cs` | Walnut bench rail, clipped recipe leaves on the left and ingredient tags on the right. Inset fire and hourglass gauges retain a distinct boil band. Keep the cauldron visible between the panels. |
| Journal | `Views/JournalView.cs` | Ruled notebook paper and stitched binding. Companion notes read as entries with completed steps set apart from the current objective. |
| Bookcase | `Views/BookcaseView.cs` | Walnut shelf rails and coloured spines beside found titles. Unfound entries use muted slips with the existing hints. |
| Build catalogue | `Views/CatalogueView.cs` | Drafting folio with tab dividers and material sample chips. Distinguish the chosen piece, colour and unmet costs at a glance. |
| Dialogue | `Views/DialogueView.cs` | A low parchment conversation card with a small speaker-colour seal and clear stacked choices. |
| Title | `Views/TitleView.cs`, `NameForm.cs` | Inked title with brass detail, save slots as labelled bookplates and a compact writing slip for naming. Keep the view of the island open. |
| Rename | `Views/NameView.cs` | Matching writing slip using the shared name form. |
| Pause | `Views/PauseView.cs` | Small leather folio with a clear vertical action list. |
| Settings | `Views/SettingsView.cs`, `Documentation/TechnicalDesign.md` | A lined settings ledger with brass sliders and a separate controls leaf. Update the technical UI section with the completed architecture. |

## Shared spell symbols

Use an original small set of illustrated spell emblems with a consistent square frame, curled brass corners, a colour tied to the spell and a pale inked symbol. The readied tile carries a small gold diamond. The reference concepts are a feather for Levitate, curling wind for Gust, a sprout for Bloom, a flame for Wisp, a house for Conjure, shears for Unmake, a bubble for Bubble, a flash for Blink and a rain cloud for Rain Call.

The theme stage will add a presentation-only Sprite reference on `Unity/Assets/Scripts/Data/ScriptableObjects/SpellDefinition.cs` and populate the existing spell assets under `Unity/Assets/Data/Spells/`. Make the assignment survive content regeneration by wiring it into the spell authoring path. The HUD and the spellbook will read the same reference. Preserve the four slot positions when some are empty, and keep a readable fallback for spells without assigned art. Save data and spell behaviour are unaffected.

## Compatibility and verification

Keep code-built uGUI, legacy Text, existing view types and existing actions, names and copy. The playtest locates the Start button by its direct `Slot1` parent and exact label, and finds Settings, dialogue choices, Sleep, Change my name and Read through their child Text. It also reads view and HUD state directly. Preserve those contracts and keep event subscriptions, saves, shortcuts and gamepad routing intact.

Unity 6000.5.3f1 is installed on this machine. After approval, compile and run the batch playtest in an isolated project copy with `-executeMethod WitchCraftEditor.Playtest.Run -witchcraftPlaytest`. Inspect the report and screenshots under `Unity/Logs/Playtest`. Verify a standard 16:9 view, a small browser viewport and an ultrawide layout. Exercise keyboard, mouse and gamepad focus, long text, disabled and selected controls, and transitions at pause. Record which checks were automated and which were manual.

Keep the work on `ui-beautify` for review.
