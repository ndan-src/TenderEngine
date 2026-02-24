# TenderScraper CLI Usage Guide

## Overview
TenderScraper can run in two modes:
1. **Background Service Mode** (default): Runs continuously as a worker service
2. **CLI Mode**: Runs once for a specific date and exits

## CLI Mode - Quick Ingestion

### Basic Usage

**Ingest yesterday's data (default):**
```powershell
dotnet run -- ingest --no-ai
```

**Ingest a specific date:**
```powershell
dotnet run -- ingest --no-ai --date=2026-02-16
```

**With AI analysis enabled (requires OpenAI API key):**
```powershell
dotnet run -- ingest --date=2026-02-16
```

## Command-Line Arguments

| Argument | Description | Example |
|----------|-------------|---------|
| `ingest` | Run ingestion mode (required) | `ingest` |
| `--no-ai` | Disable AI analysis (faster) | `--no-ai` |
| `--date=YYYY-MM-DD` | Specific date to process | `--date=2026-02-16` |

## Output Format

The CLI will display:

```
╔════════════════════════════════════════════════════════╗
║     TenderEngine - German Procurement Ingestion       ║
╚════════════════════════════════════════════════════════╝

Target Date: 2026-02-16 (Sunday)
AI Analysis: DISABLED

📡 Fetching from: Germany_Bekanntmachungsservice...
   ✓ Retrieved 234 IT tenders (CPV 72*)
   ✓ 12 high-value tenders after filtering

═══════════════════════════════════════════════════════
SUMMARY: 234 total tenders, 12 high-value
═══════════════════════════════════════════════════════

HIGH-VALUE TENDERS:

[1] Cloud-Infrastruktur für öffentliche Verwaltung
    OCID:           68a4106a-a49b-4f80-91c7-646546349094
    Lot:            LOT-0000
    Procedure:      open
    Est. Value:     €735,106.61
    Description:    Modernisierung der IT-Infrastruktur...

[2] Cybersecurity-Dienstleistungen
    OCID:           8d3df043-f8e4-432d-a73c-07d51aa73c32
    Lot:            LOT-0001
    Procedure:      neg-w-call
    Est. Value:     €450,000.00
    Description:    Implementierung von Zero-Trust...

✓ Ingestion complete!
```

## Filtering Configuration

Edit `appsettings.json` to customize filtering:

```json
{
  "TenderFilter": {
    "HighValueKeywords": [
      "Cyber", 
      "Sicherheit", 
      "Cloud", 
      "Zero-Trust", 
      "Infrastructure"
    ],
    "ExclusionKeywords": [
      "Hardware", 
      "Cleaning", 
      "Catering"
    ],
    "MinEstimatedValue": 50000
  }
}
```

## Examples

### 1. Quick Daily Check (Previous Day)
```powershell
# Run at 9am each morning to check yesterday's tenders
dotnet run -- ingest --no-ai
```

### 2. Historical Analysis
```powershell
# Check specific dates
dotnet run -- ingest --no-ai --date=2026-02-10
dotnet run -- ingest --no-ai --date=2026-02-11
dotnet run -- ingest --no-ai --date=2026-02-12
```

### 3. Batch Processing Script
Create a PowerShell script to process a date range:

```powershell
# process-range.ps1
$startDate = Get-Date "2026-02-01"
$endDate = Get-Date "2026-02-15"

for ($date = $startDate; $date -le $endDate; $date = $date.AddDays(1)) {
    $dateStr = $date.ToString("yyyy-MM-dd")
    Write-Host "Processing $dateStr..."
    dotnet run -- ingest --no-ai --date=$dateStr
}
```

### 4. With AI Analysis (Slow, Costs Money)
```powershell
# Enable AI analysis for deeper insights
# Requires OpenAI API key in appsettings.json
dotnet run -- ingest --date=2026-02-16
```

## Background Service Mode

To run as a continuous background service (original behavior):

```powershell
# Just run without arguments
dotnet run
```

This will start the `TenderIngestionWorker` which runs on a schedule.

## Publishing for Production

### 1. Build Executable
```powershell
# Self-contained Windows executable
dotnet publish -c Release -r win-x64 --self-contained

# The executable will be at:
# bin\Release\net8.0\win-x64\publish\TenderScraper.exe
```

### 2. Run Published Version
```powershell
cd bin\Release\net8.0\win-x64\publish
.\TenderScraper.exe ingest --no-ai --date=2026-02-16
```

### 3. Create Scheduled Task
```powershell
# Windows Task Scheduler - Run daily at 8am
$action = New-ScheduledTaskAction -Execute "C:\path\to\TenderScraper.exe" -Argument "ingest --no-ai"
$trigger = New-ScheduledTaskTrigger -Daily -At 8am
Register-ScheduledTask -Action $action -Trigger $trigger -TaskName "TenderIngestion" -Description "Daily tender ingestion"
```

## Troubleshooting

### No Tenders Found
- Check that the date has published data (tenders are usually published on weekdays)
- Verify the German procurement API is accessible
- Try a different date range

### All Tenders Filtered Out
- Review your filter keywords in `appsettings.json`
- Keywords might be too restrictive
- Try running without `--no-ai` to see all tenders first

### HTTP Errors
- Check internet connectivity
- Verify the API endpoint is still `https://oeffentlichevergabe.de/api/notice-exports`
- The API might be rate-limited; add delays between requests

### SDK Error (MSB4236)
- This is a .NET SDK installation issue, not code
- Reinstall .NET 8.0 SDK from https://dotnet.microsoft.com/download/dotnet/8.0
- Or run: `dotnet --info` to check installed SDKs

## Performance

| Mode | Speed | Cost | Use Case |
|------|-------|------|----------|
| `--no-ai` | Fast (5-10 sec) | Free | Daily quick checks |
| AI Enabled | Slow (2-5 min) | $0.01-0.03/tender | Deep analysis |

## Next Steps

1. **Export to CSV**: Add `--output=tenders.csv` support
2. **Database Storage**: Persist results to PostgreSQL
3. **Email Alerts**: Send notifications for high-value tenders
4. **OpenSearch Indexing**: Enable full-text search

---

**Current Date**: February 17, 2026  
**Default Behavior**: Ingests February 16, 2026 (yesterday)

