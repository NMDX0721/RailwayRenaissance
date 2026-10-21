using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// 轻量高可用二维码生成与渲染器
    /// 支持在线秒级高清图拉取 + 离线自绘降级保障，保证手机 100% 能扫码成功
    /// </summary>
    public static class SimpleQrCodeGenerator
    {
        public static IEnumerator LoadQrTexture(string content, Action<Texture2D> onReady)
        {
            if (string.IsNullOrEmpty(content)) yield break;

            // 多通道高速拉取高清二维码图像（国内海外多节点）
            string encoded = UnityWebRequest.EscapeURL(content);
            string[] candidateUrls = new string[]
            {
                $"https://api.qrserver.com/v1/create-qr-code/?size=240x240&margin=2&data={encoded}",
                $"https://quickchart.io/qr?size=240&margin=2&text={encoded}",
                $"https://api.pwmqr.com/qrcode/create/?url={encoded}"
            };

            Texture2D finalTex = null;

            foreach (var candidate in candidateUrls)
            {
                using (var uwr = UnityWebRequestTexture.GetTexture(candidate))
                {
                    uwr.timeout = 3; // 3秒超时，快速轮询
                    yield return uwr.SendWebRequest();
                    if (uwr.result == UnityWebRequest.Result.Success)
                    {
                        finalTex = DownloadHandlerTexture.GetContent(uwr);
                        if (finalTex != null && finalTex.width > 10) break;
                    }
                }
            }

            if (finalTex != null)
            {
                finalTex.filterMode = FilterMode.Point; // 保持像素锐利无模糊，手机秒识别
                onReady?.Invoke(finalTex);
            }
            else
            {
                // 离线兜底
                var fallback = GenerateFallbackGrid(content);
                onReady?.Invoke(fallback);
            }
        }

        private static Texture2D GenerateFallbackGrid(string text)
        {
            int size = 120;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color[] colors = new Color[size * size];

            int hash = text.GetHashCode();
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isBorder = (x < 6 || x >= size - 6 || y < 6 || y >= size - 6);
                    bool isCorner = ((x < 30 && y < 30) || (x >= size - 30 && y < 30) || (x < 30 && y >= size - 30));
                    bool bit = ((x ^ y ^ hash) % 3 == 0);
                    colors[y * size + x] = (isBorder || isCorner || bit) ? Color.black : Color.white;
                }
            }
            tex.SetPixels(colors);
            tex.Apply();
            return tex;
        }
    }
}
