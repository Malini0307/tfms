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

        private void LoadLookups()
        {
            var currentUserId = _userManager.GetUserId(User);

            // LC visible if status is Open or Amended and owned by current user (unless Admin)
            var lcs = _db.LetterOfCredits
                         .Where(l => (l.Status == LCStatus.Open || l.Status == LCStatus.Amended)
                                  && (User.IsInRole("Admin") || l.UserId == currentUserId))
                         .Where(l => !_db.TradeDocuments.Any(td => td.LcId == l.LcId))
                         .Select(l => new { l.LcId, Label = $"LC #{l.LcId} - {l.BeneficiaryName}" })
                         .ToList();

            // BG visible if Issued and owned by current user (unless Admin)
            var bgs = _db.BankGuarantees
                         .Where(g => g.Status == BgStatus.Issued
                                  && (User.IsInRole("Admin") || g.UserId == currentUserId))
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
            bool isAdmin = User.IsInRole("Admin");
            var docs = _service.GetAllDocuments(currentUser?.Id, isAdmin);
            return View(docs);
        }

        // Upload (User only)

        [Authorize(Roles = "User")]
        [HttpGet]
        public IActionResult Upload()
        {
            var model = new TradeDocument
            {
                Status = TdStatus.Active
            };
            LoadLookups();
            return View(model);
        }

        [Authorize(Roles = "User")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload([Bind("DocumentType,Status,LcId,GuaranteeId,ReferenceNumber")] TradeDocument doc)
        {
            doc.UploadedBy = await GetCurrentDisplayNameAsync();
            var currentUser = await _userManager.GetUserAsync(User);
            doc.UserId = currentUser?.Id;

            ModelState.Remove(nameof(TradeDocument.ReferenceNumber));
            ModelState.Remove(nameof(TradeDocument.UploadedBy));
            ModelState.Remove(nameof(TradeDocument.UploadDate));

            if (!ModelState.IsValid)
            {
                LoadLookups();
                return View(doc);
            }

            try
            {
                if (!_service.UploadDocument(doc))
                {
                    ModelState.AddModelError("", "A Trade Document already exists for the selected LC or an error occurred.");
                    LoadLookups();
                    return View(doc);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Upload Failed: {ex.GetBaseException().Message}");
                LoadLookups();
                return View(doc);
            }
        }

        // Details
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var doc = _service.ViewDocument(id);
            if (doc == null) return NotFound();
            if (!User.IsInRole("Admin"))
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (doc.UserId != currentUser?.Id
                    && !(doc.LcId != null && _db.LetterOfCredits.Any(l => l.LcId == doc.LcId && l.UserId == currentUser!.Id))
                    && !(doc.GuaranteeId != null && _db.BankGuarantees.Any(b => b.GuaranteeId == doc.GuaranteeId && b.UserId == currentUser!.Id)))
                {
                    return Forbid();
                }
            }
            LoadLookups();
            return View(doc);
        }

        // Edit (User only)
        [Authorize(Roles = "User")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var doc = _service.ViewDocument(id);
            if (doc == null) return NotFound();
            if (!User.IsInRole("Admin"))
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (doc.UserId != currentUser?.Id
                    && !(doc.LcId != null && _db.LetterOfCredits.Any(l => l.LcId == doc.LcId && l.UserId == currentUser!.Id))
                    && !(doc.GuaranteeId != null && _db.BankGuarantees.Any(b => b.GuaranteeId == doc.GuaranteeId && b.UserId == currentUser!.Id)))
                {
                    return Forbid();
                }
            }
            LoadLookups();
            return View(doc);
        }

        [Authorize(Roles = "User")]
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Edit(TradeDocument doc)
        {
            if (!ModelState.IsValid)
            {
                LoadLookups();
                return View(doc);
            }

            if (!_service.UpdateDocumentDetails(doc))
            {
                ModelState.AddModelError("", "Failed to update document.");
                LoadLookups();
                return View(doc);
            }

            return RedirectToAction(nameof(Index));
        }


    }

}
