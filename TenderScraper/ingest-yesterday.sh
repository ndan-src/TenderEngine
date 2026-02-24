#!/bin/bash
# Quick ingestion script for TenderScraper (Linux/Mac)
# Ingests previous day's German procurement data without AI analysis

echo ""
echo "========================================"
echo "TenderScraper - Quick Ingestion"
echo "========================================"
echo ""

if [ -z "$1" ]; then
    echo "Ingesting YESTERDAY'S data..."
    dotnet run -- ingest --no-ai
else
    echo "Ingesting data for: $1"
    dotnet run -- ingest --no-ai --date="$1"
fi

echo ""
echo "Done!"
