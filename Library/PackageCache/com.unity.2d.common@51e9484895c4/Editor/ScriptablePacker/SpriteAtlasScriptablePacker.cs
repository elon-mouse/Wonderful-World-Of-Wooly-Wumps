// #define DEBUGPIXEL

using System.IO;
using Unity.Jobs;
using Unity.Burst;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.U2D.Common.URaster;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEditor.U2D.Common.SpriteAtlasPacker
{

    namespace DefaultPack
    {

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
            internal int freeBox;
            // Reserved.
            internal int jobSize;
            // Reserved.
            internal int sprSize;
        }

        // Pixel Mask. Stores Rasterized Sprite Pixels.
        internal struct PixelMask
        {
            // Pixel Data
            internal Pixels pixels;
        };

        // Atlas Masks. Stores Multiple Rasterized Sprite Pixels.
        internal struct AtlasMask
        {
            // Pixel Data
            internal Pixels pixels;
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
                public NativeArray<PixelMask> spriteMasks;
                // Vector2 positions.
                [NativeDisableUnsafePtrRestriction]
                public Vector2* vertices;
                // Indices
                [NativeDisableUnsafePtrRestriction]
                public int* indices;
                // Input Pixels
                [NativeDisableUnsafePtrRestriction]
                public Color32* pixels;

                public void Execute()
                {

                    // Rasterize Source Sprite.
                    var spriteMask = spriteMasks[index];
                    spriteMask.pixels.rect.z = spriteMask.pixels.rect.w = spriteMask.pixels.minmax.z = spriteMask.pixels.minmax.w = 0;
                    spriteMask.pixels.rect.x = spriteMask.pixels.rect.y = spriteMask.pixels.minmax.x = spriteMask.pixels.minmax.y = cfg.sprSize;

                    UnsafeUtility.MemClear(spriteMask.pixels.data.GetUnsafePtr(), ((spriteMask.pixels.rect.w * spriteMask.pixels.size.x) + spriteMask.pixels.rect.z) * UnsafeUtility.SizeOf<Color32>());
                    UnityEngine.U2D.Common.URaster.RasterUtils.Rasterize(pixels, ref textureCfg, vertices, vertexCount, indices, indexCount, ref spriteMask.pixels, cfg.padding, cfg.padding);
                    byte color = UnityEngine.U2D.Common.URaster.RasterUtils.Color32ToByte(new Color32(254, 64, 64, 254));

                    // If Tight packing fill Rect.
                    if (0 == cfg.packing)
                    {
                        // For Rect
                        for (int y = spriteMask.pixels.rect.y; y <= spriteMask.pixels.rect.w; ++y)
                        {
                            for (int x = spriteMask.pixels.rect.x; x <= spriteMask.pixels.rect.z; ++x)
                            {
                                spriteMask.pixels.data[y * spriteMask.pixels.size.x + x] = (spriteMask.pixels.data[y * spriteMask.pixels.size.x + x] != 0) ? spriteMask.pixels.data[y * spriteMask.pixels.size.x + x] : color;
                            }
                        }
                    }

                    spriteMask.pixels.rect.x = math.max(0, spriteMask.pixels.minmax.x - cfg.padding);
                    spriteMask.pixels.rect.y = math.max(0, spriteMask.pixels.minmax.y - cfg.padding);
                    spriteMask.pixels.rect.z = math.min(cfg.maxSize, spriteMask.pixels.minmax.z + cfg.padding);
                    spriteMask.pixels.rect.w = math.min(cfg.maxSize, spriteMask.pixels.minmax.w + cfg.padding);
                    spriteMasks[index] = spriteMask;

                }

            }

            ////////////////////////////////////////////////////////////////
            // Atlas Packing.
            ////////////////////////////////////////////////////////////////

            internal static bool TestMask(ref AtlasMask atlasMask, ref PixelMask spriteMask, int ax, int ay, int sx, int sy)
            {
                var satlasPixel = atlasMask.pixels.data[ay * atlasMask.pixels.size.x + ax];
                var spritePixel = spriteMask.pixels.data[sy * spriteMask.pixels.size.x + sx];
                return (spritePixel > 0 && satlasPixel > 0);
            }

            internal static bool TestMask(ref AtlasMask atlasMask, ref PixelMask spriteMask, int x, int y)
            {

                var spriteRect = spriteMask.pixels.rect;

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
            internal static void ApplyMask(ref UPackConfig cfg, ref AtlasMask atlasMask, ref PixelMask spriteMask, ref int4 rect, int x, int y)
            {
                for (int j = rect.y, _j = 0; j < rect.w; ++j, ++_j)
                {
                    for (int i = rect.x, _i = 0; i < rect.z; ++i, ++_i)
                    {
                        var ax = _i + x;
                        var ay = _j + y;
                        var pixel = spriteMask.pixels.data[j * spriteMask.pixels.size.x + i];
                        if (pixel != 0)
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

            ////////////////////////////////////////////////////////////////
            // Fit Sprite in a given RECT for Best Fit
            ////////////////////////////////////////////////////////////////

            [BurstCompile]
            internal struct SpriteFitter : IJob
            {
                // Cfg
                public UPackConfig config;
                // Test Inc
                public int4 atlasXInc;
                // Test Inc.
                public int4 atlasYInc;
                // Result Index.
                public int resultIndex;
                // AtlasMask
                public AtlasMask atlasMask;
                // SpriteMask
                public PixelMask spriteMask;
                // ResultSet
                [NativeDisableContainerSafetyRestriction]
                public NativeArray<int4> resultSet;

                public void Execute()
                {
                    for (int y = atlasYInc.x; y <= atlasYInc.y; y += atlasYInc.z)
                    {
                        if (y + spriteMask.pixels.rect.w >= atlasMask.pixels.rect.y)
                            break;

                        for (int x = atlasXInc.x; x <= atlasXInc.y; x += atlasXInc.z)
                        {
                            if (x + spriteMask.pixels.rect.z >= atlasMask.pixels.rect.x)
                                continue;

                            if (TestMask(ref atlasMask, ref spriteMask, x, y))
                            {
                                resultSet[resultIndex] = new int4(x, y, 1, 0);
                                return;
                            }
                        }
                    }
                }
            }

            ////////////////////////////////////////////////////////////////
            // Best Fit.
            ////////////////////////////////////////////////////////////////

            internal static unsafe bool BestFit(ref UPackConfig cfg, ref NativeArray<SpriteFitter> fitterJob, ref NativeArray<JobHandle> fitterJobHandles, ref NativeArray<int4> resultArray, ref AtlasMask atlasMask, ref PixelMask spriteMask, ref int4 output)
            {
                bool more = true;
                int inc = math.min(atlasMask.pixels.rect.x, atlasMask.pixels.rect.y), rx = -1, ry = -1;
                for (int i = 0; i < cfg.jobSize; ++i)
                    fitterJobHandles[i] = default(JobHandle);

                while (more)
                {

                    int index = 0;
                    UnsafeUtility.MemClear(resultArray.GetUnsafePtr(), resultArray.Length * sizeof(int4));

                    // Small Search.
                    for (int y = 0; (y < atlasMask.pixels.rect.y); y += inc)
                    {
                        fitterJob[index] = new SpriteFitter() { config = cfg, atlasMask = atlasMask, spriteMask = spriteMask, atlasXInc = new int4(0, atlasMask.pixels.rect.x, atlasMask.pixels.rect.z, 0), atlasYInc = new int4(y, y + inc, atlasMask.pixels.rect.w, 0), resultSet = resultArray, resultIndex = index };
                        fitterJobHandles[index] = fitterJob[index].Schedule();
                        index++;
                    }
                    JobHandle.ScheduleBatchedJobs();
                    var jobHandle = JobHandle.CombineDependencies(fitterJobHandles);
                    jobHandle.Complete();

                    int area = atlasMask.pixels.size.x * atlasMask.pixels.size.y;
                    for (int j = 0; j < index; ++j)
                    {
                        if (resultArray[j].z == 1 && area > (resultArray[j].x * resultArray[j].y))
                        {
                            more = false;
                            rx = resultArray[j].x;
                            ry = resultArray[j].y;
                            area = rx * ry;
                        }
                    }

                    if (false == more)
                    {
                        ApplyMask(ref cfg, ref atlasMask, ref spriteMask, ref spriteMask.pixels.rect, rx, ry);
                        break;
                    }

                    if (atlasMask.pixels.rect.x >= cfg.maxSize && atlasMask.pixels.rect.y >= cfg.maxSize)
                    {
                        // Either successful or need another page.
                        break;
                    }
                    else
                    {
    #if SQUAREINCR
                        atlasMask.rect.x = math.min(cfg.maxSize, atlasMask.rect.x * 2);
                        atlasMask.rect.y = math.min(cfg.maxSize, atlasMask.rect.y * 2);
    #else
                        // Row Expansion first.
                        bool incY = (atlasMask.pixels.rect.y < atlasMask.pixels.rect.x);
                        atlasMask.pixels.rect.x = incY ? atlasMask.pixels.rect.x : math.min(cfg.maxSize, atlasMask.pixels.rect.x * 2);
                        atlasMask.pixels.rect.y = incY ? math.min(cfg.maxSize, atlasMask.pixels.rect.y * 2) : atlasMask.pixels.rect.y;
    #endif
                    }
                }

                output = new int4(rx, ry, 0, 0);
                return (rx != -1 && ry != -1);

            }

        }

    }

    internal class SpriteAtlasScriptablePacker : UnityEditor.U2D.ScriptablePacker
    {
        static unsafe bool PrepareInput(DefaultPack.UPackConfig cfg, int2 spriteSize, PackerData input)
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

                {
                    Texture2D t = new Texture2D(cfg.maxSize, cfg.maxSize, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB, 0);
                    t.SetPixelData<Color32>(atlasTexture, 0);
                    byte[] _bytes = UnityEngine.ImageConversion.EncodeToPNG(t);
                    System.IO.File.WriteAllBytes(Path.Combine(Application.dataPath, "../") + "Temp/" + "Input" + i + ".png", _bytes);
                }

                atlasTexture.Dispose();
#endif

            }

            return true;

        }

        internal bool Process(SpriteAtlasPackingSettings config, SpriteAtlasTextureSettings setting, PackerData input, bool packAtlas)
        {
            var cfg = new DefaultPack.UPackConfig();
            var quality = 3;
            var startRect = 64;

            cfg.padding = config.padding;
            cfg.bOffset = config.blockOffset * (1 << (int)quality);
            cfg.maxSize = setting.maxTextureSize;
            cfg.rotates = config.enableRotation ? 1 : 0;
            cfg.packing = config.enableTightPacking ? 1 : 0;
            cfg.freeBox = cfg.bOffset;
            cfg.jobSize = 1024;
            cfg.sprSize = setting.maxTextureSize;

            var spriteCount = input.spriteData.Length;
            var spriteBatch = math.min(spriteCount, SystemInfo.processorCount);

            // Because Atlas Masks are Serial / Raster in Jobs.
            var atlasCount = 0;
            var spriteSize = new int2(cfg.sprSize, cfg.sprSize);
            var validAtlas = false;

            // Rasterization.
            NativeArray<DefaultPack.AtlasMask> atlasMasks = new NativeArray<DefaultPack.AtlasMask>(spriteCount, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            NativeArray<DefaultPack.PixelMask> spriteMasks = new NativeArray<DefaultPack.PixelMask>(spriteBatch, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            var rasterJobHandles = new NativeArray<JobHandle>(spriteBatch, Allocator.Persistent);
            var rasterJob = new NativeArray<DefaultPack.UPack.SpriteRaster>(spriteBatch, Allocator.Persistent);

            // PolygonFitting
            var fitterJobHandles = new NativeArray<JobHandle>(cfg.jobSize, Allocator.Persistent);
            var fitterJob = new NativeArray<DefaultPack.UPack.SpriteFitter>(cfg.jobSize, Allocator.Persistent);
            var fitterResult = new NativeArray<int4>(cfg.jobSize, Allocator.Persistent);
            var random = new Unity.Mathematics.Random(0x6E624EB7u);

            // Initialize Batch Sprite Masks.
            for (int i = 0; i < spriteBatch; ++i)
            {
                // Pixel
                DefaultPack.PixelMask spriteMask = new DefaultPack.PixelMask();
                spriteMask.pixels.size = spriteSize;
                spriteMask.pixels.rect = int4.zero;
                spriteMask.pixels.minmax = new int4(spriteSize.x, spriteSize.y, 0, 0);
                spriteMask.pixels.data = new NativeArray<byte>(spriteSize.x * spriteSize.y, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                spriteMasks[i] = spriteMask;
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
                            rasterJob[index] = new DefaultPack.UPack.SpriteRaster()
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
                                pixels = (Color32*)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(input.colorData) + textureData.bufferOffset
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
                        for (int i = (atlasCount - 1); i >= 0 && false == validAtlas; --i)
                        {
                            var atlasMask = atlasMasks[i];
                            validAtlas = DefaultPack.UPack.BestFit(ref cfg, ref fitterJob, ref fitterJobHandles, ref fitterResult, ref atlasMask, ref spriteMask, ref result);
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
                            DefaultPack.AtlasMask atlasMask = new DefaultPack.AtlasMask();
                            atlasMask.pixels.data = new NativeArray<byte>(cfg.maxSize * cfg.maxSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                            atlasMask.pixels.size = new int2(cfg.maxSize, cfg.maxSize);
                            atlasMask.pixels.rect.x = atlasMask.pixels.rect.y = startRect;
                            atlasMask.pixels.rect.z = atlasMask.pixels.rect.w = cfg.bOffset;
                            validAtlas = DefaultPack.UPack.BestFit(ref cfg, ref fitterJob, ref fitterJobHandles, ref fitterResult, ref atlasMask, ref spriteMask, ref result);
                            atlasMasks[atlasCount] = atlasMask;
                            atlasCount++;
                        }

                        if (!validAtlas)
                        {
                            break;
                        }

                        // Clear Mem of SpriteMask.
#if DEBUGPIXEL
                        if (packAtlas)
                            UnityEngine.U2D.Common.URaster.RasterUtils.SaveImage(spriteMask.pixels.data, cfg.maxSize, cfg.maxSize, Path.Combine(Application.dataPath, "../") + "Temp/" + "Input" + sprite + ".png");
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
                        UnityEngine.U2D.Common.URaster.RasterUtils.SaveImage(atlasMask.pixels.data, cfg.maxSize, cfg.maxSize, Path.Combine(Application.dataPath, "../") + "Temp/" + "Packer" + j + ".png");
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

                for (int j = 0; j < spriteMasks.Length; ++j)
                    spriteMasks[j].pixels.data.Dispose();
                for (int j = 0; j < atlasMasks.Length; ++j)
                    atlasMasks[j].pixels.data.Dispose();
            }

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
