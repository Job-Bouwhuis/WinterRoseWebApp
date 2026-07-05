using WinterRose.CommandLine.Definitions;

namespace WinterRose.CommandLine.Definitions;

public sealed class ProgramArgumentBuilder
{
    private readonly ArgumentRegistry valueRegistry = new();
    private readonly ArgumentValidationRegistry validationRegistry = new();
    private Action<ArgumentRegistry, ArgumentValidationRegistry> buildSetter;

    public ProgramArgumentBuilder Register(ArgumentDefinition argDef)
    {
        valueRegistry.Register(argDef);
        return this;
    }
    
    public void Build()
        => buildSetter(valueRegistry, validationRegistry);

    public void DefineRegistryReference(Action<ArgumentRegistry, ArgumentValidationRegistry> buildSetter)
    {
        this.buildSetter = buildSetter;
    }

    internal ArgumentRuleBuilder GetRuleBuilderFor(string longName) => validationRegistry.RulesFor(longName);
}