// #define DEBUGPIXEL

using System.IO;
using Unity.Jobs;
using Unity.Burst;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.U2D.Common.URaster;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;

namespace UnityEditor.U2D.Common.SpriteAtlasPacker
{

    namespace GeometryPack
    {
        internal enum PackingStyle
        {
            // Half Square
            Ramp,
            // Default
            Default,
            // Square
            Square
        }

        internal enum PackingQuality
        {
            // Very Tight Packing.
            High,
            // Default
            Default,
            // Fast Packing
            Fast
        }

        // Internal Config params.
        internal struct UPackConfig
        {
            // Padding
            internal int padding;
            // Is Tight Packing. 1 for TIght.
            internal int packing;
            // Enable Rotation.
            internal int rotates;
            // Max Texture Size.
            internal int maxSize;
            // Block Offset.
            internal int bOffset;
            // Reserved.
            internal int jobSize;
            // Reserved.
            internal int sprSize;
            // Reserved.
            internal PackingStyle style;
        }

        // Pixel Mask. Stores Rasterized Sprite Pixels.
        internal struct ConvexMask
        {
            // Pixel Data
            internal Pixels pixels;
            // Vector Count
            internal int pointcount;
            // Area & Size
            internal float3 area;
            // Center and Size
            internal float4 aabb;
            // Convexified Polygon Data.
            [NativeDisableContainerSafetyRestriction]
            internal NativeArray<float2> convex;
        };

        // Atlas Masks. Stores Multiple Rasterized Sprite Pixels.
        internal struct AtlasMask
        {
            // Pixel Data
            internal Pixels pixels;
            // Point.
            internal int convexCount;
            // Fit Count
            internal int fitCount;
            // Max
            internal float3 corner;
            // Area
            internal float area;
            // Convexified Polygon DataSet.
            [NativeDisableContainerSafetyRestriction]
            internal NativeArray<int2> fitSet;
            // Convexified Polygon DataSet.
            [NativeDisableContainerSafetyRestriction]
            internal NativeArray<int3> convexSet;
            [NativeDisableContainerSafetyRestriction]
            internal NativeArray<float4> aabb;
            // Convexified Polygon Data.
            [NativeDisableContainerSafetyRestriction]
            internal NativeArray<float2> convex;
        };

        // Intermediate Working Dataset.
        internal struct ScratchData
        {
            // Convexified Polygon Data.
            [NativeDisableContainerSafetyRestriction]
            internal NativeArray<float2> geom;
        };

        [BurstCompile]
        internal struct UPack
        {
            ////////////////////////////////////////////////////////////////
            // Rasterization.
            ////////////////////////////////////////////////////////////////

            [BurstCompile]
            internal unsafe struct SpriteRaster : IJob
            {
                // Pack Config
                public UPackConfig cfg;
                // Texture Input
                public int2 textureCfg;
                // Index to process.
                public int index;
                // Vertex Count
                public int vertexCount;
                // Index Count;
                public int indexCount;
                // Seed
                public int seed;
                // SpriteRaster
                [NativeDisableContainerSafetyRestriction]
                public NativeArray<ConvexMask> spriteMasks;
                // Vector2 positions.
                [NativeDisableUnsafePtrRestriction]
                public Vector2* vertices;
                // Indices
                [NativeDisableUnsafePtrRestriction]
                public int* indices;
#if DEBUGPIXEL
                // Input Pixels
                [NativeDisableUnsafePtrRestriction]
                public Color32* pixels;
#endif

                public void Execute()
                {

                    // Rasterize Source Sprite.
                    var spriteMask = spriteMasks[index];
                    spriteMask.pixels.rect.z = spriteMask.pixels.rect.w = spriteMask.pixels.minmax.z = spriteMask.pixels.minmax.w = 0;
                    spriteMask.pixels.rect.x = spriteMask.pixels.rect.y = spriteMask.pixels.minmax.x = spriteMask.pixels.minmax.y = cfg.sprSize;

#if DEBUGPIXEL
                    UnsafeUtility.MemClear(spriteMask.pixels.data.GetUnsafePtr(), ((spriteMask.pixels.rect.w * spriteMask.pixels.size.x) + spriteMask.pixels.rect.z) * UnsafeUtility.SizeOf<Color32>());
                    UnityEngine.U2D.Common.URaster.RasterUtils.Rasterize(pixels, ref textureCfg, vertices, vertexCount, indices, indexCount, ref spriteMask.pixels, cfg.padding, cfg.padding);
                    byte color = UnityEngine.U2D.Common.URaster.RasterUtils.Color32ToByte(new Color32(254, 64, 64, 254));
#endif

                    spriteMask.pixels.rect.x = math.max(0, spriteMask.pixels.minmax.x - cfg.padding);
                    spriteMask.pixels.rect.y = math.max(0, spriteMask.pixels.minmax.y - cfg.padding);
                    spriteMask.pixels.rect.z = math.min(cfg.maxSize, spriteMask.pixels.minmax.z + cfg.padding);
                    spriteMask.pixels.rect.w = math.min(cfg.maxSize, spriteMask.pixels.minmax.w + cfg.padding);
                    spriteMask.pointcount = 0;

                    // Generate Convex Hull.
                    spriteMask.area = UnityEngine.U2D.Common.UTess.ConvexHull2D.Generate(ref spriteMask.convex, ref spriteMask.aabb, ref spriteMask.pointcount, seed, vertices, vertexCount, cfg.padding * 0.25f);
                    spriteMasks[index] = spriteMask;

                }
            }

            ////////////////////////////////////////////////////////////////
            // Atlas Packing.
            ////////////////////////////////////////////////////////////////

            internal static bool TestOverlap(ref UPackConfig cfg, ref AtlasMask atlasMask, ref ConvexMask spriteMask, ref NativeArray<float2> testMask, int x, int y)
            {

                // Check if we need to do this check at all.
                if (0 == atlasMask.convexCount)
                {
                    return false;
                }

                // Check if there is enough free Area.
                if (spriteMask.area.z * cfg.bOffset > atlasMask.corner.z && x < atlasMask.corner.x && y < atlasMask.corner.y)
                {
                    return true;
                }

                // Check if this go beyond active rect
                if (spriteMask.area.x + x > atlasMask.pixels.rect.x || spriteMask.area.y + y > atlasMask.pixels.rect.y)
                {
                    return true;
                }

                // Temp Alloc
                unsafe
                {
                    var testMask_ = (float2*)testMask.GetUnsafeReadOnlyPtr();
                    var sprtMask_ = (float2*)spriteMask.convex.GetUnsafeReadOnlyPtr();
                    for (int i = 0; i < spriteMask.pointcount; ++i)
                    {
                        testMask_[i] = new float2(sprtMask_[i].x + x, sprtMask_[i].y + y);
                        if (testMask_[i].x >= atlasMask.pixels.rect.x || testMask_[i].y >= atlasMask.pixels.rect.y)
                            return true;
                    }
                }

                var aabbSprite = spriteMask.aabb;
                aabbSprite.x = x;
                aabbSprite.y = y;

                // Test Collision
                for (int i = 0; i < atlasMask.convexCount; ++i)
                {
                    var atlasSpriteaabb = atlasMask.aabb[i];

                    var distx = math.abs(aabbSprite.x - atlasSpriteaabb.x);
                    var disty = math.abs(aabbSprite.y - atlasSpriteaabb.y);
                    if (distx > aabbSprite.z + atlasSpriteaabb.z && disty > aabbSprite.w + atlasSpriteaabb.w)
                        continue;

                    var atlasedSprite = atlasMask.convexSet[i];
                    var cx = UnityEngine.U2D.Common.UTess.ConvexHull2D.CheckCollisionSeparatingAxis(ref atlasMask.convex, atlasedSprite.y, atlasedSprite.z, ref testMask, 0, spriteMask.pointcount);
                    if (cx)
                        return true;
                }

                return false;
            }

#if DEBUGPIXEL
            [BurstCompile]
            internal static bool TestMask(ref AtlasMask atlasMask, ref Pixels spriteMask, int ax, int ay, int sx, int sy)
            {
                var satlasPixel = atlasMask.pixels.data[ay * atlasMask.pixels.size.x + ax];
                var spritePixel = spriteMask.data[sy * spriteMask.size.x + sx];
                return (spritePixel > 0 && satlasPixel > 0);
            }

            [BurstCompile]
            internal static bool TestMask(ref AtlasMask atlasMask, ref Pixels spriteMask, int x, int y)
            {

                var spriteRect = spriteMask.rect;

                if (TestMask(ref atlasMask, ref spriteMask, (x), (y), spriteRect.x, spriteRect.y))
                    return false;
                if (TestMask(ref atlasMask, ref spriteMask, (x), (y + (spriteRect.w - spriteRect.y)), spriteRect.x, spriteRect.y))
                    return false;
                if (TestMask(ref atlasMask, ref spriteMask, (x + (spriteRect.z - spriteRect.x)), (y), spriteRect.z, spriteRect.w))
                    return false;
                if (TestMask(ref atlasMask, ref spriteMask, (x + (spriteRect.z - spriteRect.x)), (y + (spriteRect.w - spriteRect.y)), spriteRect.z, spriteRect.w))
                    return false;
                if (TestMask(ref atlasMask, ref spriteMask, (x), (y), spriteRect.z / 2, spriteRect.y / 2))
                    return false;

                for (int j = spriteRect.y, _j = 0; j < spriteRect.w; ++j, ++_j)
                {
                    for (int i = spriteRect.x, _i = 0; i < spriteRect.z; ++i, ++_i)
                    {
                        if (TestMask(ref atlasMask, ref spriteMask, (_i + x), (_j + y), i, j))
                            return false;
                    }
                }

                return true;

            }

            [BurstCompile]
            internal static void ApplyMask(ref UPackConfig cfg, ref AtlasMask atlasMask, ref Pixels spriteMask, ref int4 rect, int x, int y)
            {
                for (int j = rect.y, _j = 0; j < rect.w; ++j, ++_j)
                {
                    for (int i = rect.x, _i = 0; i < rect.z; ++i, ++_i)
                    {
                        var ax = _i + x;
                        var ay = _j + y;
                        var pixel = spriteMask.data[j * spriteMask.size.x + i];
                        if (pixel != 0 && ax < atlasMask.pixels.size.x && ay < atlasMask.pixels.size.y)
                        {
                            atlasMask.pixels.data[ay * atlasMask.pixels.size.x + ax] = pixel;
                            atlasMask.pixels.minmax.x = math.min(ax, atlasMask.pixels.minmax.x);
                            atlasMask.pixels.minmax.y = math.min(ay, atlasMask.pixels.minmax.y);
                            atlasMask.pixels.minmax.z = math.max(ax, atlasMask.pixels.minmax.z);
                            atlasMask.pixels.minmax.w = math.max(ay, atlasMask.pixels.minmax.w);
                        }
                    }
                }
            }
#endif

            [BurstCompile]
            internal static unsafe void Pack(ref UPackConfig cfg, ref AtlasMask atlasMask, ref ConvexMask spriteMask, int x, int y)
            {

#if DEBUGPIXEL
                /*
                var fits = TestMask(ref atlasMask, ref spriteMask.pixels, x, y);
                if (!fits)
                {
                    var testMask = new NativeArray<float2>(1024, Allocator.Temp);
                    fits = TestOverlap(ref config, ref atlasMask, ref spriteMask, ref testMask, x, y);
                    if (fits)
                        Debug.LogWarning("overlap detected");
                }
                */
                ApplyMask(ref cfg, ref atlasMask, ref spriteMask.pixels, ref spriteMask.pixels.rect, x, y);
#endif

                var offset = atlasMask.convexCount != 0 ? atlasMask.convexSet[atlasMask.convexCount - 1].z : 0;
                for (int i = offset; i < offset + spriteMask.pointcount; ++i)
                {
                    atlasMask.convex[i] = new Vector2(spriteMask.convex[i - offset].x + x,spriteMask.convex[i - offset].y + y);
                }
                atlasMask.aabb[atlasMask.convexCount] = new float4(x + spriteMask.aabb.z, y + spriteMask.aabb.w, spriteMask.aabb.z, spriteMask.aabb.w);
                atlasMask.convexSet[atlasMask.convexCount] = new int3(atlasMask.convexCount++, offset, offset + spriteMask.pointcount);
                atlasMask.area = atlasMask.area + spriteMask.area.z;
                var sprite = new float2(x + (spriteMask.area.x / 2.0f), y + (spriteMask.area.y / 2.0f));
                atlasMask.corner.x = atlasMask.corner.x > sprite.x ? atlasMask.corner.x : sprite.x;
                atlasMask.corner.y = atlasMask.corner.y > sprite.y ? atlasMask.corner.y : sprite.y;
                atlasMask.corner.z = (atlasMask.corner.x * atlasMask.corner.y) - atlasMask.area;
            }

            ////////////////////////////////////////////////////////////////
            // Fit Sprite in a given RECT for Best Fit
            ////////////////////////////////////////////////////////////////

            [BurstCompile]
            internal struct SpriteFitter : IJob
            {
                // Cfg
                public UPackConfig config;
                // Test Inc
                public int2 fitOffset;
                // Result Index.
                public int resultIndex;
                // AtlasMask
                public AtlasMask atlasMask;
                // SpriteMask
                public ConvexMask spriteMask;
                // ResultSet
                [NativeDisableContainerSafetyRestriction]
                public NativeArray<float2> testMask;
                // ResultSet
                [NativeDisableContainerSafetyRestriction]
                public NativeArray<int4> resultSet;

                public void Execute()
                {
                    for (int i = fitOffset.x; i < fitOffset.y; ++i)
                    {
                        if (i >= atlasMask.fitCount)
                        {
                            break;
                        }

                        int x = (atlasMask.fitSet[i].x * config.bOffset);
                        int y = (atlasMask.fitSet[i].y * config.bOffset);
                        bool overlap = TestOverlap(ref config, ref atlasMask, ref spriteMask, ref testMask, x, y);
                        if (!overlap)
                        {
                            resultSet[resultIndex] = new int4(x, y, 1, x * y);
                            return;
                        }
                    }
                }
            }

            ////////////////////////////////////////////////////////////////
            // Best Fit.
            ////////////////////////////////////////////////////////////////

            static void UpdateAtlasMaskRampStyle(ref UPackConfig cfg, ref AtlasMask atlasMask)
            {
                if ( atlasMask.fitSet.IsCreated )
                    atlasMask.fitSet.Dispose();

                int x = (atlasMask.pixels.rect.x / cfg.bOffset);
                int y = (atlasMask.pixels.rect.y / cfg.bOffset);
                int n = math.max( x, y ), m = n * n, i = 0;
                atlasMask.fitSet = new NativeArray<int2>(m, Allocator.Persistent);

                for (int j = 0; j < n; ++j)
                {
                    for (int k = 0, l = j; k <= j; ++k, --l)
                    {
                        if ( k * cfg.bOffset > atlasMask.pixels.rect.x || l * cfg.bOffset > atlasMask.pixels.rect.y )
                            continue;
                        atlasMask.fitSet[i++] = new int2(k, l);
                    }
                }
                for (int j = 1; j < n; ++j)
                {
                    for (int k = j, l = n - 1; k < n; ++k, --l)
                    {
                        if ( k * cfg.bOffset > atlasMask.pixels.rect.x || l * cfg.bOffset > atlasMask.pixels.rect.y )
                            continue;
                        atlasMask.fitSet[i++] = new int2(k, l);
                    }
                }
                atlasMask.fitCount = i;

            }

            static void UpdateAtlasMaskSquareStyle(ref UPackConfig cfg, ref AtlasMask atlasMask)
            {
                if ( atlasMask.fitSet.IsCreated )
                    atlasMask.fitSet.Dispose();

                int x = (atlasMask.pixels.rect.x / cfg.bOffset);
                int y = (atlasMask.pixels.rect.y / cfg.bOffset);
                int n = math.max( x, y ), m = n * n, i = 0;
                atlasMask.fitSet = new NativeArray<int2>(m, Allocator.Persistent);

                for (int j = 0; j < n; ++j)
                {
                    for (int k = 0; k <= j; ++k)
                    {
                        if ( k * cfg.bOffset > atlasMask.pixels.rect.x || j * cfg.bOffset > atlasMask.pixels.rect.y )
                            continue;
                        atlasMask.fitSet[i++] = new int2(k, j);
                    }
                    for (int k = (j-1); k >=0; --k)
                    {
                        if ( j * cfg.bOffset > atlasMask.pixels.rect.x || k * cfg.bOffset > atlasMask.pixels.rect.y )
                            continue;
                        atlasMask.fitSet[i++] = new int2(j, k);
                    }
                }
                atlasMask.fitCount = i;

            }

            static void UpdateAtlasMaskFlipFlopStyle(ref UPackConfig cfg, ref AtlasMask atlasMask)
            {
                if ( atlasMask.fitSet.IsCreated )
                    atlasMask.fitSet.Dispose();

                int x = (atlasMask.pixels.rect.x / cfg.bOffset);
                int y = (atlasMask.pixels.rect.y / cfg.bOffset);
                int n = math.max( x, y ), m = n * n, i = 0;
                atlasMask.fitSet = new NativeArray<int2>(m, Allocator.Persistent);

                for (int j = 0; j < n; ++j)
                {
                    for (int k = 0; k < n; ++k)
                    {
                        if ( k * cfg.bOffset > atlasMask.pixels.rect.x || j * cfg.bOffset > atlasMask.pixels.rect.y )
                            continue;
                        atlasMask.fitSet[i++] = new int2(j, k);
                    }
                }
                atlasMask.fitCount = i;

            }

            internal static void UpdateAtlasMask(ref UPackConfig cfg, ref AtlasMask atlasMask)
            {
                switch (cfg.style)
                {
                    case PackingStyle.Default:
                        UpdateAtlasMaskFlipFlopStyle(ref cfg, ref atlasMask);
                        break;
                    case PackingStyle.Ramp:
                        UpdateAtlasMaskRampStyle(ref cfg, ref atlasMask);
                        break;
                    case PackingStyle.Square:
                        UpdateAtlasMaskSquareStyle(ref cfg, ref atlasMask);
                        break;
                }
            }

            internal static unsafe bool BestFit(ref UPackConfig cfg, ref NativeArray<SpriteFitter> fitterJob, ref NativeArray<JobHandle> fitterJobHandles, ref NativeArray<ScratchData> scratch, ref NativeArray<int4> resultArray, ref AtlasMask atlasMask, ref ConvexMask spriteMask, ref int4 output)
            {
                bool more = true;
                int rx = -1, ry = -1;
                for (int i = 0; i < cfg.jobSize; ++i)
                    fitterJobHandles[i] = default(JobHandle);

                while (more)
                {

                    int index = 0, count = atlasMask.fitCount / JobsUtility.JobWorkerCount;
                    UnsafeUtility.MemClear(resultArray.GetUnsafePtr(), resultArray.Length * sizeof(int4));

                    // Small Search.
                    for (int i = 0; i < JobsUtility.JobWorkerCount; ++i)
                    {
                        fitterJob[index] = new SpriteFitter()
                        {
                            config = cfg,
                            atlasMask = atlasMask,
                            spriteMask = spriteMask,
                            testMask = scratch[index].geom,
                            fitOffset = new int2(i * count, (i * count) + count),
                            resultSet = resultArray,
                            resultIndex = index
                        };
                        fitterJobHandles[index] = fitterJob[index].Schedule();
                        index++;
                    }

                    JobHandle.ScheduleBatchedJobs();
                    var jobHandle = JobHandle.CombineDependencies(fitterJobHandles);
                    jobHandle.Complete();

                    for (int j = 0; j < index; ++j)
                    {
                        if (resultArray[j].z == 1)
                        {
                            more = false;
                            rx = resultArray[j].x;
                            ry = resultArray[j].y;
                            break;
                        }
                    }

                    if (false == more)
                    {
                        Pack(ref cfg, ref atlasMask, ref spriteMask, rx, ry);
                        break;
                    }

                    if (atlasMask.pixels.rect.x >= cfg.maxSize && atlasMask.pixels.rect.y >= cfg.maxSize)
                    {
                        // Either successful or need another page.
                        break;
                    }
                    else
                    {
                        if (cfg.style != PackingStyle.Default)
                        {
                            atlasMask.pixels.rect.x = math.min(cfg.maxSize, atlasMask.pixels.rect.x * 2);
                            atlasMask.pixels.rect.y = math.min(cfg.maxSize, atlasMask.pixels.rect.y * 2);
                        }
                        else
                        {
                            // Row Expansion first.
                            bool incY = (atlasMask.pixels.rect.y <= atlasMask.pixels.rect.x);
                            atlasMask.pixels.rect.x = incY ? atlasMask.pixels.rect.x : math.min(cfg.maxSize, atlasMask.pixels.rect.x * 2);
                            atlasMask.pixels.rect.y = incY ? math.min(cfg.maxSize, atlasMask.pixels.rect.y * 2) : atlasMask.pixels.rect.y;
                        }
                        GeometryPack.UPack.UpdateAtlasMask(ref cfg, ref atlasMask);
                    }
                }

                output = new int4(rx, ry, 0, 0);
                return (rx != -1 && ry != -1);

            }

        }
    }

    internal class SpriteAtlasGeometryPacker : UnityEditor.U2D.ScriptablePacker
    {

        [SerializeField]
        GeometryPack.PackingStyle m_PackingStyle = GeometryPack.PackingStyle.Default;

        [SerializeField]
        GeometryPack.PackingQuality m_Quality = GeometryPack.PackingQuality.Default;

        static unsafe bool PrepareInput(GeometryPack.UPackConfig cfg, int2 spriteSize, PackerData input)
        {

            for (int i = 0; i < input.spriteData.Length; ++i)
            {

                var inputSpriteC = input.spriteData[i];
                if (inputSpriteC.rect.width + (2 * cfg.padding) > cfg.maxSize || inputSpriteC.rect.height + (2 * cfg.padding) > cfg.maxSize) return false;

#if DEBUGPIXEL
                var outputCoordX = 0;
                var outputCoordY = 0;
                var tsize = new Vector2Int(cfg.maxSize, cfg.maxSize);
                var textureDataC = input.textureData[inputSpriteC.texIndex];
                Color32* pixels = (Color32*)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(input.colorData);
                var spritePixels = UnityEngine.U2D.Common.URaster.RasterUtils.GetPixelOffsetBuffer(textureDataC.bufferOffset, pixels);
                var spriteOutput = new SpriteData();

                spriteOutput.texIndex = i;
                spriteOutput.guid = inputSpriteC.guid;
                spriteOutput.rect = new RectInt() { x = outputCoordX, y = outputCoordY, width = inputSpriteC.rect.width, height = inputSpriteC.rect.height };
                spriteOutput.output.x = 0;
                spriteOutput.output.y = 0;

                var atlasTexture = new NativeArray<Color32>(tsize.x * tsize.y, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

                for (int y = inputSpriteC.rect.y; y < (inputSpriteC.rect.y + inputSpriteC.rect.height); ++y)
                {
                    outputCoordX = 0;
                    var textureCfg = new int2(textureDataC.width, textureDataC.height);
                    for (int x = inputSpriteC.rect.x; x < (inputSpriteC.rect.x + inputSpriteC.rect.width); ++x)
                    {
                        Color32 color = UnityEngine.U2D.Common.URaster.RasterUtils.GetPixel(spritePixels, ref textureCfg, x, y);
                        int outOffset = outputCoordX + (outputCoordY * tsize.y);
                        atlasTexture[outOffset] = color;
                        outputCoordX++;
                    }
                    outputCoordY++;
                }
                atlasTexture.Dispose();
#endif

            }

            return true;

        }

        internal bool Process(SpriteAtlasPackingSettings config, SpriteAtlasTextureSettings setting, PackerData input, bool packAtlas)
        {

            var cfg = new GeometryPack.UPackConfig();
            var startRect = 64;

            cfg.packing = (int)m_Quality + 2;
            cfg.padding = config.padding;
            cfg.bOffset = config.blockOffset * (1 << cfg.packing);
            cfg.maxSize = setting.maxTextureSize;
            cfg.rotates = config.enableRotation ? 1 : 0;
            cfg.style = m_PackingStyle;
            cfg.jobSize = 1024;
            cfg.sprSize = setting.maxTextureSize;

            var spriteCount = input.spriteData.Length;
            var spriteBatch = math.min(spriteCount, SystemInfo.processorCount);

            // Because Atlas Masks are Serial / Raster in Jobs.
            var atlasCount = 0;
            var spriteSize = new int2(cfg.sprSize, cfg.sprSize);
            var validAtlas = false;

            // Rasterization.
            var atlasMasks = new NativeArray<GeometryPack.AtlasMask>(spriteCount, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            var spriteMasks = new NativeArray<GeometryPack.ConvexMask>(spriteBatch, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            var rasterJobHandles = new NativeArray<JobHandle>(spriteBatch, Allocator.Persistent);
            var rasterJob = new NativeArray<GeometryPack.UPack.SpriteRaster>(spriteBatch, Allocator.Persistent);

            // PolygonFitting
            var fitterJobHandles = new NativeArray<JobHandle>(cfg.jobSize, Allocator.Persistent);
            var fitterJob = new NativeArray<GeometryPack.UPack.SpriteFitter>(cfg.jobSize, Allocator.Persistent);
            var fitterResult = new NativeArray<int4>(cfg.jobSize, Allocator.Persistent);
            var scratch = new NativeArray<GeometryPack.ScratchData>(cfg.jobSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            var random = new Unity.Mathematics.Random(0x6E624EB7u);

            // Initialize Batch Sprite Masks.
            for (int i = 0; i < spriteBatch; ++i)
            {
                // Pixel
                GeometryPack.ConvexMask spriteMask = new GeometryPack.ConvexMask();
                spriteMask.convex = new NativeArray<float2>(65535, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                spriteMask.pointcount = 0;
                spriteMask.pixels.size = spriteSize;
                spriteMask.pixels.rect = int4.zero;
                spriteMask.pixels.minmax = new int4(spriteSize.x, spriteSize.y, 0, 0);
#if DEBUGPIXEL
                spriteMask.pixels.data = new NativeArray<byte>(spriteSize.x * spriteSize.y, Allocator.Persistent, NativeArrayOptions.ClearMemory);
#endif
                spriteMasks[i] = spriteMask;
            }

            // Temp Masks
            for (int i = 0; i < cfg.jobSize; ++i)
            {
                GeometryPack.ScratchData scratchData = new GeometryPack.ScratchData();
                scratchData.geom = new NativeArray<float2>(65535, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                scratch[i] = scratchData;
            }

            unsafe
            {

                // Prepare.
                bool prepare = PrepareInput(cfg, spriteSize, input);

                // Copy back to Processing Data
                for (int batch = 0; (batch < spriteCount && prepare); batch += spriteBatch)
                {

                    var spriteBgn = batch;
                    var spriteEnd = math.min(spriteCount, spriteBgn + spriteBatch);
                    int index = 0;

                    for (int i = spriteBgn; i < spriteEnd; ++i)
                    {
                        var inputSprite = input.spriteData[i];
                        var textureData = input.textureData[inputSprite.texIndex];

                        // Clear Mem of SpriteMask.
                        var spriteMask = spriteMasks[index];
                        spriteMask.pixels.size = spriteSize;
                        spriteMask.pixels.rect = int4.zero;
                        spriteMask.pixels.minmax = new int4(spriteSize.x, spriteSize.y, 0, 0);
                        spriteMask.pixels.texrect = new int4(inputSprite.rect.x, inputSprite.rect.y, inputSprite.rect.width, inputSprite.rect.height);
                        spriteMasks[index] = spriteMask;

                        unsafe
                        {
                            rasterJob[index] = new GeometryPack.UPack.SpriteRaster()
                            {
                                cfg = cfg,
                                vertices = (Vector2*)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(input.vertexData) + inputSprite.vertexOffset,
                                indices = (int*)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(input.indexData) + inputSprite.indexOffset,
                                textureCfg = new int2(textureData.width, textureData.height),
                                index = index,
                                seed = random.NextInt(),
                                vertexCount = inputSprite.vertexCount,
                                indexCount = inputSprite.indexCount,
                                spriteMasks = spriteMasks,
#if DEBUGPIXEL
                                pixels = (Color32*)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(input.colorData) + textureData.bufferOffset
#endif
                            };
                        }
                        rasterJobHandles[index] = rasterJob[index].Schedule();
                        index++;
                    }

                    JobHandle.ScheduleBatchedJobs();
                    var jobHandle = JobHandle.CombineDependencies(rasterJobHandles);
                    jobHandle.Complete();
                    index = 0;

                    for (int sprite = spriteBgn; sprite < spriteEnd; ++sprite)
                    {

                        var inputSpriteC = input.spriteData[sprite];
                        // Rasterize Source Sprite.
                        var spriteMask = spriteMasks[index];

                        int page = -1;
                        validAtlas = false;
                        var result = int4.zero;
                        for (int i = 0; i < atlasCount && false == validAtlas; ++i)
                        {
                            var atlasMask = atlasMasks[i];
                            validAtlas = GeometryPack.UPack.BestFit(ref cfg, ref fitterJob, ref fitterJobHandles, ref scratch, ref fitterResult, ref atlasMask, ref spriteMask, ref result);
                            if (validAtlas)
                            {
                                atlasMasks[i] = atlasMask;
                                page = i;
                            }
                        }

                        // Test
                        if (!validAtlas)
                        {
                            page = atlasCount;
                            GeometryPack.AtlasMask atlasMask = new GeometryPack.AtlasMask();
#if DEBUGPIXEL
                            atlasMask.pixels.data = new NativeArray<byte>(cfg.maxSize * cfg.maxSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
#endif
                            atlasMask.convex = new NativeArray<float2>(1024 * spriteCount, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                            atlasMask.convexSet = new NativeArray<int3>(spriteCount, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                            atlasMask.aabb = new NativeArray<float4>(spriteCount, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                            atlasMask.pixels.size = new int2(cfg.maxSize, cfg.maxSize);
                            atlasMask.pixels.rect.x = atlasMask.pixels.rect.y = startRect;
                            atlasMask.pixels.rect.z = atlasMask.pixels.rect.w = cfg.bOffset;
                            atlasMask.corner = float3.zero;
                            GeometryPack.UPack.UpdateAtlasMask(ref cfg, ref atlasMask);

                            validAtlas = GeometryPack.UPack.BestFit(ref cfg, ref fitterJob, ref fitterJobHandles, ref scratch, ref fitterResult, ref atlasMask, ref spriteMask, ref result);
                            atlasMasks[atlasCount] = atlasMask;
                            atlasCount++;
                        }

                        if (!validAtlas)
                        {
                            break;
                        }

                        // Clear Mem of SpriteMask.
#if DEBUGPIXEL
                        UnsafeUtility.MemClear(spriteMask.pixels.data.GetUnsafePtr(), ((spriteMask.pixels.rect.w * spriteMask.pixels.size.x) + spriteMask.pixels.rect.z) * UnsafeUtility.SizeOf<Color32>());
#endif

                        inputSpriteC.output.x = result.x;
                        inputSpriteC.output.y = result.y;
                        inputSpriteC.output.page = validAtlas ? page : -1;
                        input.spriteData[sprite] = inputSpriteC;
                        index++;
                    }

                    if (!validAtlas)
                    {
                        break;
                    }

                }

                if (packAtlas)
                {
#if DEBUGPIXEL
                    for (int j = 0; j < atlasCount; ++j)
                    {
                        var atlasMask = atlasMasks[j];

                        for (int a = 0; a < atlasMask.convexCount; ++a)
                        {
                            for (int b = a; b < atlasMask.convexCount; ++b)
                            {
                                {
                                    var ca = atlasMask.convexSet[a];
                                    var cb = atlasMask.convexSet[b];
                                    var cx = UnityEngine.U2D.Common.UTess.ConvexHull2D.CheckCollisionSeparatingAxis(ref atlasMask.convex, ca.y, ca.z, ref atlasMask.convex, cb.y, cb.z);
                                    if ((a == b && !cx) || (a != b && cx))
                                        Debug.LogWarning("validation failed => " + a + " : " + b + " | " + ca + " : " + cb);
                                }
                            }
                        }

                        UnityEngine.U2D.Common.URaster.RasterUtils.SaveImage(atlasMask.pixels.data, cfg.maxSize, cfg.maxSize, Path.Combine(Application.dataPath, "../") + "Temp/" + "Packer" + j + "-" + cfg.padding + ".png");
                    }
#endif
                }

                // If there is an error fallback
                if (!validAtlas)
                {
                    for (int i = 0; i < spriteCount; ++i)
                    {
                        var inputSpriteC = input.spriteData[i];
                        inputSpriteC.output.x = inputSpriteC.output.y = 0;
                        inputSpriteC.output.page = -1;
                        input.spriteData[i] = inputSpriteC;
                    }
                    Debug.LogError("Falling Back to Builtin Packing. Please check Input Sprites that may have higher size than the Max Texture Size of Atlas");
                }

                for (int j = 0; j < scratch.Length; ++j)
                {
                    scratch[j].geom.Dispose();
                }

                for (int j = 0; j < spriteMasks.Length; ++j)
                {
                    spriteMasks[j].convex.Dispose();
                }

                for (int j = 0; j < atlasMasks.Length; ++j)
                {
                    atlasMasks[j].aabb.Dispose();
                    atlasMasks[j].convex.Dispose();
                    atlasMasks[j].fitSet.Dispose();
                    atlasMasks[j].convexSet.Dispose();
                }

#if DEBUGPIXEL
                for (int j = 0; j < spriteMasks.Length; ++j)
                    spriteMasks[j].pixels.data.Dispose();
                for (int j = 0; j < atlasMasks.Length; ++j)
                    atlasMasks[j].pixels.data.Dispose();
#endif
            }

            scratch.Dispose();
            atlasMasks.Dispose();
            spriteMasks.Dispose();

            rasterJob.Dispose();
            rasterJobHandles.Dispose();

            fitterJob.Dispose();
            fitterJobHandles.Dispose();
            fitterResult.Dispose();
            return validAtlas;

        }

        protected override bool Fit(SpriteAtlasPackingSettings config, SpriteAtlasTextureSettings setting, PackerData input)
        {
            return Process(config, setting, input, false);
        }

        public override bool Pack(SpriteAtlasPackingSettings config, SpriteAtlasTextureSettings setting, PackerData input)
        {
            return Process(config, setting, input, true);
        }

    }

}
