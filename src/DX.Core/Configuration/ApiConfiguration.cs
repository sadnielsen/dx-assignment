namespace DX.Core.Configuration;

public record ApiConfiguration(string BaseUrl, Dictionary<string, string>? DefaultHeaders = null);
