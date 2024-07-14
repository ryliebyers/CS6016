using LibraryWebServer.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TestProject1")]
namespace LibraryWebServer.Controllers
{
    public class HomeController : Controller
    {
        // WARNING:
        // This very simple web server is designed to be as tiny and simple as possible
        // This is NOT the way to save user data.
        // This will only allow one user of the web server at a time (aside from major security concerns).

        // Global variables to store the current user's name and card number
        private static string user = "";
        private static int card = -1;

        // Database context to interact with the database
        private readonly LibraryContext db;

        // Constructor to initialize the database context
        public HomeController(LibraryContext _db)
        {
            db = _db;
        }

        /// <summary>
        /// Given a Patron name and CardNum, verify that they exist and match in the database.
        /// If the login is successful, sets the global variables "user" and "card"
        /// </summary>
        /// <param name="name">The Patron's name</param>
        /// <param name="cardnum">The Patron's card number</param>
        /// <returns>A JSON object with a single field: "success" with a boolean value:
        /// true if the login is accepted, false otherwise.
        /// </returns>
        [HttpPost]
        public IActionResult CheckLogin(string name, int cardnum)
        {
            // Query to check if the patron exists in the database
            var patron = db.Patrons.FirstOrDefault(p => p.Name == name && p.CardNum == cardnum);
            bool loginSuccessful = patron != null;

            // If login is not successful, return a JSON object with success = false
            if (!loginSuccessful)
            {
                return Json(new { success = false });
            }
            else
            {
                // If login is successful, set the global variables and return success = true
                user = name;
                card = cardnum;
                return Json(new { success = true });
            }
        }

        /// <summary>
        /// Logs a user out. This is implemented for you.
        /// </summary>
        /// <returns>Success</returns>
        [HttpPost]
        public ActionResult LogOut()
        {
            user = "";
            card = -1;
            return Json(new { success = true });
        }

        /// <summary>
        /// Returns a JSON array representing all known books.
        /// Each book should contain the following fields:
        /// {"isbn" (string), "title" (string), "author" (string), "serial" (uint?), "name" (string)}
        /// Every object in the list should have isbn, title, and author.
        /// Books that are not in the Library's inventory (such as Dune) should have a null serial.
        /// The "name" field is the name of the Patron who currently has the book checked out (if any)
        /// Books that are not checked out should have an empty string "" for name.
        /// </summary>
        /// <returns>The JSON representation of the books</returns>
        [HttpPost]
        public ActionResult AllTitles()
        {
            // Query to retrieve all books along with their check-out status and the name of the patron who has checked them out
            var query = from t in db.Titles
                        join i in db.Inventory on t.Isbn equals i.Isbn into ti
                        from subI in ti.DefaultIfEmpty()
                        join co in db.CheckedOut on subI.Serial equals co.Serial into ico
                        from subCo in ico.DefaultIfEmpty()
                        join p in db.Patrons on subCo.CardNum equals p.CardNum into cop
                        from subP in cop.DefaultIfEmpty()
                        select new
                        {
                            isbn = t.Isbn,
                            title = t.Title,
                            author = t.Author,
                            serial = (uint?)subI.Serial,
                            name = subP == null ? "" : subP.Name
                        };

            // Return the query result as a JSON array
            return Json(query.ToList());
        }

        /// <summary>
        /// Returns a JSON array representing all books checked out by the logged in user 
        /// The logged in user is tracked by the global variable "card".
        /// Every object in the array should contain the following fields:
        /// {"title" (string), "author" (string), "serial" (uint) (note this is not a nullable uint) }
        /// Every object in the list should have a valid (non-null) value for each field.
        /// </summary>
        /// <returns>The JSON representation of the books</returns>
        [HttpPost]
        public ActionResult ListMyBooks()
        {
            // Query to retrieve all books checked out by the logged-in user
            var query = from co in db.CheckedOut
                        join i in db.Inventory on co.Serial equals i.Serial
                        join t in db.Titles on i.Isbn equals t.Isbn
                        where co.CardNum == card
                        select new
                        {
                            title = t.Title,
                            author = t.Author,
                            serial = co.Serial
                        };

            // Return the query result as a JSON array
            return Json(query.ToList());
        }

        /// <summary>
        /// Updates the database to represent that
        /// the given book is checked out by the logged in user (global variable "card").
        /// In other words, insert a row into the CheckedOut table.
        /// You can assume that the book is not currently checked out by anyone.
        /// </summary>
        /// <param name="serial">The serial number of the book to check out</param>
        /// <returns>success</returns>
        [HttpPost]
        public ActionResult CheckOutBook(int serial)
        {
            // Find the book in the inventory using the serial number
            var book = db.Inventory.FirstOrDefault(i => i.Serial == (uint)serial);

            // If the book is found, create a new CheckedOut entry and add it to the database
            if (book != null)
            {
                var checkout = new CheckedOut
                {
                    Serial = (uint)serial,
                    CardNum = (uint)card
                };

                db.CheckedOut.Add(checkout);
                db.SaveChanges();
            }

            // Return success = true
            return Json(new { success = true });
        }

        /// <summary>
        /// Returns a book currently checked out by the logged in user (global variable "card").
        /// In other words, removes a row from the CheckedOut table.
        /// You can assume the book is checked out by the user.
        /// </summary>
        /// <param name="serial">The serial number of the book to return</param>
        /// <returns>Success</returns>
        [HttpPost]
        public ActionResult ReturnBook(int serial)
        {
            // Find the CheckedOut entry for the book using the serial number and the logged-in user's card number
            var checkout = db.CheckedOut.FirstOrDefault(c => c.Serial == (uint)serial && c.CardNum == (uint)card);

            // If the CheckedOut entry is found, remove it from the database
            if (checkout != null)
            {
                db.CheckedOut.Remove(checkout);
                db.SaveChanges();
            }

            // Return success = true
            return Json(new { success = true });
        }

        /*******************************************/
        /****** Do not modify below this line ******/
        /*******************************************/

        public IActionResult Index()
        {
            if (user == "" && card == -1)
                return View("Login");

            return View();
        }

        /// <summary>
        /// Return the Login page.
        /// </summary>
        /// <returns></returns>
        public IActionResult Login()
        {
            user = "";
            card = -1;

            ViewData["Message"] = "Please login.";

            return View();
        }

        /// <summary>
        /// Return the MyBooks page.
        /// </summary>
        /// <returns></returns>
        public IActionResult MyBooks()
        {
            if (user == "" && card == -1)
                return View("Login");

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
