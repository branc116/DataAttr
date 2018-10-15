using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataAtr.Models.Typescript
{
    public class SelectorDefinitionModel
    {
        public string Template { get; set; }
        public List<string> Parameters { get; set; }
        public string AppearedInFiles { get; set; }
        public SelectorDefinitionModel(string template, List<string> parameters, string appearsInFiles)
        {
            Template = template;
            Parameters = parameters;
            AppearedInFiles = appearsInFiles;
        }
        public string TypescriptTypeArray => Parameters.Count > 1 ? "[" + Parameters
            .Select(_ => "number | string")
            .Aggregate((j, k) => j + ", " + k) + "]" : 
            Parameters.Count > 0 ? "number | string" : 
            "undefined";
        public string GenerateTypescriptIfStatement(string selectorVarName, string paramName, string conditionalParamName)
        {
            var templateStr = Template.Replace("{0}", "${" + conditionalParamName + "}");
            var outStr = $"if({paramName} === '{Template}')\n    {selectorVarName} += '=' + `{templateStr}` + ']';";
            return outStr;
        }
    }
}
