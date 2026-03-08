using UnityEngine;

// Default score table: suited tiles use face value (1-9), honors use a fixed value.
// Balatro-style: 9 of bam = 9 points.

public static class ScoreTable
{
    public const int HonorScore = 5;

    /// <summary>
    /// Applies default scores: suited tiles = face value (1-9), honors = HonorScore.
    /// </summary>
    public static void ApplyDefaultScores(ScoringManager scoring)
    {
        if (scoring == null)
        {
            Debug.LogError("ScoreTable.ApplyDefaultScores: ScoringManager is null.");
            return;
        }

        // Suited tiles (Dots, Bam, Crack): score = face value 1-9.
        for (int v = 1; v <= 9; v++)
        {
            if (scoring.dotsValues != null && scoring.dotsValues.Length > v)
                scoring.dotsValues[v] = v;
            if (scoring.bamValues != null && scoring.bamValues.Length > v)
                scoring.bamValues[v] = v;
            if (scoring.crackValues != null && scoring.crackValues.Length > v)
                scoring.crackValues[v] = v;
        }

        // Winds, Dragons, Flowers, Seasons: fixed value.
        if (scoring.windValues != null)
            for (int i = 0; i < scoring.windValues.Length; i++)
                scoring.windValues[i] = HonorScore;
        if (scoring.dragonValues != null)
            for (int i = 0; i < scoring.dragonValues.Length; i++)
                scoring.dragonValues[i] = HonorScore;
        if (scoring.flowerValues != null)
            for (int i = 0; i < scoring.flowerValues.Length; i++)
                scoring.flowerValues[i] = HonorScore;
        if (scoring.seasonValues != null)
            for (int i = 0; i < scoring.seasonValues.Length; i++)
                scoring.seasonValues[i] = HonorScore;

        Debug.Log("ScoreTable: Applied default scores (face value for suited, fixed for honors).");
    }
}

