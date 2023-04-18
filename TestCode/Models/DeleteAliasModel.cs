using Microsoft.AspNetCore.Mvc.Rendering;

namespace TestCode.Models
{
    public class DeleteAliasModel
    {
        public List<SelectListItem> AliasNames { get; set; }
        public string Listtext { get; set; }

        public string SelectedAliasName { get; set; }
    }
}
