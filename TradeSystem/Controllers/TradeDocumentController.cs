using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Net.NetworkInformation;
using TradeSystem.Data;
using TradeSystem.Interfaces;
using TradeSystem.Models;

namespace TradeSystem.Controllers
{
    [Authorize]
    public class TradeDocumentController : Controller
    {
        private readonly ITradeDocumentService _service;
        private readonly TFMSDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public TradeDocumentController(
            ITradeDocumentService service,
            TFMSDbContext db,
            UserManager<ApplicationUser> userManager)
        {
            _service = service;
            _db = db;
            _userManager = userManager;
        }

        // Common doc types dropdown
        private static readonly string[] _docTypes = new[]
        {
            "Commercial Invoice", "Packing List", "Bill of Lading", "Air Waybill",
            "Insurance Certificate", "Certificate of Origin", "Shipping Bill"
        };

       

        private async Task<string> GetCurrentDisplayNameAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null && !string.IsNullOrWhiteSpace(user.FullName))
                return user.FullName;
            return User.Identity?.Name ?? "Unknown";
        }

        private async Task LoadLookups()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            
            // LC visible if status is Open or Amended, and user has access
            var lcs = _db.LetterOfCredits
                         .Where(l => l.Status == LCStatus.Open || l.Status == LCStatus.Amended)
                         .Where(l => isAdmin || l.CreatedByUserId == currentUser.Id)
                         .Select(l => new { l.LcId, Label = $"LC #{l.LcId} - {l.BeneficiaryName}" })
                         .ToList();

            // BG visible if Issued, and user has access
            var bgs = _db.BankGuarantees
                         .Where(g => g.Status == BgStatus.Issued)
                         .Where(g => isAdmin || g.CreatedByUserId == currentUser.Id)
                         .Select(g => new { g.GuaranteeId, Label = $"BG #{g.GuaranteeId} - {g.BeneficiaryName}" })
                         .ToList();

            ViewBag.LCs = new SelectList(lcs, "LcId", "Label");
            ViewBag.BGs = new SelectList(bgs, "GuaranteeId", "Label");
            ViewBag.DocTypes = new SelectList(_docTypes);
        }

        // List
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            
            IEnumerable<TradeDocument> docs;
            if (isAdmin)
            {
                docs = _service.GetAllDocumentsById();
            }
            else
            {
                docs = _service.GetDocumentsByUserId(currentUser.Id);
            }
            
            return View(docs);
        }

        // Upload (User only)

        [Authorize(Roles = "User")]
        [HttpGet]
        public async Task<IActionResult> Upload()
        {
            var model = new TradeDocument
            {
                Status = TdStatus.Active
            };
            await LoadLookups();
            return View(model);
        }

        [Authorize(Roles = "User")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload([Bind("DocumentType,Status,LcId,GuaranteeId,ReferenceNumber")] TradeDocument doc)
        {
            doc.UploadedBy = await GetCurrentDisplayNameAsync();

            ModelState.Remove(nameof(TradeDocument.ReferenceNumber));
            ModelState.Remove(nameof(TradeDocument.UploadedBy));
            ModelState.Remove(nameof(TradeDocument.UploadDate));

            if (!ModelState.IsValid)
            {
                await LoadLookups();
                return View(doc);
            }

            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (!_service.UploadDocument(doc, currentUser.Id))
                {
                    ModelState.AddModelError("", "Failed to upload document (duplicate reference or server error). Please try again.");
                    await LoadLookups();
                    return View(doc);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Upload Failed: {ex.GetBaseException().Message}");
                await LoadLookups();
                return View(doc);
            }
        }

        // Details
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            
            TradeDocument? doc;
            if (isAdmin)
            {
                doc = _service.ViewDocument(id);
            }
            else
            {
                doc = _service.ViewDocumentByUserId(id, currentUser.Id);
            }
            
            if (doc == null) return NotFound();
            await LoadLookups();
            return View(doc);
        }

        // Edit (User only)
        [Authorize(Roles = "User")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var doc = _service.ViewDocumentByUserId(id, currentUser.Id);
            if (doc == null) return NotFound();
            await LoadLookups();
            return View(doc);
        }

        [Authorize(Roles = "User")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TradeDocument doc)
        {
            if (!ModelState.IsValid)
            {
                await LoadLookups();
                return View(doc);
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (!_service.UpdateDocumentDetails(doc, currentUser.Id))
            {
                ModelState.AddModelError("", "Failed to update document.");
                await LoadLookups();
                return View(doc);
            }

            return RedirectToAction(nameof(Index));
        }


    }

}
