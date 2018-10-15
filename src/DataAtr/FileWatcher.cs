using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DataAtr.Models;
using Microsoft.AspNetCore.Razor.Language;

namespace DataAtr
{
    public class FileWatcher
    {
        public string Filter { get; set; }
        public string OutFile { get; private set; }
        public string Path { get; private set; }
        public List<FileModel> FileModels { get; set; }
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private bool isIn { get; set; }
        public FileWatcher(string path, string filer, string outFile)
        {
            var a = new System.IO.FileSystemWatcher(path, filer)
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };
            OutFile = outFile;
            Path = path;
            Filter = filer;
            FileModels = new List<FileModel>();
            a.Changed += async (sender, e) =>
            {
                var sw = new Stopwatch();
                sw.Start();
                if (isIn)
                    return;
                lock (this)
                {
                    if (isIn)
                        return;
                    isIn = true;
                }
                if ((e is FileSystemEventArgs ee))
                {
                    FileModels.Clear();
                    await UpdateFiles();
                    //var TsProj = new DataAtr.Models.Typescript.TypeDeffinition(new DataAtr.Models.ProjectModel { FileModels = FileModels }).TypescriptPoject();
                    //File.WriteAllText(outFile, TsProj);
                    //Console.WriteLine($"{DateTime.Now.ToLongTimeString()}: Update ({sw.ElapsedMilliseconds}ms) {(isIn ? "" : "false")}");
                    isIn = false;
                }
            };

        }
        public async Task UpdateFiles()
        {
            var files = Directory.GetFiles(Path, Filter, SearchOption.AllDirectories);
            try
            {
                var f = files.Select(async i => (i, System.IO.Path.GetRelativePath(Path, i), await i.ReadFile()))
                .Select(async i => (new RazorParser().ParseAllAll((await i).Item3), (await i).Item2))
                .Select(async i => await i);
                var a = await Task.WhenAll(f);
                //a.Map();


                var TsProject = new DataAtr.Models.Typescript.TypeDeffinition(a.Map()).TypescriptPoject();
                await File.WriteAllTextAsync(OutFile, TsProject);
            }
            catch (Exception ex)
            {

                throw;
            }

            foreach (var file in files)
            {

                var relativePath = System.IO.Path.GetRelativePath(Path, file);
                var fileModel = FileModels.FirstOrDefault(i => i.FilePath == relativePath);
                if (fileModel == null)
                {
                    fileModel = new FileModel(relativePath);
                    FileModels.Add(fileModel);
                }
                logger.Info($"Starting {file}");
                var text = await file.ReadFile();
                try
                {
                    var temp = new RazorParser().ParseAllAll(text);
                }
                catch (Exception ex)
                {

                }
                //var atrs = AllDataAtrs(text);
                //if (atrs != null)
                //    fileModel.DataAtrs.AddRange(atrs);
                //else
                //{
                //    Console.WriteLine($"ERROR: {file} is null");
                //}
            }
        }
        public List<DataAtrModel> AllDataAtrs(string text)
        {
            var i = 0;
            var atrs = new List<DataAtrModel>();
            if (text == null)
                return null;
            while ((i = text.IndexOf("data-", i)) != -1)
            {
                var end = new[] { text.IndexOf(' ', i), text.IndexOf('=', i), text.IndexOf('>', i) }
                    .Where(j => j != -1)
                    .Min();
                try
                {
                    var atr = text.Substring(i, end - i);
                    var data = GetAtrInfo(text, i, end);
                    if (data.paramNames != null && data.stringTemplate != null)
                    {
                        atrs.Add(new DataAtrModel(atr, true, data.stringTemplate, data.paramNames));
                    }
                    else
                    {
                        atrs.Add(new DataAtrModel(atr, false));
                    }
                }
                catch (Exception e)
                {
                    end = i + 4;
                }
                i = end;
            }
            return atrs;
        }
        public (string stringTemplate, List<string> paramNames) GetAtrInfo(string text, int startIndex, int endIndex)
        {
            if (text == null || startIndex >= endIndex || startIndex == -1 || endIndex == -1 || startIndex != text.IndexOf("data-", startIndex) || text[endIndex] != '=')
                return (null, null);
            var a = State.Begin;
            var parenthDepth = 0;
            var stringTemplate = string.Empty;
            var paramNames = new List<string>();
            var curParam = string.Empty;
            var stringLitrals = new List<string>();
            var curStringLitral = string.Empty;
            var stack = new Stack<State>();
            var specialCSharpChars = new HashSet<char> { '.', ' ', '\n', '\r', '?', ':', '[', ']' };
            var wholeStr = string.Empty;
            for (int i = endIndex + 2; a != State.End && i < text.Length; i++)
            {
                var v = text[i];
                wholeStr += v;
                switch (a)
                {
                    case State.Begin:
                        if (v == '"')
                            a = State.End;
                        else if (v == '@')
                        {
                            a = State.CSharp;
                            stack.Push(State.Razor);
                            stringTemplate += '{' + $"{paramNames.Count}" + '}';
                        }
                        else if (v == '$' && text[i + 1] == '{')
                        {
                            stack.Push(State.Begin);
                            stringTemplate += '{' + $"{paramNames.Count}" + '}';
                            i++;
                            a = State.TemplateVariable;
                        }
                        else
                        {
                            a = State.Razor;
                            stringTemplate += v;
                        }
                        break;
                    case State.String:
                        if (v == '"')
                        {
                            stringLitrals.Add(curStringLitral);
                            curStringLitral = string.Empty;
                            a = stack.Pop();
                        }
                        else if (v == '{')
                        {
                            stack.Push(State.String);
                            curStringLitral += '{' + $"{stringLitrals.Count}" + '}';
                            a = State.CSharp;
                        }
                        else
                        {
                            curStringLitral += v;
                        }
                        break;
                    case State.CSharp:
                        if (v == '(')
                        {
                            parenthDepth++;
                        }
                        else if (v == ')')
                        {
                            parenthDepth--;
                        }
                        else if (v == '"')
                        {
                            if (parenthDepth == 0)
                            {
                                paramNames.Add(curParam);
                                curParam = string.Empty;
                                a = State.End;
                            }
                            else
                            {
                                stack.Push(State.CSharp);
                                a = State.String;
                            }
                        }
                        else
                        {
                            if (specialCSharpChars.Contains(v))
                                curParam += '_';
                            else
                                curParam += v;
                        }
                        break;
                    case State.Razor:
                        if (v == '@')
                        {
                            stack.Push(State.Razor);
                            stringTemplate += '{' + $"{paramNames.Count}" + '}';
                            a = State.CSharp;
                        }
                        else if (v == '"')
                        {
                            a = State.End;
                        }
                        else if (v == '$' && text[i + 1] == '{')
                        {
                            stack.Push(State.Razor);
                            stringTemplate += '{' + $"{paramNames.Count}" + '}';
                            i++;
                            a = State.TemplateVariable;
                        }
                        else
                        {
                            stringTemplate += v;
                        }
                        break;
                    case State.TemplateVariable:
                        if (v == '}')
                        {
                            paramNames.Add(curParam);
                            curParam = string.Empty;
                            a = stack.Pop();
                        }
                        else
                        {
                            curParam += v;
                        }
                        break;
                    case State.End:
                        break;
                }
            }
            if (a != State.End)
                return (null, null);
            return (stringTemplate, paramNames);
        }
    }
}
