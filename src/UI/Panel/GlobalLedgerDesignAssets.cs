using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace KingdomGlobalLedgerV2
{
    // Native assets and recoloring rules used by the supplied UI Designer export.
    internal sealed class GlobalLedgerDesignAssets : IDisposable
    {
        readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        internal Sprite Get(string name, string fill = "", string border = "")
        {
            string key = name + fill + border;
            if (sprites.TryGetValue(key, out var cached)) return cached;
            using (var stream = typeof(GlobalLedgerDesignAssets).Assembly.GetManifestResourceStream("LedgerDesign." + name + ".png"))
            {
                if (stream == null) throw new InvalidOperationException("Missing ledger sprite: " + name);
                using (var buffer = new MemoryStream())
                {
                    stream.CopyTo(buffer);
                    var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    ImageConversion.LoadImage(texture, buffer.ToArray());
                    texture.filterMode = FilterMode.Point;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    if (fill.Length != 0 || border.Length != 0) Recolor(texture, fill, border);
                    var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, Border(name));
                    sprite.name = name;
                    sprites.Add(key, sprite);
                    return sprite;
                }
            }
        }

        static void Recolor(Texture2D texture, string fill, string border)
        {
            var pixels = texture.GetPixels32();
            int center = texture.height / 2 * texture.width + texture.width / 2;
            var face = pixels[center];
            var interior = new HashSet<int>();
            if (fill.Length != 0 && face.a == 0)
            {
                var queue = new Queue<int>(); queue.Enqueue(center); interior.Add(center);
                while (queue.Count > 0)
                {
                    int at = queue.Dequeue(), x = at % texture.width, y = at / texture.width;
                    foreach (int next in new[] { x > 0 ? at - 1 : -1, x + 1 < texture.width ? at + 1 : -1, y > 0 ? at - texture.width : -1, y + 1 < texture.height ? at + texture.width : -1 })
                        if (next >= 0 && pixels[next].a == 0 && interior.Add(next)) queue.Enqueue(next);
                }
            }
            var faceColor = GlobalLedgerDesign.Color(fill.Length == 0 ? "#ffffff" : fill);
            var edgeColor = GlobalLedgerDesign.Color(border.Length == 0 ? "#ffffff" : border);
            for (int i = 0; i < pixels.Length; i++)
            {
                var p = pixels[i];
                if (fill.Length != 0 && (interior.Contains(i) || (face.a > 0 && p.r == face.r && p.g == face.g && p.b == face.b && p.a > 0))) pixels[i] = faceColor;
                else if (border.Length != 0 && p.a > 0 && p.r == p.g && p.g == p.b)
                    pixels[i] = new Color(Mathf.Min(1, edgeColor.r * p.r / 146f), Mathf.Min(1, edgeColor.g * p.r / 146f), Mathf.Min(1, edgeColor.b * p.r / 146f), p.a / 255f);
            }
            texture.SetPixels32(pixels); texture.Apply();
        }

        static Vector4 Border(string name)
        {
            switch (name)
            {
                case "DecoTitleRed": return new Vector4(8, 8, 8, 8);
                case "DecoRectDark": return new Vector4(8, 8, 8, 8);
                case "DecoTitleYellow": return new Vector4(8, 8, 8, 8);
                case "ResizeHandles_1": return new Vector4(0, 0, 0, 0);
                case "ResizeHandles_0": return new Vector4(0, 0, 0, 0);
                case "DecoTitleDark": return new Vector4(8, 8, 8, 8);
                case "NavButtons_0": return new Vector4(0, 0, 0, 0);
                case "NavButtons_1": return new Vector4(0, 0, 0, 0);
                case "DropdownArrow": return new Vector4(0, 0, 0, 0);
                case "Checkbox_0": return new Vector4(0, 0, 0, 0);
                case "Button03": return new Vector4(15, 12, 15, 12);
                case "Button02": return new Vector4(15, 12, 15, 12);
                case "Checkbox_1": return new Vector4(0, 0, 0, 0);
                default: return Vector4.zero;
            }
        }

        public void Dispose()
        {
            foreach (var sprite in sprites.Values) { UnityEngine.Object.Destroy(sprite.texture); UnityEngine.Object.Destroy(sprite); }
            sprites.Clear();
        }
    }
}
