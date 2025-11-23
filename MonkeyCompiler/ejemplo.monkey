fn main() : void {
    let age: int = 5
    let name: string = "Monkey"
    let result: int = 10 * (20 / 2)

    let myArray: array<int> = [1, 2, 3, 4, 5]
    let thorsten: hash<string,string> = {"name": "Thorsten", "age": "28"}

    // Access examples
    let firstElement: int = myArray[0]
    let thorstenName: string = thorsten["name"]

    // Function literal assigned to a variable with a function type
    let add: fn(int, int) : int = fn(a: int, b: int) : int {
        return a + b
    }

    // Recursive Fibonacci function (function literal with explicit return type)
    let fibonacci: fn(int) : int = fn(x: int) : int {
        if (x == 0) {
            return 0
        } else {
            if (x == 1) {
                return 1
            } else {
                return fibonacci(x - 1) + fibonacci(x - 2)
            }
        }
    }

    // Print hash, array and fibonacci result
    print(thorsten)           // print hash completo
    print(myArray)            // print array completo
    print(fibonacci(5))       // print resultado de fibonacci(5)
    print(add(5, 7))          // print resultado de add(5,7)
}
