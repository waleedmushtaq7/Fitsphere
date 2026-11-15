using FitSphere.Data;
using FitSphere.Models;

namespace FitSphere.Services;

public record Macros(double Cal, double P, double C, double F);

public record CommonFood(string Name, double Cal, double P, double C, double F);

public class NutritionService
{
    readonly AppDatabase _db;
    public NutritionService(AppDatabase db) => _db = db;

    public static readonly string[] Meals = { "Breakfast", "Lunch", "Dinner", "Snacks" };

    public static readonly CommonFood[] Common =
    {
        new("Egg (large)", 72, 6, 0.4, 5), new("Oatmeal (1 cup cooked)", 154, 6, 27, 3), new("Banana", 105, 1.3, 27, 0.4),
        new("Chicken breast (150 g)", 248, 46, 0, 5.4), new("White rice (1 cup cooked)", 205, 4.3, 45, 0.4),
        new("Roti / chapati", 104, 3, 18, 3), new("Lentils / daal (1 cup)", 230, 18, 40, 0.8),
        new("Greek yogurt (170 g)", 100, 17, 6, 0.7), new("Whey protein (1 scoop)", 120, 24, 3, 1.5),
        new("Peanut butter (2 tbsp)", 188, 8, 6, 16), new("Whole milk (1 cup)", 149, 8, 12, 8),
        new("Apple", 95, 0.5, 25, 0.3), new("Almonds (28 g)", 164, 6, 6, 14), new("Tuna (1 can)", 120, 26, 0, 1),
        new("Salmon (150 g)", 312, 33, 0, 19), new("Whole-wheat bread (slice)", 81, 4, 14, 1.1),
        new("Potato (medium, boiled)", 130, 3, 30, 0.2), new("Beef mince (150 g, lean)", 300, 33, 0, 18),
    };

    public static Macros Totals(IEnumerable<FoodEntry> entries) =>
        new(entries.Sum(e => e.Calories), entries.Sum(e => e.Protein), entries.Sum(e => e.Carbs), entries.Sum(e => e.Fat));

    public async Task<List<FoodEntry>> DayAsync(DateTime day) =>
        (await _db.ForUser<FoodEntry>(Svc.Uid)).Where(f => f.Date.Date == day.Date).OrderBy(f => f.Id).ToList();

    public async Task<List<FoodEntry>> RangeAsync(DateTime from, DateTime to) =>
        (await _db.ForUser<FoodEntry>(Svc.Uid)).Where(f => f.Date.Date >= from.Date && f.Date.Date <= to.Date).ToList();

    public Task SaveAsync(FoodEntry e)
    {
        e.UserId = Svc.Uid;
        e.Date = e.Date.Date;
        return _db.Save(e);
    }

    public Task DeleteAsync(FoodEntry e) => _db.Delete(e);

    /// <summary>Most recent distinct foods the user has logged (for quick re-adding).</summary>
    public async Task<List<FoodEntry>> RecentAsync(int max = 8) =>
        (await _db.ForUser<FoodEntry>(Svc.Uid)).OrderByDescending(f => f.Id)
            .GroupBy(f => f.Name.ToLowerInvariant()).Select(g => g.First()).Take(max).ToList();

    /// <summary>
    /// Estimate daily targets from the profile: Mifflin-St Jeor BMR (+5 for men, -161 for women) x activity by experience,
    /// then adjusted for the goal. If sex is not set (older accounts) the midpoint (-78) is used. Users can edit every value afterwards.
    /// </summary>
    public void ApplyRecommended(User u)
    {
        double sexOffset = u.Sex == "Male" ? 5 : u.Sex == "Female" ? -161 : -78;
        double bmr = 10 * u.WeightKg + 6.25 * u.HeightCm - 5 * u.Age + sexOffset;
        double activity = u.Experience switch { "Advanced" => 1.55, "Intermediate" => 1.45, _ => 1.375 };
        double adj = u.Goal switch { "Weight Loss" => -400, "Muscle Building" => 300, "Strength" => 150, _ => 0 };
        int cal = (int)(Math.Round((bmr * activity + adj) / 10.0) * 10);
        cal = Math.Clamp(cal, 1200, 5000);
        double protPerKg = (u.Goal == "Weight Loss" || u.Goal == "Muscle Building") ? 2.0 : 1.6;
        int protein = (int)Math.Round(u.WeightKg * protPerKg);
        int fat = (int)Math.Round(cal * 0.25 / 9);
        int carbs = Math.Max(50, (int)Math.Round((cal - protein * 4 - fat * 9) / 4.0));
        u.CalorieTarget = cal; u.ProteinTarget = protein; u.FatTarget = fat; u.CarbTarget = carbs;
        u.WaterTargetMl = Math.Clamp((int)(Math.Round(u.WeightKg * 35 / 250.0) * 250), 1500, 5000);
    }
}
