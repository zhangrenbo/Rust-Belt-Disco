$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

# Prepare the alpha channel of the imagegen artwork. Only the neutral,
# edge-connected matte is removed; enclosed paper, printing and string stay opaque.
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

public static class EventDossierAlpha {
    public static void Save(string source, string destination) {
        using (var original = new Bitmap(source))
        using (var bitmap = new Bitmap(original.Width, original.Height, PixelFormat.Format32bppArgb)) {
            using (var graphics = Graphics.FromImage(bitmap)) graphics.DrawImageUnscaled(original, 0, 0);
            int width = bitmap.Width, height = bitmap.Height, count = width * height;
            var area = new Rectangle(0, 0, width, height);
            var data = bitmap.LockBits(area, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            var pixels = new byte[data.Stride * height];
            Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
            var neutral = new bool[count];
            var background = new bool[count];
            var queue = new int[count];
            int head = 0, tail = 0;
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++) {
                int p = y * data.Stride + x * 4, i = y * width + x;
                int max = Math.Max(pixels[p], Math.Max(pixels[p + 1], pixels[p + 2]));
                int min = Math.Min(pixels[p], Math.Min(pixels[p + 1], pixels[p + 2]));
                neutral[i] = max - min <= 14;
                if ((x == 0 || x == width - 1 || y == 0 || y == height - 1) && neutral[i]) {
                    background[i] = true;
                    queue[tail++] = i;
                }
            }
            while (head < tail) {
                int i = queue[head++], x = i % width, y = i / width;
                int[] neighbors = { x > 0 ? i - 1 : -1, x + 1 < width ? i + 1 : -1,
                                    y > 0 ? i - width : -1, y + 1 < height ? i + width : -1 };
                foreach (int n in neighbors) if (n >= 0 && neutral[n] && !background[n]) {
                    background[n] = true;
                    queue[tail++] = n;
                }
            }
            int transparent = 0, partial = 0;
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++) {
                int i = y * width + x, p = y * data.Stride + x * 4;
                if (background[i]) {
                    pixels[p] = pixels[p + 1] = pixels[p + 2] = pixels[p + 3] = 0;
                    transparent++;
                    continue;
                }
                bool boundary = false;
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++) {
                    int nx = x + dx, ny = y + dy;
                    if (nx >= 0 && nx < width && ny >= 0 && ny < height && background[ny * width + nx]) boundary = true;
                }
                if (boundary) {
                    int max = Math.Max(pixels[p], Math.Max(pixels[p + 1], pixels[p + 2]));
                    int min = Math.Min(pixels[p], Math.Min(pixels[p + 1], pixels[p + 2]));
                    pixels[p + 3] = (byte)Math.Min(255, Math.Max(0, (max - min - 10) * 255 / 24));
                    if (pixels[p + 3] > 0 && pixels[p + 3] < 255) partial++;
                }
            }
            Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
            bitmap.UnlockBits(data);
            if (transparent < count / 10 || partial == 0) throw new Exception("Alpha validation failed.");
            bitmap.Save(destination, ImageFormat.Png);
            Console.WriteLine("RGBA PNG: {0}x{1}; transparent={2}; partial={3}", width, height, transparent, partial);
        }
    }
}
'@

[EventDossierAlpha]::Save(
    (Join-Path $PSScriptRoot 'assets/event-dossier-bag-source-v1.png'),
    (Join-Path $PSScriptRoot 'assets/event-dossier-bag-v1.png')
)
