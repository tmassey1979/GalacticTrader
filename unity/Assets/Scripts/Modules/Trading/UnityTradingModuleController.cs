using GalacticTrader.ClientSdk.Trading;
using GalacticTrader.Desktop.Api;
using GalacticTrader.Unity.Auth;
using GalacticTrader.Unity.Shell;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GalacticTrader.Unity.Modules.Trading;

public sealed class UnityTradingModuleController : UnityShellModule
{
    [SerializeField] private string apiBaseUrl = "http://localhost:8080";
    [SerializeField] private UnityAuthController? authController;
    [SerializeField] private int listingLimit = 80;
    [SerializeField] private int transactionLimit = 40;

    private HttpClient? _httpClient;
    private TradingModuleService? _tradingService;

    public TradingModuleState? LastState { get; private set; }

    public TradingPreviewResult? LastPreview { get; private set; }

    public event Action<TradingModuleState>? StateUpdated;

    public event Action<TradingPreviewResult>? PreviewUpdated;

    public event Action<TradingOperationResult>? TradeCompleted;

    public event Action<string>? OperationFailed;

    public override async Task OnActivatedAsync(CancellationToken cancellationToken)
    {
        await base.OnActivatedAsync(cancellationToken);
        await RefreshAsync(cancellationToken);
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var session = authController?.CurrentSession;
        if (session is null)
        {
            OperationFailed?.Invoke("No active session. Sign in before loading trading.");
            return;
        }

        EnsureService(session.AccessToken);
        if (_tradingService is null)
        {
            OperationFailed?.Invoke("Trading service is not initialized.");
            return;
        }

        try
        {
            LastState = await _tradingService.LoadStateAsync(
                session.PlayerId,
                listingLimit,
                transactionLimit,
                cancellationToken);
            StateUpdated?.Invoke(LastState);
        }
        catch (Exception exception)
        {
            OperationFailed?.Invoke(exception.Message);
        }
    }

    public async Task PreviewAsync(
        Guid listingId,
        long quantity,
        float demandMultiplier,
        float riskPremium,
        float scarcityModifier,
        float factionStabilityModifier,
        float pirateActivityModifier,
        float monopolyModifier,
        CancellationToken cancellationToken = default)
    {
        var session = authController?.CurrentSession;
        if (session is null)
        {
            OperationFailed?.Invoke("No active session. Sign in before previewing.");
            return;
        }

        if (_tradingService is null)
        {
            OperationFailed?.Invoke("Trading service is not initialized.");
            return;
        }

        try
        {
            var request = new PricePreviewApiRequest
            {
                MarketListingId = listingId,
                DemandMultiplier = demandMultiplier,
                RiskPremium = riskPremium,
                ScarcityModifier = scarcityModifier,
                FactionStabilityModifier = factionStabilityModifier,
                PirateActivityModifier = pirateActivityModifier,
                MonopolyModifier = monopolyModifier
            };

            LastPreview = await _tradingService.PreviewTradeAsync(
                session.PlayerId,
                request,
                quantity,
                transactionLimit,
                cancellationToken);
            PreviewUpdated?.Invoke(LastPreview);
        }
        catch (Exception exception)
        {
            OperationFailed?.Invoke(exception.Message);
        }
    }

    public async Task ExecuteTradeAsync(
        Guid shipId,
        Guid listingId,
        TradingTradeAction action,
        long quantity,
        decimal? expectedUnitPrice = null,
        CancellationToken cancellationToken = default)
    {
        var session = authController?.CurrentSession;
        if (session is null)
        {
            OperationFailed?.Invoke("No active session. Sign in before executing trades.");
            return;
        }

        if (_tradingService is null)
        {
            OperationFailed?.Invoke("Trading service is not initialized.");
            return;
        }

        var request = new ExecuteTradeApiRequest
        {
            PlayerId = session.PlayerId,
            ShipId = shipId,
            MarketListingId = listingId,
            ActionType = (int)action,
            Quantity = quantity,
            ExpectedUnitPrice = expectedUnitPrice
        };

        var result = await _tradingService.ExecuteTradeAsync(request, cancellationToken);
        if (!result.Succeeded)
        {
            OperationFailed?.Invoke(result.Message);
            return;
        }

        TradeCompleted?.Invoke(result);
        await RefreshAsync(cancellationToken);
    }

    private void OnDestroy()
    {
        _httpClient?.Dispose();
        _httpClient = null;
    }

    private void EnsureService(string accessToken)
    {
        if (_tradingService is not null && _httpClient is not null)
        {
            return;
        }

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(apiBaseUrl.TrimEnd('/'))
        };

        var marketApiClient = new MarketApiClient(_httpClient);
        marketApiClient.SetBearerToken(accessToken);
        var economyApiClient = new EconomyApiClient(_httpClient);
        economyApiClient.SetBearerToken(accessToken);

        var dataSource = new TradingDataSource
        {
            LoadListingsAsync = (limit, cancellationToken) => marketApiClient.GetListingsAsync(limit, cancellationToken: cancellationToken),
            LoadTransactionsAsync = (playerId, limit, cancellationToken) => marketApiClient.GetTransactionsAsync(playerId, limit, cancellationToken),
            PreviewPriceAsync = (request, cancellationToken) => economyApiClient.PreviewPriceAsync(request, cancellationToken),
            ExecuteTradeAsync = (request, cancellationToken) => marketApiClient.ExecuteTradeAsync(request, cancellationToken)
        };

        _tradingService = new TradingModuleService(dataSource);
    }
}
