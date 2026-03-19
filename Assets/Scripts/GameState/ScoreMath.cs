using System;

public static class ScoreMath
{
    // Saturating arithmetic keeps score math in int range and prevents wraparound.
    public static int SaturatingAdd(int left, int right)
    {
        long sum = (long)left + right;
        if (sum > int.MaxValue)
            return int.MaxValue;
        if (sum < int.MinValue)
            return int.MinValue;
        return (int)sum;
    }

    public static int SaturatingMultiply(int left, int right)
    {
        long product = (long)left * right;
        if (product > int.MaxValue)
            return int.MaxValue;
        if (product < int.MinValue)
            return int.MinValue;
        return (int)product;
    }

    public static int SaturatingFromDouble(double value)
    {
        if (double.IsNaN(value))
            return 0;

        if (double.IsPositiveInfinity(value) || value > int.MaxValue)
            return int.MaxValue;

        if (double.IsNegativeInfinity(value) || value < int.MinValue)
            return int.MinValue;

        return (int)value;
    }
}