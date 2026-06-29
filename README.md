# Pit Fire Team Fika Fix

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![EFT](https://img.shields.io/badge/EFT-16%2E9-orange)](https://www.escapefromtarkov.com/)
[![SPT](https://img.shields.io/badge/SPT-4.0.13-blue)](https://sp-tarkov.com/)
[![Fika](https://img.shields.io/badge/Fika-2%2E3%2Ex-purple)](https://github.com/project-fika/Fika-Plugin)
[![BepInEx](https://img.shields.io/badge/BepInEx-5%2E4%2Ex-yellow)](https://github.com/BepInEx/BepInEx)
![Deployment](https://img.shields.io/badge/deployment-headless_host%2Chost_client-lightgrey)

Spawn/dedupe PitFireTeam на Fika host; companion guard для защиты последователей при сбросе ботов.

| | |
|---|---|
| **Разработчик** | [kabzon93region](https://github.com/kabzon93region) |
| **Версия** | 0.2.3 |
| **GitHub** | [PitFireTeamFikaFix](https://github.com/kabzon93region/PitFireTeamFikaFix) |
| **Deployment** | `(headless_host,host_client)` |
| **Тип** | client |

## Статус v0.2.3

Подтверждено тестом: после сброса ботов через BossSpawnControl удаляются обычные AI-боты, но боссы и последователи Pit Fire Team остаются.

## Возможности

- Корректный спавн отрядов PitFireTeam на хосте рейда (headless или listen-host)
- Dedupe — предотвращение дублирования отрядов при синхронизации
- Анти-призрак — очистка неактивных отрядов на клиенте
- Companion guard — защита последователей Pit Fire Team от массового `LeaveAll()` / сброса ботов

## Требования

- **PitFireTeam** — оригинальный мод (на хосте)
- **Fika** headless-coop или listen-host
- **SPT**: 4.0.x
- **BepInEx**: 5.4.x

## Установка

1. Установить архив на ПК, который хостит рейд: headless PC или listen-host.
2. Для headless-coop держать одинаковые версии PitFireTeam + PitFireTeamFikaFix на хосте и клиентах, если Fika требует совпадение gameplay-модов.
3. Для защиты при сбросе ботов использовать BossSpawnControl v1.5.4+ с включённым `ProtectPitFireCompanions`.

## Известные проблемы

- Recruit bridge v0.2.0 откатан в 0.2.1 — ломал высадку после countdown; код в репо, opt-in позже
- Поведение AI-компаньонов (видимость целей, память врагов, реакция на трупы) пока наследуется от оригинального PitFireTeam/SAIN/BigBrain и анализируется отдельно.

## Совместимость

- `(headless_host)` — отдельная машина Fika Headless.
- `(host_client)` — listen-host: рейд хостится на ПК игрока.

## Поддержать проект

Разовый донат картой РФ, СБП, ЮMoney, VK Pay:
**[DonationAlerts → kabzon93region](https://www.donationalerts.com/r/kabzon93region)**
