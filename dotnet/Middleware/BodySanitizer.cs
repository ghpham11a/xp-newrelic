using System.Text.RegularExpressions;

namespace dotnet_api.Middleware;

/// <summary>
/// Strips PII and secrets from request/response body strings before they are
/// sent to New Relic as custom attributes.
/// </summary>
public static class BodySanitizer
{
    private const string Redacted = "\"[REDACTED]\"";

    private static readonly List<SanitizeRule> Rules = new()
    {
        // JSON values for keys that look like secrets / tokens / keys
        JsonKeyRule(@"password|passwd|pwd|secret|token|api[_\-]?key|access[_\-]?key|auth"),

        // JSON values for common PII fields
        JsonKeyRule(@"ssn|social[_\-]?security|date[_\-]?of[_\-]?birth|dob"),
        JsonKeyRule(@"email|e[_\-]?mail"),
        JsonKeyRule(@"phone|phone[_\-]?number|mobile"),
        JsonKeyRule(@"credit[_\-]?card|card[_\-]?number|cvv|cvc|expiry"),

        // Standalone patterns
        // US SSN: 123-45-6789
        FreeformRule(@"\b\d{3}-\d{2}-\d{4}\b"),
        // Credit-card-shaped numbers (13-19 digits, optionally separated)
        FreeformRule(@"\b(?:\d[ \-]*?){13,19}\b"),
        // Bearer tokens in string values
        FreeformRule(@"Bearer\s+[A-Za-z0-9._~+/=\-]+")
    };

    /// <summary>Sanitize a body string. Returns the cleaned version.</summary>
    public static string Sanitize(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return body ?? string.Empty;

        var result = body;
        foreach (var rule in Rules)
        {
            result = rule.Pattern.Replace(result, rule.Replacement);
        }
        return result;
    }

    private static SanitizeRule JsonKeyRule(string keyPattern)
    {
        // Match: "keyName" : "value"  or  "keyName" : 12345
        var regex = $@"(""(?i:{keyPattern})""\s*:\s*)(?:""[^""]*""|\d[\d.]*)";
        return new SanitizeRule(
            new Regex(regex, RegexOptions.Compiled),
            $"$1{Redacted}");
    }

    private static SanitizeRule FreeformRule(string regex)
    {
        return new SanitizeRule(
            new Regex(regex, RegexOptions.Compiled),
            "[REDACTED]");
    }

    private record SanitizeRule(Regex Pattern, string Replacement);
}
