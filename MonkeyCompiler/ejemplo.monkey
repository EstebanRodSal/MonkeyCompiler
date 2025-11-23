fn add(a: int, b: int) : int {
    return a + b
}

fn factorial(n: int) : int {
    if (n == 0) {
        return 1
    } else {
        let previous: int = n - 1
        let result: int = factorial(previous)
        return n * result
    }
}

fn sumArray(values: array<int>) : int {
    let first: int = values[0]
    let second: int = values[1]
    let third: int = values[2]
    let sum: int = first + second + third
    return sum
}

fn buildFullName(firstName: string, lastName: string) : string {
    let space: string = " "
    let withSpace: string = firstName + space
    let fullName: string = withSpace + lastName
    return fullName
}

fn isAdult(age: int) : bool {
    if (age >= 18) {
        return true
    } else {
        return false
    }
}

fn logCalculation(label: string, value: int) : void {
    print(label)
    print(value)
    return
}

fn applyTwice(f: fn(int) : int, value: int) : int {
    let firstResult: int = f(value)
    let secondResult: int = f(firstResult)
    return secondResult
}

fn makeMultiplier(factor: int) : fn(int) : int {
    let inner: fn(int) : int = fn(x: int) : int {
        return x * factor
    }
    return inner
}

fn getCountryName(countries: hash<string,string>, code: string) : string {
    let name: string = countries[code]
    return name
}

fn main() : void {
    // Constante y aritmética básica
    let const MAX_AGE: int = 120
    let a: int = 10
    let b: int = 20
    let c: int = add(a, b)
    print("Result of add:")
    print(c)

    // Factorial
    let n: int = 5
    let factN: int = factorial(n)
    print("Factorial of 5:")
    print(factN)

    // Arreglos y acceso a elementos
    let numbers: array<int> = [1, 2, 3]
    let total: int = sumArray(numbers)
    print("Sum of [1, 2, 3]:")
    print(total)

    // Strings y concatenación
    let full: string = buildFullName("Esteban", "Rodriguez")
    print("Full name:")
    print(full)

    // Caracteres
    let initial: char = 'E'
    print("Initial:")
    print(initial)

    // Hash map con llaves y valores string
    let countries: hash<string,string> = {"CR": "Costa Rica", "DE": "Germany"}
    let country: string = getCountryName(countries, "CR")
    print("Country for CR:")
    print(country)

    
    let age: int = 6

    let adult: bool = isAdult(age)
    if (adult) {
        print("Adult")
    } else {
        print("Minor")
    }

    // Literales de función y tipos de función
    let doubleFn: fn(int) : int = fn(x: int) : int {
        return x * 2
    }
    let applied: int = applyTwice(doubleFn, 5)
    print("applyTwice(double, 5):")
    print(applied)

    // Función que retorna otra función
    let triple: fn(int) : int = makeMultiplier(3)
    let tripled: int = triple(7)
    print("makeMultiplier(3) applied to 7:")
    print(tripled)

    // Uso de una función void
    logCalculation("Final result:", tripled)

    // Arreglo de strings
    let names: array<string> = ["Monkey", "Thorsten", "Gilberth"]
    let firstName: string = names[0]
    print("First name in array:")
    print(firstName)

    return
}
