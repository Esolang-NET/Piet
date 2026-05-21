# Development Instructions

## Testing

### Run All Tests
```bash
dotnet test Esolang.Piet.slnx
```


## Work in Progress

### ILogger Support in Generator
- **Status**: Implemented ILogger support in `Esolang.Piet.Generator` (branch `v1.1.2`).
- **Functional Tests**: Added in `PietMethodGeneratorTests.cs`.
- **Known Issues**: Compilation errors (`CS0234`) in generated `PietRuntime.g.cs` due to unresolved `Microsoft.Extensions.Logging` namespace in some projects (e.g., `samples`).
- **Next Steps**:
    - Decouple generated code from strict `Microsoft.Extensions.Logging` dependency, likely by ensuring no direct namespace usage in runtime templates or ensuring dependencies are properly configured in all consuming projects.
    - Resolve argument matching/signature issues in generated runtime calls.
