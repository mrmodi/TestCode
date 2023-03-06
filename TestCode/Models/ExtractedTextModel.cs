using Microsoft.AspNetCore.Mvc.Rendering;

namespace TestCode.Models
{
    public class ExtractedTextModel
    {
        public string Text { get; set; }
       
        public List<SelectListItem> ListItems { get; set; }
        public string SelectedListItem { get; set; }
    }
}
