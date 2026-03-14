// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Drawing.Imaging;

using SkiaSharp;

namespace Kx.Core.Extensions;

public static class SkiaSharpExtensions {

    public static Rectangle ToRectangle(this SKRect rect) {
        return Rectangle.FromLTRB(
            (int)Math.Floor(rect.Left),
            (int)Math.Floor(rect.Top),
            (int)Math.Ceiling(rect.Right),
            (int)Math.Ceiling(rect.Bottom)
        );
    }

    public static SKColor ToSKColor(this System.Drawing.Color color)
       => new(color.R, color.G, color.B, color.A);


    public static SKBitmap ToSKBitmapEx(this Image image) {
        if (image is not Bitmap bmp) {
            // Falls es kein Bitmap ist, vorher konvertieren
            using var temp = new Bitmap(image);
            return temp.ToSKBitmapEx();
        }

        if (bmp.PixelFormat != PixelFormat.Format32bppPArgb)
            bmp = bmp.Clone(new Rectangle(0, 0, bmp.Width, bmp.Height), PixelFormat.Format32bppPArgb);


        SKBitmap skBmp = new(bmp.Width, bmp.Height, SKColorType.Bgra8888, SKAlphaType.Premul);


        var data = bmp.LockBits(
            new Rectangle(0, 0, bmp.Width, bmp.Height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppPArgb);

        try {
            unsafe {
                int bytesToCopy = Math.Min(bmp.Height * data.Stride, skBmp.ByteCount);
                Buffer.MemoryCopy(
                    source: (void*)data.Scan0,
                    destination: (void*)skBmp.GetPixels(),
                    destinationSizeInBytes: skBmp.ByteCount,
                    sourceBytesToCopy: bytesToCopy);
            }
        }
        finally {
            bmp.UnlockBits(data);
        }
        return skBmp;
    }


    public static SKBitmap ToSKBitmap(this Image image) {
        if (image is Bitmap bmp) {
            if (bmp.PixelFormat != PixelFormat.Format32bppPArgb)
                bmp = bmp.Clone(new Rectangle(0, 0, bmp.Width, bmp.Height), PixelFormat.Format32bppPArgb);

            var sk = new SKBitmap(bmp.Width, bmp.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
            var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);
            try {
                unsafe {
                    Buffer.MemoryCopy((void*)data.Scan0, (void*)sk.GetPixels(), sk.ByteCount, Math.Min(sk.ByteCount, data.Height * data.Stride));
                }
            }
            finally {
                bmp.UnlockBits(data);
            }
            return sk;
        }

        using var ms = new MemoryStream();
        image.Save(ms, ImageFormat.Png);
        ms.Position = 0;
        return SKBitmap.Decode(ms);
    }

    public static Bitmap ToBitmap(this SKBitmap sk) {
        var bmp = new Bitmap(sk.Width, sk.Height, PixelFormat.Format32bppPArgb);
        var bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
        try {
            unsafe {
                Buffer.MemoryCopy((void*)sk.GetPixels(), (void*)bmpData.Scan0, bmpData.Height * bmpData.Stride, sk.ByteCount);
            }
        }
        finally {
            bmp.UnlockBits(bmpData);
        }
        return bmp;
    }
}
