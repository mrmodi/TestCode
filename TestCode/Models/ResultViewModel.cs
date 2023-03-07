using Microsoft.AspNetCore.Mvc.Rendering;

namespace TestCode.Models
{
    public class ResultViewModel
    {
        public string ExtractedText { get; set; }
        public List<string> Values { get; set; }
        public List<int> Counts { get; set; }
      
        public string FilePath { get; set; }
       
        public double AverageConfidence { get; internal set; }
        public long FileSize { get; internal set; }
        public string FormattedFileSize { get; internal set; }
        public string SelectedValue { get; internal set; }
    }
}
