# Development Instructions

## Testing

### Run All Tests
```bash
dotnet test Esolang.Piet.slnx
```

## Remaining Tasks (Abstractions v2.0.0 Migration)

- [ ] Fix `PietProcessorTests.RunAndOutputString_ParsesAndRunsHelloWorldSample` (Input/Output handling).
- [ ] Fix `PietProcessorTests.ExecuteCommand_CoversArithmeticFlowAndIoCommands` (Method invocation).
- [ ] Investigate and fix diagnostic test failures in `Piet.Generator.Tests`.
