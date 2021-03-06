using System.Collections.Immutable;

namespace CheckI18NKeys;

public static class Display
{
    public static void UndefinedKeys(Dictionary<string, KeyUsage[]> undefinedKeysByFile, string sourceDir)
    {
        Console.WriteLine($"== {sourceDir} ==");
        Console.WriteLine($"Found {undefinedKeysByFile.SelectMany(o => o.Value).Count()} undefined i18n keys in {undefinedKeysByFile.Count} files:");
        
        Formatted(undefinedKeysByFile, sourceDir);
    }

    public static void Suggestions(Dictionary<string, SuggestedKeyUsage[]> suggestedFixesByFile, string sourceDir)
    {
        Console.WriteLine($"== {sourceDir} ==");
        Console.WriteLine($"Found {suggestedFixesByFile.SelectMany(o => o.Value).Count()} suggestions in {suggestedFixesByFile.Count} files:");
        
        Formatted(suggestedFixesByFile, sourceDir);
    }

    private static void Formatted<TRecord>(Dictionary<string, TRecord[]> usagesByFile, string sourceDir)
        where TRecord : KeyUsage
    {
        foreach (var file in usagesByFile)
        {
            Console.WriteLine($"* {file.Key.Replace(sourceDir, "")}:");
            
            var occurrencesByLine = file.Value
                .GroupBy(o => o.Line)
                .ToImmutableSortedDictionary(o => o.Key, o => o.ToArray());
            
            foreach (var line in occurrencesByLine)
            {
                if (line.Value.Length > 1)
                {
                    var key = $"   - [{line.Key}:";
                    Console.WriteLine(key);

                    foreach (var occurence in line.Value)
                    {
                        var k = $"{" ".PadRight(key.Length)}{occurence.Column}]";
                        Console.WriteLine($"{k.PadRight(16, ' ')}{occurence.Key}");
                        if (occurence is SuggestedKeyUsage suggestedI18NKeyUsage)
                        {
                            Console.WriteLine($"{k.PadRight(16, ' ')}{suggestedI18NKeyUsage.SuggestedKey}");
                        }
                    }
                }
                else
                {
                    var occurence = line.Value[0];
                    var address = $"[{occurence.Line}:{occurence.Column}]".PadRight(10, ' ');
                    Console.WriteLine($"   - {address} {occurence.Key}");
                    if (occurence is SuggestedKeyUsage suggestedI18NKeyUsage)
                    {
                        Console.WriteLine($"     {" ".PadRight(address.Length)} {suggestedI18NKeyUsage.SuggestedKey}");
                    }
                }
            }
        }
    }
}