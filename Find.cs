using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Quickenshtein;

namespace CheckI18NKeys;

public static class Find
{
    private const int MaxSuggestionMatchDistance = 3;
    
    private const string JavascriptSyntaxRegex = "i18n\\(\"([\\w|\\.|\\-|_]*?)\",?(.*)?\\)";
    private const string SvelteSyntaxRegex = "\\$_\\(\"([\\w|\\.|\\-|_]*?)\",?(.*)?\\)";
    
    public static readonly I18NKeyExtractor[] KeyExtractors =
    {
        new("js", "*.js", new I18NKeyExtractorRegex[]
        {
            new (JavascriptSyntaxRegex, 6)
        }),
        new("ts", "*.ts", new I18NKeyExtractorRegex[]
        {
            new (JavascriptSyntaxRegex, 6)
        }),
        new("svelte", "*.svelte", new I18NKeyExtractorRegex[]
        {
            new (JavascriptSyntaxRegex, 6),
            new (SvelteSyntaxRegex, 4)
        })
    };

    private static IEnumerable<FileInfo> RelevantSourceFiles(string sourceDirectory, HashSet<string> fileTypes)
    {
        var sourceDirectoryInfo = new DirectoryInfo(sourceDirectory);
        
        var relevantFiles = KeyExtractors
            .Where(o => fileTypes.Contains(o.Name))
            .SelectMany(extractor => sourceDirectoryInfo.EnumerateFiles(
                extractor.FilePattern,
                SearchOption.AllDirectories));

        return relevantFiles.ToImmutableArray();
    }

    public static ImmutableHashSet<string> ExpectedKeysInMasterJson(string masterJsonFile,
        string? defaultLanguagePrefix)
    {
        var jsonFile = new FileInfo(masterJsonFile);
        var jsonDocument = JToken.Parse(File.ReadAllText(jsonFile.FullName));
        var expectedI18NKeys = new List<string>();

        var tmpStack = new Stack<JToken>();
        tmpStack.Push(jsonDocument);

        while (tmpStack.Count > 0)
        {
            var currentToken = tmpStack.Pop();
            var children = currentToken.Children().ToArray();

            if (currentToken is JValue value)
            {
                expectedI18NKeys.Add(
                    defaultLanguagePrefix != null
                        ? value.Path.Replace(defaultLanguagePrefix, "")
                        : value.Path
                );
            }
            else
            {
                foreach (var childToken in children)
                {
                    tmpStack.Push(childToken);
                }
            }
        }

        return expectedI18NKeys.ToImmutableHashSet();
    }

    public static IEnumerable<I18NKeyUsage> I18NKeyUsagesInCode(string sourceDirectory, HashSet<string> fileTypes)
    {
        var relevantFiles = RelevantSourceFiles(sourceDirectory, fileTypes);
        var foundUsages = new List<I18NKeyUsage>();
        
        var fileTypeRegexes = KeyExtractors.ToDictionary(o => o.Name, o => o.Regexes.Select(p => new
        {
            p.Offset,
            Regex = new Regex(p.Regex, RegexOptions.Compiled)
        }).ToArray());

        foreach (var file in relevantFiles)
        {
            if (!fileTypeRegexes.TryGetValue(file.Extension.Replace(".", ""), out var applicableRegexes))
            {
                continue;
            }
            
            var lines = File.ReadAllLines(file.FullName);
            for (var lineNo = 0; lineNo < lines.Length; lineNo++)
            {
                var line = lines[lineNo];
                foreach (var extractorRegex in applicableRegexes)
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
            }
        }

        return foundUsages.ToImmutableArray();
    }

    public static IEnumerable<SuggestedI18NKeyUsage> SuggestedFixes(Dictionary<string, I18NKeyUsage[]> undefinedKeys,
        string[] unusedKeys)
    {
        var possibleMatches = new List<SuggestedI18NKeyUsage>();
        foreach (var undefinedKey in undefinedKeys)
        {
            foreach (var unusedKey in unusedKeys)
            {
                var distance = Levenshtein.GetDistance(unusedKey, undefinedKey.Key);
                if (distance > MaxSuggestionMatchDistance)
                {
                    continue;
                }

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

        return possibleMatches.ToImmutableArray();
    }
}