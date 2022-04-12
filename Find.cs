using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Quickenshtein;

namespace CheckI18NKeys;

public record I18NKeyUsage(string File, int Line, int Column, string Key);

public record SuggestedI18NKeyUsage(string File, int Line, int Column, string Key, string SuggestedKey, int Distance)
    : I18NKeyUsage(File, Line, Column, Key);

public static class Find
{
    private static int MaxSuggestionMatchDistance = 3;

    private static List<FileInfo> RelevantSourceFiles(string sourceDirectory)
    {
        var sourceDirectoryInfo = new DirectoryInfo(sourceDirectory);
        var relevantFiles = new List<FileInfo>();
        relevantFiles.AddRange(sourceDirectoryInfo.EnumerateFiles("*.ts", SearchOption.AllDirectories));
        relevantFiles.AddRange(sourceDirectoryInfo.EnumerateFiles("*.svelte", SearchOption.AllDirectories));
        return relevantFiles;
    }

    public static HashSet<string> ExpectedKeysInMasterJson(string masterJsonFile)
    {
        var jsonFile = new FileInfo(masterJsonFile);
        var jsonDocument = JToken.Parse(File.ReadAllText(jsonFile.FullName));
        var expectedI18NKeys = new HashSet<string>();

        var tmpStack = new Stack<JToken>();
        tmpStack.Push(jsonDocument);

        while (tmpStack.Count > 0)
        {
            var currentToken = tmpStack.Pop();
            var children = currentToken.Children().ToArray();

            if (currentToken is JValue value)
            {
                expectedI18NKeys.Add(value.Path.Replace("en.", ""));
                //Console.WriteLine($"{value.Path}:{value.Value}");
            }
            else
            {
                foreach (var childToken in children)
                {
                    tmpStack.Push(childToken);
                }
            }
        }

        return expectedI18NKeys;
    }
    
    public static I18NKeyUsage[] I18NKeyUsagesInCode(string sourceDirectory)
    {
        var relevantFiles = RelevantSourceFiles(sourceDirectory);

        var extractorRegexes = new (Regex Regex, int Offset)[]
        {
            (new Regex("i18n\\(\"(.*?)\"\\)", RegexOptions.Compiled), 6),
            (new Regex("\\$_\\(\"(.*?)\"\\)", RegexOptions.Compiled), 4)
        };

        var foundUsages = new List<I18NKeyUsage>();
        
        foreach (var file in relevantFiles)
        {
            var lineNo = 1;
            foreach (var line in File.ReadAllLines(file.FullName))
            {
                foreach (var extractorRegex in extractorRegexes)
                {
                    var extractedKeyMatches = extractorRegex.Regex.Matches(line);
                    foreach (Match extractedKeyMatch in extractedKeyMatches)
                    {
                        foundUsages.Add(new I18NKeyUsage(
                            file.FullName,
                            lineNo,
                            extractedKeyMatch.Index + extractorRegex.Offset,
                            extractedKeyMatch.Groups[1].Value));
                    }   
                }

                lineNo++;
            }
        }

        return foundUsages.ToArray();
    }
    
    public static SuggestedI18NKeyUsage[] SuggestedFixes(Dictionary<string, I18NKeyUsage[]> undefinedKeys, string[] unusedKeys)
    {
        var possibleMatches = new List<SuggestedI18NKeyUsage>();
        foreach (var undefinedKey in undefinedKeys)
        {
            foreach (var unusedKey in unusedKeys)
            {
                var distance = Levenshtein.GetDistance(unusedKey, undefinedKey.Key);

                foreach (var undefinedKeyUsage in undefinedKey.Value)
                {
                    possibleMatches.Add(new SuggestedI18NKeyUsage(
                        undefinedKeyUsage.File, 
                        undefinedKeyUsage.Line, 
                        undefinedKeyUsage.Column, 
                        undefinedKeyUsage.Key, 
                        unusedKey, 
                        distance));
                }
            }
        }

        return possibleMatches
            .Where(o => o.Distance <= MaxSuggestionMatchDistance)
            .ToArray();
    }
}