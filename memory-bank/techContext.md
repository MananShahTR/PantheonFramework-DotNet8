# Pantheon Framework - Technical Context

## Technologies Used

### Core Technologies
- **.NET 8**: The framework is built on .NET 8, leveraging its performance improvements and modern language features
- **C# 12**: Utilizing the latest language features for clean, concise code
- **ASP.NET Core**: For the API layer and web-based components

### Development Tools
- **Visual Studio / Rider**: Primary development environments
- **Git**: Version control system
- **NuGet**: Package management for .NET dependencies

### Testing Framework
- **xUnit**: For unit and integration testing
- **Moq**: For mocking dependencies in tests

## Development Setup
The project follows a standard .NET solution structure:

```
Pantheon.Framework/
├── src/
│   ├── Pantheon.Framework.Api/        # REST API for flow management
│   ├── Pantheon.Framework.Cli/        # Command-line interface
│   ├── Pantheon.Framework.Core/       # Core interfaces and models
│   ├── Pantheon.Framework.Executors/  # Flow execution engines
│   ├── Pantheon.Framework.Flow/       # Flow implementation
│   ├── Pantheon.Framework.LlmClient/  # LLM client implementations
│   ├── Pantheon.Framework.Storage/    # Storage implementations
│   └── Pantheon.Framework.Template/   # Template system
├── samples/
│   ├── ChatApplication/               # Sample chat application
│   └── SimpleSkill/                   # Simple skill implementation
└── tests/
    ├── Pantheon.Framework.Core.Tests/         # Unit tests for core components
    └── Pantheon.Framework.Integration.Tests/  # Integration tests
```

### Source Control Configuration
- **.gitignore**: Configured to exclude standard .NET build artifacts:
  - `bin/` and `obj/` directories (containing compiled files)
  - User-specific files (`.suo`, `.user`, etc.)
  - Build results (`Debug/`, `Release/`, etc.)
  - Visual Studio / Rider temporary files (`.vs/`, `.idea/`)
- **Commit Practices**: 
  - Compiled files (`bin/` and `obj/` directories) should never be committed to the repository
  - Only source code, project files, and necessary configuration files should be tracked

## Technical Constraints

1. **Platform Compatibility**: Designed to work cross-platform (Windows, Linux, macOS)
2. **Performance Considerations**: 
   - Minimize memory allocations in hot paths
   - Support for asynchronous operations to avoid blocking threads
   - Efficient serialization/deserialization for flow state
3. **Extensibility Requirements**:
   - All key components must be interface-based for extensibility
   - Plugin architecture for LLM providers and custom components
4. **Security Considerations**:
   - Secure handling of API keys and credentials for LLM services
   - Input validation to prevent prompt injection attacks
   - Proper error handling to avoid leaking sensitive information

## Dependencies

### External Libraries
- **System.Text.Json**: For JSON serialization/deserialization
- **Microsoft.Extensions.DependencyInjection**: For dependency injection
- **Microsoft.Extensions.Logging**: For logging capabilities
- **Microsoft.AspNetCore**: For web API functionality

### Third-Party Dependencies
- Mock LLM Client included for development and testing purposes
- Extensible design to support various LLM providers:
  - OpenAI
  - Azure OpenAI
  - Anthropic
  - Local models (e.g., LLaMA, Mistral)

## Configuration
- Configuration follows standard .NET patterns using `IConfiguration`
- Settings can be provided via:
  - appsettings.json files
  - Environment variables
  - Command-line arguments
  - In-memory configuration for testing

## API Design Principles
- RESTful API design for resource management
- Clean separation between API controllers and business logic
- Consistent error handling and status code usage
- API versioning support for future evolution
