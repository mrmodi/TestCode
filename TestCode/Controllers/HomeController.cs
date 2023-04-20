using IronOcr;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TestCode.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;
using Spire.Xls;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Client;
using System.Dynamic;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using Google.Protobuf;
using NLog;
using Newtonsoft.Json;
using Serilog.Sinks.File;
using Microsoft.EntityFrameworkCore;

namespace TestCode.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly IConfiguration _config;

        private readonly LogDbContext _dbContext;

        public HomeController(ILogger<HomeController> logger,  LogDbContext dbContext, IConfiguration config)
        {
            _logger = logger;           
            _dbContext = dbContext;
            _config = config;
        }

        

        public IActionResult Index()
        {
            string connectionString = _config.GetConnectionString("DefaultConnection");

            try
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

                       _logger.LogInformation($"Query executed with {model} results");
                        // Pass the model to the view
                        return View(model);

                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing Index action.");
                return View("Error");
            }
        }




        public IActionResult Privacy()
        {

            return View();
        }



        [HttpPost]
        public IActionResult Upload(List<IFormFile> files)
        {
            string connectionString = _config.GetConnectionString("DefaultConnection");

            // List<ResultViewModel> viewModels = new List<ResultViewModel>();
            try
            {
                var resultsList = new List<ResultViewModel>();

                _logger.LogInformation("\n Starting file upload");

                foreach (var file in files)
                {
                    //ResultViewModel viewModel = new ResultViewModel();

                    if (file != null && file.Length > 0)
                    {

                        var originalFileName = Path.GetFileName(file.FileName);
                        var originalExcelFileName = Path.GetFileNameWithoutExtension(file.FileName);
                        var extension = Path.GetExtension(originalFileName);


                        //Path.Combine(Path.GetTempPath(), "MyTempFolder");
                        string tempFolderPath = @"C:\Users\manan\OneDrive\Desktop\ProjectPdf";
                        Directory.CreateDirectory(tempFolderPath);

                        // string uniqueFileName = Path.GetRandomFileName();
                        string tempFilePath = Path.Combine(tempFolderPath, originalFileName);


                        _logger.LogInformation($"\n Uploading file {originalFileName}");


                        // Save the uploaded file to a temporary location
                        using (var stream = new FileStream(tempFilePath, FileMode.Create))
                        {

                            file.CopyTo(stream);
                        }

                        List<string> prefixList = new List<string>();
                        List<string> prefixAliasList = new List<string>();
                        List<int> counts = new List<int>();


                        // Call a method to extract text data from the uploaded file using IronOCR
                        var text = ExtractTextFromUploadedFile(tempFilePath);
                        string extractedText = text.ToString();


                        _logger.LogInformation($"\n Extracted text from file {originalFileName} : {extractedText}");


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


                            int rowCount = 0;

                            while (reader.Read())
                            {
                                string value = reader.GetString(0);

                                prefixList.Add(value);
                                rowCount++;

                            }
                            reader.Close();

                            _logger.LogInformation($"\n prefix Query executed with {rowCount} results");

                        }


                        using (SqlConnection connection = new SqlConnection(connectionString))
                        {
                            connection.Open();
                            string alias_query = "SELECT alias_name, listtext, name " +
                       "FROM ( " +
                       " SELECT DISTINCT t1.id, t1.listtext, t1.prefix_id, t4.alias_name, t1.code, t1.refid, t1.score, t2.folder, t3.name " +
                       " FROM lst_docname as t1 " +
                       " LEFT JOIN tbl_doc_allocations_form_names as t2 ON t1.listtext = t2.prefix " +
                       " LEFT JOIN tbl_folder t3 ON t3.folder_id = t2.folder " +
                       " LEFT JOIN aliases_table t4 ON t1.prefix_id = t4.prefix_id " +
                       " WHERE t1.id = '12000' " +
                       " AND t1.listtext <> '' " +
                       " AND t2.deleted = 'false' " +
                       " AND t2.division = '12000' " +
                       ") AS subquery " +
                       "ORDER BY subquery.folder ASC";


                            SqlCommand command = new SqlCommand(alias_query, connection);
                            SqlDataReader reader = command.ExecuteReader();

                            int rowCount = 0;

                            while (reader.Read())
                            {

                                string alias = reader.GetString(0);

                                prefixAliasList.Add(alias);
                                rowCount++;
                            }
                            reader.Close();

                           _logger.LogInformation($"\n Alias Query executed with {rowCount} results");
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

                            int rowCount = 0;

                            while (reader.Read())
                            {
                                string value = reader.GetString(0);
                                folderList.Add(new SelectListItem { Text = value, Value = value });
                                rowCount++;
                            }
                            reader.Close();

                            _logger.LogInformation($"\n Folder Query executed with {rowCount} results");

                        }




                        foreach (string value in prefixAliasList)
                        {

                            int count = Regex.Matches(extractedText, value, RegexOptions.IgnoreCase).Count;

                            counts.Add(count);

                        }

                        int maxCount = counts.Max();
                        Console.WriteLine(counts.ToString());
                        Console.WriteLine(counts.Max().ToString());
                        List<string> selectedPrefix = new List<string>();

                        _logger.LogInformation($"\n Matched prefix Maximum count: {maxCount}");

                        //selected prefix
                        if (maxCount > 0)
                        {
                            // Add only the values with the maximum count to a new list


                            for (int i = 0; i < prefixAliasList.Count; i++)
                            {
                                if (counts[i] == maxCount)
                                {
                                    selectedPrefix.Add(prefixAliasList[i]);
                                }
                            }

                            _logger.LogInformation($"\n Selected prefix: ");

                            foreach (var prefix in selectedPrefix)
                            {
                                _logger.LogInformation($"- {prefix}");
                            }
                        }

                        Console.WriteLine(prefixAliasList.Max().Count());

                        //folder for selected prefix
                        string selectedFolderQuery = "SELECT  name,listtext " +
                                            "FROM ( " +
                                            " SELECT DISTINCT t1.id, t1.listtext, t1.code, t1.refid, t1.score, t2.folder, t3.name " +
                                            " FROM lst_docname as t1 " +
                                            " LEFT JOIN tbl_doc_allocations_form_names as t2 ON t1.listtext = t2.prefix " +
                                            " LEFT JOIN tbl_folder t3 ON t3.folder_id = folder " +
                                            "Left JOIN aliases_table t4 ON t1.prefix_id = t4.prefix_id" +
                                            " WHERE t1.id = '12000' " +
                                            " AND t1.listtext <> '' " +
                                            " AND t2.deleted = 'false' " +
                                            " AND t2.division = '12000' " +
                                            " AND t4.alias_name =  @param1 " +
                                            ") AS subquery " +
                                            "ORDER BY subquery.name ASC";

                        List<string> selectedFolder = new List<string>();
                        List<string> selectedListtext = new List<string>();

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
                                string value = reader.GetString(1);
                                selectedFolder.Add(name);
                                selectedListtext.Add(value);
                            }
                            reader.Close();

                            _logger.LogInformation("\nSelected folders:");
                            foreach (var folder in selectedFolder)
                            {
                                _logger.LogInformation($"- {folder}");
                            }
                        }




                        if (file.Length > 0)
                        {
                            // Get file size
                            long fileSizeInBytes = file.Length;
                            // Get formatted file size
                            string formattedFileSize = FormatFileSize(fileSizeInBytes);


                            var ocr = new IronTesseract();

                            string pdfFilePath = @"C:\Users\manan\OneDrive\Desktop\ProjectExcel";

                            string tempExcelFilePath = Path.Combine(pdfFilePath, originalExcelFileName + ".pdf");


                            IronOcr.OcrResult result = null;

                            if (extension == ".xls" || extension == ".xlsx" || extension == ".csv")
                            {


                                result = ocr.Read(tempExcelFilePath);
                            }
                            else
                            {
                                result = ocr.Read(tempFilePath);
                            }

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

                            _logger.LogInformation($"\n Confidence Score: {averageConfidence}");
                            // Your existing code to save the uploaded file to a temporary location...

                            var viewModel = new ResultViewModel
                            {
                                ExtractedText = extractedText,
                                Prefix = prefixList,
                                Folder = folderList,
                                Counts = new List<int> { maxCount },
                                FilePath = tempFilePath,
                                AverageConfidence = averageConfidence,
                                FileSize = fileSizeInBytes,
                                FormattedFileSize = formattedFileSize,
                                SelectedPrefix = selectedListtext.FirstOrDefault(),
                                SelectedFolder = selectedFolder


                            };
                            resultsList.Add(viewModel);

                            _logger.LogInformation($"\n View Model: {JsonConvert.SerializeObject(viewModel)}");

                            try
                            {
                                var uploadLog = new UploadLog
                                {

                                    division = "12000",
                                    file_name = originalFileName,
                                    file_size = formattedFileSize,
                                    average_confidence_score = (float)averageConfidence,
                                    document_prefix = selectedListtext.FirstOrDefault(),
                                    document_folder = selectedFolder.FirstOrDefault(),
                                    extracted_text = extractedText,
                                    uploaded_time = DateTime.UtcNow

                                };
                                _dbContext.uploadLog.Add(uploadLog);
                                _dbContext.SaveChanges();
                            }
                            catch (DbUpdateException ex)
                            {
                                // If an error occurred, examine the inner exception(s) for additional details
                                var errorMessage = "";
                                var innerException = ex.InnerException;
                                while (innerException != null)
                                {
                                    errorMessage += innerException.Message + "\n";
                                    innerException = innerException.InnerException;
                                }
                                _logger.LogInformation("Error saving changes to database: " + errorMessage);
                            }


                        }
                    }
                }


                // Handle errors if the file is not valid
                return View(resultsList);
            }
            catch (Exception ex)
            {
               _logger.LogError(ex, "Error occurred while processing Upload action.");
                return View("Error");
            }
        }



        public IActionResult AdminPanel(string selectedFolderName)
        {
            string connectionString = _config.GetConnectionString("DefaultConnection");
            try {
                List<SelectListItem> folderList = new List<SelectListItem>();
                List<object> prefixList = new List<object>();



                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Get list of folders
                    string folder_query = "SELECT DISTINCT name " +
                        "FROM ( " +
                        "    SELECT DISTINCT t1.id, t1.listtext, t1.code, t1.refid, t1.score, t2.folder, t3.name " +
                        "    FROM lst_docname AS t1 " +
                        "    LEFT JOIN tbl_doc_allocations_form_names AS t2 ON t1.listtext = t2.prefix " +
                        "    LEFT JOIN tbl_folder t3 ON t3.folder_id = t2.folder " +
                        "    WHERE t1.id = '12000' " +
                        "    AND t1.listtext <> '' " +
                        "    AND t2.deleted = 'false' " +
                        "    AND t2.division = '12000' " +
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

                    // Get prefixes for selected folder
                    if (!string.IsNullOrEmpty(selectedFolderName))
                    {
                        prefixList = GetPrefixesForFolder(connection, selectedFolderName);
                    }
                }

                ViewBag.FolderList = folderList;
                ViewBag.PrefixList = prefixList;

                _logger.LogInformation($"\n Query executed with folder: {folderList.Count} and prefix: {prefixList.Count} results");
                return View("Admin");



            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing AdminPanel action.");
                return View("Error");
            }
        }

        private List<object> GetPrefixesForFolder(SqlConnection connection, string folderName)
        {


            _logger.LogInformation($"\n Getting prefixes for folder {folderName}...");

            List<object> prefixList = new List<object>();

            string prefix_query = "SELECT listtext,name,prefix_id " +
                "FROM ( " +
                "    SELECT DISTINCT t1.id, t1.listtext,t1.prefix_id, t1.code, t1.refid, t1.score, t2.folder, t3.name " +
                "    FROM lst_docname AS t1 " +
                "    LEFT JOIN tbl_doc_allocations_form_names AS t2 ON t1.listtext = t2.prefix " +
                "    LEFT JOIN tbl_folder t3 ON t3.folder_id = t2.folder " +
                "    WHERE t1.id = '12000' " +
                "    AND t1.listtext <> '' " +
                "    AND t2.deleted = 'false' " +
                "    AND t2.division = '12000' " +
                $"    AND t3.name = '{folderName}' " +
                ") AS subquery " +
                "ORDER BY subquery.listtext ASC";

            using (SqlCommand cmd = new SqlCommand(prefix_query, connection))
            {
                SqlDataReader prefixReader = cmd.ExecuteReader();

                while (prefixReader.Read())
                {
                    var prefix = new
                    {
                        listtext = prefixReader.GetString(0),
                        name = prefixReader.GetString(1),
                        prefix_id = prefixReader.GetInt32(2),
                    };
                    prefixList.Add(prefix);
                }
                prefixReader.Close();
            }

            _logger.LogInformation($"\n Found {prefixList.Count} prefixes for folder {folderName}");

            return prefixList;
        
        
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

            _logger.LogWarning("\n File size could not be formatted for value: {0}", fileSizeInBytes);
            return "0 Bytes";
                }

        private string ExtractTextFromUploadedFile(string tempFilePath)
        {

            var extension = Path.GetExtension(tempFilePath);
            var originalFileName = Path.GetFileNameWithoutExtension(tempFilePath);

            Console.WriteLine("originalFileName");

            if (extension == ".xls" || extension == ".xlsx" || extension == ".csv")
            {

                _logger.LogInformation("Saving workbook as PDF...");
                var workbook = new Workbook();
                workbook.LoadFromFile(tempFilePath);

                string pdfFilePath = @"C:\Users\manan\OneDrive\Desktop\ProjectExcel";

                string tempExcelFilePath = Path.Combine(pdfFilePath, originalFileName+".pdf");
                // Save the workbook as a PDF file
                workbook.SaveToFile(tempExcelFilePath, FileFormat.PDF);

                workbook.Dispose();

                _logger.LogInformation("\n PDF file saved to {0}", tempExcelFilePath);

                _logger.LogInformation("\n Performing OCR on PDF...");

                var ocr = new IronTesseract();
                var result = ocr.Read(tempExcelFilePath);

                var words = result.Text.Split(' ');
                var first100Words = string.Join(" ", words.Take(100));
                _logger.LogInformation("\n OCR completed.");

                _logger.LogInformation($"\n First Hundred Words of file : {first100Words}");
                return first100Words;
            }
            else
            {

                _logger.LogInformation("\n Performing OCR on file...");
                var ocr = new IronTesseract();
                var result = ocr.Read(tempFilePath);

                _logger.LogInformation("\n OCR completed.");


                var words = result.Text.Split(' ');
                var first100Words = string.Join(" ", words.Take(100));

                _logger.LogInformation($"\n First Hundred Words of file : {first100Words}");

                return first100Words;

            }
        }

        public ActionResult Add(string listtext,int prefix_id)
        {
            ViewBag.ListText = listtext;
            ViewBag.PrefixId = prefix_id;
            return View("Add");
        }

        [HttpPost]
        public ActionResult Insert(string prefixRule, string listtext, Int32 prefix_id)
        {
            var username = TempData["username"] as string;

            string connectionString = _config.GetConnectionString("DefaultConnection");
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Define the SQL command for inserting the prefix rule into the Alias table
                        string insertSql = "INSERT INTO aliases_table (prefix_id, alias_name) VALUES (@PrefixId, @PrefixRule)";
                        using (SqlCommand command = new SqlCommand(insertSql, connection, transaction))
                        {
                            // Add the prefixRule and PrefixId parameters to the command
                            command.Parameters.AddWithValue("@PrefixId", prefix_id);
                            command.Parameters.AddWithValue("@PrefixRule", prefixRule);

                            // Execute the SQL command
                            int rowsAffected = command.ExecuteNonQuery();

                            // Commit the transaction if the SQL command was successful
                            transaction.Commit();

                            _logger.LogInformation($"\n Inserted prefix rule: {prefixRule}");

                            LoginViewModel model = new LoginViewModel();

                            try
                            {
                                var addLog = new Addlog
                                {

                                    division = "12000",
                                    prefix_id = prefix_id,
                                    prefix_rule = prefixRule,
                                    addedBy = username,
                                    added_time = DateTime.UtcNow

                                };
                                _dbContext.addLog.Add(addLog);
                                _dbContext.SaveChanges();
                            }
                            catch (DbUpdateException ex)
                            {
                                // If an error occurred, examine the inner exception(s) for additional details
                                var errorMessage = "";
                                var innerException = ex.InnerException;
                                while (innerException != null)
                                {
                                    errorMessage += innerException.Message + "\n";
                                    innerException = innerException.InnerException;
                                }
                                _logger.LogInformation("Error saving changes to database: " + errorMessage);
                            }



                            // Return a success message to the AJAX call
                            return Json(new { success = true });
                        }
                    }
                    catch (Exception ex)
                    {
                        // Roll back the transaction if an error occurred and return an error message to the AJAX call
                        transaction.Rollback();

                        _logger.LogError(ex, "Error occurred while inserting prefix rule");
                        return Json(new { success = false, message = ex.Message });
                    }

                   
                }
            }
           
        }


        public ActionResult Edit(string listtext, int id)
        {
            string connectionString = _config.GetConnectionString("DefaultConnection");
            int docnameId = 12000; // replace with actual value

            // Execute the SQL query
            string sql = "SELECT alias_name,alias_id FROM aliases_table WHERE prefix_id = " +
                         "(SELECT prefix_id FROM lst_docname WHERE listtext = @listtext AND id = @id)";

            List<string> aliasNames = new List<string>();

            List<int> aliasId = new List<int>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@listtext", listtext);
                command.Parameters.AddWithValue("@id", docnameId);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    if (!reader.IsDBNull(0))
                    {
                        string aliasName = reader.GetString(0);
                      
                        aliasNames.Add(aliasName);

                        int alias_id = reader.GetInt32(1);

                        aliasId.Add(alias_id);
                    }
                }
            }

            AliasModel model = new AliasModel
            {
                AliasNames = aliasNames,
                AliasIds = aliasId
            };

            // Pass the result to the view
            ViewBag.AliasNames = aliasNames;
            ViewBag.ListText = listtext;
            ViewBag.Id = id;

            return View(aliasNames);
        }

        [HttpPost]
        public ActionResult Save(List<string> aliasNames, string listtext, Dictionary<int, string> editedAliases)
        {
            string connectionString = _config.GetConnectionString("DefaultConnection");
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Use a transaction to ensure atomicity of the updates
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Prepare the update command
                        string updateSql = "UPDATE aliases_table SET alias_name = @aliasName WHERE prefix_id = " +
                                            "(SELECT prefix_id FROM lst_docname WHERE listtext = @listtext AND id = @id)" 
                                             ;

                        using (SqlCommand updateCommand = new SqlCommand(updateSql, connection, transaction))
                        {
                            updateCommand.Parameters.Add("@aliasName", SqlDbType.NVarChar);
                            updateCommand.Parameters.Add("@listtext", SqlDbType.NVarChar).Value = listtext;
                            updateCommand.Parameters.Add("@id", SqlDbType.Int).Value = 12000;

                            // Execute the update command for each alias name
                            foreach (KeyValuePair<int, string> editedAlias in editedAliases)
                            {
                                updateCommand.Parameters["@aliasName"].Value = editedAlias.Value;
                                updateCommand.ExecuteNonQuery();
                            }
                        }

                        // Commit the transaction if everything succeeded
                        transaction.Commit();



                        // return RedirectToAction("AdminPanel");
                        return Json(new { message = "Aliases saved successfully" });
                    }
                    catch (SqlException ex)
                    {
                        // Roll back the transaction on error
                        transaction.Rollback();

                        // Provide a meaningful error message to the user
                        ViewBag.ErrorMessage = "An error occurred while updating the aliases: " + ex.Message;

                        // Reload the page with the error message
                        return View();
                    }
                }
            }
        }

        public ActionResult Delete(string listtext, int id)
        {
            string connectionString = _config.GetConnectionString("DefaultConnection");
            int docnameId = 12000; // replace with actual value

            // Execute the SQL query
            string sql = "SELECT alias_name FROM aliases_table WHERE prefix_id = " +
                         "(SELECT prefix_id FROM lst_docname WHERE listtext = @listtext AND id = @id)";

            List<SelectListItem> aliasNames = new List<SelectListItem>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@listtext", listtext);
                command.Parameters.AddWithValue("@id", docnameId);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    if (!reader.IsDBNull(0))
                    {
                        string aliasName = reader.GetString(0);
                        aliasNames.Add(new SelectListItem { Text = aliasName, Value = aliasName });
                    }
                }
            }

            
            ViewBag.AliasNames = aliasNames;
            ViewBag.Listtext = listtext;
            
            _logger.LogInformation($"\n delete rule: {aliasNames}, {listtext},{id}");

            return View();
        }

        [HttpPost]
        public ActionResult ConfirmDelete(string aliasName)
        {
            var username = TempData["username"] as string;

            string connectionString = _config.GetConnectionString("DefaultConnection");
            // Execute the SQL query to delete the record
            string sql = "DELETE FROM aliases_table WHERE alias_name = @aliasName";
            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@aliasName", aliasName);
                connection.Open();
                command.ExecuteNonQuery();
            }
            try
            {

                string listText = Request.Form["ListText"];


                var deleteLog = new DeleteLog
                {

                    division = "12000",
                    aliasName = aliasName,
                    deletedBy = username,
                    prefix = (string)listText,
                    delete_time = DateTime.UtcNow

                };
                _dbContext.deleteLog.Add(deleteLog);
                _dbContext.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                // If an error occurred, examine the inner exception(s) for additional details
                var errorMessage = "";
                var innerException = ex.InnerException;
                while (innerException != null)
                {
                    errorMessage += innerException.Message + "\n";
                    innerException = innerException.InnerException;
                }
                _logger.LogInformation("Error saving changes to database: " + errorMessage);
            }

            return RedirectToAction("AdminPanel");
        }





        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
            public IActionResult Error()
            {
                return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }


        }
    }