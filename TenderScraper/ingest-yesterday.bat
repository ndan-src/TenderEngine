@echo off
REM Quick ingestion script for TenderScraper
REM Ingests previous day's German procurement data without AI analysis

echo.
echo ========================================
echo TenderScraper - Quick Ingestion
echo ========================================
echo.

if "%1"=="" (
    echo Ingesting YESTERDAY'S data...
    dotnet run -- ingest --no-ai
) else (
    echo Ingesting data for: %1
    dotnet run -- ingest --no-ai --date=%1
)

echo.
echo Done!
pause

