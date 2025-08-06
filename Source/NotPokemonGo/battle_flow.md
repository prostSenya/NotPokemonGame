Battle Flow

1. Инициация боя

Игрок запускает битву: загружает сцену BattleScene, инициализируются Party, EnemyParty, UI и очередь.

2. Состояние ожидания ходов

BattleLoopState проверяет ReadyGauge у всех юнитов.

Когда кто-то заполняется — BattleFlowController.PickReadyUnit.

3. Выбор действия

Переход в UnitSelectState.

UI предлагает выбор: способность, цель.

4. Выполнение способности

AbilityPerformer.Execute(ability, context).

Если способность требует QTE → QteService.Execute(type, context) → QteBattleState.

5. Результат QTE

QTE успешен → наносит критический урон, иначе обычный успех/неудача.

AbilityEffectApplier.Apply(result, source, target).

6. Обновление стейта после хода

Перезапуск таймеров CD, статус‑эффектов (StatusEffect.Tick()).

Проверка на конец боя (PartyEmpty?).

7. Завершение и возврат

Победа или поражение → BattleEndState.

Возврат в OverworldScene или переход на следующую сцену.
