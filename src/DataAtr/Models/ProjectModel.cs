using System;
using System.Collections.Generic;
using System.Text;

namespace DataAtr.Models
{
    public class ProjectModel
    {
        public ProjectModel()
        {
            FileModels = new List<FileModel>();
        }
        public List<FileModel> FileModels { get; set; }
    }
}
