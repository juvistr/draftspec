namespace DraftSpec.Cli;

/// <summary>
/// Parses command-line arguments into CliOptions.
/// Extracted for testability.
/// </summary>
public static class CliOptionsParser
{
    public static CliOptions Parse(string[] args)
    {
        var options = new CliOptions();
        var positional = new List<string>();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (arg is "--help" or "-h" or "help")
            {
                options.ShowHelp = true;
            }
            else if (arg is "--format" or "-f")
            {
                if (i + 1 >= args.Length)
                {
                    options.Error = "--format requires a value (console, json, markdown, html)";
                    return options;
                }

                options.Format = args[++i].ToLowerInvariant();
            }
            else if (arg is "--output" or "-o")
            {
                if (i + 1 >= args.Length)
                {
                    options.Error = "--output requires a file path";
                    return options;
                }

                options.OutputFile = args[++i];
            }
            else if (arg == "--css-url")
            {
                if (i + 1 >= args.Length)
                {
                    options.Error = "--css-url requires a URL";
                    return options;
                }

                options.CssUrl = args[++i];
            }
            else if (arg == "--force")
            {
                options.Force = true;
            }
            else if (arg is "--parallel" or "-p")
            {
                options.Parallel = true;
            }
            else if (arg == "--no-cache")
            {
                options.NoCache = true;
            }
            else if (arg is "--bail" or "-b")
            {
                options.Bail = true;
            }
            else if (!arg.StartsWith('-'))
            {
                positional.Add(arg);
            }
            else
            {
                options.Error = $"Unknown option: {arg}";
                return options;
            }
        }

        if (positional.Count > 0)
            options.Command = positional[0].ToLowerInvariant();
        if (positional.Count > 1)
        {
            // For 'new' command, second arg is the spec name
            if (options.Command == "new")
                options.SpecName = positional[1];
            else
                options.Path = positional[1];
        }

        if (positional.Count > 2 && options.Command == "new")
            options.Path = positional[2];

        return options;
    }
}
