using System;
using System.IO;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using System.Collections.Generic;

namespace UnityEngine.U2D.Common.URaster
{

    internal struct Pixels
    {
        // Border to minize search criteria.
        internal int4 rect;
        // Intermediate MinMax.
        internal int4 minmax;
        // Input Rect.
        internal int4 texrect;
        // Actual Size
        internal int2 size;
        // Rasterized Texture Data.
        [NativeDisableContainerSafetyRestriction]
        internal NativeArray<byte> data;
    };

    internal struct RasterUtils
    {

        ////////////////////////////////////////////////////////////////
        // Pixel Fetch.
        ////////////////////////////////////////////////////////////////
        internal static unsafe Color32* GetPixelOffsetBuffer(int offset, Color32* pixels)
        {
            return pixels + offset;
        }

        internal static unsafe Color32 GetPixel(Color32* pixels, ref int2 textureCfg, int x, int y)
        {
            int offset = x + (y * textureCfg.x);
            return *(pixels + offset);
        }

        internal static byte Color32ToByte(Color32 rgba)
        {
            var r = math.min(((int)(rgba.r / 128) + (int)(rgba.r != 0 ? 1 : 0)), 3);
            var g = math.min(((int)(rgba.g / 128) + (int)(rgba.g != 0 ? 1 : 0)), 3);
            var b = math.min(((int)(rgba.b / 128) + (int)(rgba.b != 0 ? 1 : 0)), 3);
            var a = (int)(rgba.a != 0 ? 3 : 0);
            return (byte)(a | (b << 2) | (g << 4) | r << 6);
        }

        internal static Color32 ByteToColor32(byte rgba)
        {
            Color32 c = new Color32();
            int rgba_ = (int)rgba;
            c.r = (byte)(((rgba_ >> 6) & (0x03)) * 64);
            c.g = (byte)(((rgba_ >> 4) & (0x03)) * 64);
            c.b = (byte)(((rgba_ >> 2) & (0x03)) * 64);
            c.a = (byte)(((rgba_ & 0x03) != 0) ? 255 : 0);
            return c;
        }

        ////////////////////////////////////////////////////////////////
        // Rasterization.
        ////////////////////////////////////////////////////////////////

        internal static float Min3(float a, float b, float c)
        {
            var bc = math.min(b, c);
            return math.min(a, bc);
        }

        internal static float Max3(float a, float b, float c)
        {
            var bc = math.max(b, c);
            return math.max(a, bc);
        }

        internal static int Orient2d(float2 a, float2 b, float2 c)
        {
            return (int)((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x));
        }

        internal static bool IsValidColorByte(byte c)
        {
            return (0 != (c & 0xFC) && 0 != (c & 0x03));
        }

        internal static unsafe byte Pixelate(ref Pixels pixelMask, ref int2 textureCfg, Color32* pixels, byte fillColorByte, int sx, int sy, int x, int y)
        {
            int _x = x - pixelMask.texrect.x;
            int _y = y - pixelMask.texrect.y;
            byte c = fillColorByte;
            Color32 src = GetPixel(pixels, ref textureCfg, sx, sy);
            c = Color32ToByte(src);
            c = IsValidColorByte(c) ? c : fillColorByte;
            pixelMask.data[(_y * pixelMask.size.x) + _x] = c;
            pixelMask.minmax.x = math.min(_x, pixelMask.minmax.x);
            pixelMask.minmax.y = math.min(_y, pixelMask.minmax.y);
            pixelMask.minmax.z = math.max(_x, pixelMask.minmax.z);
            pixelMask.minmax.w = math.max(_y, pixelMask.minmax.w);
            return c;
        }

        internal static unsafe void Pad(ref Pixels pixelMask, byte srcColorByte, byte tgtColorByte, int dx, int dy, int padx, int pady)
        {
            if (!IsValidColorByte(srcColorByte))
                return;

            for (int y = -pady; y < pady; ++y)
            {
                for (int x = -padx; x < padx; ++x)
                {
                    int _x = math.min(math.max(dx + x, 0), pixelMask.size.x) - pixelMask.texrect.x;
                    int _y = math.min(math.max(dy + y, 0), pixelMask.size.y) - pixelMask.texrect.y;
                    if (_x < 0 || _y < 0 || _x > pixelMask.size.x || _y > pixelMask.size.y)
                        continue;

                    if (0 == pixelMask.data[(_y * pixelMask.size.x) + _x])
                    {
                        pixelMask.data[(_y * pixelMask.size.x) + _x] = tgtColorByte;
                        pixelMask.minmax.x = math.min(_x, pixelMask.minmax.x);
                        pixelMask.minmax.y = math.min(_y, pixelMask.minmax.y);
                        pixelMask.minmax.z = math.max(_x, pixelMask.minmax.z);
                        pixelMask.minmax.w = math.max(_y, pixelMask.minmax.w);
                    }
                }
            }
        }

        internal static unsafe void RasterizeTriangle(ref Pixels pixelMask, Color32* pixels, ref int2 textureCfg, byte fillColorByte, ref float2 v0, ref float2 v1, ref float2 v2, int padx, int pady)
        {
            // Compute triangle bounding box
            int minX = (int)Min3(v0.x, v1.x, v2.x);
            int minY = (int)Min3(v0.y, v1.y, v2.y);
            int maxX = (int)Max3(v0.x, v1.x, v2.x);
            int maxY = (int)Max3(v0.y, v1.y, v2.y);

            // Padded Color
            var padColor = new Color32(64, 64, 254, 254);
            var padColorByte = Color32ToByte(padColor);

            // Clip against bounds
            minX = math.max(minX, 0);
            minY = math.max(minY, 0);
            maxX = math.min(maxX, pixelMask.rect.x - 1);
            maxY = math.min(maxY, pixelMask.rect.y - 1);

            // Triangle setup
            int A01 = (int)(v0.y - v1.y), B01 = (int)(v1.x - v0.x);
            int A12 = (int)(v1.y - v2.y), B12 = (int)(v2.x - v1.x);
            int A20 = (int)(v2.y - v0.y), B20 = (int)(v0.x - v2.x);

            // Barycentric coordinates at minX/minY corner
            float2 p = new float2(minX, minY);
            int w0_row = Orient2d(v1, v2, p);
            int w1_row = Orient2d(v2, v0, p);
            int w2_row = Orient2d(v0, v1, p);

            // Rasterize
            for (int sx = minY; sx <= maxY; ++sx)
            {
                // Barycentric coordinates at start of row
                int w0 = w0_row;
                int w1 = w1_row;
                int w2 = w2_row;

                for (int sy = minX; sy <= maxX; ++sy)
                {
                    // If p is on or inside all edges, render pixel.
                    if ((w0 | w1 | w2) >= 0)
                    {
                        int paddedx = sy + padx;
                        int paddedy = sx + pady;
                        var clr = Pixelate(ref pixelMask, ref textureCfg, pixels, fillColorByte, sy, sx, paddedx, paddedy);
                        Pad(ref pixelMask, clr, padColorByte, paddedx, paddedy, padx, pady);
                    }

                    // One step to the right
                    w0 += A12;
                    w1 += A20;
                    w2 += A01;
                }

                // One row step
                w0_row += B12;
                w1_row += B20;
                w2_row += B01;
            }
        }

        internal static unsafe bool Rasterize(Color32* pixels, ref int2 textureCfg, Vector2* vertices, int vertexCount, int* indices, int indexCount, ref Pixels pixelMask, int padx, int pady)
        {
            var _v = float2.zero;
            // Fill Color when corresponding pixel on rasterization is Transparent. If we don't fill overlaps can occur in tessellated regions.
            var _fill = new Color32(64, 254, 64, 254);
            var _fillByte = Color32ToByte(_fill);

            for (int i = 0; i < indexCount; i = i + 3)
            {
                int i1 = indices[i + 0];
                int i2 = indices[i + 1];
                int i3 = indices[i + 2];

                float2 v1 = vertices[i1];
                float2 v2 = vertices[i2];
                float2 v3 = vertices[i3];

                if (Orient2d(v1, v2, v3) < 0)
                {
                    _v = v1;
                    v1 = v2;
                    v2 = _v;
                }

                RasterizeTriangle(ref pixelMask, pixels, ref textureCfg, _fillByte, ref v1, ref v2, ref v3, padx, pady);
            }

            return true;
        }

        internal static void SaveImage(NativeArray<byte> image, int w, int h, string path)
        {
            var t = new Texture2D(w, h, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB, 0);
            var p = new NativeArray<Color32>(image.Length, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            for (int i = 0; i < image.Length; ++i)
                p[i] = ByteToColor32(image[i]);
            t.SetPixelData<Color32>(p, 0);
            byte[] _bytes = t.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, _bytes);
        }

    }

}
