namespace GalacticTrader.Services.Fleet;

using GalacticTrader.Data;
using GalacticTrader.Data.Models;
using Microsoft.EntityFrameworkCore;

public sealed class FleetService : IFleetService
{
    private static readonly IReadOnlyDictionary<string, ShipTemplate> Templates =
        new Dictionary<string, ShipTemplate>(StringComparer.OrdinalIgnoreCase)
        {
            ["scout"] = new("scout", "Scout", 180, 120, 90, 160, 120, 35, 6, 2, 95_000m),
            ["hauler"] = new("hauler", "Hauler", 260, 180, 110, 620, 80, 55, 10, 2, 180_000m),
            ["escort"] = new("escort", "Escort", 360, 260, 140, 260, 105, 60, 14, 4, 310_000m),
            ["battleship"] = new("battleship", "Battleship", 520, 420, 210, 340, 130, 85, 18, 6, 580_000m)
        };

    private readonly GalacticTraderDbContext _dbContext;

    public FleetService(GalacticTraderDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<IReadOnlyList<ShipTemplateDto>> GetShipTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var templates = Templates.Values
            .Select(template => new ShipTemplateDto
            {
                Key = template.Key,
                ShipClass = template.ShipClass,
                HullIntegrity = template.HullIntegrity,
                ShieldCapacity = template.ShieldCapacity,
                ReactorOutput = template.ReactorOutput,
                CargoCapacity = template.CargoCapacity,
                SensorRange = template.SensorRange,
                SignatureProfile = template.SignatureProfile,
                CrewSlots = template.CrewSlots,
                Hardpoints = template.Hardpoints,
                PurchasePrice = template.PurchasePrice
            })
            .OrderBy(template => template.PurchasePrice)
            .ToList();

        return Task.FromResult<IReadOnlyList<ShipTemplateDto>>(templates);
    }

    public async Task<ShipDto?> PurchaseShipAsync(PurchaseShipRequest request, CancellationToken cancellationToken = default)
    {
        if (!Templates.TryGetValue(request.TemplateKey, out var template))
        {
            throw new InvalidOperationException($"Unknown ship template '{request.TemplateKey}'.");
        }

        var player = await _dbContext.Players.FirstOrDefaultAsync(existing => existing.Id == request.PlayerId, cancellationToken);
        if (player is null)
        {
            return null;
        }

        if (player.LiquidCredits < template.PurchasePrice)
        {
            throw new InvalidOperationException("Insufficient credits for ship purchase.");
        }

        var ship = new Ship
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            Name = string.IsNullOrWhiteSpace(request.Name) ? $"{template.ShipClass}-{Random.Shared.Next(1000, 9999)}" : request.Name.Trim(),
            ShipClass = template.ShipClass,
            HullIntegrity = template.HullIntegrity,
            MaxHullIntegrity = template.HullIntegrity,
            ShieldCapacity = template.ShieldCapacity,
            MaxShieldCapacity = template.ShieldCapacity,
            ReactorOutput = template.ReactorOutput,
            CargoCapacity = template.CargoCapacity,
            CargoUsed = 0,
            SensorRange = template.SensorRange,
            SignatureProfile = template.SignatureProfile,
            CrewSlots = template.CrewSlots,
            Hardpoints = template.Hardpoints,
            HasInsurance = true,
            InsuranceRate = 0.015m,
            IsActive = true,
            IsInCombat = false,
            StatusId = 0,
            PurchasePrice = template.PurchasePrice,
            PurchasedAt = DateTime.UtcNow,
            CurrentValue = template.PurchasePrice
        };

        player.LiquidCredits -= template.PurchasePrice;
        player.NetWorth += template.PurchasePrice;
        player.FleetStrengthRating += ComputeShipCombatScore(ship);

        _dbContext.Ships.Add(ship);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapShip(ship, new List<ShipModule>(), 0);
    }

    public async Task<IReadOnlyList<ShipDto>> GetPlayerShipsAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        var ships = await _dbContext.Ships
            .AsNoTracking()
            .Include(ship => ship.Modules)
            .Include(ship => ship.Crew)
            .Where(ship => ship.PlayerId == playerId)
            .OrderBy(ship => ship.Name)
            .ToListAsync(cancellationToken);

        return ships.Select(ship => MapShip(ship, ship.Modules.ToList(), ship.Crew.Count)).ToList();
    }

    public async Task<ShipDto?> GetShipAsync(Guid shipId, CancellationToken cancellationToken = default)
    {
        var ship = await _dbContext.Ships
            .AsNoTracking()
            .Include(existing => existing.Modules)
            .Include(existing => existing.Crew)
            .FirstOrDefaultAsync(existing => existing.Id == shipId, cancellationToken);

        return ship is null ? null : MapShip(ship, ship.Modules.ToList(), ship.Crew.Count);
    }

    public async Task<ShipDto?> InstallModuleAsync(InstallShipModuleRequest request, CancellationToken cancellationToken = default)
    {
        var ship = await _dbContext.Ships
            .AsNoTracking()
            .Include(existing => existing.Modules)
            .Include(existing => existing.Crew)
            .FirstOrDefaultAsync(existing => existing.Id == request.ShipId, cancellationToken);
        if (ship is null)
        {
            return null;
        }

        var normalizedType = NormalizeModuleType(request.ModuleType);
        var isHardpointModule = normalizedType is "weapon" or "launcher" or "turret";
        var occupiedHardpoints = ship.Modules.Count(module =>
            NormalizeModuleType(module.ModuleType) is "weapon" or "launcher" or "turret");

        if (isHardpointModule && occupiedHardpoints >= ship.Hardpoints)
        {
            throw new InvalidOperationException("No hardpoint slots available.");
        }

        var availableEquipmentSlots = ship.Hardpoints + Math.Max(2, ship.CrewSlots / 2);
        if (ship.Modules.Count >= availableEquipmentSlots)
        {
            throw new InvalidOperationException("No equipment slots available.");
        }

        var tier = Math.Clamp(request.Tier, 1, 5);
        var module = new ShipModule
        {
            Id = Guid.NewGuid(),
            ShipId = ship.Id,
            ModuleType = normalizedType,
            Name = string.IsNullOrWhiteSpace(request.Name) ? $"{normalizedType.ToUpperInvariant()}-T{tier}" : request.Name.Trim(),
            Tier = tier,
            HealthPoints = 100 + (tier * 20),
            MaxHealthPoints = 100 + (tier * 20),
            PurchasePrice = 8_000m * tier,
            InstalledAt = DateTime.UtcNow
        };

        _dbContext.ShipModules.Add(module);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var projectedShip = CloneShip(ship);
        ApplyModuleEffects(projectedShip, module);
        projectedShip.CurrentValue += module.PurchasePrice;
        var projectedModules = ship.Modules.Concat([module]).ToList();

        return MapShip(projectedShip, projectedModules, ship.Crew.Count);
    }

    public async Task<CrewMemberDto?> HireCrewAsync(HireCrewRequest request, CancellationToken cancellationToken = default)
    {
        var playerExists = await _dbContext.Players.AnyAsync(player => player.Id == request.PlayerId, cancellationToken);
        if (!playerExists)
        {
            return null;
        }

        if (request.ShipId.HasValue)
        {
            var ship = await _dbContext.Ships
                .Include(existing => existing.Crew)
                .FirstOrDefaultAsync(existing => existing.Id == request.ShipId.Value && existing.PlayerId == request.PlayerId, cancellationToken);

            if (ship is null)
            {
                return null;
            }

            if (ship.Crew.Count >= ship.CrewSlots)
            {
                throw new InvalidOperationException("Selected ship has no crew slots available.");
            }
        }

        var role = string.IsNullOrWhiteSpace(request.Role) ? "Generalist" : request.Role.Trim();
        var skills = RollCrewSkills(role);
        var crew = new Crew
        {
            Id = Guid.NewGuid(),
            PlayerId = request.PlayerId,
            ShipId = request.ShipId,
            Name = string.IsNullOrWhiteSpace(request.Name) ? $"Crew-{Random.Shared.Next(1000, 9999)}" : request.Name.Trim(),
            Role = role,
            CombatSkill = skills.Combat,
            EngineeringSkill = skills.Engineering,
            NavigationSkill = skills.Navigation,
            Morale = 70,
            Loyalty = 60,
            Salary = request.Salary <= 0m ? 1_800m : request.Salary,
            ExperienceLevel = 1,
            ExperiencePoints = 0,
            HiredAt = DateTime.UtcNow,
            IsActive = true
        };

        _dbContext.Crew.Add(crew);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapCrew(crew);
    }

    public async Task<CrewMemberDto?> ProgressCrewAsync(Guid crewId, CrewProgressRequest request, CancellationToken cancellationToken = default)
    {
        var crew = await _dbContext.Crew.FirstOrDefaultAsync(existing => existing.Id == crewId, cancellationToken);
        if (crew is null)
        {
            return null;
        }

        var gainedXp = Math.Max(0, request.ExperienceGained);
        crew.ExperiencePoints += gainedXp;
        var levelFromXp = Math.Max(1, (int)(crew.ExperiencePoints / 1_000) + 1);
        if (levelFromXp > crew.ExperienceLevel)
        {
            var levelsGained = levelFromXp - crew.ExperienceLevel;
            crew.ExperienceLevel = levelFromXp;
            crew.CombatSkill = Math.Clamp(crew.CombatSkill + levelsGained * 2, 0, 100);
            crew.EngineeringSkill = Math.Clamp(crew.EngineeringSkill + levelsGained * 2, 0, 100);
            crew.NavigationSkill = Math.Clamp(crew.NavigationSkill + levelsGained * 2, 0, 100);
        }

        crew.Morale = Math.Clamp(crew.Morale + (request.MissionOutcomeScore / 8), 0, 100);
        crew.Loyalty = Math.Clamp(crew.Loyalty + (request.MissionOutcomeScore >= 0 ? 1 : -2), 0, 100);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapCrew(crew);
    }

    public async Task<bool> FireCrewAsync(Guid crewId, CancellationToken cancellationToken = default)
    {
        var crew = await _dbContext.Crew.FirstOrDefaultAsync(existing => existing.Id == crewId, cancellationToken);
        if (crew is null)
        {
            return false;
        }

        _dbContext.Crew.Remove(crew);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<EscortSummaryDto?> GetEscortSummaryAsync(
        Guid playerId,
        FleetFormation formation = FleetFormation.Defensive,
        CancellationToken cancellationToken = default)
    {
        var ships = await _dbContext.Ships
            .AsNoTracking()
            .Include(ship => ship.Modules)
            .Include(ship => ship.Crew)
            .Where(ship => ship.PlayerId == playerId && ship.IsActive)
            .ToListAsync(cancellationToken);

        if (ships.Count == 0)
        {
            return null;
        }

        var fleetStrength = ships.Sum(ComputeShipCombatScore);
        var escortStrength = ships.Where(IsEscortCandidate).Sum(ComputeShipCombatScore);
        var crewImpact = ComputeCrewImpact(ships.SelectMany(ship => ship.Crew).Where(crew => crew.IsActive).ToList());

        var baseCoordination = Math.Clamp((ships.Count / 10f) + (crewImpact.NavigationModifier * 0.2f), 0f, 1f);
        var formationBoost = formation switch
        {
            FleetFormation.Defensive => 0.12f,
            FleetFormation.Spearhead => 0.08f,
            _ => 0.03f
        };
        var coordinationBonus = Math.Clamp(baseCoordination + formationBoost, 0f, 1f);

        var fleetBaseline = Math.Max(1f, fleetStrength);
        var convoyBonus = Math.Clamp((escortStrength / fleetBaseline) * 0.45f + coordinationBonus * 0.35f + crewImpact.EngineeringModifier * 0.2f, 0f, 1.25f);
        var protectiveRange = (float)(ships.Average(ship => ship.SensorRange) * (1f + convoyBonus));
        var combatModifier = 1f + convoyBonus + (crewImpact.CombatModifier * 0.35f);

        return new EscortSummaryDto
        {
            PlayerId = playerId,
            FleetStrength = fleetStrength,
            EscortStrength = escortStrength,
            ConvoyBonus = convoyBonus,
            Formation = formation,
            ProtectiveRange = protectiveRange,
            CoordinationBonus = coordinationBonus,
            CombatModifier = combatModifier,
            CrewImpact = crewImpact
        };
    }

    public async Task<ConvoySimulationResult?> SimulateConvoyAsync(ConvoySimulationRequest request, CancellationToken cancellationToken = default)
    {
        var summary = await GetEscortSummaryAsync(request.PlayerId, request.Formation, cancellationToken);
        if (summary is null)
        {
            return null;
        }

        var formationRiskModifier = request.Formation switch
        {
            FleetFormation.Defensive => -8,
            FleetFormation.Spearhead => -4,
            _ => 2
        };

        var expectedLossPercent = (int)Math.Clamp(
            40f
            - (summary.EscortStrength / 40f)
            - (summary.ConvoyBonus * 15f)
            - (summary.CoordinationBonus * 10f)
            + formationRiskModifier,
            2f,
            70f);

        var projectedProtectedValue = request.ConvoyValue * (1m - (expectedLossPercent / 100m));

        return new ConvoySimulationResult
        {
            Summary = summary,
            ExpectedLossPercent = expectedLossPercent,
            ProjectedProtectedValue = decimal.Round(projectedProtectedValue, 2)
        };
    }

    private static bool IsEscortCandidate(Ship ship)
    {
        return ship.ShipClass.Contains("escort", StringComparison.OrdinalIgnoreCase)
            || ship.ShipClass.Contains("battle", StringComparison.OrdinalIgnoreCase)
            || ship.Hardpoints >= 3;
    }

    private static int ComputeShipCombatScore(Ship ship)
    {
        var moduleBonus = ship.Modules.Sum(module => module.Tier * 4);
        return ship.HullIntegrity + ship.ShieldCapacity + (ship.Hardpoints * 18) + moduleBonus;
    }

    private static CrewImpactModifiersDto ComputeCrewImpact(IReadOnlyList<Crew> crew)
    {
        if (crew.Count == 0)
        {
            return new CrewImpactModifiersDto
            {
                CombatModifier = 0f,
                EngineeringModifier = 0f,
                NavigationModifier = 0f,
                MoraleFactor = 0f,
                LoyaltyFactor = 0f
            };
        }

        return new CrewImpactModifiersDto
        {
            CombatModifier = (float)(crew.Average(member => member.CombatSkill) / 100d),
            EngineeringModifier = (float)(crew.Average(member => member.EngineeringSkill) / 100d),
            NavigationModifier = (float)(crew.Average(member => member.NavigationSkill) / 100d),
            MoraleFactor = (float)(crew.Average(member => member.Morale) / 100d),
            LoyaltyFactor = (float)(crew.Average(member => member.Loyalty) / 100d)
        };
    }

    private static string NormalizeModuleType(string moduleType)
    {
        return string.IsNullOrWhiteSpace(moduleType)
            ? "utility"
            : moduleType.Trim().ToLowerInvariant();
    }

    private static void ApplyModuleEffects(Ship ship, ShipModule module)
    {
        switch (module.ModuleType)
        {
            case "shield":
                ship.MaxShieldCapacity += module.Tier * 25;
                ship.ShieldCapacity = Math.Min(ship.MaxShieldCapacity, ship.ShieldCapacity + (module.Tier * 20));
                break;
            case "engine":
                ship.SensorRange += module.Tier * 4;
                ship.SignatureProfile += module.Tier * 2;
                break;
            case "cargo":
                ship.CargoCapacity += module.Tier * 60;
                break;
            case "reactor":
                ship.ReactorOutput += module.Tier * 15;
                break;
            case "sensor":
                ship.SensorRange += module.Tier * 8;
                break;
            case "armor":
                ship.MaxHullIntegrity += module.Tier * 30;
                ship.HullIntegrity = Math.Min(ship.MaxHullIntegrity, ship.HullIntegrity + (module.Tier * 20));
                break;
            default:
                break;
        }
    }

    private static (int Combat, int Engineering, int Navigation) RollCrewSkills(string role)
    {
        var normalized = role.Trim().ToLowerInvariant();
        return normalized switch
        {
            "pilot" => (Random.Shared.Next(25, 55), Random.Shared.Next(20, 45), Random.Shared.Next(45, 80)),
            "engineer" => (Random.Shared.Next(20, 45), Random.Shared.Next(45, 80), Random.Shared.Next(25, 55)),
            "gunner" => (Random.Shared.Next(45, 80), Random.Shared.Next(20, 45), Random.Shared.Next(20, 45)),
            _ => (Random.Shared.Next(30, 60), Random.Shared.Next(30, 60), Random.Shared.Next(30, 60))
        };
    }

    private static ShipDto MapShip(Ship ship, IReadOnlyList<ShipModule> modules, int crewCount)
    {
        return new ShipDto
        {
            Id = ship.Id,
            PlayerId = ship.PlayerId,
            Name = ship.Name,
            ShipClass = ship.ShipClass,
            HullIntegrity = ship.HullIntegrity,
            MaxHullIntegrity = ship.MaxHullIntegrity,
            ShieldCapacity = ship.ShieldCapacity,
            MaxShieldCapacity = ship.MaxShieldCapacity,
            ReactorOutput = ship.ReactorOutput,
            CargoCapacity = ship.CargoCapacity,
            SensorRange = ship.SensorRange,
            SignatureProfile = ship.SignatureProfile,
            CrewSlots = ship.CrewSlots,
            Hardpoints = ship.Hardpoints,
            CurrentValue = ship.CurrentValue,
            Modules = modules.Select(module => new ShipModuleDto
            {
                Id = module.Id,
                ModuleType = module.ModuleType,
                Name = module.Name,
                Tier = module.Tier,
                PurchasePrice = module.PurchasePrice
            }).ToList(),
            CrewCount = crewCount
        };
    }

    private static CrewMemberDto MapCrew(Crew crew)
    {
        return new CrewMemberDto
        {
            Id = crew.Id,
            PlayerId = crew.PlayerId,
            ShipId = crew.ShipId,
            Name = crew.Name,
            Role = crew.Role,
            CombatSkill = crew.CombatSkill,
            EngineeringSkill = crew.EngineeringSkill,
            NavigationSkill = crew.NavigationSkill,
            Morale = crew.Morale,
            Loyalty = crew.Loyalty,
            ExperienceLevel = crew.ExperienceLevel,
            ExperiencePoints = crew.ExperiencePoints
        };
    }

    private static Ship CloneShip(Ship ship)
    {
        return new Ship
        {
            Id = ship.Id,
            PlayerId = ship.PlayerId,
            Name = ship.Name,
            ShipClass = ship.ShipClass,
            HullIntegrity = ship.HullIntegrity,
            MaxHullIntegrity = ship.MaxHullIntegrity,
            ShieldCapacity = ship.ShieldCapacity,
            MaxShieldCapacity = ship.MaxShieldCapacity,
            ReactorOutput = ship.ReactorOutput,
            CargoCapacity = ship.CargoCapacity,
            CargoUsed = ship.CargoUsed,
            SensorRange = ship.SensorRange,
            SignatureProfile = ship.SignatureProfile,
            CrewSlots = ship.CrewSlots,
            Hardpoints = ship.Hardpoints,
            CurrentValue = ship.CurrentValue
        };
    }

    private readonly record struct ShipTemplate(
        string Key,
        string ShipClass,
        int HullIntegrity,
        int ShieldCapacity,
        int ReactorOutput,
        int CargoCapacity,
        int SensorRange,
        int SignatureProfile,
        int CrewSlots,
        int Hardpoints,
        decimal PurchasePrice);
}
