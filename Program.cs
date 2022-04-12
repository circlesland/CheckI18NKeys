using System.CommandLine;

namespace CheckI18NKeys;

public static class Program
{
    static async Task Main(string[] args)
    {
        var o1 = new Option<string>(new[] {"--source-dir", "-src"},
            "The source code directory of the svelte-i18n app to examine") {IsRequired = true};
        var o2 = new Option<string>(new[] {"--master-json", "-json"},
            "The path to an authoritative json i18n file that contains all keys") {IsRequired = true};
        var o3 = new Option<bool?>(new[] {"--fix"},
            "Applies all suggested fixes if set") {IsRequired = false};
        
        var rootCommand = new RootCommand {o1, o2, o3};

        rootCommand.SetHandler(
            (string sourceDir, string masterJson, bool? fix) => Run(sourceDir, masterJson, fix),
            o1,
            o2,
            o3);

        await rootCommand.InvokeAsync(args);
    }

    private static void Run(string sourceDir, string masterJsonFile, bool? fix)
    {
        var i18NKeyUsages = Find.I18NKeyUsagesInCode(sourceDir);

        var occurrencesByKey = i18NKeyUsages
            .GroupBy(o => o.Key)
            .ToDictionary(o => o.Key, o => o.ToArray());

        var expectedI18NKeys = Find.ExpectedKeysInMasterJson(masterJsonFile);

        var undefinedKeys = occurrencesByKey
            .Where(o => !expectedI18NKeys.Contains(o.Key))
            .ToDictionary(o => o.Key, o => o.Value);

        var unusedKeys = expectedI18NKeys
            .Where(o => !occurrencesByKey.ContainsKey(o))
            .ToArray();

        Display.UndefinedKeys(undefinedKeys, sourceDir);

        var suggestedFixesByFile = Find.SuggestedFixes(undefinedKeys, unusedKeys)
            .GroupBy(o => o.File)
            .ToDictionary(o => o.Key, o => o.ToArray());

        Console.WriteLine();
        Display.Suggestions(suggestedFixesByFile, sourceDir);

        if (fix == true)
        {
            Console.WriteLine("Applying all suggested fixes ...");
            Apply.Suggestions(suggestedFixesByFile);
        }
        
        Console.WriteLine("Fin");
    }
}