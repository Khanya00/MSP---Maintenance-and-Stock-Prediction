using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MSP.Data;
using MSP.Models;
using static MSP.Helper;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols;
using System.Configuration;

namespace MSP.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private IWebHostEnvironment Environment;
        private IConfiguration Configuration;


        public ProductsController(ApplicationDbContext context, IWebHostEnvironment _environment, IConfiguration _configuration)
        {
            _context = context;
            Environment = _environment;
            Configuration = _configuration;
        }



        #region Importing Excel Data Code

        [HttpPost]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        public IActionResult Index(IFormFile postedFile)
        {
            if (postedFile != null)
            {
                //Create a Folder.
                string path = Path.Combine(this.Environment.WebRootPath, "Uploads");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                //Save the uploaded Excel file.
                string fileName = Path.GetFileName(postedFile.FileName);
                string filePath = Path.Combine(path, fileName);
                using (FileStream stream = new FileStream(filePath, FileMode.Create))
                {
                    postedFile.CopyTo(stream);
                }

                //Read the connection string for the Excel file.
                string conString = this.Configuration.GetConnectionString("ExcelConString");
                DataTable dt = new DataTable();
                conString = string.Format(conString, filePath);

                using (OleDbConnection connExcel = new OleDbConnection(conString))
                {
                    using (OleDbCommand cmdExcel = new OleDbCommand())
                    {
                        using (OleDbDataAdapter odaExcel = new OleDbDataAdapter())
                        {
                            cmdExcel.Connection = connExcel;

                            //Get the name of First Sheet.
                            connExcel.Open();
                            DataTable dtExcelSchema;
                            dtExcelSchema = connExcel.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                            string sheetName = dtExcelSchema.Rows[0]["TABLE_NAME"].ToString();
                            connExcel.Close();

                            //Read Data from First Sheet.
                            connExcel.Open();
                            cmdExcel.CommandText = "SELECT * From [" + sheetName + "]";
                            odaExcel.SelectCommand = cmdExcel;
                            odaExcel.Fill(dt);
                            connExcel.Close();
                        }
                    }
                }
                //Insert the Data read from the Excel file to Database Table.
                conString = this.Configuration.GetConnectionString("DefaultConnection");
                using (SqlConnection con = new SqlConnection(conString))
                {
                    using (SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(con))
                    {
                        //Set the database table name.
                        sqlBulkCopy.DestinationTableName = "dbo.Products";

                        //[OPTIONAL]: Map the Excel columns with that of the database table.
                        sqlBulkCopy.ColumnMappings.Add("ProductId", "ProductId");
                        sqlBulkCopy.ColumnMappings.Add("ProductName", "ProductName");
                        sqlBulkCopy.ColumnMappings.Add("UnitsPerCase", "UnitsPerCase");
                        sqlBulkCopy.ColumnMappings.Add("UnitsPerPack", "UnitsPerPack");
                        sqlBulkCopy.ColumnMappings.Add("ShelfLife", "ShelfLife");
                        sqlBulkCopy.ColumnMappings.Add("DepartmentName", "DepartmentName");
                        sqlBulkCopy.ColumnMappings.Add("ProductTypeName", "ProductTypeName");


                        con.Open();
                        sqlBulkCopy.WriteToServer(dt);
                        con.Close();
                    }
                }
            }

            ViewBag.Success = "File Imported and excel data saved into database";
            return View();
        }

                #endregion

        // GET: Products
        public async Task<IActionResult> Index()
        {
            
            return View(await _context.Products.ToListAsync());
        }

        public async Task<IActionResult> GetProductList()
        {
            return Json(await _context.Products.ToListAsync());
        }

        public JsonResult PMDashboardCount()
        {
            try
            {
                string[] pmDashboardCount = new string[3];

                SqlConnection con = new SqlConnection(Configuration.GetConnectionString("DefaultConnection"));
                SqlCommand cmd = new SqlCommand("select count(ProductTypeName) as 'Short Life Perishable', (select count(ProductTypeName) from Products where ProductTypeName='Non-Perishable') as 'Non-Perishable', (select count(ProductTypeName) from Products where ProductTypeName='Longer Life Perishable') as 'Longer Life Perishable'  from Products where ProductTypeName='Short Life Perishable'", con);
                con.Open();
                DataTable dt = new DataTable();
                SqlDataAdapter cmd1 = new SqlDataAdapter(cmd);
                cmd1.Fill(dt);

                if (dt.Rows.Count == 0)
                {
                    pmDashboardCount[0] = "0";
                    pmDashboardCount[1] = "0";
                    pmDashboardCount[2] = "0";

                }
                else
                {
                    pmDashboardCount[0] = dt.Rows[0]["Short Life Perishable"].ToString();
                    pmDashboardCount[1] = dt.Rows[0]["Non-Perishable"].ToString();
                    pmDashboardCount[2] = dt.Rows[0]["Longer Life Perishable"].ToString();
                }

                return Json(new { pmDashboardCount });
            }
            catch (Exception)
            {
                throw;
            }
        }

        // GET: Products/AddorEdit (Insert)
        // GET: Products/AddorEdit/5 (Update)

        [NoDirectAccess]
        public async Task<IActionResult> AddorEdit(int id=0)
        {
            if (id == 0)
            {
                return View(new Product());
            }
            else
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound();
                }
                return View(product);
            }
        }

        //// POST: Products/Create
        //// To protect from overposting attacks, enable the specific properties you want to bind to.
        //// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Create([Bind("ProductId,ProductName,UnitsPerCase,UnitsPerPack,ShelfLife,DepartmentName,ProductTypeName")] Product product)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        _context.Add(product);
        //        await _context.SaveChangesAsync();
        //        return RedirectToAction(nameof(Index));
        //    }
        //    return View(product);
        //}

        //// GET: Products/Edit/5
        //public async Task<IActionResult> Edit(double? id)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var product = await _context.Products.FindAsync(id);
        //    if (product == null)
        //    {
        //        return NotFound();
        //    }
        //    return View(product);
        //}

        // POST: Products/AddOrEdit/
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrEdit( [Bind("ProductId,ProductName,UnitsPerCase,UnitsPerPack,ShelfLife,DepartmentName,ProductTypeName")] Product product)
        {

            if (ModelState.IsValid)
            {
                //Insert
                if (product.ProductId == 0)
                {
                    _context.Add(product);
                    await _context.SaveChangesAsync();
                }
                //Update
                else
                {
                    try
                    {
                        _context.Update(product);
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!ProductExists(product.ProductId))
                        {
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
                return Json(new { isValid = true, html = Helper.RenderRazorViewToString(this, "Index", _context.Products.ToList()) });
            }
            return Json(new { isValid = false, html = Helper.RenderRazorViewToString(this, "AddOrEdit", _context.Products.ToList()) });
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? id)
        {
            var product = await _context.Products.FindAsync(id);
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return Json(new {html = Helper.RenderRazorViewToString(this, "Index", _context.Products.ToList()) });
        }

        private bool ProductExists(int? id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }
    }
}
