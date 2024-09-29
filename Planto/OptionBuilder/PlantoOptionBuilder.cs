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

    public PlantoOptions Build()
    {
        return _options;
    }
}