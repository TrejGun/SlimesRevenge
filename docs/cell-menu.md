# Cell menu

Tap a cell to open a context menu. The menu only renders buttons; each entry decides its own visibility from `CellMenuContext`.

## Open

| Tap | Result |
| --- | --- |
| Slime cell | Mess / Collect / Devour / Info (whichever apply) |
| Adjacent living foe | Attack + Info |
| Adjacent empty | Move (no menu) |

Far cells are not opened in this pass.

## Entries

| Entry | Visible when | Activate |
| --- | --- | --- |
| Attack | Adjacent living occupant (8 dirs, Chebyshev) | Substance picker → `TryAttack` (disabled when volume ≤ 1) |
| Mess | Actor cell, no puddle, volume > 0 | Substance picker → `TryMakeMess` (disabled when volume ≤ 1) |
| Collect | Actor cell, puddle, volume < capacity | `TryCollectPuddle` |
| Devour | Actor cell, corpses present, not digesting | 1 corpse → devour immediately; many → picker (`Digesting.CanBegin` disables too-large) |
| Info | Anything on the cell (occupant, corpses, puddle, or slime itself) | 1 inspectable → panel; many → flat picker (no class grouping) |

If no entry is visible, the menu does not stay open.

## Info panel

Info opens a **card** on `PopupHost` (not a text block in CellMenu):

| Target | Card |
| --- | --- |
| Living / corpse / slime | `CreatureCard` (icons for volume + statuses) |
| Puddle | `SubstanceCard` |

Icon clicks push nested `SubstanceCard` / `StatusCard`. See [Architecture/popup-cards.md](Architecture/popup-cards.md).

## Stack

`CellMenu` keeps a level stack (`Push` / `Back` / dim cancels everything) for action pickers. Info cards use a **separate** `PopupHost` stack above the menu.
