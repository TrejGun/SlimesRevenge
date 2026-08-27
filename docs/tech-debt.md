# Техдолг (ревью перед коммитом)

Снимок на `dev` (ahead 1 от origin + большой dirty tree). Ниже — долг по геймплею/инфре, который стоит явно принять или вырезать до коммита.

## Блокеры / лучше не мешать в один коммит

1. **Огромный untracked `Assets/AstarPathfindingProject/`** — вендорная библиотека. Коммитить отдельно (или submodule), не вместе с механикой статусов/брони.
2. **Art churn** (Animals/Slimes, удаление Tiles/старого Slime.png, Title.png) — отдельный art-коммит; уже был прецедент с ElvGames в `8d7177cb`.
3. **A\* teardown**: `GridPath` обязан `DestroyImmediate` temp host и сбрасывать `AstarPath.active`. Отложенный `Destroy` в Play Mode оставлял дохлый синглтон → chase/flee замирали после первого pathfind (баг дуэли: собака укусила и перестала догонять). EditMode раньше маскировал это `DestroyImmediate`.

## Геймплей — принятый долг (можно жить, но зафиксировать)

4. ~~`Regeneration` на каждом `RefreshVolumeStatuses`~~ — **исправлено**: рост на `OnPulse`; при том же доминанте traits не пересоздаются (FIFO слоты).
5. ~~**`Corrosion > 0` ⇒ HP pierces armor DR**~~ — **исправлено**: сначала `StripArmor(Corrosion)`, затем `Power` против оставшегося DR (без pierce).
6. ~~**Двойной путь гашения огня**~~ — **исправлено**: `Water.OnApply` только `AddStatus(Wet)`; гашение через `Burning`/`Corroding`.CancelsWith(Wet) и `Wet.CancelsWith(Burning)`.
7. **Много `is Slime`** в Combat / Floor / TurnManager / Creature — осознанно оставлены (явнее флагов `UsesVolumeHealth` и т.п.).
8. ~~**`Creature.OnStruck` мёртв**~~ — **удалён**; retaliation остаётся через snapshot в `Combat.Attack`.
9. ~~**`delayPulse` / `Extend`**~~ — **удалены** как неиспользуемый API.
10. ~~**`TickingHarm.Damage` vs `Substance.Power`**~~ — **исправлено**: тик переименован в `PulsePower`.
11. **Docs vs код**: `docs/` в целом свежий (`armor`, `puddles`, `slime-dominance`); следить, чтобы CI/README не ссылались на удалённый `shields.md`.

## Инфра / структура

12. **Сцены `Game.unity` / `Main.unity` + ProjectSettings** в том же dirty tree — риск «случайного» коммита локальных editor settings вместе с логикой.
13. **Тестовые хелперы Spawn** размножены по фикстурам (разный `SetArmor` / FillStarting) — дрейф при новых животных.
14. **Полный EditMode suite** после всех изменений не гонялся end-to-end в этой сессии; гонялись точечные фильтры. Перед merge — полный прогон + фикс A\* teardown.

## Рекомендация по коммиту

| Можно коммитить сейчас (отдельными кусками) | Подождать |
| --- | --- |
| Механика: volume dominance, armor/corrosion/power, scorpion, regeneration, floor priorities, связанные тесты + `docs/` | Вместе с сырым A\* vendor + art + scenes «как получилось» |
| | Пока A\* тесты flaky без teardown |

**Вердикт для ревьюера:** геймплейный слой готов к коммиту **как отдельные логические коммиты**, с явным принятием пункта 7 (4–6, 8–10 уже закрыты). Не коммитить «весь working tree одним махом». Перед push/PR — починить изоляцию A\* тестов (п.3) или временно `Ignore` flaky, если осознанно.
