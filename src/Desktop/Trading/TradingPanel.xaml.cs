using GalacticTrader.Desktop.Api;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GalacticTrader.Desktop.Trading;

public partial class TradingPanel : UserControl
{
    private readonly DesktopSession _session;
    private readonly EconomyApiClient _economyApiClient;
    private readonly MarketApiClient _marketApiClient;
    private readonly MarketIntelligenceApiClient _marketIntelligenceApiClient;
    private readonly NpcApiClient _npcApiClient;
    private readonly ObservableCollection<TradeTransactionDisplayRow> _tradeRows = [];
    private readonly List<TradeTransactionDisplayRow> _allTradeRows = [];
    private readonly ObservableCollection<TradeHeatmapDisplayRow> _heatmapRows = [];
    private readonly ObservableCollection<TradingListingSummaryDisplayRow> _listingSummaryRows = [];
    private readonly ObservableCollection<TradingListingMomentumDisplayRow> _listingMomentumRows = [];
    private readonly ObservableCollection<NpcCompetitorDisplayRow> _competitorRows = [];
    private readonly List<TradeExecutionResultApiDto> _recentTransactions = [];
    private bool _isSyncingQuantity;
    private bool _hasLoaded;

    public TradingPanel(
        DesktopSession session,
        EconomyApiClient economyApiClient,
        MarketApiClient marketApiClient,
        MarketIntelligenceApiClient marketIntelligenceApiClient,
        NpcApiClient npcApiClient)
    {
        _session = session;
        _economyApiClient = economyApiClient;
        _marketApiClient = marketApiClient;
        _marketIntelligenceApiClient = marketIntelligenceApiClient;
        _npcApiClient = npcApiClient;

        InitializeComponent();
        TransactionsGrid.ItemsSource = _tradeRows;
        HeatmapGrid.ItemsSource = _heatmapRows;
        ListingSummaryGrid.ItemsSource = _listingSummaryRows;
        ListingMomentumGrid.ItemsSource = _listingMomentumRows;
        CompetitorsGrid.ItemsSource = _competitorRows;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_hasLoaded)
        {
            return;
        }

        _hasLoaded = true;
        await RefreshTransactionsAsync();
    }

    private async void OnRefreshTransactionsClick(object sender, RoutedEventArgs e)
    {
        await RefreshTransactionsAsync();
    }

    private async void OnPreviewClick(object sender, RoutedEventArgs e)
    {
        if (!TryReadInputs(out var listingId, out var quantity, out var request))
        {
            return;
        }

        SetBusy(true);
        try
        {
            var preview = await _economyApiClient.PreviewPriceAsync(request);
            var summary = TradePreviewSummaryBuilder.Build(preview, _recentTransactions, quantity);
            var smugglingRisk = SmugglingRiskIndicatorBuilder.Build(
                request.RiskPremium,
                request.PirateActivityModifier,
                request.MonopolyModifier,
                request.DemandMultiplier);
            PreviewSummaryText.Text =
                $"Current {summary.CurrentPrice:N2} | Calculated {summary.CalculatedPrice:N2} | " +
                $"Spread {summary.Spread:+0.00;-0.00;0.00} ({summary.SpreadPercent:+0.00;-0.00;0.00}%) | " +
                $"Est. Fee {summary.EstimatedTariffAmount:N2} @ {summary.EstimatedTariffRate:P1}";
            SmugglingRiskText.Text = $"Smuggling Risk: {smugglingRisk.Band} ({smugglingRisk.Score:N1})";
            SetStatus("Preview generated from economy simulation and recent tariff history.", isError: false);
        }
        catch (Exception exception)
        {
            SetStatus(exception.Message, isError: true);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task RefreshTransactionsAsync()
    {
        SetBusy(true);
        try
        {
            var transactions = await _marketApiClient.GetTransactionsAsync(_session.PlayerId, limit: 40);
            var marketSummaryTask = _marketIntelligenceApiClient.GetSummaryAsync(limit: 6);
            var npcAgentsTask = _npcApiClient.GetAgentsAsync();
            await Task.WhenAll(marketSummaryTask, npcAgentsTask);
            _recentTransactions.Clear();
            _recentTransactions.AddRange(transactions);

            _allTradeRows.Clear();
            foreach (var transaction in transactions)
            {
                _allTradeRows.Add(new TradeTransactionDisplayRow
                {
                    ListingId = transaction.MarketListingId.ToString()[..8],
                    Action = transaction.ActionType switch
                    {
                        0 => "Buy",
                        1 => "Sell",
                        _ => $"Type {transaction.ActionType}"
                    },
                    Quantity = transaction.Quantity,
                    UnitPrice = transaction.UnitPrice,
                    TariffAmount = transaction.TariffAmount,
                    TotalPrice = transaction.TotalPrice,
                    Status = transaction.Status
                });
            }
            ApplyTradeFilters();
            var listingSummaries = TradingListingSummaryProjector.Build(transactions, maxRows: 6);
            ReplaceRows(_listingSummaryRows, listingSummaries);

            var marketSummary = marketSummaryTask.Result;
            var heatmapRows = TradeHeatmapProjector.Build(marketSummary, maxRows: 6);
            ReplaceRows(_heatmapRows, heatmapRows);

            var curveSnapshot = TradeSupplyDemandCurveBuilder.Build(transactions, maxPoints: 12);
            SupplyDemandSummaryText.Text =
                $"Demand {curveSnapshot.DemandUnits:N0} ({curveSnapshot.DemandRatio:P0}) | " +
                $"Supply {curveSnapshot.SupplyUnits:N0} ({curveSnapshot.SupplyRatio:P0})";
            SupplyDemandBars.ItemsSource = curveSnapshot.Points;

            var momentumRows = TradingListingMomentumProjector.Build(transactions, maxRows: 6);
            ReplaceRows(_listingMomentumRows, momentumRows);

            var competitorRows = NpcCompetitorPresenceProjector.Build(npcAgentsTask.Result, maxRows: 6);
            ReplaceRows(_competitorRows, competitorRows);

            SetStatus($"Loaded {_tradeRows.Count} recent trades for fee baseline.", isError: false);
        }
        catch (Exception exception)
        {
            SetStatus(exception.Message, isError: true);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private bool TryReadInputs(
        out Guid listingId,
        out long quantity,
        out PricePreviewApiRequest request)
    {
        if (!Guid.TryParse(ListingIdText.Text.Trim(), out listingId))
        {
            SetStatus("Market Listing Id must be a valid GUID.", isError: true);
            quantity = 0;
            request = new PricePreviewApiRequest();
            return false;
        }

        if (!long.TryParse(QuantityText.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out quantity) || quantity <= 0)
        {
            SetStatus("Quantity must be a positive integer.", isError: true);
            request = new PricePreviewApiRequest();
            return false;
        }

        if (!TryParseFloat(DemandText.Text, out var demandMultiplier, "Demand") ||
            !TryParseFloat(RiskText.Text, out var riskPremium, "Risk") ||
            !TryParseFloat(ScarcityText.Text, out var scarcityModifier, "Scarcity") ||
            !TryParseFloat(FactionText.Text, out var factionStabilityModifier, "Faction") ||
            !TryParseFloat(PirateText.Text, out var pirateActivityModifier, "Pirate") ||
            !TryParseFloat(MonopolyText.Text, out var monopolyModifier, "Monopoly"))
        {
            request = new PricePreviewApiRequest();
            return false;
        }

        request = new PricePreviewApiRequest
        {
            MarketListingId = listingId,
            DemandMultiplier = demandMultiplier,
            RiskPremium = riskPremium,
            ScarcityModifier = scarcityModifier,
            FactionStabilityModifier = factionStabilityModifier,
            PirateActivityModifier = pirateActivityModifier,
            MonopolyModifier = monopolyModifier
        };

        return true;
    }

    private bool TryParseFloat(string raw, out float value, string label)
    {
        if (!float.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        {
            SetStatus($"{label} must be a numeric value.", isError: true);
            return false;
        }

        return true;
    }

    private void OnActionFilterChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyTradeFilters();
    }

    private void OnListingFilterTextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyTradeFilters();
    }

    private void OnQuantityTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isSyncingQuantity || QuantitySlider is null || QuantityText is null)
        {
            return;
        }

        if (!long.TryParse(QuantityText.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var quantity) ||
            quantity <= 0)
        {
            return;
        }

        var clamped = Math.Clamp((double)quantity, QuantitySlider.Minimum, QuantitySlider.Maximum);
        _isSyncingQuantity = true;
        try
        {
            QuantitySlider.Value = clamped;
        }
        finally
        {
            _isSyncingQuantity = false;
        }
    }

    private void OnQuantitySliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isSyncingQuantity || QuantitySlider is null || QuantityText is null)
        {
            return;
        }

        _isSyncingQuantity = true;
        try
        {
            QuantityText.Text = Math.Round(QuantitySlider.Value).ToString(CultureInfo.InvariantCulture);
        }
        finally
        {
            _isSyncingQuantity = false;
        }
    }

    private void SetBusy(bool isBusy)
    {
        PreviewButton.IsEnabled = !isBusy;
        RefreshButton.IsEnabled = !isBusy;
        QuantityText.IsEnabled = !isBusy;
        QuantitySlider.IsEnabled = !isBusy;
        ActionFilterCombo.IsEnabled = !isBusy;
        ListingFilterText.IsEnabled = !isBusy;
    }

    private void SetStatus(string message, bool isError)
    {
        StatusText.Text = message;
        StatusText.Foreground = isError
            ? new SolidColorBrush(Color.FromRgb(255, 147, 147))
            : new SolidColorBrush(Color.FromRgb(157, 183, 226));
    }

    private static void ReplaceRows<TRow>(ObservableCollection<TRow> target, IReadOnlyList<TRow> source)
    {
        target.Clear();
        foreach (var row in source)
        {
            target.Add(row);
        }
    }

    private void ApplyTradeFilters()
    {
        if (ActionFilterCombo is null || ListingFilterText is null)
        {
            return;
        }

        var options = new TradingTransactionFilterOptions
        {
            Action = (ActionFilterCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All",
            ListingKeyword = ListingFilterText.Text
        };

        var filtered = TradingTransactionFilter.Apply(_allTradeRows, options);
        ReplaceRows(_tradeRows, filtered);
    }
}
