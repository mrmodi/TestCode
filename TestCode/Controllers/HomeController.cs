using IronOcr;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Drawing;
using TestCode.Models;
using System.IO;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static IronOcr.OcrResult;
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

               

                List<string> values = new List<string>();
                List<int> counts = new List<int>();


                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT listtext " +
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


                    SqlCommand command = new SqlCommand(query, connection);
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        string value = reader.GetString(0);
                        values.Add(value);
                    }
                    reader.Close();
                }

                // Call a method to extract text data from the uploaded file using IronOCR
                var text = ExtractTextFromUploadedFile(tempFilePath);


                string extractedText = text.ToString();

               

                foreach (string value in values)
                {

                    int count = Regex.Matches(extractedText, value, RegexOptions.IgnoreCase).Count;
                    counts.Add(count);

                    }

                ResultViewModel viewModel = new ResultViewModel
                {
                    ExtractedText = extractedText,
                    Values = values,
                    Counts = counts
                };

              
                return View(viewModel);
            }

            // Handle errors if the file is not valid
            return View("Error");
        }

        private string ExtractTextFromUploadedFile(string tempFilePath)
        {
            var ocr = new IronTesseract();
            var result = ocr.Read(tempFilePath);

            foreach (var page in result.Pages)
            {
                foreach (var word in page.Words)
                {
                    // Iterate through the characters in the word and get their confidence scores
                    foreach (var character in word.Characters)
                    {
                        var characterText = character.Text;
                        var confidenceScore = character.Confidence;

                        // Do something with the character text and its confidence score

                        Console.WriteLine($"Character: {characterText}, Confidence Score: {confidenceScore}");
                    }
                }
            }


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