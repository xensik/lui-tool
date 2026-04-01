namespace LuiTool.Cli;

sealed class CliArgument
{
    public string Name { get; }
    public string Description { get; }
    public bool Required { get; }

    public CliArgument(string name, string description, bool required = true)
    {
        Name = name;
        Description = description;
        Required = required;
    }
}