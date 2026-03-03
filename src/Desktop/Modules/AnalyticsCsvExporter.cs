using GalacticTrader.Desktop.Api;
using System.Text;

namespace GalacticTrader.Desktop.Modules;

public static class AnalyticsCsvExporter
{
    public static string BuildCsv(
        IReadOnlyList<TradeExecutionResultApiDto> transactions,
        IReadOnlyList<CombatLogApiDto> combats)
    {
        var builder = new StringBuilder();
        builder.AppendLine("section,id,timestamp,metric1,metric2,metric3,metric4");

        foreach (var transaction in transactions)
        {
            builder.Append("trade,");
            builder.Append(transaction.TradeTransactionId);
            builder.Append(',');
            builder.Append(DateTime.UtcNow.ToString("u"));
            builder.Append(',');
            builder.Append(transaction.Quantity);
            builder.Append(',');
            builder.Append(transaction.UnitPrice);
            builder.Append(',');
            builder.Append(transaction.TariffAmount);
            builder.Append(',');
            builder.Append(transaction.TotalPrice);
            builder.AppendLine();
        }

        foreach (var combat in combats)
        {
            builder.Append("combat,");
            builder.Append(combat.Id);
            builder.Append(',');
            builder.Append(combat.BattleEndedAt.ToUniversalTime().ToString("u"));
            builder.Append(',');
            builder.Append(combat.DurationSeconds);
            builder.Append(',');
            builder.Append(combat.TotalTicks);
            builder.Append(',');
            builder.Append(combat.InsurancePayout);
            builder.Append(',');
            builder.Append(combat.BattleOutcome.Replace(',', ';'));
            builder.AppendLine();
        }

        return builder.ToString();
    }
}
