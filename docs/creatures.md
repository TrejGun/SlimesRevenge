# Существа

Поведение AI (personality к слайму, fear/hate между мобами): [behavior.md](behavior.md).

| Название | Хит-поинты | Броня | Субстанции |
| --- | ---: | ---: | --- |
| Slime | 0 | 0 | 2× Water, 1× Oil, 1× Poison, 1× Acid, 1× Lava |
| Rat | 3 | 0 | 1× Blood |
| Cat | 5 | 0 | 2× Blood |
| Dog | 10 | 0 | 3× Blood |
| Bat | 3 | 0 | 2× Blood |
| Scorpion | 3 | 1 | 1× Acid |

Пожирание трупа: слайм может начать, если его **заполненный** объём ≥ объёма трупа.

Объём = жизнь слайма (`IsAlive` при `UnitCount > 0`). Атака и лужа тратят 1 юнит; при последнем юните (`CanSpend == false`) эти пункты меню disabled — пустой substance picker не открывается.

Ближний бой (игрок и мобы): соседство = **Chebyshev 1** (8 клеток), единый API — [`GridStep.IsAdjacent`](../Assets/Scripts/Runtime/World/GridStep.cs) / `CreatureMoves.IsAdjacent`.

- **Карта:** на клетке рисуется только труп, умерший последним (лужа / живой моб могут быть рядом отдельно).
- **Меню Devour:** все трупы стека; у каждого `Name · N vol · cur/max turns` (объём и таймер гниения; max = MaxHitPoints); слишком большие — disabled. См. [cell-menu.md](cell-menu.md).

