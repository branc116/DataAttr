using System;
using System.Threading.Tasks;
using System.IO;
using DataAtr;
using System.Linq;

namespace DataAtsr.Test
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Use is like this:\n    dataAtr [folderWithRazorFiles] [out.d.ts file]");
            }
            var inDir = args[0];
            var outFile = args[1];
            var file = new FileWatcher(inDir, "*.cshtml", outFile);
            await file.UpdateFiles();
            //System.Console.WriteLine(file.FileModels.Select(i => i.ToString()).Aggregate((i, j) => i + "\n" + j));
            //var outTs = new TypescriptFileBuilder(new DataAtr.Models.ProjectModel() { FileModels = file.FileModels }).GetTypescript();
            //var TsProj = new DataAtr.Models.Typescript.TypeDeffinition(new DataAtr.Models.ProjectModel { FileModels = file.FileModels }).TypescriptPoject();
            //File.WriteAllText(outFile, TsProj);
            //Console.WriteLine("Started");
            while (Console.ReadKey().KeyChar != '\0');
        }
    }
}
