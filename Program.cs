using System.CommandLine;
using Newtonsoft.Json;

namespace CheckI18NKeys;

public static class Program
{
    static async Task<int> Main(string[] args)
    {
        var o1 = new Option<string>(new[] {"--source-dir", "-src"},
            "The source code directory of the svelte-i18n app to examine.") {IsRequired = true};

        var o2 = new Option<string>(new[] {"--master-json", "-master"},
            "The path to an authoritative json i18n file that contains all keys.") {IsRequired = true};

        var o3 = new Option<string>(new[] {"--file-types", "-types"},
            "A comma separated list of file types to include. Options are: " +
            $"{string.Join(",", Find.KeyExtractors.Select(o => o.Name))}") {IsRequired = true};

        var o4 = new Option<string?>(new[] {"--default-language-prefix", "-lang"},
            "The language of the master json file followed by a dot. Example: 'en.' or 'de.'") {IsRequired = false};

        var o5 = new Option<bool?>(new[] {"--fix"},
            "Applies all suggested fixes if set.") {IsRequired = false};

        var o6 = new Option<bool?>(new[] {"--output-json", "-json"},
            "Output a json object with the results when the process finished.") {IsRequired = false};

        var rootCommand = new RootCommand {o1, o2, o3, o4, o5, o6};

        rootCommand.SetHandler((
                    string sourceDir,
                    string masterJson,
                    string fileTypes,
                    string? defaultLanguagePrefix,
                    bool? fix,
                    bool? json)
                => Task.FromResult(Run(sourceDir, masterJson, fileTypes, defaultLanguagePrefix, fix, json)),
            o1,
            o2,
            o3,
            o4,
            o5,
            o6);

        return await rootCommand.InvokeAsync(args);
    }

    private static int Run(
        string sourceDir,
        string masterJsonFile,
        string fileTypes,
        string? defaultLanguagePrefix,
        bool? fix,
        bool? json)
    {
        var chosenFileTypes = ValidateInputs(sourceDir, masterJsonFile, defaultLanguagePrefix, fileTypes);
        var i18NKeyUsages = Find.I18NKeyUsagesInCode(sourceDir, chosenFileTypes);

        var occurrencesByKey = i18NKeyUsages
            .GroupBy(o => o.Key)
            .ToDictionary(o => o.Key, o => o.ToArray());

        var expectedI18NKeys = Find.ExpectedKeysInMasterJson(masterJsonFile, defaultLanguagePrefix);

        var unusedKeys = expectedI18NKeys
            .Where(o => !occurrencesByKey.ContainsKey(o))
            .ToArray();

        var undefinedKeys = occurrencesByKey
            .Where(o => !expectedI18NKeys.Contains(o.Key))
            .ToDictionary(o => o.Key, o => o.Value);

        var suggestedFixesByFile = Find.SuggestedFixes(undefinedKeys, unusedKeys)
            .GroupBy(o => o.File)
            .ToDictionary(o => o.Key, o => o.ToArray());

        if (fix.HasValue && fix.Value)
        {
            Apply.Suggestions(suggestedFixesByFile);
        }

        PrintResult(sourceDir, fix, json, undefinedKeys, suggestedFixesByFile);

        return undefinedKeys.Count > 0
            ? 99
            : 0;
    }

    private static void PrintResult(
        string sourceDir, 
        bool? fix, 
        bool? json,
        Dictionary<string, KeyUsage[]> undefinedKeys,
        Dictionary<string, SuggestedKeyUsage[]> suggestedFixesByFile)
    {
        var undefinedKeysByFile = undefinedKeys
            .SelectMany(o => o.Value)
            .GroupBy(o => o.File)
            .ToDictionary(o => o.Key, o => o.ToArray());

        if (!json.HasValue || !json.Value)
        {
            Display.UndefinedKeys(undefinedKeysByFile, sourceDir);
            Console.WriteLine();
            Display.Suggestions(suggestedFixesByFile, sourceDir);

            if (fix.HasValue && fix.Value)
            {
                Console.WriteLine();
                Console.WriteLine($"APPLIED ALL SUGGESTIONS IN {suggestedFixesByFile.Count} FILES");
            }
        }
        else
        {
            var jsonResult = JsonConvert.SerializeObject(new
            {
                UndefinedKeys = undefinedKeysByFile,
                Suggestions = suggestedFixesByFile,
                AppliedSuggestions = fix.HasValue && fix.Value
            }, Formatting.Indented);

            Console.WriteLine(jsonResult);
        }
    }

    private static HashSet<string> ValidateInputs(string sourceDir, string masterJsonFile,
        string? defaultLanguagePrefix, string fileTypes)
    {
        var availableFileTypes = Find.KeyExtractors.Select(o => o.Name).ToHashSet();
        var chosenFileTypes = fileTypes
            .Split(",", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Intersect(availableFileTypes)
            .ToHashSet();

        var exceptions = new List<Exception>();
        if (chosenFileTypes.Count == 0)
        {
            exceptions.Add(new ArgumentException(
                $"You must specify at least one of the following file types: {string.Join(",", availableFileTypes)}"));
        }

        if (!File.Exists(masterJsonFile))
        {
            exceptions.Add(
                new FileNotFoundException($"Couldn't find the specified master json file at: {masterJsonFile}"));
        }

        if (!Directory.Exists(sourceDir))
        {
            exceptions.Add(new FileNotFoundException($"Couldn't find the specified source directory at: {sourceDir}"));
        }

        if (defaultLanguagePrefix != null && !defaultLanguagePrefix.EndsWith("."))
        {
            exceptions.Add(new ArgumentException(
                $"The default language prefix must end with a dot '.'. You wrote: {defaultLanguagePrefix}"));
        }

        if (exceptions.Count > 0)
        {
            throw new AggregateException(exceptions);
        }

        return chosenFileTypes;
    }
}