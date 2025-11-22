// Ast/Types.cs
namespace MonkeyCompiler.Ast
{
    public class TypeNode
    {
        public string Name { get; }

        public TypeNode(string name)
        {
            Name = name;
        }

        public override string ToString() => Name;
    }
}