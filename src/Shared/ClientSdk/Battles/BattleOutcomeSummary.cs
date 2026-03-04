namespace GalacticTrader.ClientSdk.Battles;

public sealed record BattleOutcomeSummary(
    int TotalBattles,
    int VictoryCount,
    int DefeatCount,
    int OtherOutcomeCount,
    double AverageDurationSeconds,
    decimal TotalInsurancePayout,
    DateTime? LastBattleEndedUtc);
