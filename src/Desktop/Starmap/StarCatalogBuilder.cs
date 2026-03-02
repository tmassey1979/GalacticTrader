using System.Windows.Media.Media3D;

namespace GalacticTrader.Desktop.Starmap;

public static class StarCatalogBuilder
{
    public static IReadOnlyList<StarNode> CreateStars()
    {
        var stars = new List<StarNode>();
        var random = new Random(4242);
        var names = new[]
        {
            "Sol", "Aquila", "Vega", "Helios", "Nova", "Orion", "Horizon", "Zenith",
            "Aster", "Draco", "Lynx", "Hydra", "Arcadia", "Sirius", "Deneb", "Cetus",
            "Caelum", "Perseus", "Rigel", "Arcturus", "Lyra", "Altair", "Polaris", "Cygnus"
        };

        for (var i = 0; i < names.Length; i++)
        {
            var spiral = 26 + (i * 5.2);
            var angle = (i * 0.76) + (random.NextDouble() * 0.16);

            var x = Math.Cos(angle) * spiral + random.NextDouble() * 10 - 5;
            var y = random.NextDouble() * 80 - 40;
            var z = Math.Sin(angle) * spiral + random.NextDouble() * 10 - 5;
            var magnitude = 1.8 + random.NextDouble() * 3.2;
            var isHub = i % 6 == 0;

            stars.Add(new StarNode(names[i], new Point3D(x, y, z), magnitude, isHub));
        }

        return stars;
    }
}
