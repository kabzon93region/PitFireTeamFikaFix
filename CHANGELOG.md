# Changelog

## v0.2.1 (2026-06-12)

- **Rollback:** отключены патчи recruit bridge v0.2.0 (`GesturesQuickPanel`, `Player.Say`, `PitFireRecruitRequestPacket`)
- **Причина:** после v0.2.0 у части клиентов не срабатывала высадка после countdown (рейд 7KI448 и др.)
- Поведение spawn/dedupe как в **v0.1.11**; код recruit оставлен в репозитории для будущего opt-in

## v0.2.0 (2026-06-12)

- **P1 recruit bridge:** клиент на headless-хосте видит жест Cooperation у observed-ботов и может рекрутить их в рейде
- UI: postfix на `GesturesQuickPanel` для `ObservedPlayer` без `BotOwner`
- RPC `PitFireRecruitRequestPacket` → хост вызывает `TryAskFollowMeRequest` на реальном `BotOwner`
- FollowMe: хост прокидывает `BotEventHandler.SayPhrase` для ambient-рекрута

## v0.1.11 (2026-06-22)

- **Fix:** `ClientQueueProfileDedupePatch` — отдельные трекеры для `SendCharacter` и `QueueProfile`
- **Причина:** Fika штатно вызывает оба метода для одного бота; общий `TryTrack` блокировал `QueueProfile` после первого `SendCharacter` → **все AI боты не спавнились на клиенте** (`Blocked duplicate QueueProfile`)
- Анти-призрак сохранён: повторный `SendCharacter` или повторный `QueueProfile` для того же profileId по-прежнему блокируется

## v0.1.10 (2026-06-21)

- **Fix:** убран `HostSendCharacterDedupePatch` — блокировал **второй** `SendCharacter` при повторном `CreateBot` PitFire, из‑за чего comp хоста **не спавнился** (`Blocked duplicate SendCharacter on host`)
- Анти-призрак на клиенте остаётся: `ClientQueueProfileDedupePatch` + `ClientObservedPlayerDedupePatch` + `ClientGhostReconcile`
- Хост: только `CreateBotDedupePatch` (как v0.1.8)

## v0.1.9 (2026-06-21)

- **Fix:** `HostSendCharacterDedupePatch` — хост не шлёт второй `SendCharacter` для того же AI ProfileId (источник призрака на клиенте)
- **Fix:** `ClientQueueProfileDedupePatch` — клиент игнорирует повторный `SendCharacter` / `QueueProfile`
- **Fix:** улучшен `ClientObservedPlayerDedupePatch` — sweep дубликатов по ProfileId, отложенная очистка (`ClientGhostReconcile`)
- **Fix:** `CoopRaidCleanupPatch` — сброс трекеров при `CoopGame.Stop`
- Цель: призрак comp хоста **только на хосте допустим**, на клиенте — убрать

## v0.1.5 (2026-06-12)

- **Fix:** `ClientSpawnGuardPatch` — на Fika-клиенте блокируются `SpawnGroupBots`, `SpawnBossFollower`, `CreateFollowerProfiles`, `PreFetchBossProfiles`, `ActivateBotAtPosition` (у PitFire `HasFika()` с неверным типом `Fika.Core.Coop.*`)
- Цель: убрать локальный «призрак» сопартийца хоста на клиенте

## v0.1.4 (2026-06-21)

- **Fix:** transpiler на **async state machine MoveNext** (Bots.Add был вне обёртки CreateBot — v0.1.3 не перехватывал)
- `TryAddBot` вместо void SafeBotsAdd — без падения на duplicate key
- Лог: `Patched CreateBot state machine MoveNext`

## v0.1.3 (2026-06-21)

- **Fix:** transpiler на `Bots.Add` → `SafeBotsAdd` (prefix не останавливал параллельные async-вызовы)
- **Fix:** блокирующий `Wait()` + повторная проверка `Bots` после ожидания
- **Fix:** finalizer разворачивает `AggregateException` (DebugPlus ловил Fatal)
- **Fix:** уничтожение дублирующего PlayerBody при dedupe

## v0.1.2 (2026-06-21)

- **Fix:** CreateBot dedupe async-safe (SemaphoreSlim до завершения Task; finalizer на async отпускал guard рано)
- **Fix:** swallow `same key already added` на Bots.Add как последний барьер
- **Release:** DLL в `BepInEx/plugins/PitFireTeamFikaFix/`; при обновлении удалить старый `plugins/PitFireTeamFikaFix.dll` в корне plugins
- **Важно:** ставить на **хост и всех клиентов** — в логе клиента должно быть `v0.1.2`, не `idle`

## v0.1.1 (2026-06-21)

- **Fix:** `IsFikaLoaded()` использовал устаревший namespace `Fika.Core.Coop.*` → патчи не применялись (`Fika not loaded — idle`)
- Теперь детект через `Fika.Core.Main.Utils.FikaBackendUtils` (AccessTools)
- Патчи применяются всегда; без Fika — только предупреждение в логе

## v0.1.0 (2026-06-18)

- P0: блок `SpawnFollowers` и `SpawnDebugFollower` на Fika-клиенте
- P2: dedupe `HostGameController.CreateBot` при повторном ProfileId + mutex in-flight по profileId
- Логи: `[PITFIRE_FIKA]`
