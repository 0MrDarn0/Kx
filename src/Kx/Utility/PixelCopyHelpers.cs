// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Runtime.InteropServices;
using SkiaSharp;

namespace Kx.Utility;

internal static unsafe class PixelCopyHelpers {
    public static void EnsureZeroRowBuffer(ref byte[]? zeroRowBuffer, int requiredSize) {
        if (zeroRowBuffer == null || zeroRowBuffer.Length < requiredSize)
            zeroRowBuffer = new byte[requiredSize];
    }

    public static void CopyStrideAware(
        byte* src,
        int srcStride,
        byte* dst,
        int dstStride,
        int rowBytesToCopy,
        int height,
        ref byte[]? zeroRowBuffer) {
        if (src == null)
            throw new ArgumentNullException(nameof(src));
        if (dst == null)
            throw new ArgumentNullException(nameof(dst));

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(srcStride);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dstStride);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rowBytesToCopy);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        if (srcStride == dstStride && srcStride == rowBytesToCopy) {
            long total = (long)rowBytesToCopy * height;
            Buffer.MemoryCopy(src, dst, total, total);
            return;
        }

        if (dstStride > rowBytesToCopy) {
            int pad = dstStride - rowBytesToCopy;
            EnsureZeroRowBuffer(ref zeroRowBuffer, pad);
        }

        for (int y = 0; y < height; y++) {
            byte* sRow = src + (long)y * srcStride;
            byte* dRow = dst + (long)y * dstStride;

            int copy = Math.Min(rowBytesToCopy, srcStride);
            Buffer.MemoryCopy(sRow, dRow, rowBytesToCopy, copy);

            if (dstStride > copy) {
                int pad = dstStride - copy;
                if (zeroRowBuffer != null && zeroRowBuffer.Length >= pad) {
                    Marshal.Copy(zeroRowBuffer, 0, new IntPtr(dRow + copy), pad);
                } else {
                    for (int i = 0; i < pad; i++)
                        dRow[copy + i] = 0;
                }
            }
        }
    }

    public static bool TryCopyFromSkPixmap(
        SKPixmap pixmap,
        IntPtr dst,
        int dstStride,
        ref byte[]? zeroRowBuffer) {
        if (pixmap.GetPixels() == IntPtr.Zero)
            return false;
        int srcStride = pixmap.RowBytes;
        int height = pixmap.Height;
        int rowBytesToCopy = Math.Min(srcStride, dstStride);

        byte* srcPtr = (byte*)pixmap.GetPixels();
        byte* dstPtr = (byte*)dst;

        try {
            CopyStrideAware(srcPtr, srcStride, dstPtr, dstStride, rowBytesToCopy, height, ref zeroRowBuffer);
            return true;
        }
        catch {
            return false;
        }
    }

    public static void CopyFromBitmapData(
        IntPtr bmpScan0,
        int bmpStride,
        IntPtr dst,
        int dstStride,
        int rowBytesToCopy,
        int height,
        ref byte[]? zeroRowBuffer) {
        byte* src = (byte*)bmpScan0;
        byte* dstPtr = (byte*)dst;
        CopyStrideAware(src, bmpStride, dstPtr, dstStride, rowBytesToCopy, height, ref zeroRowBuffer);
    }
}
