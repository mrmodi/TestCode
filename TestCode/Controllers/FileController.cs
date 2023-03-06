using IronOcr;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc;

namespace TestCode.Controllers
{
    public class FileController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }


        [HttpPost]
        public IActionResult Upload(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {

                string tempFolderPath = Path.Combine(Path.GetTempPath(), "MyTempFolder");
                Directory.CreateDirectory(tempFolderPath);

                string uniqueFileName = Path.GetRandomFileName();
            //    string tempFilePath = Path.Combine(tempFolderPath, uniqueFileName);

                // Save the uploaded file to a temporary location
               // var fileName = Path.GetFileName(file.FileName);
               // var path = Path.Combine(Path.GetTempPath(), fileName);
                using (var stream = new FileStream(tempFolderPath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                // Call a method to extract text data from the uploaded file using IronOCR
                var text = ExtractTextFromUploadedFile(tempFolderPath);

                // Pass the extracted text data to a view for display
                return View("Result", text);
            }

            // Handle errors if the file is not valid
            return View("Error");
        }

        private string ExtractTextFromUploadedFile(string path)
        {
            var ocr = new IronTesseract();
            var result = ocr.Read(path);

            return result.Text;
        }
    }
}
