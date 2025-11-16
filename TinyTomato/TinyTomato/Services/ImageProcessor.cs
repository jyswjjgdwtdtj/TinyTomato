using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.Buffers;
using System.Runtime;
using System.Runtime.CompilerServices;
using Avalonia.Data;
using System.Globalization;

namespace TinyTomato.Services;

public static class ImageProcessor
{

    public static List<VecI> Gilbert2D(int width, int height)
    {
        var coords = new List<VecI>(width * height);
        if (width >= height)
            Generate2D(0, 0, width, 0, 0, height, coords);
        else
            Generate2D(0, 0, 0, height, width, 0, coords);
        return coords;
    }

    private static void Generate2D(int x, int y, int ax, int ay, int bx, int by, List<VecI> coords)
    {
        int w = Math.Abs(ax + ay);
        int h = Math.Abs(bx + by);
        int dax = Math.Sign(ax), day = Math.Sign(ay);
        int dbx = Math.Sign(bx), dby = Math.Sign(by);

        if (h == 1)
        {
            for (int i = 0; i < w; i++)
            {
                coords.Add(new VecI { X = x, Y = y });
                x += dax; y += day;
            }
            return;
        }

        if (w == 1)
        {
            for (int i = 0; i < h; i++)
            {
                coords.Add(new VecI { X = x, Y = y });
                x += dbx; y += dby;
            }
            return;
        }

        int ax2 =(int)Math.Floor((double)ax / 2); int ay2 = (int)Math.Floor((double)ay / 2);
        int bx2 = (int)Math.Floor((double)bx / 2); int by2 =(int)Math.Floor((double) by / 2);

        int w2 = Math.Abs(ax2 + ay2);
        int h2 = Math.Abs(bx2 + by2);

        if (2 * w > 3 * h)
        {
            if ((w2 % 2) != 0 && (w > 2))
            {
                ax2 += dax; ay2 += day;
            }

            Generate2D(x, y, ax2, ay2, bx, by, coords);
            Generate2D(x + ax2, y + ay2, ax - ax2, ay - ay2, bx, by, coords);
        }
        else
        {
            if ((h2 % 2) != 0 && (h > 2))
            {
                bx2 += dbx; by2 += dby;
            }

            Generate2D(x, y, bx2, by2, ax2, ay2, coords);
            Generate2D(x + bx2, y + by2, ax, ay, bx - bx2, by - by2, coords);
            Generate2D(x + (ax - dax) + (bx2 - dbx), y + (ay - day) + (by2 - dby),
                -bx2, -by2, -(ax - ax2), -(ay - ay2), coords);
        }
    }

    // Encrypt image by rearranging pixels along curve using WriteableBitmap
    public unsafe static void EncryptImage(CacheItem cacheItem)
    {
        int width = cacheItem.Image.PixelSize.Width;
        int height = cacheItem.Image.PixelSize.Height;
        var curve = cacheItem.GilCoords;
        int total = width * height;
        var img = cacheItem.Image;
        int offset = (int)Math.Round((Math.Sqrt(5) - 1) / 2 * total);

        var outImg = new WriteableBitmap(new PixelSize(width, height), new Avalonia.Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);

        using (var src = img.Lock())
        using (var dst = outImg.Lock())
        {
            uint* srcPtr = (uint*)src.Address;
            uint* dstPtr = (uint*)dst.Address;
            int row = src.RowBytes/4;
            for (int i = 0; i < total; i++)
            {
                var oldPos = curve[i];
                var newPos = curve[(i + offset) % total];
                uint* s = srcPtr + oldPos.Y * row + oldPos.X;
                uint* d = dstPtr + newPos.Y * row + newPos.X;
                *d = *s;
            }
        }

        try { cacheItem.Image.Dispose(); } catch { }
        cacheItem.Image = outImg;
    }

    // Decrypt image
    public unsafe static void DecryptImage(CacheItem cacheItem)
    {
        int width = cacheItem.Image.PixelSize.Width;
        int height = cacheItem.Image.PixelSize.Height;
        var curve = cacheItem.GilCoords;
        int total = width * height;
        var img = cacheItem.Image;
        int offset = (int)Math.Round((Math.Sqrt(5) - 1) / 2 * total);

        var outImg = new WriteableBitmap(new PixelSize(width, height), new Avalonia.Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);

        using (var src = img.Lock())
        using (var dst = outImg.Lock())
        {
            uint* srcPtr = (uint*)src.Address;
            uint* dstPtr = (uint*)dst.Address;
            int row = src.RowBytes/4;
            for (int i = 0; i < total; i++)
            {
                var newPos = curve[i];
                var oldPos = curve[(int)((i + offset)%(double)total)];
                uint* s = srcPtr + oldPos.Y * row + oldPos.X;
                uint* d = dstPtr + newPos.Y * row + newPos.X;
                *d = *s;
            }
        }
        try { cacheItem.Image.Dispose(); } catch { }
        cacheItem.Image = outImg;
    }
}