# Pit Fire Team Fika Fix

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![EFT](https://img.shields.io/badge/EFT-16%2E9-orange)](https://www.escapefromtarkov.com/)
[![SPT](https://img.shields.io/badge/SPT-4.0.13-blue)](https://sp-tarkov.com/)
[![Fika](https://img.shields.io/badge/Fika-2%2E3%2Ex-purple)](https://github.com/project-fika/Fika-Plugin)
[![BepInEx](https://img.shields.io/badge/BepInEx-5%2E4%2Ex-yellow)](https://github.com/BepInEx/BepInEx)
![Deployment](https://img.shields.io/badge/deployment-headless_host-lightgrey)

﻿# Pit Fire Team Fika Fix

| | |
|---|---|
| **Разработчик** | [kabzon93region](https://github.com/kabzon93region) |
| **Версия** | 0.2.1 |
| **GitHub** | [PitFireTeamFikaFix](https://github.com/kabzon93region/PitFireTeamFikaFix) |
| **Deployment** | `(headless_host)` |
| **Тип** | client |

## Возможности

- Корректный спавн отрядов PitFireTeam на headless-хосте
- Dedupe — предотвращение дублирования отрядов при синхронизации
- Анти-призрак — очистка неактивных отрядов на клиенте

## Требования

- **PitFireTeam** — оригинальный мод (на хосте)
- **Fika** headless-coop
- **SPT**: 4.0.x
- **BepInEx**: 5.4.x

## Установка

1. Скопировать PitFireTeamFikaFix.dll в BepInEx/plugins/ на headless-хосте
2. Клиентам — по README (часть патчей client-side)

## Известные проблемы

- Recruit bridge v0.2.0 откатан в 0.2.1 — ломал высадку после countdown; код в репо, opt-in позже

## Совместимость

- headless_host — ставится на headless-хост; клиентам часть патчей тоже нужна

## Поддержать проект

Разовый донат картой РФ, СБП, ЮMoney, VK Pay:
**[DonationAlerts → kabzon93region](https://www.donationalerts.com/r/kabzon93region)**
