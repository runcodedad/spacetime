namespace Spacetime.Common;

/// <summary>
/// Utilities for comparing score byte sequences used by the PoS/T consensus.
/// </summary>
public static class ScoreComparer
{
    /// <summary>
    /// Compares two byte sequences as big-endian unsigned integers.
    /// Returns -1 if <paramref name="a"/&gt; &lt; &paramref name="b"; 0 if equal; 1 if greater.
    /// </summary>
    public static int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var min = Math.Min(a.Length, b.Length);
        for (var i = 0; i < min; i++)
        {
            var ai = a[i];
            var bi = b[i];
            if (ai != bi) return ai < bi ? -1 : 1;
        }

        if (a.Length == b.Length) return 0;
        return a.Length < b.Length ? -1 : 1;
    }

    /// <summary>Array overload for convenience.</summary>
    public static int Compare(byte[] a, byte[] b) => Compare(a.AsSpan(), b.AsSpan());

    /// <summary>Returns true when <paramref name="score"/> is strictly less than <paramref name="target"/>.</summary>
    public static bool IsBelowTarget(ReadOnlySpan<byte> score, ReadOnlySpan<byte> target) => Compare(score, target) < 0;

    /// <summary>Array overload for convenience.</summary>
    public static bool IsBelowTarget(byte[] score, byte[] target) => IsBelowTarget(score.AsSpan(), target.AsSpan());
}
