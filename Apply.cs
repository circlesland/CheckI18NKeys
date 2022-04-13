namespace CheckI18NKeys;

public static class Apply
{
    public static void Suggestions(Dictionary<string, SuggestedKeyUsage[]> suggestedFixesByFile)
    {
        foreach (var suggestionsForFile in suggestedFixesByFile)
        {
            var suggestionsArr = suggestionsForFile.Value;
            var lines = File.ReadAllLines(suggestionsForFile.Key);
            
            for (var i = suggestionsArr.Length - 1; i >= 0; i--)
            {
                var suggestion = suggestionsArr[i];
                
                var keyStart = suggestion.Column;
                var keyEnd = suggestion.Column + suggestion.Key.Length;
                
                var currentLine = lines[suggestion.Line];
                
                var head = currentLine[..keyStart];
                var tail = currentLine[keyEnd..];

                lines[suggestion.Line] =  head + suggestion.SuggestedKey + tail;
            }

            File.WriteAllLines(suggestionsForFile.Key, lines);
        }
    }
}