using DataAtr.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataAtr
{
    public class TypescriptFileBuilder
    {
        private ProjectModel _projectModel;
        public TypescriptFileBuilder(ProjectModel projectModel)
        {
            _projectModel = projectModel;
        }
        public string GetTypescript()
        {
            return Helpers.GetTypescriptHeader() + _projectModel
                .FileModels
                .SelectMany(i => i.DataAtrs)
                .Select(i => i.ToTypescriptMethod())
                .Aggregate((i, j) => $"{i}\n\n{j}")
                + Helpers.GetTypescriptFooter();
        }
    }
}
