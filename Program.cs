using FindI18nMistakes.Implementation;
using I18NChecks;
using Quickenshtein;

namespace FindI18nMistakes;

public class Program
{
    public static void Main()
    {
        var sourceDir = "/home/daniel/src/circles-world/o-platform/shell";
        var i18NKeyUsages = Find.I18NKeyUsagesInCode(sourceDir).ToArray();

        var expectedI18NKeys =
            Find.ExpectedKeysInMasterJson("/home/daniel/src/circles-world/o-platform/shell/src/i18n/lang/en.json");

        var occurringKeys = i18NKeyUsages.GroupBy(o => o.Key)
            .ToDictionary(o => o.Key, o => o.ToArray());

        var undefinedKeys = occurringKeys.Where(o => !expectedI18NKeys.Contains(o.Key))
            .ToDictionary(o => o.Key, o => o.Value);

        var unusedKeys = expectedI18NKeys.Where(o => !occurringKeys.ContainsKey(o))
            .ToArray();
        
        var possibleMatches = new List<(int Distance, I18nKeyUsage[] UndefinedKeys, string UnusedKey)>();
        foreach (var undefinedKey in undefinedKeys)
        {
            foreach (var unusedKey in unusedKeys)
            {
                var distance = Levenshtein.GetDistance(unusedKey, undefinedKey.Key);
                possibleMatches.Add((distance, undefinedKey.Value, unusedKey));
            }
        }

        var likelyMatches = possibleMatches.Where(o => o.Distance < 4);
        var flatLikelyMatches = likelyMatches.SelectMany(o => o.UndefinedKeys.Select(p => (p, o.UnusedKey)))
            .ToArray();
        var likelyMatchesByFile = flatLikelyMatches.GroupBy(o => o.p.File).ToArray();

        
        Console.WriteLine($"Found {undefinedKeys.Count} undefined i18n keys in {sourceDir}");
        Console.WriteLine("=================================================================");

        foreach (var file in undefinedKeys.SelectMany(o => o.Value).GroupBy(o => o.File))
        {
            Console.WriteLine($"* {file.Key.Replace(sourceDir, "")}:");
            var occurrencesByLine = file.GroupBy(o => o.Line);
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
                    }
                }
                else
                {
                    var occurence = line.First();
                    var address = $"[{occurence.Line}:{occurence.Column}]".PadRight(10, ' ');
                    Console.WriteLine($"   - {address} {occurence.Key}");
                }
            }
        }
        
        
        Console.WriteLine("");
        Console.WriteLine($"Found suggestions for {likelyMatchesByFile.Length} files:");
        Console.WriteLine("=================================================================");

        foreach (var file in likelyMatchesByFile)
        {
            Console.WriteLine($"* {file.Key.Replace(sourceDir, "")}:");
            var occurrencesByLine = file.GroupBy(o => o.p.Line);
            foreach (var line in occurrencesByLine)
            {
                if (line.Count() > 1)
                {
                    var key = $"   - [{line.Key}:";
                    Console.WriteLine(key);

                    foreach (var occurence in line)
                    {
                        var k = $"{" ".PadRight(key.Length)}{occurence.p.Column}]";
                        Console.WriteLine($"{k.PadRight(16, ' ')}{occurence.p.Key}");
                        Console.WriteLine($"{k.PadRight(16, ' ')}{occurence.UnusedKey}");
                    }
                }
                else
                {
                    var occurence = line.First();
                    var address = $"[{occurence.p.Line}:{occurence.p.Column}]".PadRight(10, ' ');
                    Console.WriteLine($"   - {address} {occurence.p.Key}");
                    Console.WriteLine($"     {" ".PadRight(address.Length)} {occurence.UnusedKey}");
                }
            }
        }
        
        // Apply the suggestions
        foreach (var suggestionsForFile in likelyMatchesByFile)
        {
            var suggestionsArr = suggestionsForFile.ToArray();
            var fileContents = File.ReadAllLines(suggestionsForFile.Key);
            for (var i = suggestionsArr.Length - 1; i >= 0; i--)
            {
                var suggestion = suggestionsArr[i];
                
                var keyStart = suggestion.p.Column;
                var keyEnd = suggestion.p.Column + suggestion.p.Key.Length;

                var tail = fileContents[suggestion.p.Line - 1].Substring(keyEnd);
                var head = fileContents[suggestion.p.Line - 1].Substring(0, keyStart);

                fileContents[suggestion.p.Line - 1] = head + suggestion.UnusedKey + tail;
                
                File.WriteAllLines(suggestionsForFile.Key, fileContents);
            }
        }
    }
}