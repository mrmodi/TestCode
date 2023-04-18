namespace TestCode.Models
{
    public class Addlog
    {
        public int log_id { get; set; }
        public string division { get; set; }

        public int prefix_id { get; set; }

        public string prefix_rule { get; set;}

        public DateTime added_time { get; set; }

        public string addedBy { get; set; }
    }
}
