package com.ghpham11a.springboot.config;

import com.newrelic.telemetry.Attributes;
import com.newrelic.telemetry.micrometer.NewRelicRegistry;
import com.newrelic.telemetry.micrometer.NewRelicRegistryConfig;
import io.micrometer.core.instrument.util.NamedThreadFactory;
import java.time.Duration;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
@ConditionalOnProperty(name = "newrelic.micrometer.enabled", havingValue = "true")
public class NewRelicMicrometerConfig {

    @Value("${newrelic.micrometer.api-key}")
    private String apiKey;

    @Value("${newrelic.micrometer.step:30s}")
    private Duration step;

    @Value("${spring.application.name}")
    private String appName;

    @Bean
    public NewRelicRegistryConfig newRelicRegistryConfig() {
        return new NewRelicRegistryConfig() {
            @Override
            public String apiKey() {
                return apiKey;
            }

            @Override
            public Duration step() {
                return step;
            }

            @Override
            public String serviceName() {
                return appName;
            }

            @Override
            public boolean useLicenseKey() {
                return true;
            }

            @Override
            public String get(String key) {
                return null;
            }
        };
    }

    @Bean(destroyMethod = "close")
    public NewRelicRegistry newRelicRegistry(NewRelicRegistryConfig config) {
        NewRelicRegistry registry = NewRelicRegistry.builder(config)
                .commonAttributes(
                        new Attributes()
                                .put("host", System.getenv().getOrDefault("HOSTNAME", "localhost"))
                                .put("application", appName))
                .build();
        registry.start(new NamedThreadFactory("newrelic-micrometer"));
        return registry;
    }
}
