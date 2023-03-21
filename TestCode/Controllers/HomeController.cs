    using IronOcr;
    using Microsoft.AspNetCore.Mvc;
    using System.Diagnostics;
    using TestCode.Models;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using System.Data;
    using Microsoft.Data.SqlClient;
    using System.Text.RegularExpressions;
    

    namespace TestCode.Controllers
    {
        public class HomeController : Controller
        {
            private readonly ILogger<HomeController> _logger;

            string connectionString = "Server=67.231.31.66;Database=CapStone2023_OCR;User ID=CapStone_User;Password=r8#PF9%0Sw;trustServerCertificate = yes;";

            public HomeController(ILogger<HomeController> logger)
            {
                _logger = logger;
            }

            public IActionResult Index()
            {


                string query = "SELECT id, listtext, code, refid, score, folder, name " +
                   "FROM ( " +
                   "  SELECT DISTINCT t1.id, t1.listtext, t1.code, t1.refid, t1.score, t2.folder, t3.name " +
                   "  FROM lst_docname as t1 " +
                   "  LEFT JOIN tbl_doc_allocations_form_names as t2 ON t1.listtext = t2.prefix " +
                   "  LEFT JOIN tbl_folder t3 ON t3.folder_id = folder " +
                   "  WHERE t1.id = '12000' " +
                   "  AND t1.listtext <> '' " +
                   "  AND t2.deleted = 'false' " +
                   "  AND t2.division = '12000' " +
                   ") AS subquery " +
                   "ORDER BY subquery.folder ASC";

                // Create a new SqlConnection and SqlCommand
                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // Open the connection
                    connection.Open();

                    // Create a new SqlDataAdapter and fill a DataTable with the results
                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        DataTable table = new DataTable();
                        adapter.Fill(table);

                        // Convert the DataTable to a list of strings
                        var listItems = table.AsEnumerable()
                                             .Select(row => new SelectListItem
                                             {
                                                 Value = row.Field<string>("listtext"),
                                                 Text = row.Field<string>("listtext")
                                             })
                                             .ToList();

                        var model = new ExtractedTextModel
                        {
                            ListItems = listItems
                        };

                        // Pass the model to the view
                        return View(model);
                    }
                }
            }

              
        

            public IActionResult Privacy()
            {

                return View();
            }



        [HttpPost]
        public IActionResult Upload(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                
                    var originalFileName = Path.GetFileName(file.FileName);
                    var fileExtension = Path.GetExtension(originalFileName);


                    //Path.Combine(Path.GetTempPath(), "MyTempFolder");
                    string tempFolderPath = @"C:\Users\manan\OneDrive\Desktop\ProjectPdf";
                    Directory.CreateDirectory(tempFolderPath);

                    // string uniqueFileName = Path.GetRandomFileName();
                    string tempFilePath = Path.Combine(tempFolderPath, originalFileName);


                    // Save the uploaded file to a temporary location
                    using (var stream = new FileStream(tempFilePath, FileMode.Create))
                    {

                        file.CopyTo(stream);
                    }

                    List<string> prefixList = new List<string>();
                    List<int> counts = new List<int>();


                    // Call a method to extract text data from the uploaded file using IronOCR
                    var text = ExtractTextFromUploadedFile(tempFilePath);
                    string extractedText = text.ToString();

                    // prefix dropdown
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        string prefix_query = "SELECT listtext,name " +
                                        "FROM ( " +
                                        " SELECT DISTINCT t1.id, t1.listtext, t1.code, t1.refid, t1.score, t2.folder, t3.name " +
                                        " FROM lst_docname as t1 " +
                                        " LEFT JOIN tbl_doc_allocations_form_names as t2 ON t1.listtext = t2.prefix " +
                                        " LEFT JOIN tbl_folder t3 ON t3.folder_id = folder " +
                                        " WHERE t1.id = '12000' " +
                                        " AND t1.listtext <> '' " +
                                        " AND t2.deleted = 'false' " +
                                        " AND t2.division = '12000' " +
                                        ") AS subquery " +
                                        "ORDER BY subquery.folder ASC";


                        SqlCommand command = new SqlCommand(prefix_query, connection);
                        SqlDataReader reader = command.ExecuteReader();

                        while (reader.Read())
                        {
                            string value = reader.GetString(0);
                            prefixList.Add(value);
                        }
                        reader.Close();
                    }


                    //folder dropdown
                    List<SelectListItem> folderList = new List<SelectListItem>();

                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        string folder_query = "SELECT  distinct name " +
                                        "FROM ( " +
                                        " SELECT DISTINCT t1.id, t1.listtext, t1.code, t1.refid, t1.score, t2.folder, t3.name " +
                                        " FROM lst_docname as t1 " +
                                        " LEFT JOIN tbl_doc_allocations_form_names as t2 ON t1.listtext = t2.prefix " +
                                        " LEFT JOIN tbl_folder t3 ON t3.folder_id = folder " +
                                        " WHERE t1.id = '12000' " +
                                        " AND t1.listtext <> '' " +
                                        " AND t2.deleted = 'false' " +
                                        " AND t2.division = '12000' " +
                                        ") AS subquery " +
                                        "ORDER BY subquery.name ASC";


                        SqlCommand command = new SqlCommand(folder_query, connection);
                        SqlDataReader reader = command.ExecuteReader();

                        while (reader.Read())
                        {
                            string value = reader.GetString(0);
                            folderList.Add(new SelectListItem { Text = value, Value = value });
                        }
                        reader.Close();
                    }




                    foreach (string value in prefixList)
                    {

                        int count = Regex.Matches(extractedText, value, RegexOptions.IgnoreCase).Count;
                        counts.Add(count);

                    }

                    int maxCount = counts.Max();
                    List<string> selectedPrefix = new List<string>();

                    //selected prefix
                    if (maxCount > 0)
                    {
                        // Add only the values with the maximum count to a new list


                        for (int i = 0; i < prefixList.Count; i++)
                        {
                            if (counts[i] == maxCount)
                            {
                                selectedPrefix.Add(prefixList[i]);
                            }
                        }
                    }


                    //folder for selected prefix
                    string selectedFolderQuery = "SELECT  name " +
                                        "FROM ( " +
                                        " SELECT DISTINCT t1.id, t1.listtext, t1.code, t1.refid, t1.score, t2.folder, t3.name " +
                                        " FROM lst_docname as t1 " +
                                        " LEFT JOIN tbl_doc_allocations_form_names as t2 ON t1.listtext = t2.prefix " +
                                        " LEFT JOIN tbl_folder t3 ON t3.folder_id = folder " +
                                        " WHERE t1.id = '12000' " +
                                        " AND t1.listtext <> '' " +
                                        " AND t2.deleted = 'false' " +
                                        " AND t2.division = '12000' " +
                                        " AND t1.listtext =  @param1 " +
                                        ") AS subquery " +
                                        "ORDER BY subquery.name ASC";

                    List<string> selectedFolder = new List<string>();
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        SqlCommand command = new SqlCommand(selectedFolderQuery, connection);
                        if (maxCount > 0)
                            command.Parameters.AddWithValue("@param1", selectedPrefix.FirstOrDefault());
                        else
                            command.Parameters.AddWithValue("@param1", "select an option");
                        connection.Open();
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            string name = reader.GetString(0);
                            selectedFolder.Add(name);
                        }
                        reader.Close();
                    }




                    if (file.Length > 0)
                    {
                        // Get file size
                        long fileSizeInBytes = file.Length;
                        // Get formatted file size
                        string formattedFileSize = FormatFileSize(fileSizeInBytes);


                        var ocr = new IronTesseract();
                        var result = ocr.Read(tempFilePath);

                        double totalConfidence = 0.0;
                        int characterCount = 0;
                        foreach (var page in result.Pages)
                        {
                            foreach (var word in page.Words)
                            {
                                foreach (var character in word.Characters)
                                {
                                    totalConfidence += character.Confidence;
                                    characterCount++;
                                }
                            }
                        }
                        //double averageConfidence = characterCount > 0 ? (totalConfidence / characterCount) / 100 : 0.00;
                        double averageConfidence = (totalConfidence / characterCount) / 100;


                        // Your existing code to save the uploaded file to a temporary location...

                        ResultViewModel viewModel = new ResultViewModel
                        {
                            ExtractedText = extractedText,
                            Prefix = prefixList,
                            Folder = folderList,
                            Counts = new List<int> { maxCount },
                            FilePath = tempFilePath,
                            AverageConfidence = averageConfidence,
                            FileSize = fileSizeInBytes,
                            FormattedFileSize = formattedFileSize,
                            SelectedPrefix = selectedPrefix.FirstOrDefault(),
                            SelectedFolder = selectedFolder

                        };


                        return View(viewModel);
                    }
                
            }
            // Handle errors if the file is not valid
            return View("Error");
            }
           
      
       
        private string FormatFileSize(long fileSizeInBytes)
                {
                    const int scale = 1024;
                    string[] orders = new string[] { "TB", "GB", "MB", "KB", "Bytes" };
                    long max = (long)Math.Pow(scale, orders.Length - 1);

                    foreach (string order in orders)
                    {
                        if (fileSizeInBytes > max)
                            return string.Format("{0:##.##} {1}", decimal.Divide(fileSizeInBytes, max), order);

                        max /= scale;
                    }

                    return "0 Bytes";
                }

                private string ExtractTextFromUploadedFile(string tempFilePath)
            {
                var ocr = new IronTesseract();
                var result = ocr.Read(tempFilePath);
 
                var words = result.Text.Split(' ');
                var first100Words = string.Join(' ', words.Take(100));

                return first100Words;
            }
       

            [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
            public IActionResult Error()
            {
                return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }


        }
    }