using System.Text.RegularExpressions;
using I18NChecks;
using Newtonsoft.Json.Linq;

namespace FindI18nMistakes.Implementation;

public class Find
{
    public static IEnumerable<I18nKeyUsage> I18NKeyUsagesInCode(string sourceDirectory)
    {
        var relevantFiles = FindRelevantSourceFiles(sourceDirectory);

        var extractorRegexes = new (Regex Regex, int Offset)[]
        {
            (new Regex("i18n\\(\"(.*?)\"\\)", RegexOptions.Compiled), 6),
            (new Regex("\\$_\\(\"(.*?)\"\\)", RegexOptions.Compiled), 4)
        };

        var foundUsages = new HashSet<I18nKeyUsage>();
        
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
                        foundUsages.Add(new I18nKeyUsage(
                            file.FullName,
                            lineNo,
                            extractedKeyMatch.Index + extractorRegex.Offset,
                            extractedKeyMatch.Groups[1].Value));
                    }   
                }

                lineNo++;
            }
        }

        return foundUsages;
    }

    private static List<FileInfo> FindRelevantSourceFiles(string sourceDirectory)
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
}