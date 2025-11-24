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

fn max3(a: int, b: int, c: int) : int {
    let maxValue: int = a

    if (b > maxValue) {
        let newMax: int = b
        return max3Internal(newMax, c)
    } else {
        return max3Internal(maxValue, c)
    }
}

fn max3Internal(currentMax: int, other: int) : int {
    if (other > currentMax) {
        return other
    } else {
        return currentMax
    }
}

fn main() : void {
    let x: int = add(3, 4)
    let fact5: int = factorial(5)

    let numbers: array<int> = [1, 2, 3]
    let total: int = sumArray(numbers)

    let fullName: string = buildFullName("Ada", "Lovelace")

    let age: int = 20
    let adult: bool = isAdult(age)

    let biggest: int = max3(10, 5, 8)

    print(x)
    print(fact5)
    print(total)
    print(fullName)
    print(adult)
    print(biggest)
}
