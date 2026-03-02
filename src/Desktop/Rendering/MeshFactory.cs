using System.Windows.Media.Media3D;

namespace GalacticTrader.Desktop.Rendering;

public static class MeshFactory
{
    public static MeshGeometry3D CreateUnitCubeMesh()
    {
        var mesh = new MeshGeometry3D();

        var points = new[]
        {
            new Point3D(-0.5, -0.5, -0.5),
            new Point3D(0.5, -0.5, -0.5),
            new Point3D(0.5, 0.5, -0.5),
            new Point3D(-0.5, 0.5, -0.5),
            new Point3D(-0.5, -0.5, 0.5),
            new Point3D(0.5, -0.5, 0.5),
            new Point3D(0.5, 0.5, 0.5),
            new Point3D(-0.5, 0.5, 0.5)
        };

        foreach (var point in points)
        {
            mesh.Positions.Add(point);
        }

        var indices = new[]
        {
            0, 2, 1, 0, 3, 2,
            4, 5, 6, 4, 6, 7,
            0, 1, 5, 0, 5, 4,
            2, 3, 7, 2, 7, 6,
            1, 2, 6, 1, 6, 5,
            3, 0, 4, 3, 4, 7
        };

        foreach (var index in indices)
        {
            mesh.TriangleIndices.Add(index);
        }

        return mesh;
    }

    public static MeshGeometry3D CreateSphereMesh(double radius, int thetaDiv, int phiDiv)
    {
        var mesh = new MeshGeometry3D();

        for (var phi = 0; phi <= phiDiv; phi++)
        {
            var phiRatio = (double)phi / phiDiv;
            var polar = Math.PI * phiRatio;
            var y = radius * Math.Cos(polar);
            var ringRadius = radius * Math.Sin(polar);

            for (var theta = 0; theta <= thetaDiv; theta++)
            {
                var thetaRatio = (double)theta / thetaDiv;
                var azimuth = 2 * Math.PI * thetaRatio;
                var x = ringRadius * Math.Cos(azimuth);
                var z = ringRadius * Math.Sin(azimuth);
                mesh.Positions.Add(new Point3D(x, y, z));
            }
        }

        var rowLength = thetaDiv + 1;
        for (var phi = 0; phi < phiDiv; phi++)
        {
            for (var theta = 0; theta < thetaDiv; theta++)
            {
                var current = (phi * rowLength) + theta;
                var next = current + rowLength;

                mesh.TriangleIndices.Add(current);
                mesh.TriangleIndices.Add(next + 1);
                mesh.TriangleIndices.Add(next);

                mesh.TriangleIndices.Add(current);
                mesh.TriangleIndices.Add(current + 1);
                mesh.TriangleIndices.Add(next + 1);
            }
        }

        return mesh;
    }
}
