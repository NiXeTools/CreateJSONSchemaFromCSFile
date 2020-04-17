using CommandLine;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json.Schema.Generation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CreateJSONSchemaFromCSFile
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(x => {

                    var syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(x.CSFile));

                    CSharpCompilation compilation = CSharpCompilation.Create(
                        "assemblyName",
                        new[] { syntaxTree },
                        new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                            MetadataReference.CreateFromFile(typeof(System.ComponentModel.DefaultValueAttribute).Assembly.Location) },
                        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                    using (var dllStream = new MemoryStream())
                    using (var pdbStream = new MemoryStream())
                    {
                        var dllPath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName() + ".dll");
                        var emitResult = compilation.Emit(dllPath);
                        if (emitResult.Success)
                        {
                            var assembly = Assembly.LoadFile(dllPath);
                            var type = assembly.GetTypes().FirstOrDefault(y => y.Name == x.ClassName);

                            JSchemaGenerator generator = new JSchemaGenerator();
                            var schema = generator.Generate(type);
                            Console.WriteLine(schema.ToString());
                            File.WriteAllText(x.Output, schema.ToString());

                        }
                        else
                        {
                            foreach (var item in emitResult.Diagnostics)
                            {
                                Console.WriteLine($"Message:{item.GetMessage()}");
                            }
                        }
                    }
                });
            
        }
    }

    class Options
    {
        [Option('c', "csfile")]
        public string CSFile { get; set; }

        [Option('n', "classname")]
        public string ClassName { get; set; }

        [Option('a', "additionaldlls", Separator = ';', Required = false)]
        public IList<string> AdditionalDlls { get; set; }

        [Option('o', "outputfile")]
        public string Output { get; set; }
    }
}
