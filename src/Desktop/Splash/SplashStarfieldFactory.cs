using GalacticTrader.Desktop.Rendering;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace GalacticTrader.Desktop.Splash;

public static class SplashStarfieldFactory
{
    public static Model3DGroup CreateStarfield()
    {
        var random = new Random(2001);
        var modelGroup = new Model3DGroup();

        for (var index = 0; index < 180; index++)
        {
            var x = random.NextDouble() * 240 - 120;
            var y = random.NextDouble() * 100 - 50;
            var z = -(random.NextDouble() * 260 + 60);
            var size = random.NextDouble() * 0.75 + 0.2;
            var brightness = (byte)(190 + random.Next(60));
            var color = Color.FromRgb(brightness, brightness, 255);

            modelGroup.Children.Add(BoxModelFactory.Create(
                center: new Point3D(x, y, z),
                sizeX: size,
                sizeY: size,
                sizeZ: size,
                color: color,
                opacity: 0.9));
        }

        return modelGroup;
    }
}
