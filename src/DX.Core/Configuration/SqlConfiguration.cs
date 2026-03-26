namespace DX.Core.Configuration;

public record SqlConfiguration(string ConnectionString, int CommandTimeoutSeconds = 30);
