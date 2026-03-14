using UnityEngine;

public static class ScoreTable
{
    // Winds and dragons: 10 points each.
    public const int HonorScore = 10;

    // Flowers and seasons: static 10 points each (bonus).
    public const int BonusScore = 10;

    // Applies default scores: suited = face value (1-9), winds/dragons = 10, flowers/seasons = 10.
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

        // Winds and dragons: 10 points each.
        if (scoring.windValues != null)
            for (int i = 0; i < scoring.windValues.Length; i++)
                scoring.windValues[i] = HonorScore;
        if (scoring.dragonValues != null)
            for (int i = 0; i < scoring.dragonValues.Length; i++)
                scoring.dragonValues[i] = HonorScore;

        // Flowers and seasons: static 10 points each (bonus).
        if (scoring.flowerValues != null)
            for (int i = 0; i < scoring.flowerValues.Length; i++)
                scoring.flowerValues[i] = BonusScore;
        if (scoring.seasonValues != null)
            for (int i = 0; i < scoring.seasonValues.Length; i++)
                scoring.seasonValues[i] = BonusScore;

        Debug.Log("ScoreTable: Applied default scores (face value suited, 10 wind/dragon, 10 flower/season).");
    }
}

