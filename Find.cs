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
    private const string LabelSyntaxRegex = "<Label.*?key=\"(.*?)\">.*? </Label>";
    private const string CollapsedLabelSyntaxRegex = "<Label.*?key=\"(.*?)\"\\s*?/>";

    public static readonly KeyExtractor[] KeyExtractors =
    {
        new("js", "*.js", new[] {JavascriptSyntaxRegex}),
        new("ts", "*.ts", new[] {JavascriptSyntaxRegex}),
        new("svelte", "*.svelte", new[]
        {
            JavascriptSyntaxRegex, 
            SvelteSyntaxRegex, 
            LabelSyntaxRegex, 
            CollapsedLabelSyntaxRegex
        })
    };

    private static IEnumerable<FileInfo> RelevantSourceFiles(string sourceDirectory, ISet<string> fileTypes)
    {
        var sourceDirectoryInfo = new DirectoryInfo(sourceDirectory);

        var relevantFiles = KeyExtractors
            .Where(o => fileTypes.Contains(o.Name))
            .SelectMany(extractor => sourceDirectoryInfo.EnumerateFiles(
                extractor.FilePattern,
                SearchOption.AllDirectories));

        return relevantFiles.ToImmutableArray();
    }

    public static ISet<string> ExpectedKeysInMasterJson(string masterJsonFile,
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

    public static IEnumerable<KeyUsage> I18NKeyUsagesInCode(string sourceDirectory, ISet<string> fileTypes)
    {
        var relevantFiles = RelevantSourceFiles(sourceDirectory, fileTypes);
        var foundUsages = new List<KeyUsage>();

        var fileTypeRegexes = KeyExtractors.ToDictionary(
            o => o.Name,
            o => o.Regexes
                .Select(p => new Regex(p, RegexOptions.Compiled))
                .ToArray());

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
                    if (file.Extension == ".svelte" &&  line.Contains("<Label"))
                    {
                        
                    }
                    
                    var extractedKeyMatches = extractorRegex.Matches(line);
                    foreach (Match extractedKeyMatch in extractedKeyMatches)
                    {
                        var index = line.IndexOf(extractedKeyMatch.Groups[1].Value, StringComparison.Ordinal);
                        foundUsages.Add(new KeyUsage(
                            file.FullName,
                            lineNo,
                            index,
                            extractedKeyMatch.Groups[1].Value));
                    }
                }
            }
        }

        return foundUsages.ToImmutableArray();
    }

    public static IEnumerable<SuggestedKeyUsage> SuggestedFixes(IDictionary<string, KeyUsage[]> undefinedKeys,
        string[] unusedKeys)
    {
        var possibleMatches = new List<SuggestedKeyUsage>();
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
                    possibleMatches.Add(new SuggestedKeyUsage(
                        undefinedKeyUsage.File,
                        undefinedKeyUsage.Line,
                        undefinedKeyUsage.Column,
                        undefinedKeyUsage.Key,
                        unusedKey));
                }
            }
        }

        return possibleMatches.ToImmutableArray();
    }
}