using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildScript
{
    private static readonly string Eol = Environment.NewLine;
    private const bool defaultDeveloperBuild = false;
    private const string defaultCustomName = "SEEP";

    public static void Build()
    {
        var options = GetValidatedOptions();

        var buildTarget = (BuildTarget)Enum.Parse(typeof(BuildTarget), options["buildTarget"]);
        Build(buildTarget, bool.Parse(options["developmentBuild"]));
    }

    private static Dictionary<string, string> GetValidatedOptions()
    {
        ParseCommandLineArguments(out Dictionary<string, string> validatedOptions);
        if (!validatedOptions.TryGetValue("projectPath", out _))
        {
            Console.WriteLine("Missing argument -projectPath");
            EditorApplication.Exit(110);
        }

        if (!validatedOptions.TryGetValue("buildTarget", out var buildTarget))
        {
            Console.WriteLine("Missing argument -buildTarget");
            EditorApplication.Exit(120);
        }

        if (!Enum.IsDefined(typeof(BuildTarget), buildTarget ?? string.Empty))
        {
            Console.WriteLine($"{buildTarget} is not a defined {nameof(BuildTarget)}");
            EditorApplication.Exit(121);
        }

        if (!validatedOptions.TryGetValue("developerBuild", out var developerBuild))
        {
            Console.WriteLine($"Missing argument -developerBuild, defaulting to {defaultDeveloperBuild}.");
            validatedOptions.Add("developerBuild", defaultDeveloperBuild.ToString());
        }
        else if (developerBuild == "")
        {
            Console.WriteLine($"Invalid argument -developerBuild, defaulting to {defaultDeveloperBuild}.");
            validatedOptions.Add("developerBuild", defaultDeveloperBuild.ToString());
        }

        if (!validatedOptions.TryGetValue("customBuildPath", out _))
        {
            Console.WriteLine("Missing argument -customBuildPath");
            EditorApplication.Exit(130);
        }

        if (!validatedOptions.TryGetValue("customBuildName", out var customBuildName))
        {
            Console.WriteLine($"Missing argument -customBuildName, defaulting to {defaultCustomName}.");
            validatedOptions.Add("customBuildName", defaultCustomName);
        }
        else if (customBuildName == "")
        {
            Console.WriteLine($"Invalid argument -customBuildName, defaulting to {defaultCustomName}.");
            validatedOptions.Add("customBuildName", defaultCustomName);
        }

        return validatedOptions;
    }

    private static void ParseCommandLineArguments(out Dictionary<string, string> providedArguments)
    {
        providedArguments = new Dictionary<string, string>();
        string[] args = Environment.GetCommandLineArgs();

        Console.WriteLine(
            $"{Eol}" +
            $"###########################{Eol}" +
            $"#    Parsing settings     #{Eol}" +
            $"###########################{Eol}" +
            $"{Eol}"
        );

        // Extract flags with optional values
        for (int current = 0, next = 1; current < args.Length; current++, next++)
        {
            // Parse flag
            bool isFlag = args[current].StartsWith("-");
            if (!isFlag) continue;
            string flag = args[current].TrimStart('-');

            // Parse optional value
            bool flagHasValue = next < args.Length && !args[next].StartsWith("-");
            string value = flagHasValue ? args[next].TrimStart('-') : "";
            string displayValue = "\"" + value + "\"";

            // Assign
            Console.WriteLine($"Found flag \"{flag}\" with value {displayValue}.");
            providedArguments.Add(flag, value);
        }
    }

    private static void Build(BuildTarget buildTarget, bool isDevelopment)
    {
        var options = new BuildPlayerOptions()
        {
            options = BuildOptions.CompressWithLz4HC,
            scenes = new [] {"SampleScene"},
            target = buildTarget
        };
        if (isDevelopment)
            options.options &= BuildOptions.Development;

        var summary = BuildPipeline.BuildPlayer(options)
            .summary;
        ReportSummary(summary);
        ExitWithResult(summary.result);
    }

    private static void ReportSummary(BuildSummary summary)
    {
        Console.WriteLine(
            $"{Eol}" +
            $"###########################{Eol}" +
            $"#      Build results      #{Eol}" +
            $"###########################{Eol}" +
            $"{Eol}" +
            $"Duration: {summary.totalTime.ToString()}{Eol}" +
            $"Warnings: {summary.totalWarnings.ToString()}{Eol}" +
            $"Errors: {summary.totalErrors.ToString()}{Eol}" +
            $"Size: {summary.totalSize.ToString()} bytes{Eol}" +
            $"{Eol}"
        );
    }

    private static void ExitWithResult(BuildResult result)
    {
        switch (result)
        {
            case BuildResult.Succeeded:
                Console.WriteLine("Build succeeded!");
                EditorApplication.Exit(0);
                break;
            case BuildResult.Failed:
                Console.WriteLine("Build failed!");
                EditorApplication.Exit(101);
                break;
            case BuildResult.Cancelled:
                Console.WriteLine("Build cancelled!");
                EditorApplication.Exit(102);
                break;
            case BuildResult.Unknown:
            default:
                Console.WriteLine("Build result is unknown!");
                EditorApplication.Exit(103);
                break;
        }
    }
}