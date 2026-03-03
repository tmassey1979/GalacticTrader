using Assimp;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using AssimpMaterial = Assimp.Material;
using WpfMaterial = System.Windows.Media.Media3D.Material;
using WpfVector3D = System.Windows.Media.Media3D.Vector3D;

namespace GalacticTrader.Desktop.Assets;

public sealed class ExternalModelLoader
{
    private const PostProcessSteps ImportSteps =
        PostProcessSteps.Triangulate |
        PostProcessSteps.JoinIdenticalVertices |
        PostProcessSteps.GenerateSmoothNormals |
        PostProcessSteps.PreTransformVertices |
        PostProcessSteps.SortByPrimitiveType;

    public Model3DGroup? TryLoad(ExternalModelAsset asset)
    {
        var modelPath = TryResolvePath(asset.RelativePath);
        if (modelPath is null)
        {
            Trace.WriteLine($"3D asset missing: {asset.RelativePath}");
            return null;
        }

        try
        {
            using var context = new AssimpContext();
            var scene = context.ImportFile(modelPath, ImportSteps);
            if (scene is null || scene.MeshCount == 0)
            {
                Trace.WriteLine($"3D asset import returned no meshes: {modelPath}");
                return null;
            }

            return ConvertToModel(scene);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"3D asset import failed for {modelPath}: {ex.Message}");
            return null;
        }
    }

    private static Model3DGroup ConvertToModel(Scene scene)
    {
        var group = new Model3DGroup();
        foreach (var mesh in scene.Meshes)
        {
            group.Children.Add(ConvertMesh(mesh, scene.Materials));
        }

        NormalizeToUnitBounds(group);
        return group;
    }

    private static GeometryModel3D ConvertMesh(Mesh mesh, IList<AssimpMaterial> materials)
    {
        var geometry = new MeshGeometry3D();

        foreach (var vertex in mesh.Vertices)
        {
            geometry.Positions.Add(new Point3D(vertex.X, vertex.Y, vertex.Z));
        }

        if (mesh.HasNormals)
        {
            foreach (var normal in mesh.Normals)
            {
                geometry.Normals.Add(new WpfVector3D(normal.X, normal.Y, normal.Z));
            }
        }

        if (mesh.HasTextureCoords(0))
        {
            foreach (var uv in mesh.TextureCoordinateChannels[0])
            {
                geometry.TextureCoordinates.Add(new Point(uv.X, 1d - uv.Y));
            }
        }

        foreach (var face in mesh.Faces)
        {
            if (face.IndexCount < 3)
            {
                continue;
            }

            var anchor = face.Indices[0];
            for (var i = 1; i < face.IndexCount - 1; i++)
            {
                geometry.TriangleIndices.Add(anchor);
                geometry.TriangleIndices.Add(face.Indices[i]);
                geometry.TriangleIndices.Add(face.Indices[i + 1]);
            }
        }

        var material = CreateMaterial(mesh.MaterialIndex, materials);
        return new GeometryModel3D(geometry, material)
        {
            BackMaterial = material
        };
    }

    private static WpfMaterial CreateMaterial(int materialIndex, IList<AssimpMaterial> materials)
    {
        var color = Color.FromRgb(185, 196, 215);
        var opacity = 1d;

        if (materialIndex >= 0 && materialIndex < materials.Count)
        {
            var sourceMaterial = materials[materialIndex];
            if (sourceMaterial.HasColorDiffuse)
            {
                color = ToWpfColor(sourceMaterial.ColorDiffuse);
            }

            if (sourceMaterial.HasOpacity)
            {
                opacity = Math.Clamp(sourceMaterial.Opacity, 0, 1);
            }
        }

        var brush = new SolidColorBrush(color)
        {
            Opacity = opacity
        };
        brush.Freeze();

        var diffuse = new DiffuseMaterial(brush);
        diffuse.Freeze();
        return diffuse;
    }

    private static Color ToWpfColor(Color4D color)
    {
        static byte ToByte(float channel)
        {
            return (byte)Math.Clamp(Math.Round(channel * 255d), 0, 255);
        }

        return Color.FromArgb(
            ToByte(color.A),
            ToByte(color.R),
            ToByte(color.G),
            ToByte(color.B));
    }

    private static void NormalizeToUnitBounds(Model3DGroup model)
    {
        var bounds = model.Bounds;
        if (bounds.IsEmpty)
        {
            return;
        }

        var maxDimension = Math.Max(bounds.SizeX, Math.Max(bounds.SizeY, bounds.SizeZ));
        if (maxDimension <= double.Epsilon)
        {
            return;
        }

        var center = new Point3D(
            bounds.X + (bounds.SizeX / 2d),
            bounds.Y + (bounds.SizeY / 2d),
            bounds.Z + (bounds.SizeZ / 2d));

        var scale = 1d / maxDimension;
        var transform = new Transform3DGroup();
        transform.Children.Add(new TranslateTransform3D(-center.X, -center.Y, -center.Z));
        transform.Children.Add(new ScaleTransform3D(scale, scale, scale));
        model.Transform = transform;
    }

    private static string? TryResolvePath(string relativePath)
    {
        var normalizedRelativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);

        var directPaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, normalizedRelativePath),
            Path.Combine(Directory.GetCurrentDirectory(), normalizedRelativePath)
        };

        foreach (var path in directPaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        var probeDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        for (var depth = 0; depth < 10 && probeDirectory is not null; depth++)
        {
            var candidatePath = Path.Combine(probeDirectory.FullName, normalizedRelativePath);
            if (File.Exists(candidatePath))
            {
                return candidatePath;
            }

            probeDirectory = probeDirectory.Parent;
        }

        return null;
    }
}
