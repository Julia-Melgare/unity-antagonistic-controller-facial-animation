using System;
using System.Diagnostics;
using System.Linq;

public static class SoftmaxFunction
{
    public static float[] Softmax(float[] scores, float temperature = 1f)
    {
        // Apply temperature scaling
        float[] scaled = scores.Select(s => s / temperature).ToArray();

        // Subtract max for numerical stability (prevents float overflow on exp)
        float max = scaled.Max();
        float[] exps = scaled.Select(s => MathF.Exp(s - max)).ToArray();

        float sum = exps.Sum();
        return exps.Select(e => e / sum).ToArray();
    }

    public static int Sample(float[] probs)
    {
        float roll = UnityEngine.Random.value;  // [0, 1)
        float cumulative = 0f;
        for (int i = 0; i < probs.Length; i++)
        {
            cumulative += probs[i];
            if (roll < cumulative)
                return i;
        }
        return probs.Length - 1;  // fallback for floating point edge cases
    }

    public static int SoftmaxSample(float[] scores, float temperature = 1f)
    {
        float[] probs = Softmax(scores, temperature);
        return Sample(probs);
    }
}