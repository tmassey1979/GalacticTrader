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
    private readonly ObservableCollection<TradeTransactionDisplayRow> _tradeRows = [];
    private readonly List<TradeExecutionResultApiDto> _recentTransactions = [];
    private bool _hasLoaded;

    public TradingPanel(
        DesktopSession session,
        EconomyApiClient economyApiClient,
        MarketApiClient marketApiClient)
    {
        _session = session;
        _economyApiClient = economyApiClient;
        _marketApiClient = marketApiClient;

        InitializeComponent();
        TransactionsGrid.ItemsSource = _tradeRows;
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
            PreviewSummaryText.Text =
                $"Current {summary.CurrentPrice:N2} | Calculated {summary.CalculatedPrice:N2} | " +
                $"Spread {summary.Spread:+0.00;-0.00;0.00} ({summary.SpreadPercent:+0.00;-0.00;0.00}%) | " +
                $"Est. Fee {summary.EstimatedTariffAmount:N2} @ {summary.EstimatedTariffRate:P1}";
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
            _recentTransactions.Clear();
            _recentTransactions.AddRange(transactions);

            _tradeRows.Clear();
            foreach (var transaction in transactions)
            {
                _tradeRows.Add(new TradeTransactionDisplayRow
                {
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

    private void SetBusy(bool isBusy)
    {
        PreviewButton.IsEnabled = !isBusy;
        RefreshButton.IsEnabled = !isBusy;
    }

    private void SetStatus(string message, bool isError)
    {
        StatusText.Text = message;
        StatusText.Foreground = isError
            ? new SolidColorBrush(Color.FromRgb(255, 147, 147))
            : new SolidColorBrush(Color.FromRgb(157, 183, 226));
    }
}
