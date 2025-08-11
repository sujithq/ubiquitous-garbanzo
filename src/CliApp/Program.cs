static int Usage()
{
    Console.WriteLine("Simple CLI sample\n");
    Console.WriteLine("Usage:");
    Console.WriteLine("  greet --name <NAME>");
    Console.WriteLine("  add --a <INT> --b <INT>");
    return 1;
}

if (args.Length == 0)
    return Usage();

switch (args[0])
{
    case "greet":
    {
        var name = GetOption("--name", "-n");
        if (string.IsNullOrWhiteSpace(name)) return Usage();
        Console.WriteLine($"Hello, {name}!");
        return 0;
    }
    case "add":
    {
        var aStr = GetOption("--a");
        var bStr = GetOption("--b");
        if (!int.TryParse(aStr, out var a) || !int.TryParse(bStr, out var b)) return Usage();
        Console.WriteLine(a + b);
        return 0;
    }
    case "--help":
    case "-h":
        return Usage();
    default:
        return Usage();
}

string? GetOption(params string[] names)
{
    foreach (var n in names)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], n, StringComparison.Ordinal))
                return args[i + 1];
        }
    }
    return null;
}
