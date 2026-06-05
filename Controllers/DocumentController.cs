using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using DocumentUpload_App.Models;

namespace DocumentUpload_App.Controllers
{
   
    public class DocumentController : Controller
    {
        private AppDbContext db = new AppDbContext();

        // GET: Document List Page
        // Dashboard - Sabhi documents dikhao
        public ActionResult Index()
        {
            if (Session["User_Name"] == null)
                return RedirectToAction("Login", "Account");
            return View();
        }

        // My Documents - Sirf login user ke documents
        public ActionResult MyDocuments()
        {
            if (Session["User_Name"] == null)
                return RedirectToAction("Login", "Account");
            return View("Index"); // Same view use karega
        }

        // GET: Search by Category (TextChange AJAX call)
        public JsonResult SearchByCategory(string term, bool isMyDocs = false)
        {
            if (Session["User_Name"] == null)
                return Json(new { error = "Session expired" }, JsonRequestBehavior.AllowGet);

            string currentUser = Session["User_Name"].ToString();

            // isMyDocs true hai to sirf login user ke docs
            var query = isMyDocs
                ? db.Documents.Where(d => d.EntryBy == currentUser)
                : db.Documents.AsQueryable();

            if (!string.IsNullOrEmpty(term))
            {
                query = query.Where(d =>
                    (d.MasterDocumentName != null && d.MasterDocumentName.Contains(term)) ||
                    (d.Topic != null && d.Topic.Contains(term)) ||
                    (d.DocumentType != null && d.DocumentType.Contains(term)) ||
                    (d.EntryBy != null && d.EntryBy.Contains(term))
                );
            }

            var docs = query
                .OrderByDescending(d => d.EntryDate)
                .Select(d => new {
                    d.EnteryNo,
                    d.MasterDocumentName,
                    d.Topic,
                    d.DocumentType,
                    d.EntryBy,
                    d.EntryDate,
                    d.DocumentPath,
                    d.No_Of_visitors
                })
                .ToList();

            return Json(docs, JsonRequestBehavior.AllowGet);
        }

        // GET: Category list for dropdown (AJAX)
        public JsonResult GetCategories(string term)
        {
            var cats = db.Categories
                         .Where(c => c.IsActive && c.CategoryName.Contains(term))
                         .Select(c => new { c.CategoryId, c.CategoryName })
                         .ToList();

            return Json(cats, JsonRequestBehavior.AllowGet);
        }

        // POST: Add New Document
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult AddDocument(string masterDocumentName, string topic,
                                      string documentType, string description,
                                      HttpPostedFileBase file)
        {
            try
            {
                if (file == null || file.ContentLength == 0)
                    return Json(new { success = false, message = "Please select a file." });

                // Save file to ~/Uploads/Documents/
                string uploadFolder = Server.MapPath("~/Uploads/Documents/");
                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);

                string fileName = System.IO.Path.GetFileNameWithoutExtension(file.FileName)
                                    + "_" + DateTime.Now.Ticks
                                    + System.IO.Path.GetExtension(file.FileName);
                string fullPath = System.IO.Path.Combine(uploadFolder, fileName);
                string relativePath = "~/Uploads/Documents/" + fileName;

                file.SaveAs(fullPath);

                // Auto-add category if not exists
                string topicName = topic?.Trim();
                if (!string.IsNullOrEmpty(topicName))
                {
                    bool catExists = db.Categories.Any(c => c.CategoryName == topicName && c.IsActive);
                    if (!catExists)
                    {
                        db.Categories.Add(new Category
                        {
                            CategoryName = topicName,
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        });
                        db.SaveChanges();
                    }
                }

                // Save document record
                var doc = new Document
                {
                    MasterDocumentName = masterDocumentName,
                    Topic = topicName,
                    DocumentType = documentType,
                    EntryDate = DateTime.Now,
                    EntryBy = Session["User_Name"].ToString(),
                    Path = relativePath,
                    DocumentPath = relativePath,
                    No_Of_visitors = 0
                };

                db.Documents.Add(doc);
                db.SaveChanges();

                return Json(new { success = true, message = "Document uploaded successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // POST: Delete Document
        [HttpPost]
        public JsonResult DeleteDocument(int id)
        {
            try
            {
                var doc = db.Documents.Find(id);
                if (doc == null)
                    return Json(new { success = false, message = "Document not found." });

                // Delete physical file
                string filePath = Server.MapPath(doc.DocumentPath);
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);

                db.Documents.Remove(doc);
                db.SaveChanges();

                return Json(new { success = true, message = "Document deleted." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }

        public ActionResult ViewDocument(int id)
        {
            var doc = db.Documents.Find(id);
            if (doc == null)
                return HttpNotFound();

            string fullPath = Server.MapPath(doc.DocumentPath);
            if (!System.IO.File.Exists(fullPath))
                return HttpNotFound("File not found.");

            string fileName = System.IO.Path.GetFileName(doc.DocumentPath);
            string mimeType = MimeMapping.GetMimeMapping(fileName);
            string encodedFileName = Uri.EscapeDataString(fileName);

            string[] previewable = { ".pdf", ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
            string ext = System.IO.Path.GetExtension(fileName).ToLower();

            if (previewable.Contains(ext))
            {
                Response.Headers["Content-Disposition"] =
                    "inline; filename=\"" + fileName + "\"; filename*=UTF-8''" + encodedFileName;
            }
            else
            {
                Response.Headers["Content-Disposition"] =
                    "attachment; filename=\"" + fileName + "\"; filename*=UTF-8''" + encodedFileName;
            }

            doc.No_Of_visitors = (doc.No_Of_visitors ?? 0) + 1;
            db.SaveChanges();

            return File(fullPath, mimeType);
        }

        // ---- Download File ----
        public ActionResult DownloadDocument(int id)
        {
            var doc = db.Documents.Find(id);
            if (doc == null)
                return HttpNotFound();

            string fullPath = Server.MapPath(doc.DocumentPath);
            if (!System.IO.File.Exists(fullPath))
                return HttpNotFound("File not found.");

            string fileName = System.IO.Path.GetFileName(doc.DocumentPath);
            string mimeType = MimeMapping.GetMimeMapping(fileName);

            // Spaces aur special chars handle karo
            string encodedFileName = Uri.EscapeDataString(fileName);

            Response.Headers["Content-Disposition"] =
                "attachment; filename=\"" + fileName + "\"; filename*=UTF-8''" + encodedFileName;

            return File(fullPath, mimeType);
        }

        // ---- Get Document Info for QR Card ----
        public JsonResult GetDocumentForQR(int id)
        {
            var doc = db.Documents.Find(id);
            if (doc == null)
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);

            // Public download URL for QR
            string fileName = System.IO.Path.GetFileName(doc.DocumentPath);
            string publicUrl = Request.Url.GetLeftPart(UriPartial.Authority)
                   + Url.Action("PublicView", "Document", new { id = id });

            // Generate QR Code as Base64 image
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(publicUrl, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);
            Bitmap qrBitmap = qrCode.GetGraphic(10);

            string base64QR;
            using (var ms = new System.IO.MemoryStream())
            {
                qrBitmap.Save(ms, ImageFormat.Png);
                base64QR = Convert.ToBase64String(ms.ToArray());
            }

            return Json(new
            {
                success = true,
                documentName = doc.MasterDocumentName,
                category = doc.Topic,
                uploadedBy = doc.EntryBy,
                date = doc.EntryDate.HasValue
                                   ? doc.EntryDate.Value.ToString("dd MMM yyyy") : "-",
                documentType = doc.DocumentType,
                qrBase64 = base64QR,
                downloadUrl = publicUrl
            }, JsonRequestBehavior.AllowGet);
        }






        // No [Authorize] - Public access
        [AllowAnonymous]
        public ActionResult PublicView(int id)
        {
            var doc = db.Documents.Find(id);
            if (doc == null)
                return HttpNotFound();

            // Visitor count badhao
            doc.No_Of_visitors = (doc.No_Of_visitors ?? 0) + 1;
            db.SaveChanges();

            // Same category ke top 10 documents
            var relatedDocs = db.Documents
                                .Where(d => d.Topic == doc.Topic && d.EnteryNo != id)
                                .OrderByDescending(d => d.No_Of_visitors)
                                .Take(10)
                                .ToList();

            ViewBag.Document = doc;
            ViewBag.RelatedDocs = relatedDocs;

            return View();
        }

        // Public file serve - No login
        [AllowAnonymous]
        public ActionResult PublicDownload(int id)
        {
            var doc = db.Documents.Find(id);
            if (doc == null) return HttpNotFound();

            string fullPath = Server.MapPath(doc.DocumentPath);
            if (!System.IO.File.Exists(fullPath))
                return HttpNotFound();

            string fileName = System.IO.Path.GetFileName(doc.DocumentPath);
            string mimeType = MimeMapping.GetMimeMapping(fileName);
            string encodedName = Uri.EscapeDataString(fileName);
            string[] previewable = { ".pdf", ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
            string ext = System.IO.Path.GetExtension(fileName).ToLower();

            if (previewable.Contains(ext))
                Response.Headers["Content-Disposition"] =
                    "inline; filename=\"" + fileName + "\"; filename*=UTF-8''" + encodedName;
            else
                Response.Headers["Content-Disposition"] =
                    "attachment; filename=\"" + fileName + "\"; filename*=UTF-8''" + encodedName;

            return File(fullPath, mimeType);
        }

    }
}