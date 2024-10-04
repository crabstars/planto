namespace Planto.OptionBuilder;

public class PlantoOptions
{
    public ValueGeneration ValueGeneration { get; set; } = ValueGeneration.Default;

    /// <summary>
    /// how many max parallel sql connections should the library allowed to make to the database
    /// </summary>
    public int? MaxDegreeOfParallelism { get; set; }

    public string? TableSchema { get; set; }

    public bool ColumnCheckValueGenerator { get; set; } = true;
}