using UnityEngine;
using Random = UnityEngine.Random;


// Example score table that assigns random values in a fixed range
// to every tile type on startup. Used to quickly get non-zero
// scoring behavior without hand-tuning each tile.

public static class ScoreTable
{
    // Inclusive range for all tile scores.
    public const int MinScore = 7;
    public const int MaxScore = 12;

    
    // Applies random scores in [MinScore, MaxScore] to all tile
    // value arrays on the provided ScoringManager instance.
    
    public static void ApplyRandomScores(ScoringManager scoring)
    {
        if (scoring == null)
        {
            Debug.LogError("ScoreTable.ApplyRandomScores: ScoringManager is null.");
            return;
        }

        // Suited tiles (Dots, Bam, Crack) values 1-9.
        for (int v = 1; v <= 9; v++)
        {
            if (scoring.dotsValues != null && scoring.dotsValues.Length > v)
                scoring.dotsValues[v] = Random.Range(MinScore, MaxScore + 1);

            if (scoring.bamValues != null && scoring.bamValues.Length > v)
                scoring.bamValues[v] = Random.Range(MinScore, MaxScore + 1);

            if (scoring.crackValues != null && scoring.crackValues.Length > v)
                scoring.crackValues[v] = Random.Range(MinScore, MaxScore + 1);
        }

        // Winds.
        if (scoring.windValues != null)
        {
            for (int i = 0; i < scoring.windValues.Length; i++)
                scoring.windValues[i] = Random.Range(MinScore, MaxScore + 1);
        }

        // Dragons.
        if (scoring.dragonValues != null)
        {
            for (int i = 0; i < scoring.dragonValues.Length; i++)
                scoring.dragonValues[i] = Random.Range(MinScore, MaxScore + 1);
        }

        // Flowers.
        if (scoring.flowerValues != null)
        {
            for (int i = 0; i < scoring.flowerValues.Length; i++)
                scoring.flowerValues[i] = Random.Range(MinScore, MaxScore + 1);
        }

        // Seasons.
        if (scoring.seasonValues != null)
        {
            for (int i = 0; i < scoring.seasonValues.Length; i++)
                scoring.seasonValues[i] = Random.Range(MinScore, MaxScore + 1);
        }

        Debug.Log("ScoreTable: Applied random scores to all tile types.");
    }
}

