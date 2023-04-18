using System.ComponentModel.DataAnnotations;

namespace TestCode.Models
{
    public class UploadLog
    {
        [Key]
        public int log_id { get; set; }
        public string division { get; set; }
        public string file_name { get; set; }
        public string file_size  { get; set; }
        public float average_confidence_score { get; set; }
        public string document_prefix { get; set; }
        public string document_folder { get; set; }
        public string extracted_text { get; set; }
        public DateTime uploaded_time { get; set; }
      
    }
}
