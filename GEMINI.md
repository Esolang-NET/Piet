# Development Instructions

## Testing

### Run All Tests
```bash
dotnet test Esolang.Piet.slnx
```

## Remaining Tasks (Abstractions v2.0.0 Migration)

- [x] Fix `PietProcessorTests.RunAndOutputString_ParsesAndRunsHelloWorldSample` (Input/Output handling).
- [x] Fix `PietProcessorTests.ExecuteCommand_CoversArithmeticFlowAndIoCommands` (Method invocation).
- [x] Investigate and fix diagnostic test failures in Piet.Generator.Tests.
