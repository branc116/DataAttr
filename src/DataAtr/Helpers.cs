using DataAtr.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DataAtr { 
    public static class Helpers {
        public static async Task<string> ReadFile(this string path) {
            string fileText = null;
            var attempts = 1;
            while(fileText == null) {
                try {
                    fileText = await File.ReadAllTextAsync(path);
                    return fileText;
                }
                catch {
                    if (attempts % 10 == 0) 
                        System.Console.WriteLine($"Can't read file {path} {attempts++} times");
                    await Task.Delay(10);
                }
            }
            return null;
        }
        public static string GetTypescriptHeader() => @"
import 'jquery';
export class Selectors {
";
        public static string GetTypescriptFooter() => "}";
        public static string ToTypescriptMethod(this DataAtrModel dataAtrModel) 
        {
            var retString = "    public " + dataAtrModel.AtrName.KebbabToCamilCase() + '(';
            var selector = '[' + dataAtrModel.AtrName;
            var paramz = "";
            if (!(dataAtrModel.ValueParameters is null)) {
                var niceParamNames = dataAtrModel.ValueParameters.Select(i => i.Trim('_')).ToList();
                if (dataAtrModel.HasValue && dataAtrModel.ValueParameters.Any())
                {
                    paramz = niceParamNames
                        .Select(i => i + "?: string | number")
                        .Aggregate((i, j) => $"{i}, {j}");
                    var fullSelector = selector + "=" + string.Format(dataAtrModel.ValueTemplate, niceParamNames.Select(i => "${" + i + "}").ToArray()) + ']';
                    retString += paramz + ") {\n"
                              + "        if("
                              + niceParamNames
                                    .Select(i => i + " !== undefined")
                                    .Aggregate((i, j) => $"{i} && {j}")
                              + ") {\n"
                              + $"            return $(`{fullSelector}`);\n"
                              + "        } else {\n"
                              + "             return $('" + selector + "]');\n"
                              + "        }\n"
                              + "    }";
                }
                else
                {
                    retString += ") {\n"
                              + "         return $('" + selector + "]');\n"
                              + "    }";
                }
            }else
            {
                retString += ") {\n"
                          + "         return $('" + selector + "]');\n"
                          + "    }";
            }
            
            return retString;
        }
        public static string KebbabToCamilCase(this string str)
        {
            int state = 0;
            var outChars = new char[str.Length];
            var i = 0;
            foreach(var chr in str)
            {
                if (chr == '-')
                    state = 1;
                else if(state == 1)
                {
                    outChars[i] = chr.ToUpper();
                    i++;
                    state = 0;
                }else if (state == 0)
                {
                    outChars[i] = chr;
                    i++;
                }
            }
            return new string(outChars).TrimEnd('\0');
        }
        public static char ToUpper(this char chr)
        {
            if (chr >= 'a' && chr <= 'z')
            {
                return (char)(chr + ('A' - 'a'));
            }
            return chr;
        }
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            var seenKeys = new HashSet<TKey>();
            foreach (var element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }
        public static TSource AggregateOrNull<TSource>(this IEnumerable<TSource> source, Func<TSource, TSource, TSource> func)
        {
            if (source.Any())
            {
                return source.Aggregate(func);
            }
            return default;
        }
        public static Models.ProjectModel Map(this IEnumerable<(ParsingObject, string)> parsingObject)
        {
            return new ProjectModel
            {
                FileModels = parsingObject.Select(i => new FileModel(i.Item2)
                {
                    DataAtrs = i.Item1.Map().ToList()
                }).ToList()
            };
        }
        public static IEnumerable<DataAtrModel> Map(this ParsingObject parsingObject)
        {
            foreach(var atr in parsingObject?.CurrentHtmlObject?.Attributes.Where(i => i.AttributeType == AttributeType.DataAttribute || i.AttributeType == AttributeType.Attribute || i.AttributeType == AttributeType.Id) ?? Enumerable.Empty<DataAtrModel>())
            {
                yield return atr;
            }
            foreach(var parameters in parsingObject?.TokenParameters?.Where(i => (i?.CurrentHtmlObject?.Tag ?? "") != "script") ?? Enumerable.Empty<ParsingObject>())
            {
                foreach (var atr in parameters.Map())
                {
                    yield return atr;
                }
            }
        }
        public static bool IsFormatValid(this string str, params object[] args)
        {
            try
            {
                string.Format(str, args);
                return true;
            }catch
            {
                return false;
            }
        }
    }
}