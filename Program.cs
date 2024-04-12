using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

BenchmarkRunner.Run<Benchmarks>();

/// <summary>
///     | Method                | Rows  | Mean        | Error     | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
///     |---------------------- |------ |------------:|----------:|----------:|------:|--------:|--------:|----------:|------------:|
///     | Microsoft_Data_Sqlite | 30    |    27.12 us |  0.119 us |  0.099 us |  0.09 |    0.00 |  0.1221 |   2.02 KB |        0.09 |
///     | System_Data_Sqlite    | 30    |   289.44 us |  2.776 us |  2.461 us |  1.00 |    0.00 |  0.9766 |   23.2 KB |        1.00 |
///     |                       |       |             |           |           |       |         |         |           |             |
///     | Microsoft_Data_Sqlite | 1000  |   203.00 us |  4.043 us |  4.965 us |  0.47 |    0.01 |  2.4414 |  39.91 KB |        0.65 |
///     | System_Data_Sqlite    | 1000  |   432.66 us |  6.345 us |  5.625 us |  1.00 |    0.00 |  2.9297 |  61.09 KB |        1.00 |
///     |                       |       |             |           |           |       |         |         |           |             |
///     | Microsoft_Data_Sqlite | 10000 | 1,869.34 us | 35.607 us | 42.387 us |  1.15 |    0.03 | 23.4375 | 391.47 KB |        0.95 |
///     | System_Data_Sqlite    | 10000 | 1,628.18 us | 13.814 us | 12.921 us |  1.00 |    0.00 | 23.4375 | 412.68 KB |        1.00 |
/// </summary>
[MemoryDiagnoser]
public class Benchmarks
{
    private const string Path = "benchmark.db";

    [Params(30, 1000, 10_000)]
    public int Rows { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        File.Delete(Path);
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={Path}");
        connection.Open();

        using var command = new Microsoft.Data.Sqlite.SqliteCommand(@"CREATE TABLE data (id INT PRIMARY KEY, text TEXT)", connection);
        command.ExecuteNonQuery();

        command.CommandText = "INSERT INTO data (id, text) VALUES (@id, @text)";
        var idParam = new Microsoft.Data.Sqlite.SqliteParameter { ParameterName = "id" };
        var textParam = new Microsoft.Data.Sqlite.SqliteParameter { ParameterName = "text" };
        command.Parameters.Add(idParam);
        command.Parameters.Add(textParam);

        for (var i = 0; i < Rows; i++)
        {
            idParam.Value = i;
            textParam.Value = "Text " + i;
            command.ExecuteNonQuery();
        }
    }

    [Benchmark]
    public int Microsoft_Data_Sqlite()
    {
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={Path}");
        connection.Open();
        using var command = new Microsoft.Data.Sqlite.SqliteCommand("SELECT * FROM data", connection);
        using var reader = command.ExecuteReader();

        var sum = 0;
        while (reader.Read())
        {
            sum += reader.GetInt32(0) + reader.GetString(1).Length;
        }

        return sum;
    }

    [Benchmark(Baseline = true)]
    public int System_Data_Sqlite()
    {
        using var connection = new System.Data.SQLite.SQLiteConnection($"Data Source={Path}");
        connection.Open();
        using var command = new System.Data.SQLite.SQLiteCommand("SELECT * FROM data", connection);
        using var reader = command.ExecuteReader();

        var sum = 0;
        while (reader.Read())
        {
            sum += reader.GetInt32(0) + reader.GetString(1).Length;
        }

        return sum;
    }
}