using System;
using System.Runtime.InteropServices;
using UnityEngine;

/// Managed wrapper around the native Edge Impulse FOMO library (libei_fomo).
/// Falls back gracefully (Available == false) when the native plugin is not
/// present for the current platform, so the demo still runs.
public static class EdgeImpulseFOMO
{
    const string LIB = "ei_fomo"; // libei_fomo.dylib (macOS) / libei_fomo.so (Android)

    [StructLayout(LayoutKind.Sequential)]
    public struct BoundingBox
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string label;
        public float value;
        public int x, y, width, height;
    }

    [DllImport(LIB)] static extern int ei_fomo_classify(byte[] rgb, int width, int height, [In, Out] BoundingBox[] outBoxes, int maxBoxes);
    [DllImport(LIB)] static extern int ei_fomo_input_width();
    [DllImport(LIB)] static extern int ei_fomo_input_height();

    static bool? _available;
    public static bool Available
    {
        get
        {
            if (_available.HasValue) return _available.Value;
            try { ei_fomo_input_width(); _available = true; }
            catch (DllNotFoundException) { _available = false; }
            catch (EntryPointNotFoundException) { _available = false; }
            return _available.Value;
        }
    }

    /// Resizes the texture to the model input, runs FOMO and returns the most
    /// confident detection. Returns false only if native inference is unavailable.
    public static bool TryClassifyTexture(Texture2D src, out string topLabel, out float topValue, string preferLabel)
    {
        topLabel = "cap_correct";
        topValue = 0f;
        if (!Available || src == null) return false;

        int w, h;
        try { w = ei_fomo_input_width(); h = ei_fomo_input_height(); }
        catch { return false; }

        byte[] rgb = ToRgb(src, w, h);
        var boxes = new BoundingBox[16];
        int n;
        try { n = ei_fomo_classify(rgb, w, h, boxes, boxes.Length); }
        catch { return false; }

        if (n <= 0) return true; // no caps detected -> not a defect

        int best = -1;
        float bestScore = -1f;
        for (int i = 0; i < n; i++)
        {
            float score = boxes[i].value + (boxes[i].label == preferLabel ? 1e-4f : 0f);
            if (score > bestScore) { bestScore = score; best = i; }
        }
        topLabel = boxes[best].label;
        topValue = boxes[best].value;
        return true;
    }

    static byte[] ToRgb(Texture2D src, int w, int h)
    {
        // Read the raw sRGB bytes (GetPixels32) and resize on the CPU. This avoids
        // the linear<->sRGB conversion that Graphics.Blit/ReadPixels applies in a
        // Linear-colour-space project, which would feed the model gamma-wrong pixels.
        Color32[] px;
        try { px = src.GetPixels32(); }
        catch { return ToRgbViaBlit(src, w, h); }

        int sw = src.width, sh = src.height;
        int side = Mathf.Min(sw, sh);
        int ox = (sw - side) / 2;
        int oy = (sh - side) / 2;
        var outb = new byte[w * h * 3];
        for (int y = 0; y < h; y++)
        {
            // GetPixels32 is bottom-up; Edge Impulse wants top-down.
            int sRow = oy + side - 1 - Mathf.Clamp((int)((y + 0.5f) * side / h), 0, side - 1);
            for (int x = 0; x < w; x++)
            {
                int sCol = ox + Mathf.Clamp((int)((x + 0.5f) * side / w), 0, side - 1);
                var c = px[sRow * sw + sCol];
                int o = (y * w + x) * 3;
                outb[o] = c.r; outb[o + 1] = c.g; outb[o + 2] = c.b;
            }
        }
        return outb;
    }

    // Fallback for non-readable textures (e.g. a live camera RenderTexture).
    static byte[] ToRgbViaBlit(Texture2D src, int w, int h)
    {
        var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        float side = Mathf.Min(src.width, src.height);
        var scale = new Vector2(side / src.width, side / src.height);
        var offset = new Vector2((1f - scale.x) * 0.5f, (1f - scale.y) * 0.5f);
        Graphics.Blit(src, rt, scale, offset);
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tmp = new Texture2D(w, h, TextureFormat.RGB24, false);
        tmp.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tmp.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        var raw = tmp.GetRawTextureData();       // RGB24, bottom-up
        var outb = new byte[w * h * 3];          // EI expects top-down
        for (int y = 0; y < h; y++)
            Array.Copy(raw, (h - 1 - y) * w * 3, outb, y * w * 3, w * 3);
        UnityEngine.Object.Destroy(tmp);
        return outb;
    }
}
