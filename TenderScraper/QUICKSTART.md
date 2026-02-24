# Quick Start - Command Line Ingestion

## Run Right Now (Ingest February 16, 2026 - Yesterday)

Open PowerShell or Command Prompt in the TenderScraper folder and run:

```powershell
dotnet run -- ingest --no-ai
```

## What This Does

1. ✅ Fetches all IT tenders (CPV 72*) from February 16, 2026
2. ✅ Applies your filters from appsettings.json (Cyber, Cloud, Zero-Trust keywords)
3. ✅ Shows estimated values and procedure types
4. ✅ Displays a formatted list of high-value tenders
5. ❌ **SKIPS AI analysis** (fast, no OpenAI API costs)

## Expected Output

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

## Alternative Methods

### Method 1: Using the Batch File (Windows)
```cmd
ingest-yesterday.bat
```

### Method 2: Using the Shell Script (Linux/Mac)
```bash
chmod +x ingest-yesterday.sh
./ingest-yesterday.sh
```

### Method 3: Specific Date
```powershell
dotnet run -- ingest --no-ai --date=2026-02-10
```

## Troubleshooting

### "No high-value tenders found"
- Sunday (Feb 16) might have no new tenders published
- Try Friday: `dotnet run -- ingest --no-ai --date=2026-02-14`
- Or Thursday: `dotnet run -- ingest --no-ai --date=2026-02-13`

### SDK Error (MSB4236)
Your .NET SDK installation has an issue. This is NOT a code problem. To fix:

1. Check your SDK version:
   ```powershell
   dotnet --info
   ```

2. If corrupted, reinstall .NET 8.0 SDK:
   - Download: https://dotnet.microsoft.com/download/dotnet/8.0

3. Or add this to TenderScraper.csproj:
   ```xml
   <PropertyGroup>
     <TargetFramework>net8.0</TargetFramework>
     <UseWorkloadAutoImportProps>false</UseWorkloadAutoImportProps>
   </PropertyGroup>
   ```

### "builder.Services.AddHttpClient" not found
This is already installed! The package `Microsoft.Extensions.Http` (version 10.0.3) is in your project.

Check TenderScraper.csproj - you should see:
```xml
<PackageReference Include="Microsoft.Extensions.Http" Version="10.0.3" />
```

If missing, restore packages:
```powershell
dotnet restore
```

## What Data You Get

Each tender shows:

| Field | Description | Example |
|-------|-------------|---------|
| **OCID** | Unique tender identifier | `68a4106a-a49b-4f80-91c7-646546349094` |
| **Lot** | Lot/package number | `LOT-0000` |
| **Procedure** | Procurement type | `open`, `neg-w-call`, `restricted` |
| **Est. Value** | Contract value in EUR | `€735,106.61` |
| **Description** | Tender description (first 150 chars) | Truncated for readability |

## Current Settings (appsettings.json)

Your filters are currently set to:

**High-Value Keywords** (must contain at least one):
- Cyber
- Sicherheit
- Cloud
- Zero-Trust
- Infrastructure

**Exclusion Keywords** (auto-reject if contains):
- Hardware
- Cleaning
- Catering

**Minimum Value**: €50,000

## Next Steps

1. **Run the command** to see yesterday's tenders
2. **Adjust filters** in appsettings.json if needed
3. **Export to CSV** (future feature)
4. **Enable AI analysis** when you're ready (requires OpenAI API key)

---

**Ready to run?** Just execute:
```powershell
dotnet run -- ingest --no-ai
```

