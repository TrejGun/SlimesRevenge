# Доминантные статусы слайма

Когда у слайма одна субстанция занимает **≥ 80%** объёма, `Creature.RefreshVolumeStatuses` делегирует в `VolumeDominance.Refresh`: клон доминанта + `ApplyDominance(owner, queue)`.

## Коллабораторы

| Класс | Роль |
| --- | --- |
| `StatusQueue` | FIFO-список, Blocks/CancelsWith на add, Find/Count, Pulse |
| `VolumeDominance` | сэмпл доминанта, ClearOwned, ApplyDominance, пассивные Find/Blocks |

`Creature` только держит оба и проксирует API. `FindStatus` = dominance ∪ очередь (по longevity).

| API | Кто знает детали |
| --- | --- |
| `FindDominanceStatus` / `DominanceBlocks` / `ApplyDominance` | субстанция |
| `Blocks` / `CancelsWith` / `ClearedWhenDominanceChanges` | статус |
| огонь ×2 | `Flammable.Amplify` ↔ Oil / Burning / Lava |

Слайм по-прежнему через `is Slime`. Очередь тикает FIFO; пассивы доминанта в пульс не входят.
