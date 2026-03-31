namespace LuiTool.Cli;

sealed class CliApp
{
    private readonly Dictionary<string, CliCommand> commands;
    private readonly string name;

    public CliApp(string name)
    {
        this.name = name;
        commands = new Dictionary<string, CliCommand>(StringComparer.OrdinalIgnoreCase);
    }

    public void Register(CliCommand command)
    {
        if (!commands.TryAdd(command.Name, command))
            throw new InvalidOperationException($"command '{command.Name}' is already registered");
    }

    public int Run(string[] args)
    {
        if (args.Length == 0)
        {
            PrintHelp();
            return 1;
        }

        if (IsHelpToken(args[0]))
        {
            if (args.Length > 1 && commands.TryGetValue(args[1], out var helpCommand))
            {
                PrintCommandHelp(helpCommand);
                return 0;
            }

            PrintHelp();
            return 0;
        }

        if (!commands.TryGetValue(args[0], out var command))
        {
            Console.Error.WriteLine($"unknown command '{args[0]}'");
            Console.Error.WriteLine();
            PrintHelp();
            return 1;
        }

        var commandArgs = args.Skip(1).ToArray();

        if (commandArgs.Length == 1 && IsHelpToken(commandArgs[0]))
        {
            PrintCommandHelp(command);
            return 0;
        }

        if (!command.TryBind(commandArgs, out var invocation, out var error))
        {
            Console.Error.WriteLine(error);
            Console.Error.WriteLine();
            PrintCommandHelp(command);
            return 1;
        }

        try
        {
            return command.Handler(invocation!);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private void PrintHelp()
    {
        Console.WriteLine(name);
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  luitool <command> [arguments]");
        Console.WriteLine();
        Console.WriteLine("Commands:");

        foreach (var command in commands.Values.OrderBy(command => command.Name))
        {
            Console.WriteLine($"  {command.GetUsage()}");
            Console.WriteLine($"    {command.Description}");
        }

        Console.WriteLine();
        Console.WriteLine("Run 'luitool help <command>' or 'luitool <command> --help' for command details.");
    }

    private static void PrintCommandHelp(CliCommand command)
    {
        Console.WriteLine($"Usage: luitool {command.GetUsage()}");
        Console.WriteLine();
        Console.WriteLine(command.Description);

        if (command.Arguments.Count == 0)
            return;

        Console.WriteLine();
        Console.WriteLine("Arguments:");

        foreach (var argument in command.Arguments)
        {
            var suffix = argument.Required ? "required" : "optional";
            Console.WriteLine($"  {argument.Name} ({suffix})");
            Console.WriteLine($"    {argument.Description}");
        }
    }

    private static bool IsHelpToken(string value)
    {
        return value is "help" or "--help" or "-h";
    }
}