namespace Planto.OptionBuilder;

public class PlantoOptionBuilder
{
    private readonly PlantoOptions _options = new PlantoOptions();

    public PlantoOptionBuilder SetValueGeneration(ValueGeneration valueGeneration)
    {
        _options.ValueGeneration = valueGeneration;
        return this;
    }

    public PlantoOptions Build()
    {
        return _options;
    }
}