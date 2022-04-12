namespace CheckI18NKeys;

public static class Display
{
    public static void UndefinedKeys(Dictionary<string, I18NKeyUsage[]> undefinedKeysByFile, string sourceDir)
    {
        Console.WriteLine($"Found {undefinedKeysByFile.Count} undefined i18n keys in {sourceDir}");
        Console.WriteLine("=================================================================");
        
        Print(undefinedKeysByFile, sourceDir);
    }

    public static void Suggestions(Dictionary<string, SuggestedI18NKeyUsage[]> suggestedFixesByFile, string sourceDir)
    {
        Console.WriteLine($"Found suggestions for {suggestedFixesByFile.Count} files:");
        Console.WriteLine("=================================================================");
        
        Print(suggestedFixesByFile, sourceDir);
    }

    private static void Print<TRecord>(Dictionary<string, TRecord[]> suggestedFixesByFile, string sourceDir)
        where TRecord : I18NKeyUsage
    {
        foreach (var file in suggestedFixesByFile)
        {
            Console.WriteLine($"* {file.Key.Replace(sourceDir, "")}:");
            var occurrencesByLine = file.Value.GroupBy(o => o.Line);
            foreach (var line in occurrencesByLine)
            {
                if (line.Count() > 1)
                {
                    var key = $"   - [{line.Key}:";
                    Console.WriteLine(key);

                    foreach (var occurence in line)
                    {
                        var k = $"{" ".PadRight(key.Length)}{occurence.Column}]";
                        Console.WriteLine($"{k.PadRight(16, ' ')}{occurence.Key}");
                        if (occurence is SuggestedI18NKeyUsage suggestedI18NKeyUsage)
                        {
                            Console.WriteLine($"{k.PadRight(16, ' ')}{suggestedI18NKeyUsage.SuggestedKey}");
                        }
                    }
                }
                else
                {
                    var occurence = line.First();
                    var address = $"[{occurence.Line}:{occurence.Column}]".PadRight(10, ' ');
                    Console.WriteLine($"   - {address} {occurence.Key}");
                    if (occurence is SuggestedI18NKeyUsage suggestedI18NKeyUsage)
                    {
                        Console.WriteLine($"     {" ".PadRight(address.Length)} {suggestedI18NKeyUsage.SuggestedKey}");
                    }
                }
            }
        }
    }
}