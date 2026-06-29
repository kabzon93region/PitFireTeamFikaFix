# Publish to GitHub — Pit Fire Team Fika Fix

**Статус:** `ready`  
**GitHub:** Release + zip  
**Версия:** `0.2.2`  
**Deployment:** `(headless_host)`

## 1. Подготовка (уже сделано этим скриптом)

Папка: `github-repos/PitFireTeamFikaFix/`

## 2. Создать репозиторий и запушить

```powershell
cd github-repos/PitFireTeamFikaFix
git init
git add .
git commit -m "Source backup Pit Fire Team Fika Fix v0.2.2"
git branch -M main
git remote add origin https://github.com/kabzon93region/PitFireTeamFikaFix.git
git push -u origin main
```

Или автоматически:

```powershell
python CURSORAIMODING/tools/publish/publish_github_release.py PitFireTeamFikaFix --create-repo
```

## 3. GitHub Release

Прикрепить zip (только игровые файлы, без INSTALL.md):

`\\Servant\data\Games\EscapeFromTarkov4\CURSORAIMODING\releases\PitFireTeamFikaFix_(headless_host)_v0.2.2_2026-06-29.zip`

```powershell
gh release create v0.2.2 "\\Servant\data\Games\EscapeFromTarkov4\CURSORAIMODING\releases\PitFireTeamFikaFix_(headless_host)_v0.2.2_2026-06-29.zip" ^
  --title "Pit Fire Team Fika Fix v0.2.2" ^
  --notes-file CHANGELOG.md
```

## Описание репозитория (suggested)

Spawn/dedupe PitFireTeam на Fika headless; анти-призрак на клиенте.

SPT 4.0 + Fika 2.3 headless stack. Deployment: `(headless_host)`.
