# Changelog

## v1.2 - 2018.29.12
- Lazy converter expression compilation (some public methods of `ConverterCollection` don't need to be compiled)
- No more failing tests
- Some visistors covered by tests
- [Breaking] Introduced explicit public API, some classes made internal

## v1.1 - 2018.10.03
- Support concurrent validation and convertation recording

## v1.0 - 2018.09.15
- Set TargetFramework to net472.
- Switch to SDK-style project format and dotnet core build tooling.
- Use [Vostok.Logging.Abstractions](https://github.com/vostok/logging.abstractions) as a logging framework facade.
- Use [Nerdbank.GitVersioning](https://github.com/AArnott/Nerdbank.GitVersioning) to automate generation of assembly 
  and nuget package versions.
- Update [GroBuf](https://github.com/skbkontur/GroBuf) dependency to v1.4.
- Update [GrobExp.Compiler](https://github.com/skbkontur/GrobExp.Compiler) dependency to v1.2.
