using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataAtr.Models.Typescript
{
    public class DataSelectorDefinitionModel
    {
        private const string SelectorVarName = "selector";
        private const string ParamName = "dataAttrTemplate";
        private const string ConditionalParamName = "dataAttrParams";
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public string AttributeValue { get; set; }
        public string CamilCaseAttribute => AttributeValue.KebbabToCamilCase();
        public string TypescriptType => $"{CamilCaseAttribute}Type";
        public string TypescriptConditionalType => $"{CamilCaseAttribute}Conditional";
        private string TypescriptCondtionalTypesDefinition => SelectorDefinitionModels
                        .DistinctBy(i => i.Template)
                        .Select(i => "T extends \"" + i.Template + "\" ? " + i.TypescriptTypeArray)
                        .Aggregate((i, j) => i + " :\n    " + j) + "\n    : never";
        public List<SelectorDefinitionModel> SelectorDefinitionModels { get; set; }

        public DataSelectorDefinitionModel()
        {
            AttributeValue = string.Empty;
            SelectorDefinitionModels = new List<SelectorDefinitionModel>();
        }
        public DataSelectorDefinitionModel(string attributeValue, List<SelectorDefinitionModel> selectorDefinitionModels)
        {
            AttributeValue = attributeValue;
            SelectorDefinitionModels = selectorDefinitionModels
                .DistinctBy(i => (i.Template, i.AppearedInFiles))
                .ToList();
        }

        public string GenerateTypescriptTypes()
        {
            var templates = SelectorDefinitionModels
                .DistinctBy(i => i.Template)
                .Select(i => '"' +  i.Template + '"')
                .Aggregate((i, j) => i + " | " + j);
            var conditionalTemplates = TypescriptCondtionalTypesDefinition;
            var types = $"export type {TypescriptType} = {templates};";
            var conditionalTypes = $"export type {TypescriptConditionalType}<T extends {TypescriptType}> = {conditionalTemplates};";
            return types + "\n" + conditionalTypes;
        }
        public string GenerateTypescriptMethods()
        {
            var outStr = $"public {CamilCaseAttribute}<T extends {TypescriptType}>({ParamName}?: T, {ConditionalParamName}?: {TypescriptConditionalType}<T>) " + "{\n    " + $"var {SelectorVarName} = '[{AttributeValue}';";
            var condtions = SelectorDefinitionModels
                .DistinctBy(i => i.Template)
                .Select(i => i.GenerateTypescriptIfStatement(SelectorVarName, ParamName, ConditionalParamName)
                    .Replace("\n", "\n    "))
                .Aggregate((i, j) => i + "\nelse " + j) +
                $"\n    else \n        {SelectorVarName} += ']';\n";
            var returnStaement = $"    return $({SelectorVarName})\n"+ "}\n";
            return $"{outStr}\n    {condtions}\n{returnStaement}";
        }
        public string GenerateTypescriptComment()
        {

            var files = SelectorDefinitionModels
                .GroupBy(i => i.AppearedInFiles)
                .Select(i => i.Key + ":<><>" + i
                    .Select(j => {
                        logger.Debug($"Template is: {j.Template}");
                        return AttributeValue + "=" + string
                             .Format(j.Template, j.Parameters
                                 .Select(k => "{" + k + "}")
                                 .ToArray());
                    })
                    .Aggregate((j, k) => j + "<><>" + k))
                .Aggregate((i, j) => i + "\n  * " + j)
                .Replace("<><>", "\n  *    ");
            var comment = $@"
/**
  * 
  * {files}
  */
";
            return comment;
        }
    }
}