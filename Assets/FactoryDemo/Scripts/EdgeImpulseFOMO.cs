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
        var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(src, rt);
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
