@echo off
title ChickenDist WhatsApp Bot
echo Starting WhatsApp Bot...
cd /d "%~dp0bot"
if not exist index.js (
    cd /d "D:\قطع غيار وتوزيع\قطع غيار وتوزيع\ChickenDistUpdates-main\ChickenDistUpdates-main\bot"
)
node index.js
pause
