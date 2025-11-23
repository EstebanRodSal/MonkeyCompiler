# MonkeyCompiler

**Full compiler for the Monkey language – Instituto Tecnológico de Costa Rica (ITCR), Sede Regional San Carlos**

**Authors:**

- Esteban Rodríguez Salas
- Emmanuel Jiménez Salas

## Overview

This repository contains the implementation of a complete compiler for the **Monkey** programming language, developed for the TEC course _Compilers and Interpreters_. Monkey is a small, statically typed language inspired by _“Writing an Interpreter in Go”_, but extended to support:

- Static typing
- Explicit type annotations
- Functions and nested blocks
- Arrays and hash maps
- First-class function literals
- A required `main()` entry point
- Compilation to **.NET CIL** using **Reflection.Emit**

The project implements the full compilation pipeline using **ANTLR4** and **C#**, producing a runnable .NET assembly.

---

## Compiler Architecture

### 1. **Scanning (Lexer)**

- Implemented with ANTLR4.
- Supports single-line (`//`) and multi-line (`/* */`) comments, including nested comments.
- Recognizes integers, booleans, chars, strings, identifiers with `_`, and reserved keywords.
- Reports lexical errors (invalid chars, unclosed strings).
- Produces token objects with: type, lexeme, line, column.

### 2. **Parsing (Syntax Analysis)**

- Grammar based on the provided EBNF specification.
- ANTLR4 generates a top-down parser.
- Detects and reports syntax errors with precise location.

### 3. **AST Generation**

- AST nodes generated using ANTLR visitors.
- Includes graphical AST visualization tools.

### 4. **Type Checking & Semantic Analysis**

- Implemented with the Visitor pattern.
- Includes hierarchical **symbol tables** for nested scopes.
- Validates:
  - Redeclaration of identifiers
  - Use-before-declaration
  - Type compatibility in expressions and assignment
  - Parameter matching in function calls
  - Valid return statements
  - Array and hash map indexing rules
  - Function literal declarations and scopes

### 5. **Code Generation (CIL)**

- Uses **Reflection.Emit** to generate:
  - Dynamic assemblies
  - Methods and locals
  - Stack-based CIL instructions
- Produces executable .NET IL code for the entire Monkey language.
