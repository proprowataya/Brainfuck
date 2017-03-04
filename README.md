# Brainfuck Interpreter

## Requirement
- [.NET Core](https://www.microsoft.com/net/core) (>= 1.1.0)

## Getting Started

### Build and run
```
git clone https://github.com/proprowataya/Brainfuck.git
cd Brainfuck
dotnet restore
dotnet build
dotnet run --project Brainfuck.Repl/Brainfuck.Repl.csproj
```

### Hello world
```
$ dotnet run --project Brainfuck.Repl/Brainfuck.Repl.csproj
Brainfuck Interpreter on .NET Core

> +++++++++[>++++++++>+++++++++++>+++++<<<-]>.>++.+++++++..+++.>-.
> ------------.<++++++++.--------.+++.------.--------.>+.
>
Run in interpreter
Hello, world!
Elapsed 00:00:00.0024956

Run using compiler
Hello, world!
Elapsed 00:00:00.0414027
```
