# Security Policy

## MCP Trust Model

DraftSpec includes an MCP (Model Context Protocol) server that enables AI assistants to run specs and generate test scaffolds. **By design, this executes arbitrary C# code** via `dotnet script`.

### Trust Assumptions

The MCP server assumes:

1. **Trusted AI Assistant**: Only connect DraftSpec MCP to AI assistants you trust. The assistant can execute any C# code through the `run_spec` tool.

2. **Trusted Spec Files**: Spec files (`.spec.csx`) are C# scripts with full system access. Only run specs from trusted sources.

3. **Local Execution**: The MCP server is designed for local development, not production or multi-tenant environments.

### Security Recommendations

- **Use trusted AI assistants only** - The MCP server grants code execution capabilities
- **Review generated scaffolds** - The `scaffold_specs` tool generates code; review before execution
- **Consider containerization** - For additional isolation, run specs in containers
- **Keep dependencies updated** - Regularly update DraftSpec and dotnet-script

## Deployment Guidance

### Recommended: Local Development Only

The MCP server is designed for **local development environments** where you control both the AI assistant and the machine running specs.

```bash
# Typical local usage - safe when you control both ends
dotnet run --project src/DraftSpec.Mcp
```

### Network Exposure: NOT Recommended

**Never expose the MCP server to untrusted networks.** The server:
- Has no built-in authentication
- Executes arbitrary code from any connected client
- Runs with the privileges of the host process

### Container Isolation

For additional security, run the MCP server in a container with restricted permissions:

```dockerfile
# Example Dockerfile for isolated execution
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine
WORKDIR /app
COPY . .

# Run as non-root user
RUN adduser -D specuser
USER specuser

# Limit capabilities
CMD ["dotnet", "run", "--project", "src/DraftSpec.Mcp"]
```

```bash
# Run with restricted permissions
docker run --rm -it \
  --read-only \
  --tmpfs /tmp \
  --network none \
  --cap-drop ALL \
  draftspec-mcp
```

### Execution Modes

| Mode | Isolation | Performance | Use Case |
|------|-----------|-------------|----------|
| **In-process** | Shared process | Fast (cached compilation) | Trusted local development |
| **Subprocess** | Process boundary | Slower (new process per run) | Slightly more isolation |
| **Container** | Full isolation | Slowest | Untrusted or shared environments |

### CI/CD Environments

When using DraftSpec in CI/CD pipelines:
- Run in ephemeral containers that are destroyed after each job
- Use read-only file systems where possible
- Limit network access to only required endpoints
- Never persist credentials in spec files

### Existing Mitigations

DraftSpec implements these security measures:

- **Path traversal prevention**: Spec names are validated to prevent directory escape attacks (`PathValidator.ValidateFileName`)
- **String escaping**: Generated code escapes user input to prevent injection (`Scaffolder.EscapeString`)
- **Atomic file creation**: Temp files use `FileMode.CreateNew` to prevent TOCTOU race conditions
- **No shell injection**: Process arguments are passed as arrays, not shell strings
- **CSS sanitization**: HTML formatter strips XSS vectors from custom CSS

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 0.x     | :white_check_mark: |

## Reporting a Vulnerability

If you discover a security vulnerability in DraftSpec, please report it responsibly:

1. **Do not** open a public GitHub issue for security vulnerabilities
2. Email the maintainers directly or use GitHub's private vulnerability reporting feature
3. Include:
   - Description of the vulnerability
   - Steps to reproduce
   - Potential impact
   - Suggested fix (if any)

We aim to respond to security reports within 48 hours and will work with you to understand and address the issue.

## Security Considerations

### HTML Formatter

When using the HTML formatter with `CustomCss`, the CSS content is sanitized to prevent XSS attacks. The following patterns are stripped:
- `</style>` tags (prevents escaping CSS context)
- `<script` tags
- `<link` tags
- `<import` tags

**Safe usage:**
```csharp
var formatter = new HtmlFormatter(new HtmlOptions
{
    CustomCss = ".my-class { color: blue; }"  // Safe
});
```

### Spec File Execution

DraftSpec executes spec files using `dotnet script`. Only run spec files from trusted sources, as they have full access to your system.

### Path Handling

The CLI validates that spec files are within the project directory to prevent path traversal attacks.

## Disclosure Timeline

- **Day 0**: Vulnerability reported
- **Day 1-2**: Initial response and acknowledgment
- **Day 3-14**: Investigation and fix development
- **Day 15-30**: Release patch and coordinate disclosure
