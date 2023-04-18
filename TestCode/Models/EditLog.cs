namespace TestCode.Models
{
    public class EditLog
    {
        public int log_id { get; set; }
        public string division { get; set; }

        public string prefix { get; set; }

        public string old_rule { get; set; }

        public string edited_rule { get; set; }      

        public DateTime edit_time { get; set; }

        public string editedBy { get; set; }
    }
}
