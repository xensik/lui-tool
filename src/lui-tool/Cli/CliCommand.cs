namespace LuiTool.Cli;

sealed class CliInvocation
{
    private readonly IReadOnlyDictionary<string, string?> arguments;

    public CliInvocation(IReadOnlyDictionary<string, string?> arguments)
    {
        this.arguments = arguments;
    }

    public string GetRequired(string name)
    {
        if (!arguments.TryGetValue(name, out var value) || string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"missing required argument '{name}'");

        return value;
    }

    public string? GetOptional(string name)
    {
        return arguments.TryGetValue(name, out var value) ? value : null;
    }
}

delegate int CliCommandHandler(CliInvocation invocation);

sealed class CliCommand
{
    public string Name { get; }
    public string Description { get; }
    public IReadOnlyList<CliArgument> Arguments { get; }
    public CliCommandHandler Handler { get; }

    public CliCommand(string name, string description, IReadOnlyList<CliArgument> arguments, CliCommandHandler handler)
    {
        Name = name;
        Description = description;
        Arguments = arguments;
        Handler = handler;
    }

    public bool TryBind(string[] values, out CliInvocation? invocation, out string? error)
    {
        var requiredCount = Arguments.Count(argument => argument.Required);

        if (values.Length < requiredCount)
        {
            invocation = null;
            error = $"missing required arguments for command '{Name}'";
            return false;
        }

        if (values.Length > Arguments.Count)
        {
            invocation = null;
            error = $"too many arguments for command '{Name}'";
            return false;
        }

        var boundArguments = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < Arguments.Count; index++)
        {
            boundArguments[Arguments[index].Name] = index < values.Length ? values[index] : null;
        }

        invocation = new CliInvocation(boundArguments);
        error = null;
        return true;
    }

    public string GetUsage()
    {
        if (Arguments.Count == 0)
            return Name;

        var parts = Arguments.Select(argument => argument.Required ? $"<{argument.Name}>" : $"[{argument.Name}]");
        return $"{Name} {string.Join(' ', parts)}";
    }
}