namespace CheckI18NKeys;

public record I18NKeyExtractorRegex(string Regex, int Offset);

public record I18NKeyExtractor(string Name, string FilePattern, I18NKeyExtractorRegex[] Regexes);

public record I18NKeyUsage(string File, int Line, int Column, string Key);

public record SuggestedI18NKeyUsage(string File, int Line, int Column, string Key, string SuggestedKey, int Distance)
    : I18NKeyUsage(File, Line, Column, Key);