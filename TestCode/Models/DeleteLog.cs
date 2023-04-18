namespace TestCode.Models
{
    public class DeleteLog
    {
        public int log_id { get; set; }
        public string division { get; set; }
        public DateTime delete_time { get; set; }

        public string deletedBy { get; set; }

        public string prefix { get; set; }

        public string aliasName { get; set; }
    }
}
