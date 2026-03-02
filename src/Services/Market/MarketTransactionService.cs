namespace GalacticTrader.Services.Market;

using GalacticTrader.Data;
using GalacticTrader.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public sealed class MarketTransactionService : IMarketTransactionService
{
    private readonly GalacticTraderDbContext _dbContext;
    private readonly ILogger<MarketTransactionService> _logger;

    public MarketTransactionService(GalacticTraderDbContext dbContext, ILogger<MarketTransactionService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<TradeExecutionResult> ExecuteTradeAsync(
        ExecuteTradeRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateTradeRequest(request);

        var now = DateTime.UtcNow;

        var player = await _dbContext.Players.FirstOrDefaultAsync(player => player.Id == request.PlayerId, cancellationToken)
            ?? throw new InvalidOperationException("Player not found.");
        var ship = await _dbContext.Ships
            .FirstOrDefaultAsync(targetShip => targetShip.Id == request.ShipId && targetShip.PlayerId == request.PlayerId, cancellationToken)
            ?? throw new InvalidOperationException("Ship not found or not owned by player.");
        var listing = await _dbContext.MarketListings
            .Include(marketListing => marketListing.Market)
            .ThenInclude(market => market.Sector)
            .ThenInclude(sector => sector.ControlledByFaction)
            .Include(marketListing => marketListing.Commodity)
            .FirstOrDefaultAsync(marketListing => marketListing.Id == request.MarketListingId, cancellationToken)
            ?? throw new InvalidOperationException("Market listing not found.");

        await ValidateAntiExploitAsync(
            request.PlayerId,
            listing.CommodityId,
            request.Quantity,
            now,
            cancellationToken);

        ValidateExpectedPrice(request.ExpectedUnitPrice, listing.CurrentPrice);

        var quantity = request.Quantity;
        var unitPrice = listing.CurrentPrice;
        var subtotal = decimal.Round(unitPrice * quantity, 2);
        var tariffRate = CalculateTariffRate(listing);
        var taxRate = listing.Market.Sector?.ControlledByFaction?.TaxRate ?? 0.02m;
        var tariffAmount = decimal.Round(subtotal * tariffRate, 2);
        var taxAmount = decimal.Round(subtotal * taxRate, 2);
        var feeAmount = decimal.Round(subtotal * 0.005m, 2);
        var totalAmount = request.ActionType == TradeActionType.Buy
            ? subtotal + tariffAmount + taxAmount + feeAmount
            : subtotal - (tariffAmount + taxAmount + feeAmount);

        if (request.ActionType == TradeActionType.Buy)
        {
            await ExecuteBuyAsync(player, ship, listing, quantity, totalAmount, cancellationToken);
        }
        else
        {
            await ExecuteSellAsync(player, ship, listing, quantity, totalAmount, cancellationToken);
        }

        var transaction = new TradeTransaction
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            SellerId = request.ActionType == TradeActionType.Sell ? player.Id : Guid.Empty,
            CommodityId = listing.CommodityId,
            FromMarketId = listing.MarketId,
            ToMarketId = listing.MarketId,
            Quantity = quantity,
            PricePerUnit = unitPrice,
            TotalPrice = subtotal,
            Tariff = tariffAmount,
            TaxAmount = taxAmount,
            TransactionFee = feeAmount,
            InsuranceCost = 0m,
            NetProfit = request.ActionType == TradeActionType.Sell ? totalAmount : -totalAmount,
            Status = "completed",
            UsedSmugglingRoute = listing.Commodity?.LegalityFactor < 0,
            CreatedAt = now,
            CompletedAt = now
        };

        _dbContext.TradeTransactions.Add(transaction);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Trade executed. TransactionId={TransactionId}, PlayerId={PlayerId}, Action={Action}, Quantity={Quantity}, UnitPrice={UnitPrice}",
            transaction.Id,
            player.Id,
            request.ActionType,
            quantity,
            unitPrice);

        return new TradeExecutionResult
        {
            TradeTransactionId = transaction.Id,
            PlayerId = player.Id,
            ShipId = ship.Id,
            MarketListingId = listing.Id,
            ActionType = request.ActionType,
            Quantity = quantity,
            UnitPrice = unitPrice,
            Subtotal = subtotal,
            TariffAmount = tariffAmount + taxAmount + feeAmount,
            TotalPrice = totalAmount,
            RemainingPlayerCredits = player.LiquidCredits,
            Status = transaction.Status
        };
    }

    public async Task<TradeExecutionResult?> ReverseTradeAsync(
        ReverseTradeRequest request,
        CancellationToken cancellationToken = default)
    {
        var transaction = await _dbContext.TradeTransactions
            .FirstOrDefaultAsync(trade => trade.Id == request.TradeTransactionId, cancellationToken);
        if (transaction is null)
        {
            return null;
        }

        if (!transaction.Status.Equals("completed", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only completed transactions can be reversed.");
        }

        if (DateTime.UtcNow - transaction.CreatedAt > TimeSpan.FromHours(24))
        {
            throw new InvalidOperationException("Transaction reversal window has expired.");
        }

        var player = await _dbContext.Players.FirstOrDefaultAsync(p => p.Id == transaction.PlayerId, cancellationToken)
            ?? throw new InvalidOperationException("Transaction player was not found.");
        var listing = await _dbContext.MarketListings
            .FirstOrDefaultAsync(marketListing =>
                marketListing.MarketId == transaction.FromMarketId &&
                marketListing.CommodityId == transaction.CommodityId,
                cancellationToken)
            ?? throw new InvalidOperationException("Related listing was not found.");

        // Refund/reversal logic: restore buyer credits and listing quantity based on net direction.
        if (transaction.NetProfit < 0)
        {
            player.LiquidCredits += Math.Abs(transaction.NetProfit);
            listing.AvailableQuantity = Math.Max(0, listing.AvailableQuantity - transaction.Quantity);
        }
        else
        {
            player.LiquidCredits = Math.Max(0, player.LiquidCredits - transaction.NetProfit);
            listing.AvailableQuantity += transaction.Quantity;
        }

        transaction.Status = "reversed";
        transaction.CompletedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new TradeExecutionResult
        {
            TradeTransactionId = transaction.Id,
            PlayerId = transaction.PlayerId,
            ShipId = Guid.Empty,
            MarketListingId = listing.Id,
            ActionType = transaction.NetProfit < 0 ? TradeActionType.Buy : TradeActionType.Sell,
            Quantity = transaction.Quantity,
            UnitPrice = transaction.PricePerUnit,
            Subtotal = transaction.TotalPrice,
            TariffAmount = transaction.Tariff + transaction.TaxAmount + transaction.TransactionFee,
            TotalPrice = transaction.NetProfit,
            RemainingPlayerCredits = player.LiquidCredits,
            Status = transaction.Status
        };
    }

    public async Task<IReadOnlyList<TradeExecutionResult>> GetPlayerTransactionsAsync(
        Guid playerId,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var takeCount = Math.Clamp(limit, 1, 500);
        var transactions = await _dbContext.TradeTransactions
            .AsNoTracking()
            .Where(transaction => transaction.PlayerId == playerId)
            .OrderByDescending(transaction => transaction.CreatedAt)
            .Take(takeCount)
            .ToListAsync(cancellationToken);

        return transactions.Select(transaction => new TradeExecutionResult
        {
            TradeTransactionId = transaction.Id,
            PlayerId = transaction.PlayerId,
            ShipId = Guid.Empty,
            MarketListingId = Guid.Empty,
            ActionType = transaction.NetProfit < 0 ? TradeActionType.Buy : TradeActionType.Sell,
            Quantity = transaction.Quantity,
            UnitPrice = transaction.PricePerUnit,
            Subtotal = transaction.TotalPrice,
            TariffAmount = transaction.Tariff + transaction.TaxAmount + transaction.TransactionFee,
            TotalPrice = transaction.NetProfit,
            RemainingPlayerCredits = 0m,
            Status = transaction.Status
        }).ToList();
    }

    private static void ValidateTradeRequest(ExecuteTradeRequest request)
    {
        if (request.PlayerId == Guid.Empty || request.ShipId == Guid.Empty || request.MarketListingId == Guid.Empty)
        {
            throw new InvalidOperationException("PlayerId, ShipId, and MarketListingId are required.");
        }

        if (request.Quantity <= 0 || request.Quantity > 100_000)
        {
            throw new InvalidOperationException("Quantity must be between 1 and 100000.");
        }
    }

    private static void ValidateExpectedPrice(decimal? expectedUnitPrice, decimal actualPrice)
    {
        if (!expectedUnitPrice.HasValue || actualPrice <= 0)
        {
            return;
        }

        var delta = Math.Abs(expectedUnitPrice.Value - actualPrice) / actualPrice;
        if (delta > 0.20m)
        {
            throw new InvalidOperationException("Expected unit price is too far from current market price.");
        }
    }

    private async Task ValidateAntiExploitAsync(
        Guid playerId,
        Guid commodityId,
        long quantity,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var oneMinuteAgo = now.AddMinutes(-1);
        var recentTradesCount = await _dbContext.TradeTransactions
            .CountAsync(transaction =>
                transaction.PlayerId == playerId &&
                transaction.CreatedAt >= oneMinuteAgo,
                cancellationToken);

        if (recentTradesCount > 25)
        {
            throw new InvalidOperationException("Rate limit exceeded for trade operations.");
        }

        var repetitiveVolume = await _dbContext.TradeTransactions
            .Where(transaction =>
                transaction.PlayerId == playerId &&
                transaction.CommodityId == commodityId &&
                transaction.CreatedAt >= now.AddHours(-1))
            .SumAsync(transaction => (long?)transaction.Quantity, cancellationToken) ?? 0L;

        if (repetitiveVolume + quantity > 1_000_000)
        {
            throw new InvalidOperationException("Potential exploit detected: abnormal trading volume.");
        }
    }

    private static decimal CalculateTariffRate(MarketListing listing)
    {
        if (listing.Commodity?.LegalityFactor < 0)
        {
            return 0.12m;
        }

        return 0.03m;
    }

    private async Task ExecuteBuyAsync(
        Player player,
        Ship ship,
        MarketListing listing,
        long quantity,
        decimal totalAmount,
        CancellationToken cancellationToken)
    {
        if (listing.AvailableQuantity < quantity)
        {
            throw new InvalidOperationException("Insufficient quantity available in market listing.");
        }

        if (player.LiquidCredits < totalAmount)
        {
            throw new InvalidOperationException("Insufficient player credits.");
        }

        var volumePerUnit = listing.Commodity?.Volume ?? 1f;
        var cargoDelta = (int)Math.Ceiling(quantity * volumePerUnit);
        if (ship.CargoUsed + cargoDelta > ship.CargoCapacity)
        {
            throw new InvalidOperationException("Insufficient cargo capacity.");
        }

        listing.AvailableQuantity -= quantity;
        player.LiquidCredits -= totalAmount;
        ship.CargoUsed += cargoDelta;

        var existingCargo = await _dbContext.Cargo
            .FirstOrDefaultAsync(
                cargo => cargo.ShipId == ship.Id && cargo.CommodityId == listing.CommodityId,
                cancellationToken);
        if (existingCargo is null)
        {
            _dbContext.Cargo.Add(new Cargo
            {
                Id = Guid.NewGuid(),
                ShipId = ship.Id,
                CommodityId = listing.CommodityId,
                Quantity = quantity,
                ValuePerUnit = listing.CurrentPrice,
                LoadedAt = DateTime.UtcNow
            });
        }
        else
        {
            existingCargo.Quantity += quantity;
            existingCargo.ValuePerUnit = listing.CurrentPrice;
        }

    }

    private async Task ExecuteSellAsync(
        Player player,
        Ship ship,
        MarketListing listing,
        long quantity,
        decimal netAmount,
        CancellationToken cancellationToken)
    {
        var cargo = await _dbContext.Cargo
            .FirstOrDefaultAsync(
                entry => entry.ShipId == ship.Id && entry.CommodityId == listing.CommodityId,
                cancellationToken);
        if (cargo is null || cargo.Quantity < quantity)
        {
            throw new InvalidOperationException("Insufficient cargo quantity for sell action.");
        }

        cargo.Quantity -= quantity;
        if (cargo.Quantity == 0)
        {
            _dbContext.Cargo.Remove(cargo);
        }

        var volumePerUnit = listing.Commodity?.Volume ?? 1f;
        var cargoDelta = (int)Math.Ceiling(quantity * volumePerUnit);
        ship.CargoUsed = Math.Max(0, ship.CargoUsed - cargoDelta);

        listing.AvailableQuantity = Math.Min(listing.MaxQuantity, listing.AvailableQuantity + quantity);
        player.LiquidCredits += Math.Max(0, netAmount);

    }
}
