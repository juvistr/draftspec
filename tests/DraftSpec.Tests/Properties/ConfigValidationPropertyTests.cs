using DraftSpec.Cli.Configuration;
using FsCheck;
using FsCheck.Fluent;

namespace DraftSpec.Tests.Properties;

/// <summary>
/// Property-based tests for configuration validation.
/// These tests verify validation constraints that must hold for all inputs.
/// </summary>
public class ConfigValidationPropertyTests
{
    [Test]
    public void Validate_IsIdempotent()
    {
        // Property: Calling Validate() twice returns the same errors
        Prop.ForAll<int, int, int>((timeout, maxPar, line) =>
        {
            var config = new DraftSpecProjectConfig
            {
                Timeout = timeout,
                MaxParallelism = maxPar,
                Coverage = new CoverageConfig
                {
                    Thresholds = new ThresholdsConfig { Line = line }
                }
            };

            var errors1 = config.Validate();
            var errors2 = config.Validate();

            return errors1.Count == errors2.Count &&
                   errors1.SequenceEqual(errors2);
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void Validate_RejectsNonPositiveTimeout()
    {
        // Property: Non-positive timeout produces an error
        Prop.ForAll<int>(timeout =>
        {
            if (timeout <= 0)
            {
                var config = new DraftSpecProjectConfig { Timeout = timeout };
                var errors = config.Validate();
                return errors.Contains("timeout must be a positive number");
            }
            return true;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void Validate_AcceptsPositiveTimeout()
    {
        // Property: Positive timeout produces no timeout error
        Prop.ForAll<int>(timeout =>
        {
            if (timeout > 0)
            {
                var config = new DraftSpecProjectConfig { Timeout = timeout };
                var errors = config.Validate();
                return !errors.Any(e => e.Contains("timeout"));
            }
            return true;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void Validate_RejectsNonPositiveMaxParallelism()
    {
        // Property: Non-positive maxParallelism produces an error
        Prop.ForAll<int>(maxPar =>
        {
            if (maxPar <= 0)
            {
                var config = new DraftSpecProjectConfig { MaxParallelism = maxPar };
                var errors = config.Validate();
                return errors.Contains("maxParallelism must be a positive number");
            }
            return true;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void Validate_AcceptsPositiveMaxParallelism()
    {
        // Property: Positive maxParallelism produces no maxParallelism error
        Prop.ForAll<int>(maxPar =>
        {
            if (maxPar > 0)
            {
                var config = new DraftSpecProjectConfig { MaxParallelism = maxPar };
                var errors = config.Validate();
                return !errors.Any(e => e.Contains("maxParallelism"));
            }
            return true;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void Validate_RejectsLineThresholdOutOfRange()
    {
        // Property: Line threshold outside [0, 100] produces an error
        Prop.ForAll<double>(line =>
        {
            if (line < 0 || line > 100)
            {
                var config = new DraftSpecProjectConfig
                {
                    Coverage = new CoverageConfig
                    {
                        Thresholds = new ThresholdsConfig { Line = line }
                    }
                };
                var errors = config.Validate();
                return errors.Contains("coverage.thresholds.line must be between 0 and 100");
            }
            return true;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void Validate_AcceptsLineThresholdInRange()
    {
        // Property: Line threshold in [0, 100] produces no line threshold error
        Prop.ForAll<int>(line =>
        {
            var normalizedLine = Math.Abs(line % 101); // 0 to 100
            var config = new DraftSpecProjectConfig
            {
                Coverage = new CoverageConfig
                {
                    Thresholds = new ThresholdsConfig { Line = normalizedLine }
                }
            };
            var errors = config.Validate();
            return !errors.Any(e => e.Contains("coverage.thresholds.line"));
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void Validate_RejectsBranchThresholdOutOfRange()
    {
        // Property: Branch threshold outside [0, 100] produces an error
        Prop.ForAll<double>(branch =>
        {
            if (branch < 0 || branch > 100)
            {
                var config = new DraftSpecProjectConfig
                {
                    Coverage = new CoverageConfig
                    {
                        Thresholds = new ThresholdsConfig { Branch = branch }
                    }
                };
                var errors = config.Validate();
                return errors.Contains("coverage.thresholds.branch must be between 0 and 100");
            }
            return true;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public async Task Validate_RequiresTrustedKeysWhenSigningRequired()
    {
        // Property: requireSignedPlugins=true without trustedPublicKeyTokens produces error
        var config = new DraftSpecProjectConfig
        {
            Plugins = new PluginsConfig
            {
                RequireSignedPlugins = true,
                TrustedPublicKeyTokens = null
            }
        };

        var errors = config.Validate();
        await Assert.That(errors).Contains("plugins.trustedPublicKeyTokens is required when plugins.requireSignedPlugins is true");
    }

    [Test]
    public async Task Validate_AcceptsSigningWithTrustedKeys()
    {
        // Property: requireSignedPlugins=true with trustedPublicKeyTokens produces no plugin error
        var config = new DraftSpecProjectConfig
        {
            Plugins = new PluginsConfig
            {
                RequireSignedPlugins = true,
                TrustedPublicKeyTokens = ["abc123"]
            }
        };

        var errors = config.Validate();
        await Assert.That(errors.Any(e => e.Contains("trustedPublicKeyTokens"))).IsFalse();
    }

    [Test]
    public void Validate_NoDuplicateErrors()
    {
        // Property: Validation never produces duplicate error messages
        Prop.ForAll<int, int, int>((timeout, maxPar, line) =>
        {
            var config = new DraftSpecProjectConfig
            {
                Timeout = timeout,
                MaxParallelism = maxPar,
                Coverage = new CoverageConfig
                {
                    Thresholds = new ThresholdsConfig
                    {
                        Line = line,
                        Branch = timeout // Reuse value
                    }
                }
            };

            var errors = config.Validate();
            var uniqueErrors = errors.Distinct().ToList();

            return errors.Count == uniqueErrors.Count;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public async Task Validate_EmptyConfigProducesNoErrors()
    {
        // Property: Empty/default config is valid
        var config = new DraftSpecProjectConfig();
        var errors = config.Validate();
        await Assert.That(errors).IsEmpty();
    }
}
