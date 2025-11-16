using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia;
using Avalonia.Platform;
using ReactiveUI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace TinyTomato.Services;

public class ImageCache: ReactiveObject
{
    public ObservableCollection<CacheItem> CacheList { get; } = new();

    public static ImageCache Instance { get; } = new ImageCache(100); 

    public ImageCache(int capacity)
    {
        Capacity = Math.Max(1, capacity);
        PropertyChanged+= (s, e) =>
        {
            if (e.PropertyName == nameof(CurrentIndex))
            {
                CurrentItem = GetBitmapByIndex(CurrentIndex);
            }
        };
    }

    public int Capacity
    {
        get;
        private set=>this.RaiseAndSetIfChanged(ref field,value);
    }

    public int Count => CacheList.Count;

    // Add image from path; loads via ImageSharp and converts into an Avalonia WriteableBitmap
    public unsafe void AddBitmap(string path)
    {
        using var img = Image.Load<Rgba32>(path);
        int width = img.Width;
        int height = img.Height;

        var origin = new WriteableBitmap(new PixelSize(width, height), new Avalonia.Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);

        using var dst = origin.Lock();
        byte* dstPtr = (byte*)dst.Address;
        int dstRowBytes = dst.RowBytes;

        for (int y = 0; y < height; y++)
        {
            byte* dRow = dstPtr + y * dstRowBytes;
            for (int x = 0; x < width; x++)
            {
                var p = img[x, y]; // Rgba32 (R,G,B,A) via indexer
                int off = x * 4;
                dRow[off + 0] = p.B;
                dRow[off + 1] = p.G;
                dRow[off + 2] = p.R;
                dRow[off + 3] = p.A;
            }
        }

        var item = new CacheItem(path, origin);
        CacheList.Add(item);

        if (CacheList.Count > Capacity)
        {
            var old = CacheList[0];
            try { old.Origin?.Dispose(); } catch { }
            try { old.Image?.Dispose(); } catch { }
            CacheList.RemoveAt(0);
        }
        CurrentIndex = CacheList.Count-1;
        Console.WriteLine(CurrentIndex);
    }
    public int CurrentIndex
    {
        get => field;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            CurrentItem = GetBitmapByIndex(CurrentIndex);
        }
    }
    public CacheItem? CurrentItem
    {
        get;
        private set=>this.RaiseAndSetIfChanged(ref field,value);
    }
    
    public void Next()
    {
        CurrentIndex = Math.Min(CurrentIndex + 1, ImageCache.Instance.Count - 1);
    }

    public void Previous()
    {
        CurrentIndex = Math.Max(0, CurrentIndex - 1);
    }
    public CacheItem? GetBitmapByIndex(int index)
    {
        if (index < 0 || index >= CacheList.Count) return null;
        return CacheList[index];
    }

    public int GetIndexByBitmap(CacheItem ci)
    {
        return CacheList.IndexOf(ci);
    }

    ~ImageCache()
    {
        foreach (var item in CacheList)
        {
            try { item.Origin?.Dispose(); } catch { }
            try { item.Image?.Dispose(); } catch { }
        }
        CacheList.Clear();
    }

    private void Clear(int index)
    {
        var c=CacheList[index];
        c.Origin?.Dispose();
        c.Image?.Dispose();
        CacheList.RemoveAt(index);
        CurrentIndex = -1;
    }
    

    public void ClearCurrent()
    {
        Clear(CurrentIndex);
        if(CurrentIndex!=0)
            CurrentIndex--;
        else if(CurrentIndex==0&&CacheList.Count==0)
            CurrentIndex=-1;
    }
    

    public void ClearAll()
    {
        foreach (var item in CacheList)
        {
            try { item.Origin?.Dispose(); } catch { }
            try { item.Image?.Dispose(); } catch { }
        }
        CacheList.Clear();
        CurrentIndex = -1;
    }
    public void EncryptCurrent()
    {
        if (CurrentIndex < 0) return;
        var item = ImageCache.Instance.GetBitmapByIndex(CurrentIndex);
        if (item == null) return;
        item.En();
    }

    public void DecryptCurrent()
    {
        if (CurrentIndex < 0) return;
        var item = ImageCache.Instance.GetBitmapByIndex(CurrentIndex);
        if (item == null) return;
        item.De();
    }

    public void EncryptAll()
    {
        Parallel.For(0, ImageCache.Instance.Count, (i) =>
        {
            var item = ImageCache.Instance.GetBitmapByIndex(i);
            if (item == null) return;
            item.En();
        });
    }

    public void DecryptAll()
    {
        Parallel.For(0, ImageCache.Instance.Count, (i) =>
        {
            var item = ImageCache.Instance.GetBitmapByIndex(i);
            if (item == null) return;
            item.De();
        });
    }

    public void BackAll()
    {
        foreach (var item in CacheList)
        {
            Back(item);
        }
    }

    public void BackCurrent()
    {
        Back(CurrentItem);
    }

    public unsafe void Back(CacheItem? ci)
    {
        if(ci is null) return;
        ci.Image?.Dispose();
        var clone = new WriteableBitmap(ci.Origin.PixelSize, new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        using (var s = ci.Origin.Lock())
        using (var d = clone.Lock())
        {
            byte* srcPtr = (byte*)s.Address;
            byte* dstPtr = (byte*)d.Address;
            Buffer.MemoryCopy(srcPtr, dstPtr, d.RowBytes*ci.Origin.PixelSize.Height, d.RowBytes * ci.Origin.PixelSize.Height);
        }
        ci.Image = clone;
    }
}

public class CacheItem:ReactiveObject, IDisposable
{
    public unsafe CacheItem(string path, WriteableBitmap origin)
    {
        Path = path;
        Origin= origin;
        GilCoords = ImageProcessor.Gilbert2D(origin.PixelSize.Width, origin.PixelSize.Height).ToArray();

        // clone origin into Image
        var clone = new WriteableBitmap(origin.PixelSize, new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        using (var s = origin.Lock())
        using (var d = clone.Lock())
        {
            byte* srcPtr = (byte*)s.Address;
            byte* dstPtr = (byte*)d.Address;
            Buffer.MemoryCopy(srcPtr, dstPtr, d.RowBytes*origin.PixelSize.Height, d.RowBytes * origin.PixelSize.Height);
        }
        Image = clone;
    }

    public string Path
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }


    public WriteableBitmap Image
    {
        get => field;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public WriteableBitmap Origin
    {
        get => field;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public VecI[] GilCoords
    {
        get;
        set;
    }

    public void En()
    {
        ImageProcessor.EncryptImage(this);
    }

    public void De()
    {
        ImageProcessor.DecryptImage(this);
    }

    public void Dispose()
    {
        try { Image?.Dispose(); } catch { }
        try { Origin?.Dispose(); } catch { }
    }
}
public struct VecI
{
    public int X;
    public int Y;
}