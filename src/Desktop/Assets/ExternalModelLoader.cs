using Assimp;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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

            return ConvertToModel(scene, modelPath);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"3D asset import failed for {modelPath}: {ex.Message}");
            return null;
        }
    }

    private static Model3DGroup ConvertToModel(Scene scene, string modelPath)
    {
        var group = new Model3DGroup();
        foreach (var mesh in scene.Meshes)
        {
            group.Children.Add(ConvertMesh(mesh, scene, modelPath));
        }

        NormalizeToUnitBounds(group);
        return group;
    }

    private static GeometryModel3D ConvertMesh(Mesh mesh, Scene scene, string modelPath)
    {
        var geometry = new MeshGeometry3D();
        var minX = double.PositiveInfinity;
        var minY = double.PositiveInfinity;
        var minZ = double.PositiveInfinity;
        var maxX = double.NegativeInfinity;
        var maxY = double.NegativeInfinity;
        var maxZ = double.NegativeInfinity;

        foreach (var vertex in mesh.Vertices)
        {
            geometry.Positions.Add(new Point3D(vertex.X, vertex.Y, vertex.Z));
            minX = Math.Min(minX, vertex.X);
            minY = Math.Min(minY, vertex.Y);
            minZ = Math.Min(minZ, vertex.Z);
            maxX = Math.Max(maxX, vertex.X);
            maxY = Math.Max(maxY, vertex.Y);
            maxZ = Math.Max(maxZ, vertex.Z);
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
        else
        {
            PopulateGeneratedTextureCoordinates(geometry, mesh, minX, minY, minZ, maxX, maxY, maxZ);
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

        var material = CreateMaterial(
            mesh.MaterialIndex,
            scene.Materials,
            scene,
            modelPath,
            geometry.TextureCoordinates.Count == geometry.Positions.Count);
        return new GeometryModel3D(geometry, material)
        {
            BackMaterial = material
        };
    }

    private static void PopulateGeneratedTextureCoordinates(
        MeshGeometry3D geometry,
        Mesh mesh,
        double minX,
        double minY,
        double minZ,
        double maxX,
        double maxY,
        double maxZ)
    {
        if (mesh.Vertices.Count == 0)
        {
            return;
        }

        var spanX = Math.Max(maxX - minX, double.Epsilon);
        var spanY = Math.Max(maxY - minY, double.Epsilon);
        var spanZ = Math.Max(maxZ - minZ, double.Epsilon);
        var useY = spanY >= spanZ;

        foreach (var vertex in mesh.Vertices)
        {
            var u = (vertex.X - minX) / spanX;
            var projection = useY
                ? (vertex.Y - minY) / spanY
                : (vertex.Z - minZ) / spanZ;
            geometry.TextureCoordinates.Add(new Point(u, 1d - projection));
        }
    }

    private static WpfMaterial CreateMaterial(
        int materialIndex,
        IList<AssimpMaterial> materials,
        Scene scene,
        string modelPath,
        bool hasTextureCoordinates)
    {
        var color = Color.FromRgb(185, 196, 215);
        var opacity = 1d;
        AssimpMaterial? sourceMaterial = null;

        if (materialIndex >= 0 && materialIndex < materials.Count)
        {
            sourceMaterial = materials[materialIndex];
            if (sourceMaterial.HasColorDiffuse)
            {
                color = ToWpfColor(sourceMaterial.ColorDiffuse);
            }

            if (sourceMaterial.HasOpacity)
            {
                opacity = Math.Clamp(sourceMaterial.Opacity, 0, 1);
            }
        }

        var brush = hasTextureCoordinates && sourceMaterial is not null
            ? TryCreateTextureBrush(sourceMaterial, scene, modelPath) ?? CreateProceduralHullBrush(color)
            : CreateProceduralHullBrush(color);

        if (opacity < 1d)
        {
            var opacityBrush = brush.CloneCurrentValue();
            opacityBrush.Opacity = opacity;
            opacityBrush.Freeze();
            brush = opacityBrush;
        }

        var diffuse = new DiffuseMaterial(brush);
        diffuse.Freeze();

        var specularBrush = new SolidColorBrush(Color.FromArgb(120, 220, 232, 255));
        specularBrush.Freeze();
        var specular = new SpecularMaterial(specularBrush, 28d);
        specular.Freeze();

        var materialGroup = new MaterialGroup();
        materialGroup.Children.Add(diffuse);
        materialGroup.Children.Add(specular);
        materialGroup.Freeze();
        return materialGroup;
    }

    private static Brush? TryCreateTextureBrush(AssimpMaterial material, Scene scene, string modelPath)
    {
        if (!material.HasTextureDiffuse)
        {
            return null;
        }

        var texturePath = material.TextureDiffuse.FilePath;
        if (string.IsNullOrWhiteSpace(texturePath))
        {
            return null;
        }

        var image = TryLoadTextureImage(texturePath, scene, modelPath);
        if (image is null)
        {
            return null;
        }

        var brush = new ImageBrush(image)
        {
            Stretch = Stretch.Fill
        };
        brush.Freeze();
        return brush;
    }

    private static ImageSource? TryLoadTextureImage(string texturePath, Scene scene, string modelPath)
    {
        if (texturePath.StartsWith('*') &&
            int.TryParse(texturePath[1..], out var embeddedIndex))
        {
            return TryLoadEmbeddedTexture(scene, embeddedIndex);
        }

        var normalizedPath = texturePath
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);
        var modelDirectory = Path.GetDirectoryName(modelPath);

        var candidates = new[]
        {
            Path.IsPathRooted(normalizedPath) ? normalizedPath : null,
            modelDirectory is null ? null : Path.Combine(modelDirectory, normalizedPath),
            Path.Combine(AppContext.BaseDirectory, normalizedPath),
            Path.Combine(Directory.GetCurrentDirectory(), normalizedPath)
        };

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate) || !File.Exists(candidate))
            {
                continue;
            }

            var fileImage = TryLoadBitmapFromFile(candidate);
            if (fileImage is not null)
            {
                return fileImage;
            }
        }

        return null;
    }

    private static ImageSource? TryLoadEmbeddedTexture(Scene scene, int textureIndex)
    {
        if (!scene.HasTextures ||
            textureIndex < 0 ||
            textureIndex >= scene.TextureCount)
        {
            return null;
        }

        var texture = scene.Textures[textureIndex];
        if (texture.IsCompressed && texture.HasCompressedData)
        {
            return TryLoadBitmapFromBytes(texture.CompressedData);
        }

        if (!texture.HasNonCompressedData ||
            texture.Width <= 0 ||
            texture.Height <= 0)
        {
            return null;
        }

        var texels = texture.NonCompressedData;
        var pixelCount = texture.Width * texture.Height;
        if (texels.Length < pixelCount)
        {
            return null;
        }

        var pixels = new byte[pixelCount * 4];
        for (var i = 0; i < pixelCount; i++)
        {
            var texel = texels[i];
            var offset = i * 4;
            pixels[offset] = texel.B;
            pixels[offset + 1] = texel.G;
            pixels[offset + 2] = texel.R;
            pixels[offset + 3] = texel.A;
        }

        var bitmap = BitmapSource.Create(
            texture.Width,
            texture.Height,
            96d,
            96d,
            PixelFormats.Bgra32,
            null,
            pixels,
            texture.Width * 4);
        bitmap.Freeze();
        return bitmap;
    }

    private static ImageSource? TryLoadBitmapFromFile(string filePath)
    {
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(filePath, UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Unable to load texture image from {filePath}: {ex.Message}");
            return null;
        }
    }

    private static ImageSource? TryLoadBitmapFromBytes(byte[] data)
    {
        try
        {
            using var stream = new MemoryStream(data);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Unable to decode embedded texture image: {ex.Message}");
            return null;
        }
    }

    private static Brush CreateProceduralHullBrush(Color tint)
    {
        const int width = 96;
        const int height = 96;
        var pixels = new byte[width * height * 4];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var grain = ((x * 17 + y * 29) % 41) / 40d;
                var panelBand = ((x + (y / 3)) % 22) < 11 ? 0.1d : -0.06d;
                var intensity = Math.Clamp(0.72d + (grain * 0.2d) + panelBand, 0.4d, 1.2d);

                var offset = ((y * width) + x) * 4;
                pixels[offset] = (byte)Math.Clamp(tint.B * intensity, 0d, 255d);
                pixels[offset + 1] = (byte)Math.Clamp(tint.G * intensity, 0d, 255d);
                pixels[offset + 2] = (byte)Math.Clamp(tint.R * intensity, 0d, 255d);
                pixels[offset + 3] = 255;
            }
        }

        var bitmap = BitmapSource.Create(
            width,
            height,
            96d,
            96d,
            PixelFormats.Bgra32,
            null,
            pixels,
            width * 4);
        bitmap.Freeze();

        var brush = new ImageBrush(bitmap)
        {
            TileMode = TileMode.Tile,
            Viewport = new Rect(0, 0, 0.18, 0.18),
            ViewportUnits = BrushMappingMode.RelativeToBoundingBox,
            Stretch = Stretch.Fill
        };
        brush.Freeze();
        return brush;
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
