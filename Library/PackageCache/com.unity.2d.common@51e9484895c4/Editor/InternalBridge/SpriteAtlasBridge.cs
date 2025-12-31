using System;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor.U2D.SpritePacking;
using UnityEngine;
using UnityEngine.U2D;

namespace UnityEditor.U2D.Common
{
    static class SpriteAtlasBridge
    {
        public static Texture2D[] GetSpriteAtlasTextures(SpriteAtlas spriteAtlas)
        {
            return spriteAtlas?.GetPreviewTextures();
        }

        public static Texture2D GetSpriteTexture(Sprite sprite, bool fromAtlas)
        {
            return SpriteAtlasUtility.GetSpriteTexture(sprite, fromAtlas);
        }

        public static TextureFormat GetSpriteAtlasTextureFormat(SpriteAtlas spriteAtlas, BuildTarget target)
        {
            return spriteAtlas?.GetTextureFormat(target) ?? TextureFormat.RGBA32;
        }

        public static SpriteFitDataTask SpriteAtlasFitDataAsync(SpriteAtlas spriteAtlas, int spriteCount)
        {
            var t = new SpriteFitDataTask(spriteAtlas);
            t.StartFitJob(spriteCount);
            return t;
        }

                public static JobHandle Fit(string spriteAtlasPath, NativeArray<UnityEditor.U2D.SpritePacking.SpriteFitInfo> sprites)
        {
            JobHandle result = default;
            unsafe
            {
                result = UnityEditor.U2D.SpritePacking.SpritePackUtility.FitSpriteAtlas(spriteAtlasPath, sprites);
            }
            return result;
        }
    }

    class SpriteFitDataTask : IDisposable
    {
        NativeArray<SpriteFitInfo> m_Data;
        JobHandle m_JobHandle;
        string m_AssetPath;

        public SpriteFitDataTask(SpriteAtlas spriteAtlas)
        {
            m_AssetPath = AssetDatabase.GetAssetPath(spriteAtlas);
        }

        public void StartFitJob(int spriteCount)
        {
            m_Data= new NativeArray<SpriteFitInfo>(spriteCount, Allocator.Persistent);
            m_JobHandle = SpritePackUtility.FitSpriteAtlas(m_AssetPath, m_Data);
        }

        public int Count => m_Data.IsCreated? m_Data.Length : 0;

        public int GetPage(int index)
        {
            if (m_Data.IsCreated && index < m_Data.Length)
                return m_Data[index].page;
            return -1;
        }

        public Vector2Int GetPageSize(int index)
        {
            if (m_Data.IsCreated && index < m_Data.Length)
                return new Vector2Int(m_Data[index].textureWidth, m_Data[index].textureHeight);
            return default;
        }

        public RectInt GetRect(int index)
        {
            if (m_Data.IsCreated && index < m_Data.Length)
                return m_Data[index].rect;
            return default;
        }

        public GUID GetSpriteID(int index)
        {
            if (m_Data.IsCreated && index < m_Data.Length)
                return m_Data[index].spriteGuid;
            return default;
        }

        public async Task WaitForJob()
        {
            while (!m_JobHandle.IsCompleted)
                await Task.Delay(10);
        }

        public void Dispose()
        {
            if(m_Data.IsCreated)
                m_Data.Dispose();
        }
    }
}
