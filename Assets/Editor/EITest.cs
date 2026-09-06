using UnityEngine;

/// Quick check that the native model detects the sample images through the
/// Unity inference path. Run: Unity -batchmode -quit -executeMethod EITest.Run
public static class EITest
{
    public static void Run()
    {
        Debug.Log($"EITest: EI available = {EdgeImpulseFOMO.Available}");
        string[] names = { "Samples/cap_correct_01", "Samples/cap_incorrect_01", "Samples/cap_incorrect_02", "Samples/cap_incorrect_03", "Samples/cap_incorrect_04" };
        foreach (var name in names)
        {
            var tex = Resources.Load<Texture2D>(name);
            if (tex == null) { Debug.Log($"EITest: {name} NOT FOUND"); continue; }
            EdgeImpulseFOMO.TryClassifyTexture(tex, out string label, out float conf, "cap_incorrect");
            Debug.Log($"EITest: {name} -> label={label} conf={conf:0.00}");
        }
    }
}
