namespace CheckI18NKeys;

public static class Apply
{
    public static void Suggestions(Dictionary<string, SuggestedI18NKeyUsage[]> suggestedFixesByFile)
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
                
                var currentLine = lines[suggestion.Line - 1];
                
                var head = currentLine[..keyStart];
                var tail = currentLine[keyEnd..];

                lines[suggestion.Line - 1] =  head + suggestion.SuggestedKey + tail;
            }

            File.WriteAllLines(suggestionsForFile.Key, lines);
        }
    }
}