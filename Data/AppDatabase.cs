using FitSphere.Models;
using SQLite;

namespace FitSphere.Data;

/// <summary>Data layer: owns the SQLite connection and exposes small generic helpers.</summary>
public class AppDatabase
{
    SQLiteAsyncConnection _db;
    readonly SemaphoreSlim _gate = new(1, 1);
    bool _ready;

    public async Task EnsureAsync()
    {
        if (_ready) return;
        await _gate.WaitAsync();
        try
        {
            if (_ready) return;
            var path = System.IO.Path.Combine(FileSystem.AppDataDirectory, "fitsphere.db3");
            _db = new SQLiteAsyncConnection(path, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
            await _db.CreateTablesAsync(CreateFlags.None,
                typeof(User), typeof(Exercise), typeof(WorkoutPlan), typeof(PlanExercise),
                typeof(WorkoutSession), typeof(SetLog), typeof(FoodEntry), typeof(WaterLog),
                typeof(BodyRecord), typeof(RestDay));
            if (await _db.Table<Exercise>().CountAsync() == 0)
                await _db.InsertAllAsync(SeedData.Exercises());
            _ready = true;
        }
        finally { _gate.Release(); }
    }

    public async Task<List<T>> Query<T>(string sql, params object[] args) where T : new()
    {
        await EnsureAsync();
        return await _db.QueryAsync<T>(sql, args);
    }

    public Task<List<T>> ForUser<T>(int userId) where T : new() =>
        Query<T>($"SELECT * FROM {typeof(T).Name} WHERE UserId = ?", userId);

    public async Task<T> Get<T>(int id) where T : new()
    {
        var rows = await Query<T>($"SELECT * FROM {typeof(T).Name} WHERE Id = ?", id);
        return rows.FirstOrDefault();
    }

    /// <summary>Insert when Id == 0, otherwise replace.</summary>
    public async Task Save<T>(T item)
    {
        await EnsureAsync();
        await _db.InsertOrReplaceAsync(item);
    }

    public async Task Delete<T>(T item)
    {
        await EnsureAsync();
        await _db.DeleteAsync(item);
    }

    public async Task Exec(string sql, params object[] args)
    {
        await EnsureAsync();
        await _db.ExecuteAsync(sql, args);
    }
}
