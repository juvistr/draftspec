using System.Security;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock implementation of IPathValidator for testing.
/// </summary>
public class MockPathValidator : IPathValidator
{
    private bool _validatePathWithinBaseShouldFail;
    private string? _validatePathWithinBaseError;
    private bool _validateFileNameShouldFail;
    private string? _validateFileNameError;

    public List<(string Path, string? BaseDirectory)> ValidatePathWithinBaseCalls { get; } = [];
    public List<string> ValidateFileNameCalls { get; } = [];

    public MockPathValidator WithPathWithinBaseFailure(string error = "Path must be within the working directory")
    {
        _validatePathWithinBaseShouldFail = true;
        _validatePathWithinBaseError = error;
        return this;
    }

    public MockPathValidator WithFileNameFailure(string error = "Invalid filename")
    {
        _validateFileNameShouldFail = true;
        _validateFileNameError = error;
        return this;
    }

    public void ValidatePathWithinBase(string path, string? baseDirectory = null)
    {
        ValidatePathWithinBaseCalls.Add((path, baseDirectory));
        if (_validatePathWithinBaseShouldFail)
            throw new SecurityException(_validatePathWithinBaseError);
    }

    public bool TryValidatePathWithinBase(string path, string? baseDirectory, out string? errorMessage)
    {
        ValidatePathWithinBaseCalls.Add((path, baseDirectory));
        if (_validatePathWithinBaseShouldFail)
        {
            errorMessage = _validatePathWithinBaseError;
            return false;
        }
        errorMessage = null;
        return true;
    }

    public void ValidateFileName(string name)
    {
        ValidateFileNameCalls.Add(name);
        if (_validateFileNameShouldFail)
            throw new ArgumentException(_validateFileNameError, nameof(name));
    }

    public bool TryValidateFileName(string name, out string? errorMessage)
    {
        ValidateFileNameCalls.Add(name);
        if (_validateFileNameShouldFail)
        {
            errorMessage = _validateFileNameError;
            return false;
        }
        errorMessage = null;
        return true;
    }
}
