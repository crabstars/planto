namespace Planto.OptionBuilder;

public class PlantoOptions
{
    public ValueGeneration ValueGeneration { get; set; } = ValueGeneration.Default;

    /// <summary>
    /// how many max parallel sql connections should the libart allowed to make to the database
    /// </summary>
    public int? MaxDegreeOfParallelism { get; set; }
}