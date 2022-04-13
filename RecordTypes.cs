namespace CheckI18NKeys;

public record KeyExtractor(string Name, string FilePattern, string[] Regexes);

public record KeyUsage(string File, int Line, int Column, string Key);

public record SuggestedKeyUsage(string File, int Line, int Column, string Key, string SuggestedKey)
    : KeyUsage(File, Line, Column, Key);