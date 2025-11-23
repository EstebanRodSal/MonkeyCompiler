// ===============================
// Variables y constantes globales
// ===============================

let const MAX_FIB: int = 10
//let const MAX_FIB: int = 10

let greeting: string = "Hello from the Monkey compiler!"
let numbers: array<int> = [1, 2, 3, 4, 5]

let person: hash<string, string> = {
    "name": "Thorsten",
    "role": "Author of Monkey"
}

let ages: hash<string, int> = {
    "Alice": 30,
    "Bob": 25
}

let letterA: char = 'A'

// Función de primer orden almacenada en una variable
let doubleFn: fn(int) : int = fn(x: int) : int {
    return x + x
}

// =======================================
// Funciones auxiliares y de demostración
// =======================================

fn fib(n: int) : int {
    if (n == 0) {
        return 0
    } else {
        if (n == 1) {
            return 1
        } else {
            return fib(n - 1) + fib(n - 2)
        }
    }
}

fn increment(x: int) : int {
    return x + 1
}

// Función que recibe otra función como parámetro (functionType)
fn applyTwice(value: int, f: fn(int) : int) : int {
    let first: int = f(value)
    let second: int = f(first)
    return second
}

// Imprime la serie de Fibonacci desde 0 hasta limit (inclusive) usando recursión
fn printFibFrom(i: int, limit: int) : void {
    if (i > limit) {
        // caso base: nada más que hacer
        return
    } else {
        let current: int = fib(i)
        print(current)
        printFibFrom(i + 1, limit)
    }
}

fn printFibSeries(limit: int) : void {
    print("Fibonacci series:")
    printFibFrom(0, limit)
}

// Imprime algunos elementos de un array
fn demoArray(arr: array<int>) : void {
    print("Array demo:")
    let first: int = arr[0]
    let third: int = arr[2]
    let last: int = arr[4]

    print(first)
    print(third)
    print(last)
}

// Demostración de hash<string, string> y hash<string, int>
fn demoHashes() : void {
    print("Hash demo (person):")
    let name: string = person["name"]
    let role: string = person["role"]
    print(name)
    print(role)

    print("Hash demo (ages):")
    let aliceAge: int = ages["Alice"]
    let bobAge: int = ages["Bob"]
    print(aliceAge)
    print(bobAge)
}

// Demostración de tipos básicos y comparaciones
fn demoBasics() : void {
    print("Basics demo:")

    let x: int = 5
    let y: int = 8
    let message: string = "simple comparison"
    let isLess: bool = x < y
    let sameLetter: bool = letterA == 'A'

    print(message)
    print(isLess)
    print(sameLetter)
}

// Función que devuelve otra función (fn() : fn(int):int)
fn makeAdder(delta: int) : fn(int) : int {
    let addDelta: fn(int) : int = fn(value: int) : int {
        return value + delta
    }
    return addDelta
}

// =========================
// Función principal (main)
// =========================

fn main() : void {
    print("=== Monkey Language Full Demo ===")
    print(greeting)

    // Demostraciones básicas
    demoBasics()

    // Demostración de Fibonacci
    printFibSeries(MAX_FIB)

    // Uso directo de fib en main
    let n: int = 8
    let fibN: int = fib(n)
    print("fib(8):")
    print(fibN)

    // Demostración de arrays
    demoArray(numbers)

    // Demostración de hashes
    demoHashes()

    // Uso de función almacenada en variable (doubleFn)
    let value: int = 5
    let doubled: int = doubleFn(value)
    print("doubleFn(5):")
    print(doubled)

    // Uso de función de orden superior applyTwice
    let applied: int = applyTwice(value, increment)
    print("applyTwice(5, increment):")
    print(applied)

    // Uso de función que devuelve otra función (makeAdder)
    let addTen: fn(int) : int = makeAdder(10)
    let resultAddTen: int = addTen(7)
    print("makeAdder(10)(7):")
    print(resultAddTen)

    // Llamada inmediata a un literal de función (IIFE)
    let squared: int = fn(x: int) : int {
        return x * x
    }(3)

    print("square(3) via IIFE:")
    print(squared)

    // Pequeño if/else con bool
    let isSmall: bool = value < 10
    if (isSmall) {
        print("value is small")
    } else {
        print("value is big")
    }

    // Uso de char sólo para que el tipo se pruebe bien
    print("Character demo:")
    print(letterA)
}
