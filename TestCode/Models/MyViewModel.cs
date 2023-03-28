namespace TestCode.Models
{
    public class MyViewModel
    {
        public List<object> prefixList;

        public string SelectedFolderName { get;  set; }
        public object FolderList { get; internal set; }
    }
}