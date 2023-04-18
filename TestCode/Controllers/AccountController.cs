using Microsoft.Data.SqlClient;// add this namespace for SqlConnection
using Microsoft.AspNetCore.Mvc;
using TestCode.Models;

namespace TestCode.Controllers
{
    public class AccountController : Controller
    {

        private readonly ILogger<HomeController> _logger;

        string connectionString = "Server=67.231.31.66;Database=CapStone2023_OCR;User ID=CapStone_User;Password=r8#PF9%0Sw;trustServerCertificate = yes;";
        public AccountController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }
       // private readonly string _connectionString; // define the connection string

        

        public ActionResult Login()
        {
            if (HttpContext.Session.GetString("LoggedIn") == "true")
            {
                return RedirectToAction("AdminPanel", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                using (SqlConnection connection = new SqlConnection(connectionString)) // create a new SqlConnection object using the connection string
                {
                    connection.Open(); // open the connection

                    // create a new SqlCommand object to query the database and check if the user is an executive
                    SqlCommand command = new SqlCommand("SELECT COUNT(*) FROM tbl_Employees WHERE EmployeeLogin = @Name AND " +
                                                         "usrPassword = @Password AND " +
                                                         "EmployeeType_ID = 10 ", connection);
                    command.Parameters.AddWithValue("@Name", model.UserName);
                    command.Parameters.AddWithValue("@Password", model.Password);

                    int count = (int)command.ExecuteScalar(); // execute the query and retrieve the count of matching records

                    HttpContext.Session.SetString("LoggedIn", "true");

                    if (count == 1)
                    {
                        TempData["username"] = model.UserName;
                        // If the user is an executive, log them in and redirect to the home page
                        return RedirectToAction("AdminPanel", "Home" );
                    }
                    else
                    {
                        // If the user is not an executive, show an error message
                        ModelState.AddModelError("", "Invalid login attempt.");
                    }
                }
            }

            // If there was a validation error or the user is not an executive, redisplay the login page with the validation errors
            return View(model);
        }

        public ActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }

    }
}
