package com.ghpham11a.springboot.filter;

import com.newrelic.api.agent.NewRelic;
import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import java.io.IOException;
import java.nio.charset.StandardCharsets;
import org.springframework.core.Ordered;
import org.springframework.core.annotation.Order;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;
import org.springframework.web.util.ContentCachingRequestWrapper;
import org.springframework.web.util.ContentCachingResponseWrapper;

/**
 * Servlet filter that captures HTTP request and response bodies, sanitizes
 * them via {@link BodySanitizer}, and attaches the cleaned strings to the
 * current New Relic transaction as custom attributes.
 *
 * Attributes recorded:
 * <ul>
 *   <li>{@code request.body}  – sanitized request payload (max 4 096 chars)</li>
 *   <li>{@code response.body} – sanitized response payload (max 4 096 chars)</li>
 * </ul>
 */
@Component
@Order(Ordered.HIGHEST_PRECEDENCE + 10)
public class NewRelicBodyCaptureFilter extends OncePerRequestFilter {

    /** New Relic truncates custom attributes at 4 096 characters. */
    private static final int MAX_ATTR_LENGTH = 4096;

    @Override
    protected void doFilterInternal(HttpServletRequest request,
                                    HttpServletResponse response,
                                    FilterChain filterChain)
            throws ServletException, IOException {

        ContentCachingRequestWrapper wrappedRequest =
                new ContentCachingRequestWrapper(request);
        ContentCachingResponseWrapper wrappedResponse =
                new ContentCachingResponseWrapper(response);

        try {
            filterChain.doFilter(wrappedRequest, wrappedResponse);
        } finally {
            recordBody("request.body", wrappedRequest.getContentAsByteArray());
            recordBody("response.body", wrappedResponse.getContentAsByteArray());

            // Copy the cached response body back to the real output stream so
            // the client actually receives it.
            wrappedResponse.copyBodyToResponse();
        }
    }

    private static void recordBody(String attribute, byte[] raw) {
        if (raw == null || raw.length == 0) {
            return;
        }
        String body = new String(raw, StandardCharsets.UTF_8);
        String sanitized = BodySanitizer.sanitize(body);
        if (sanitized.length() > MAX_ATTR_LENGTH) {
            sanitized = sanitized.substring(0, MAX_ATTR_LENGTH);
        }
        NewRelic.addCustomParameter(attribute, sanitized);
    }
}
