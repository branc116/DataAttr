using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataAtr.Models.Typescript
{
    public class TypeDeffinition
    {
        public List<DataSelectorDefinitionModel> DataSelectorDefinitionModels { get; set; }
        public TypeDeffinition(ProjectModel projectModel) 
        {
            DataSelectorDefinitionModels = projectModel
                .FileModels
                .SelectMany(i => i.DataAtrs.Select(j => (j.AtrName, j.HasValue, j.ValueParameters, j.ValueTemplate, i.FilePath)))
                .Where(i => i.ValueTemplate != null && i.ValueParameters != null)
                .GroupBy(i => i.AtrName)
                .Select(i => new DataSelectorDefinitionModel(i.Key, i
                    .Select(j => new SelectorDefinitionModel(j.ValueTemplate, j.ValueParameters, j.FilePath))
                    .ToList()))
                .ToList();
        }
        public string TypescriptTypes()
        {
            return DataSelectorDefinitionModels.Select(i => i.GenerateTypescriptTypes())
                .Aggregate((i, j) => i + "\n\n" + j);
        }
        public string TypescriptClass()
        {
            var outStr = "export class Selector {" + DataSelectorDefinitionModels.Select(i => i.GenerateTypescriptComment() + i.GenerateTypescriptMethods().Replace("\n", "\n    ")).Aggregate((i, j) => i + "\n    " + j) + "\n}";
            return outStr;
        }
        public string TypescriptPoject()
        {
            return TypescriptTypes() + "\n" + TypescriptClass();
        }
    }
}
