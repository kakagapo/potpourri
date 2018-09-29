using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace CompileAPIDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            string code =
@"using System;

namespace HelloWorld
{
  class Program
  {
    static void Main(string[] args)
    {
      Console.Out.WriteLine(""Hello world!!!"");
      Console.Read();
    }
  }
}";

            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(code);
            CSharpCompilation compilation = CSharpCompilation.Create(
              "HelloWorld.exe",
              options: new CSharpCompilationOptions(OutputKind.ConsoleApplication),
              syntaxTrees: new[] { tree },
              references: new[]
              {
                  MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
              });

            using (var stream = new MemoryStream())
            {
                EmitResult compileResult = compilation.Emit(stream);
                Assembly assembly = Assembly.Load(stream.GetBuffer());
                assembly.EntryPoint.Invoke(null,
                  BindingFlags.NonPublic | BindingFlags.Static,
                  null, new object[] { null }, null);
            }
        }
    }
}
