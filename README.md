# 🏗️ TenderEngine

A full-stack procurement intelligence platform that automatically ingests German public sector tenders, runs AI-powered analysis, and surfaces actionable insights through a live dashboard — helping UK SMEs identify and evaluate relevant opportunities in the DACH market.

---

## ✨ Features

- **Automated daily ingestion** — Downloads and parses German eForms XML notices from [oeffentlichevergabe.de](https://oeffentlichevergabe.de) via a scheduled GitHub Action
- **CPV filtering** — Focuses on IT & digital services tenders (CPV division 72)
- **Bilingual content** — Titles, descriptions, and buyer names are automatically translated from German to English via GPT-4o-mini
- **AI analysis** — Each tender is scored for suitability (1–10), red-flagged for fatal flaws (e.g. residency requirements, mandatory German bidding), and assessed for tech stack and certifications required
- **Persistent storage** — All tender data is stored in a PostgreSQL database (Supabase)
- **Angular dashboard** — Browse, filter, and sort tenders; view bilingual detail pages; track trends over time
- **Trend analytics** — Charts for sector spend, most active regions, average contract value by category, and repeat contracting authorities

---

## 🗂️ Project Structure

```
TenderEngine/
├── TenderScraper/          # .NET 8 console app — ingestion, parsing, AI analysis
│   ├── Infrastructure/     # EF Core DbContext and Tender entity
│   ├── Migrations/         # EF Core database migrations
│   ├── Models/             # Interfaces and shared DTOs
│   └── Services/           # Providers, translation, AI, orchestration
│
├── tender-dashboard/       # Angular 17 front-end
│   └── src/app/
│       ├── components/     # TenderList, TenderDetail, TrendsDashboard
│       ├── services/       # Supabase client service
│       ├── models/         # TypeScript Tender interface
│       └── data/           # NUTS code lookup table
│
└── .github/workflows/
    ├── schedule-scraper.yml    # Daily ingestion (03:00 UTC)
    └── deploy-dashboard.yml    # Angular → GitHub Pages on push to main
```

---

## 🛠️ Tech Stack

| Layer | Technology |
|---|---|
| Ingestion | .NET 8, C#, EF Core, CsvHelper, PdfPig |
| AI | OpenAI GPT-4o-mini |
| Database | PostgreSQL via Supabase |
| Front-end | Angular 17, Angular Material, Chart.js |
| Data source | [oeffentlichevergabe.de](https://oeffentlichevergabe.de) eForms XML |
| CI/CD | GitHub Actions |
| Hosting | GitHub Pages (dashboard) |

---

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- A [Supabase](https://supabase.com) project (free tier works)
- An [OpenAI API key](https://platform.openai.com/api-keys)

### 1. Clone the repo

```bash
git clone https://github.com/ndan-src/TenderEngine.git
cd TenderEngine
```

### 2. Configure secrets (local development)

The scraper uses [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) — secrets are **never** stored in `appsettings.json`.

```bash
cd TenderScraper
dotnet user-secrets set "ConnectionStrings:Postgres" "Host=...;Database=postgres;Username=...;Password=...;Ssl Mode=Require"
dotnet user-secrets set "OpenAI:ApiKey" "sk-..."
```

### 3. Apply database migrations

```bash
dotnet ef database update
```

### 4. Run the scraper

```bash
# Ingest yesterday's tenders (default)
dotnet run -- ingest

# Ingest a specific date
dotnet run -- ingest --date=2026-02-25

# Skip AI analysis (faster, no OpenAI cost)
dotnet run -- ingest --no-ai
```

### 5. Run the dashboard locally

```bash
cd ../tender-dashboard
npm install
npm start
```

Then open [http://localhost:4200](http://localhost:4200).

---

## ⚙️ GitHub Actions Setup

Two workflows run automatically:

| Workflow | Trigger | What it does |
|---|---|---|
| `schedule-scraper.yml` | Daily at 03:00 UTC + manual | Runs `dotnet run -- ingest` on the latest data |
| `deploy-dashboard.yml` | Push to `main` (in `tender-dashboard/`) + manual | Builds Angular app and deploys to GitHub Pages |

### Required Repository Secrets

Go to **Settings → Secrets and variables → Actions** and add:

| Secret | Value |
|---|---|
| `SUPABASE_CONN` | Your full Postgres connection string |
| `OPENAI_APIKEY` | Your OpenAI API key (`sk-...`) |

### Manual trigger

Both workflows support manual triggering via **Actions → [workflow name] → Run workflow**.

---

## 📊 Dashboard

The live dashboard is hosted at:

**[https://ndan-src.github.io/TenderEngine/](https://ndan-src.github.io/TenderEngine/)**

### Pages

| Page | URL | Description |
|---|---|---|
| Tender List | `/tenders` | Paginated, filterable table of all ingested tenders |
| Tender Detail | `/tenders/:id` | Full bilingual detail view with AI analysis, buyer info, and portal link |
| Trends | `/trends` | Charts for sector spend, regions, avg contract value, repeat buyers |

---

## 🗄️ Data Model

Key fields stored per tender:

| Field | Description |
|---|---|
| `TitleDe` / `TitleEn` | Original German title and English translation |
| `DescriptionDe` / `DescriptionEn` | Full description, both languages |
| `BuyerName` / `BuyerNameEn` | Contracting authority, both languages |
| `CpvCode` | CPV classification code |
| `ValueEuro` | Estimated contract value |
| `SubmissionDeadline` | Bid submission deadline |
| `BuyerPortalUrl` | Direct link to the tender portal |
| `SuitabilityScore` | AI score 1–10 for UK SME suitability |
| `FatalFlaws` | AI-identified showstoppers |
| `TechStack` | Technologies/frameworks detected in the tender |
| `HardCertifications` | Required certifications (e.g. BSI C5, ISO 27001) |

---

## 📄 Licence

Private repository — all rights reserved.

