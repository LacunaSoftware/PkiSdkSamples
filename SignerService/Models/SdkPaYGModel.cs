namespace Lacuna.SignerService.Models;

public record SdkPaYGModel {
	public string UserId { get; init; } = default!;
	public string SdkHash { get; init; } = default!;
	public string TypeCode { get; init; } = default!;
	public bool Success { get; init; } = default!;
	public string Details { get; init; } = default!;
}
public class SdkPaayo {
	public string SdkLicense { get; init; } = default!;
	public DateTimeOffset ValidUntil { get; init; } = default!;
	public string ErrorMessage { get; init; } = default!;
}

public class SdkPaYGReturnModel {
	public bool Success { get; init; } = default!;
	public string Details { get; init; } = default!;
}
