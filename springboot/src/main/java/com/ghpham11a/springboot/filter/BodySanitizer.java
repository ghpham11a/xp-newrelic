package com.ghpham11a.springboot.filter;

import java.util.List;
import java.util.regex.Pattern;

/**
 * Strips PII and secrets from request/response body strings before they are
 * sent to New Relic as custom attributes.
 *
 * Each rule is a compiled regex paired with a replacement token so the
 * attribute value still shows the structure of the payload without leaking
 * sensitive data.  Add or remove rules as your domain requires.
 */
public final class BodySanitizer {

    private BodySanitizer() {}

    private static final String REDACTED = "\"[REDACTED]\"";

    private static final List<SanitizeRule> RULES = List.of(
            // JSON values for keys that look like secrets / tokens / keys
            jsonKeyRule("password|passwd|pwd|secret|token|api[_-]?key|access[_-]?key|auth"),

            // JSON values for common PII fields
            jsonKeyRule("ssn|social[_-]?security|date[_-]?of[_-]?birth|dob"),
            jsonKeyRule("email|e[_-]?mail"),
            jsonKeyRule("phone|phone[_-]?number|mobile"),
            jsonKeyRule("credit[_-]?card|card[_-]?number|cvv|cvc|expiry"),

            // Standalone patterns (not necessarily inside a JSON key)
            // US SSN: 123-45-6789
            freeformRule("\\b\\d{3}-\\d{2}-\\d{4}\\b"),
            // Credit-card-shaped numbers (13–19 digits, optionally separated)
            freeformRule("\\b(?:\\d[ -]*?){13,19}\\b"),
            // Bearer tokens in string values
            freeformRule("Bearer\\s+[A-Za-z0-9._~+/=-]+")
    );

    /** Sanitize a body string. Returns the cleaned version. */
    public static String sanitize(String body) {
        if (body == null || body.isBlank()) {
            return body;
        }
        String result = body;
        for (SanitizeRule rule : RULES) {
            result = rule.pattern.matcher(result).replaceAll(rule.replacement);
        }
        return result;
    }

    // ---- helpers ----------------------------------------------------------

    /**
     * Builds a regex that matches a JSON key whose name matches {@code keyPattern}
     * followed by its string or numeric value, and replaces the value with [REDACTED].
     * Handles both string values ("...") and bare numbers/booleans.
     */
    private static SanitizeRule jsonKeyRule(String keyPattern) {
        // Match: "keyName" : "value"  or  "keyName" : 12345
        String regex = "(\"(?i:" + keyPattern + ")\"\\s*:\\s*)(?:\"[^\"]*\"|\\d[\\d.]*)";
        return new SanitizeRule(
                Pattern.compile(regex),
                "$1" + REDACTED
        );
    }

    /** Replaces any match of {@code regex} with [REDACTED]. */
    private static SanitizeRule freeformRule(String regex) {
        return new SanitizeRule(Pattern.compile(regex), "[REDACTED]");
    }

    private record SanitizeRule(Pattern pattern, String replacement) {}
}
