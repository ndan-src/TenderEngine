using TenderScraper.Models;

namespace TenderScraper.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public class IngestionOrchestrator
{
    private readonly IEnumerable<ITenderProvider> _providers;
    private readonly TenderFilterService _filterService;
    private readonly TenderUrlExtractor _urlExtractor;
    private readonly TenderDocumentDownloader _downloader;
    private readonly ILogger<IngestionOrchestrator> _logger;
    private readonly DeepAnalysisService _deepAnalysisService;

    public IngestionOrchestrator(
        IEnumerable<ITenderProvider> providers,
        TenderFilterService filterService,
        TenderUrlExtractor urlExtractor,
        TenderDocumentDownloader downloader,
        ILogger<IngestionOrchestrator> logger, DeepAnalysisService deepAnalysisService)
    {
        _providers = providers;
        _filterService = filterService;
        _urlExtractor = urlExtractor;
        _downloader = downloader;
        _logger = logger;
        _deepAnalysisService = deepAnalysisService;
    }

    public async Task RunDailyIngestion(DateTime targetDate)
    {
        _logger.LogInformation("Starting daily ingestion for {Date}", targetDate.ToShortDateString());

        foreach (var provider in _providers)
        {
            try
            {
                _logger.LogInformation("Processing provider: {Provider}", provider.ProviderName);
                
                // 1. Fetch normalized raw tenders from the provider (e.g., German CSV ZIP)
                var rawTenders = await provider.FetchLatestNoticesAsync(targetDate);

                foreach (var tender in rawTenders)
                {
                    // 2. Filter for Division 72 / High Value keywords
                    if (_filterService.IsHighValue(tender))
                    {
                        _logger.LogInformation("High-value tender found: {Title} ({Id})", tender.Title, tender.OCID);
                        
                        await ProcessHighValueTender(tender);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing provider {Provider}", provider.ProviderName);
            }
        }
    }

    public async Task ProcessHighValueTender(RawTender tender)
    {
        // 3. Extract the Portal URL from the description
        string portalUrl = _urlExtractor.GetTenderPortalUrl(tender.Description);
        
        if (string.IsNullOrEmpty(portalUrl))
        {
            _logger.LogWarning("No portal URL found for tender {Id}", tender.OCID);
            // We still save the metadata even if the doc download link isn't obvious
            await StoreTenderMetadataOnly(tender);
            return;
        }

        try
        {
            // 4. Download the actual tender documents (Technical Specs) via Raw HTTP
            _logger.LogInformation("Downloading documents from {Url}", portalUrl);
            byte[] docZip = await _downloader.DownloadTenderZipAsync(portalUrl);

            if (docZip != null && docZip.Length > 0)
            {
                // 5. Hand off to the "Deep Analysis" pipeline
                // This would unzip, run PdfPig, and call the AI Summarizer
                await ExecuteDeepAnalysisPipeline(tender, docZip);
            }
            else
            {
                _logger.LogWarning("Could not retrieve ZIP from portal for {Id}", tender.OCID);
                await StoreTenderMetadataOnly(tender);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download/process documents for {Id}", tender.OCID);
        }
    }

    private async Task ExecuteDeepAnalysisPipeline(RawTender tender, byte[] zipData)
    {
        // 1. Save the raw ZIP to Azure Blob Storage for the Librarian's audit trail
       // await _blobStorage.UploadAsync($"{tender.OCID}.zip", zipData);

        // 2. Run the Deep Analysis (PdfPig + AI)
        await _deepAnalysisService.ProcessTenderDocumentsAsync(tender, zipData);
    
        _logger.LogInformation("Deep analysis complete for {Id}", tender.OCID);
    }

    private async Task StoreTenderMetadataOnly(RawTender tender)
    {
        // Fallback: Just save what we have from the CSVs
        _logger.LogInformation("Storing metadata only for {Id}", tender.OCID);
        
        // TODO: Store in SQL/OpenSearch
    }
}