@echo off
REM Chạy Saigon Audio Guide trên máy (Windows). Cần: .NET SDK 10 và MySQL của XAMPP đang Start.
chcp 65001 >nul
echo Dang khoi dong... (neu loi ket noi, hay bam Start MySQL trong XAMPP Control Panel)
dotnet run --project src\AudioGuide.API --urls http://localhost:5000
pause
