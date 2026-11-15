using FitSphere.Data;
using FitSphere.Models;

namespace FitSphere.Services;

public class Replacement
{
    public Exercise Ex { get; set; }
    public string Reason { get; set; }
}

public class ExerciseService
{
    readonly AppDatabase _db;
    List<Exercise> _cache;

    public ExerciseService(AppDatabase db) => _db = db;

    public async Task<List<Exercise>> AllAsync() =>
        _cache ??= (await _db.Query<Exercise>("SELECT * FROM Exercise ORDER BY Name"));

    public static readonly string[] MuscleGroups = { "Chest", "Back", "Legs", "Shoulders", "Arms", "Core", "Cardio" };
    public static readonly string[] EquipmentTypes = { "Bodyweight", "Dumbbell", "Barbell", "Machine", "Cable" };

    /// <summary>
    /// Rule-based alternatives: same muscle group, different exercise, using equipment the user owns.
    /// Ranked by movement type (compound/isolation), then same equipment, then what the user has trained before.
    /// </summary>
    public async Task<List<Replacement>> ReplacementsAsync(Exercise src, int max = 6)
    {
        var all = await AllAsync();
        var have = new HashSet<string>(Svc.User.EquipmentList) { "Bodyweight" };

        var used = new HashSet<int>();
        var sets = await _db.Query<SetLog>(
            "SELECT s.* FROM SetLog s JOIN WorkoutSession w ON s.SessionId = w.Id WHERE w.UserId = ? AND s.Completed = 1", Svc.Uid);
        foreach (var s in sets) used.Add(s.ExerciseId);

        var result = new List<(Replacement r, int score)>();
        foreach (var e in all.Where(e => e.Id != src.Id && e.MuscleGroup == src.MuscleGroup && have.Contains(e.Equipment)))
        {
            int score = 0;
            var parts = new List<string> { $"Targets {e.MuscleGroup.ToLower()} like {src.Name}" };
            if (e.IsCompound == src.IsCompound) { score += 2; parts.Add(e.IsCompound ? "also a compound lift" : "also an isolation move"); }
            if (e.Equipment == src.Equipment) { score += 1; parts.Add($"same equipment ({e.Equipment.ToLower()})"); }
            else parts.Add($"uses {e.Equipment.ToLower()}, which is in your equipment list");
            if (used.Contains(e.Id)) { score += 1; parts.Add("you've trained it before"); }
            result.Add((new Replacement { Ex = e, Reason = string.Join(" · ", parts) }, score));
        }
        return result.OrderByDescending(x => x.score).ThenBy(x => x.r.Ex.Name).Take(max).Select(x => x.r).ToList();
    }
}
