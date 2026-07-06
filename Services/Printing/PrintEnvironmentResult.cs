namespace WindowsPrinter.Services.Printing;

public sealed record PrintEnvironmentResult(bool Succeeded, string? Message)
{
    public static PrintEnvironmentResult Success() => new(true, null);

    public static PrintEnvironmentResult Failure(string message) => new(false, message);
}
