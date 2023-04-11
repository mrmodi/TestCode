using Microsoft.AspNetCore.Mvc.Rendering;

namespace TestCode.Models
{
    public class ResultViewModel
    {
        public string ExtractedText { get; set; }
        public List<string> Prefix { get; set; }
        public List<int> Counts { get; set; }
      
        public string FilePath { get; set; }
       
        public double AverageConfidence { get; internal set; }
        public long FileSize { get; internal set; }
        public string FormattedFileSize { get; internal set; }
        public string SelectedPrefix { get; internal set; }
         public List<string> SelectedFolder { get;  set; }
        public List<SelectListItem> Folder { get; internal set; }

        public List<string> UploadedFiles { get; set; }
    }
}
