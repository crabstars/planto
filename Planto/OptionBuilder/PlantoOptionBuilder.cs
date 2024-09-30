namespace Planto.OptionBuilder;

public class PlantoOptionBuilder
{
    private readonly PlantoOptions _options = new();

    public PlantoOptionBuilder SetValueGeneration(ValueGeneration valueGeneration)
    {
        _options.ValueGeneration = valueGeneration;
        return this;
    }

    public PlantoOptionBuilder SetMaxDegreeOfParallelism(int maxDegreeOfParallelism)
    {
        _options.MaxDegreeOfParallelism = maxDegreeOfParallelism;
        return this;
    }
    
    /// <summary>
    /// If tableSchema is not specified (null or empty), INFORMATION_SCHEMA will include all schemas
    /// </summary>
    /// <param name="tableSchema"></param>
    /// <returns></returns>
    public PlantoOptionBuilder SetDefaultSchema(string tableSchema)
    {
        _options.TableSchema = tableSchema;
        return this;
    }

    public PlantoOptions Build()
    {
        return _options;
    }
}