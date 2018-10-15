using System.Collections.Generic;
using System.Linq;

namespace DataAtr.Models {
    public class FileModel {
        public FileModel(string filePath)
        {
            FilePath = filePath;
            DataAtrs = new List<DataAtrModel>();
        }
        public string FilePath { get; set; }
        public List<DataAtrModel> DataAtrs { get; set; }
        public override string ToString() {
            if (DataAtrs.Any()) 
                return FilePath + ": \n  " + 
                DataAtrs.Select(i => {
                    if (i.HasValue)
                    {
                        return i.AtrName + " = " + (i.ValueTemplate ?? "") + ", " + ((i.ValueParameters?.Any() ?? false) ? i.ValueParameters.Aggregate((ii, jj) => ii + ", " + jj) : "none");
                    }
                    return i.AtrName;
                }).Aggregate((i,j) => i + "\n  " + j);
            else return FilePath;
        }
    }
}