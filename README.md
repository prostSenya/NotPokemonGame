[readme.md](https://github.com/user-attachments/files/22012748/readme.md)
💎 NotPokemonGame  
JRPG/TD с пошаговыми боями и QTE на Unity  
Unity • C# • VContainer

---

🎮 **О проекте**  
NotPokemonGame — это смесь пошаговой JRPG и тактических элементов. Игрок управляет отрядом 4×4, сражаясь в боях с использованием системы стихий, очков действий (AP) и QTE‑событий. Архитектура построена на сервисном подходе и DI (VContainer), что обеспечивает модульность и расширяемость.

---

🎬 **Геймплей**
- ⚔️ **Пошаговый бой** — порядок ходов определяется инициативой и очками действий.
- 🔮 **Стихийная система** — взаимодействие стихий, DoT/HoT, бафы и дебафы.
- 🎭 **QTE события** — активируются при использовании способностей, не ставят бой на паузу (отдельное боевое состояние).
- 🎮 **UI** — презентеры управляют отображением, разделяя бизнес‑логику и представление.

---

🏗️ **Архитектура проекта**

📚 **Структура проекта**
```
Source/
  NotPokemonGo/
    Abilities/                 # Способности, AP‑логика, таргетинг
    Battle/                    # Пошаговый бой, стейты, обработка ходов
    Characters/                # Герои и враги, статы
    Effects/                   # Статусы (DoT/HoT), бафы/дебафы, таймеры
    QTE/                       # Типы QTE, логика, визуализация
    Services/                  # Сервисы и провайдеры (Audio, StaticData и др.)
    UI/                        # MVP/MVVM UI, презентеры и вью
    Infrastructure/            # DI-инсталлеры, загрузка сцен, стейт-машины
```

---

🏗️ **Архитектурные решения**
- **State Machine** — управление игровыми состояниями (меню, бой, QTE и др.).
- **DI через VContainer** — модульная организация зависимостей.
- **Service Layer** — изоляция общей логики: звук, загрузка данных, фабрики объектов.
- **MVP/MVVM для UI** — разделение логики и отображения.
- **Расширяемость** — добавление новых способностей, QTE и эффектов без изменения существующего кода.

---

🔧 **Примеры реализации паттернов**

🎭 AbilityApplicatorService (сервис активирует применяет армаменты или кастаменты к нужным персонажам):
```csharp
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Abilities.AbilityActions.Armaments;
using Abilities.AbilityActions.Castaments;
using Effects;
using Factories;
using Services;
using Statuses;
using Statuses.Services;
using Units;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Abilities
{
    public class AbilityApplicatorService : IAbilityApplicatorService
    {
        private readonly ICoroutineRunner _coroutineRunner;
        private readonly IArmamentViewFactory _armamentViewFactory;
        private readonly IStatusFactory _statusFactory;
        private readonly IEffectResolver _effectResolver;
        private readonly IStatusResolver _statusResolver;
        private readonly ISourceProvider _sourceProvider;
        private readonly IAbilityProvider _abilityProvider;

        public AbilityApplicatorService(
            IArmamentViewFactory armamentViewFactory,
            IStatusFactory statusFactory,
            IEffectResolver effectResolver,
            ICoroutineRunner coroutineRunner,
            IStatusResolver statusResolver,
            ISourceProvider sourceProvider,
            IAbilityProvider abilityProvider)
        {
            _armamentViewFactory = armamentViewFactory;
            _statusFactory = statusFactory;
            _effectResolver = effectResolver;
            _coroutineRunner = coroutineRunner;
            _statusResolver = statusResolver;
            _sourceProvider = sourceProvider;
            _abilityProvider = abilityProvider;
        }

        public void Apply(CastamentSetup setup, params Unit[] targets)
        {
            foreach (var target in targets)
            {
                List<EffectInfo> effects = CreateEffects(setup.EffectsSetup);
                List<Status> statuses = CreateStatuses(setup.Statuses, target);

                ApplyEffectsOnTarget(target, statuses, effects);

                if (setup.ParticleSystem != null)
                {
                    ParticleSystem effect = Object.Instantiate(setup.ParticleSystem);
                    effect.transform.position = target.transform.position;
                    effect.Play();
                }
            }
        }

        public void Apply(ArmamentSetup setup, params Unit[] targets)
        {
            foreach (var target in targets)
            {
                List<EffectInfo> effects = CreateEffects(setup.EffectsSetup);
                List<Status> statuses = CreateStatuses(setup.Statuses, target);

                if (_sourceProvider.Source == null)
                {
                    Debug.LogError("No sourceProvider has been setup");
                }

                ArmamentView armamentView =
                    _armamentViewFactory.Create(_sourceProvider.Source.abilityPos.position,
                        setup.ArmamentView, target);

                _coroutineRunner.StartCoroutine(PlayArmamentAbility(statuses, effects, armamentView, target));
            }
        }

        private IEnumerator PlayArmamentAbility(List<Status> statuses, List<EffectInfo> effects,
            ArmamentView armamentView, Unit target)
        {
            while (Vector3.Distance(target.transform.position, armamentView.transform.position) > 0.1f)
                yield return null;

            ApplyEffectsOnTarget(target, statuses, effects);
        }

        private List<EffectInfo> CreateEffects(List<EffectSetup> effects) =>
            effects.Select(s => new EffectInfo(s.Value, s.TargetType, s.Type)).ToList();

        private List<Status> CreateStatuses(IEnumerable<StatusSetup> setups, Unit target) =>
            setups.Select(s => _statusFactory.Create(s, target, _effectResolver)).ToList();

        private void ApplyEffectsOnTarget(Unit target, List<Status> statuses, List<EffectInfo> effects)
        {
            foreach (var status in statuses)
                _statusResolver.Resolve(status, target);

            foreach (var effectInfo in effects) 
                _effectResolver.ApplyEffect(target, effectInfo);
        }
    }
}
```

⚔️ Состояние боя. Выбор персонажа готового к ходу (BattleStateMachine):
```csharp
using Battlefields;
using Infrastructure.StateMachines.BattleStateMachine.Payloads;
using Infrastructure.StateMachines.States.Interfaces;
using Units;

namespace Infrastructure.StateMachines.BattleStateMachine.States
{
    public class SelectReadyUnitState : IPayloadedState<Battlefield>
    {
        private readonly IBattleStateMachine _battleStateMachine;
        private readonly IBattleUnitContainer _battleUnitContainer;

        public SelectReadyUnitState(IBattleStateMachine battleStateMachine, IBattleUnitContainer battleUnitContainer)
        {
            _battleStateMachine = battleStateMachine;
            _battleUnitContainer = battleUnitContainer;
        }
        
        public void Enter(Battlefield unitActionPayload)
        {
            foreach (Unit unit in unitActionPayload.Units) 
                _battleUnitContainer.Add(unit);

            Unit unitSource = _battleUnitContainer.Give();

            if (unitSource != null)
            {
                _battleStateMachine.Enter<UnitActionState, UnitActionPayload>(
                    new UnitActionPayload
                    (
                        unitSource,
                        unitActionPayload)
                );
                
                unitActionPayload.Units.Clear();
            }
            else
            {
                _battleStateMachine.Enter<UpdateBattleTickState, Battlefield>(unitActionPayload);
            }
        }

        public void Exit()
        {
        }
    }
}
```

---

⚔️ **Боевая система**
- Инициатива/ловкость влияет на порядок ходов.
- Каждое действие тратит очки AP.
- Поддерживаются DoT/HoT эффекты, зависящие от внутриигрового времени.

🎭 **QTE события**
- Разные типы (тайминги, точность, удержание и т.п.).
- Успех или провал влияет на силу или результат способности.

🎮 **UI**
- Реализован через презентеры (MVP/MVVM).
- Логика отделена от Unity‑вью для тестируемости.

---

🛠️ **Технологический стек**
Категория | Технология | Назначение
--------- | ---------- | ----------
🎮 Движок | Unity | Платформа разработки
💻 Язык | C# | Основной язык программирования
🔌 Dependency Injection | VContainer | Организация зависимостей
🎨 UI Pattern | MVP/MVVM | Архитектура UI
📱 Платформы | PC, WebGL | Мультиплатформенная поддержка

---

📷 **Скриншоты / Демонстрация**
Добавьте в `/docs/media` изображения и гифки:
```
/docs/media/
  battle_4x4.png
  qte_example.gif
  ui_screen.png
```
И вставьте сюда для визуального представления проекта.

