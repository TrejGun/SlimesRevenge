# Удар vs лужа

Общий пайплайн:

| Путь | Урон (`StrikePower` / `Power`) + `Corrosion` | `Substance.Apply` (статус) |
| --- | --- | --- |
| **Удар** (`Combat.Attack`) | Да: `StripArmor(Corrosion)`, затем `Power` vs оставшийся DR | Да, если цель жива |
| **Лужа** (`Floor.ApplyContact`) | Нет | Да (лужа исчезает; слайм лужу не трогает) |

Статус с лужи/удара начинает тикать на **ходах жертвы** (`RefreshStatuses`), не в момент контакта.

## По субстанциям

| Субстанция | Удар | Apply (удар и лужа одинаково) |
| --- | --- | --- |
| Water | `Power` 1, `Corrosion` 0 | `Wet`; гасит `Burning`/`Corroding` через `CancelsWith` |
| Oil | `Power` 1 | `Instability` + timed `Flammable` |
| Poison | `Power` 1 | `Poisoned` (не на `Poisonous`) |
| Acid | `Power` 1 + `Corrosion` 1 | `Corroding` (пульсы: броня → HP); удар: сначала strip, потом Power vs DR |
| Blood | `Power` 1 | `Vampirism.Drink` если есть вампиризм |
| Lava | `Power` 1, ×2 при `Flammable` | `Burning` (пульсы в HP, броню не трогает) |

Броня: см. [armor.md](armor.md).
