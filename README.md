# Brainfuck Interpreter

A Simple Brainfuck Interpreter on .NET Core

## Requirement
- [.NET Core](https://www.microsoft.com/net/core) (>= 1.1.0)

## Getting Started

### Build and run
```
git clone https://github.com/proprowataya/Brainfuck.git
cd Brainfuck
dotnet restore
dotnet build --configuration Release
cd Brainfuck.Repl/bin/Release/netcoreapp1.0
dotnet Brainfuck.Repl.dll
```

### Hello world
```
$ dotnet Brainfuck.Repl.dll
Brainfuck Interpreter on .NET Core

> +++++++++[>++++++++>+++++++++++>+++++<<<-]>.>++.+++++++..+++.>-.
> ------------.<++++++++.--------.+++.------.--------.>+.
>
===== Compiler =====
Hello, world!
Elapsed 00:00:00.1611676

===== Interpreter =====
Hello, world!
Elapsed 00:00:00.0034193
```

```
$ echo "+++++++++[>++++++++>+++++++++++>+++++<<<-]>.>++.+++++++..+++.>-.------------.<++++++++.--------.+++.------.--------.>+." > helloworld.bf
$ dotnet Brainfuck.Repl.dll helloworld.bf
Brainfuck Interpreter on .NET Core

Hello, world!
Elapsed 00:00:00.0004621
```
