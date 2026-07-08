using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace TeacherForm;

internal static class AppIcons
{
    private static readonly Color Accent = Color.FromArgb(19, 104, 97);
    private static readonly Color Text = Color.FromArgb(25, 43, 57);
    private static readonly Dictionary<string, Image> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static void ApplyToButton(Button button, string iconName)
    {
        bool isLargeAction = string.Equals(button.Name, "large-action-button", StringComparison.OrdinalIgnoreCase);
        bool isSidebar = string.Equals(button.Name, "sidebar-button", StringComparison.OrdinalIgnoreCase);
        Color iconColor = button.ForeColor.ToArgb() == Color.White.ToArgb() ? Color.White : Accent;
        button.Image = GetIcon(iconName, iconColor, isLargeAction ? 22 : 18);
        button.ImageAlign = ContentAlignment.MiddleLeft;
        button.TextAlign = isLargeAction || isSidebar ? ContentAlignment.MiddleLeft : ContentAlignment.MiddleCenter;
        button.TextImageRelation = TextImageRelation.ImageBeforeText;
    }

    public static PictureBox Picture(string iconName, int size, Color? color = null)
    {
        return new PictureBox
        {
            Image = GetIcon(iconName, color ?? Accent, size),
            Size = new Size(size + 2, size + 2),
            SizeMode = PictureBoxSizeMode.CenterImage,
            Margin = new Padding(0, 0, 8, 0)
        };
    }

    public static ImageList CreateImageList(int size, params (string Key, string IconName, Color Color)[] entries)
    {
        ImageList imageList = new()
        {
            ColorDepth = ColorDepth.Depth32Bit,
            ImageSize = new Size(size, size)
        };

        foreach ((string key, string iconName, Color color) in entries)
        {
            Image image = GetIcon(iconName, color, size);
            imageList.Images.Add(key, image);
        }

        return imageList;
    }

    public static void SetFormIcon(Form form, string iconName)
    {
        using Image image = GetIcon(iconName, Accent, 32);
        using Bitmap bitmap = new(image);
        IntPtr handle = bitmap.GetHicon();
        try
        {
            form.Icon = (Icon)Icon.FromHandle(handle).Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    private static Image GetIcon(string iconName, Color color, int size)
    {
        string key = $"{iconName}|{color.ToArgb()}|{size}";
        if (!Cache.TryGetValue(key, out Image? cached))
        {
            cached = CreateTintedIcon(iconName, color, size);
            Cache[key] = cached;
        }

        return (Image)cached.Clone();
    }

    private static Bitmap CreateTintedIcon(string iconName, Color color, int size)
    {
        string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Icons", "Feather", $"{iconName}.png");
        if (!File.Exists(iconPath))
        {
            return CreateFallbackIcon(color, size);
        }

        using Image source = Image.FromFile(iconPath);
        using Bitmap scaled = new(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (Graphics graphics = Graphics.FromImage(scaled))
        {
            graphics.Clear(Color.Transparent);
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.DrawImage(source, new Rectangle(0, 0, size, size));
        }

        Bitmap tinted = new(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color pixel = scaled.GetPixel(x, y);
                tinted.SetPixel(x, y, Color.FromArgb(pixel.A, color));
            }
        }

        return tinted;
    }

    private static Bitmap CreateFallbackIcon(Color color, int size)
    {
        Bitmap bitmap = new(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using Pen pen = new(color, Math.Max(2, size / 10));
        Rectangle rect = new(2, 2, size - 4, size - 4);
        graphics.DrawEllipse(pen, rect);
        graphics.DrawLine(pen, size / 2, size / 4, size / 2, size / 2);
        graphics.DrawLine(pen, size / 2, (size * 3) / 4, size / 2, (size * 3) / 4);
        return bitmap;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public static Color AccentColor => Accent;
    public static Color TextColor => Text;
}
