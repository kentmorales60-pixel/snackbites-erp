using Microsoft.AspNet.Identity;
using SnackBites.Erp2.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace SnackBites.Erp2.Controllers
{
    [Authorize]
    public class RequisitionsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            var list = db.Requisitions.OrderByDescending(r => r.Id).ToList();
            return View(list);
        }

        [Authorize(Roles = "RequisitionStaff")]
        public ActionResult Create()
        {
            var vm = new RequisitionCreateViewModel();
            for (int i = 0; i < 5; i++) vm.Lines.Add(new RequisitionLineInput());
            LoadItems();
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "RequisitionStaff")]
        public ActionResult Create(RequisitionCreateViewModel vm)
        {
            var posted = vm.Lines ?? new List<RequisitionLineInput>();

            // keep only filled rows, and add up duplicates of the same item
            var lines = posted
                .Where(l => l.ItemId.HasValue && l.Qty.HasValue && l.Qty.Value > 0)
                .GroupBy(l => l.ItemId.Value)
                .Select(g => new RequisitionLine
                {
                    ItemId = g.Key,
                    QtyRequested = g.Sum(x => x.Qty.Value)
                })
                .ToList();

            if (!lines.Any())
                ModelState.AddModelError("", "Add at least one item with a quantity.");

            if (!ModelState.IsValid)
            {
                if (vm.Lines == null || vm.Lines.Count == 0)
                {
                    vm.Lines = new List<RequisitionLineInput>();
                    for (int i = 0; i < 5; i++) vm.Lines.Add(new RequisitionLineInput());
                }
                LoadItems();
                return View(vm);
            }

            var req = new Requisition
            {
                RequisitionNo = "TEMP",
                Department = vm.Department,
                Status = RequisitionStatus.Submitted,
                RequestedById = User.Identity.GetUserId(),
                DateRequested = DateTime.Now,
                Lines = lines
            };

            db.Requisitions.Add(req);
            db.SaveChanges();

            req.RequisitionNo = "REQ-" + req.Id.ToString("00000");
            db.SaveChanges();

            return RedirectToAction("Details", new { id = req.Id });
        }

        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var req = db.Requisitions
                        .Include(r => r.Lines.Select(l => l.Item))
                        .SingleOrDefault(r => r.Id == id);

            if (req == null) return HttpNotFound();
            return View(req);
        }

        [Authorize(Roles = "CoOwner1")]
        public ActionResult Review(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var req = db.Requisitions
                        .Include(r => r.Lines.Select(l => l.Item))
                        .SingleOrDefault(r => r.Id == id);

            if (req == null) return HttpNotFound();
            if (req.Status != RequisitionStatus.Submitted)
                return RedirectToAction("Details", new { id });

            return View(req);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "CoOwner1")]
        public ActionResult Review(int id, string decision, string remarks)
        {
            var req = db.Requisitions.Find(id);
            if (req == null) return HttpNotFound();
            if (req.Status != RequisitionStatus.Submitted)
                return RedirectToAction("Details", new { id });

            if (decision == "approve")
            {
                req.Status = RequisitionStatus.Approved;
            }
            else if (decision == "reject")
            {
                if (string.IsNullOrWhiteSpace(remarks))
                {
                    TempData["Error"] = "Please enter a reason for rejecting.";
                    return RedirectToAction("Review", new { id });
                }
                req.Status = RequisitionStatus.ReturnedForRevision;
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            req.Co1ReviewedById = User.Identity.GetUserId();
            req.Co1ReviewedOn = DateTime.Now;
            req.ReviewRemarks = remarks;
            db.SaveChanges();

            return RedirectToAction("Details", new { id });
        }

        public ActionResult Slip(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var req = db.Requisitions
                        .Include(r => r.Lines.Select(l => l.Item))
                        .SingleOrDefault(r => r.Id == id);

            if (req == null) return HttpNotFound();

            // the slip only exists once Co-Owner 1 has approved
            if (req.Status == RequisitionStatus.Submitted ||
                req.Status == RequisitionStatus.ReturnedForRevision ||
                req.Status == RequisitionStatus.Cancelled)
                return RedirectToAction("Details", new { id });

            ViewBag.RequestedBy = db.Users.Where(u => u.Id == req.RequestedById)
                                          .Select(u => u.UserName).FirstOrDefault();
            ViewBag.ApprovedBy = db.Users.Where(u => u.Id == req.Co1ReviewedById)
                                         .Select(u => u.UserName).FirstOrDefault();
            return View(req);
        }

        // Requisition Staff can revise a requisition that was returned by Co-Owner 1
        // OR put on hold because the Main Owner rejected the purchase budget.
        [Authorize(Roles = "RequisitionStaff")]
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var userId = User.Identity.GetUserId();
            var req = db.Requisitions.Include(r => r.Lines).SingleOrDefault(r => r.Id == id);

            if (req == null) return HttpNotFound();
            if (req.RequestedById != userId ||
                (req.Status != RequisitionStatus.ReturnedForRevision && req.Status != RequisitionStatus.OnHold))
                return RedirectToAction("Details", new { id });

            var vm = new RequisitionCreateViewModel { Department = req.Department };
            foreach (var l in req.Lines)
                vm.Lines.Add(new RequisitionLineInput { ItemId = l.ItemId, Qty = l.QtyRequested });
            while (vm.Lines.Count < 5) vm.Lines.Add(new RequisitionLineInput());

            ViewBag.RequisitionId = req.Id;
            ViewBag.RequisitionNo = req.RequisitionNo;
            ViewBag.ReviewRemarks = req.ReviewRemarks;
            LoadItems();
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "RequisitionStaff")]
        public ActionResult Edit(int id, RequisitionCreateViewModel vm)
        {
            var userId = User.Identity.GetUserId();
            var req = db.Requisitions.Include(r => r.Lines).SingleOrDefault(r => r.Id == id);

            if (req == null) return HttpNotFound();
            if (req.RequestedById != userId ||
                (req.Status != RequisitionStatus.ReturnedForRevision && req.Status != RequisitionStatus.OnHold))
                return RedirectToAction("Details", new { id });

            var posted = vm.Lines ?? new List<RequisitionLineInput>();

            var newLines = posted
                .Where(l => l.ItemId.HasValue && l.Qty.HasValue && l.Qty.Value > 0)
                .GroupBy(l => l.ItemId.Value)
                .Select(g => new RequisitionLine
                {
                    ItemId = g.Key,
                    QtyRequested = g.Sum(x => x.Qty.Value)
                })
                .ToList();

            if (!newLines.Any())
                ModelState.AddModelError("", "Add at least one item with a quantity.");

            if (!ModelState.IsValid)
            {
                vm.Lines = posted;
                while (vm.Lines.Count < 5) vm.Lines.Add(new RequisitionLineInput());
                ViewBag.RequisitionId = req.Id;
                ViewBag.RequisitionNo = req.RequisitionNo;
                ViewBag.ReviewRemarks = req.ReviewRemarks;
                LoadItems();
                return View(vm);
            }

            // replace the old lines with the revised ones
            // (an on-hold requisition already had its reserved stock released)
            db.RequisitionLines.RemoveRange(req.Lines.ToList());
            foreach (var l in newLines) req.Lines.Add(l);

            req.Department = vm.Department;
            req.Status = RequisitionStatus.Submitted;   // goes back to Co-Owner 1
            req.Co1ReviewedById = null;
            req.Co1ReviewedOn = null;
            req.ReviewRemarks = null;
            db.SaveChanges();

            return RedirectToAction("Details", new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "RequisitionStaff")]
        public ActionResult Cancel(int id)
        {
            var userId = User.Identity.GetUserId();
            var req = db.Requisitions.Find(id);

            if (req == null) return HttpNotFound();
            if (req.RequestedById == userId &&
                (req.Status == RequisitionStatus.ReturnedForRevision || req.Status == RequisitionStatus.OnHold))
            {
                req.Status = RequisitionStatus.Cancelled;
                db.SaveChanges();
            }
            return RedirectToAction("Details", new { id });
        }

        public ActionResult IssuanceSlip(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var req = db.Requisitions
                        .Include(r => r.Lines.Select(l => l.Item))
                        .SingleOrDefault(r => r.Id == id);

            if (req == null) return HttpNotFound();
            if (req.Status != RequisitionStatus.Reserved &&
                req.Status != RequisitionStatus.Issued &&
                req.Status != RequisitionStatus.Closed)
                return RedirectToAction("Details", new { id });

            return View(req);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "InventoryStaff")]
        public ActionResult Issue(int id, string receivedBy)
        {
            var req = db.Requisitions.Find(id);
            if (req == null) return HttpNotFound();
            if (req.Status != RequisitionStatus.Reserved)
                return RedirectToAction("Details", new { id });

            if (string.IsNullOrWhiteSpace(receivedBy))
            {
                TempData["Error"] = "Enter the name of the staff member receiving the items.";
                return RedirectToAction("IssuanceSlip", new { id });
            }

            req.Status = RequisitionStatus.Issued;
            req.IssuedById = User.Identity.GetUserId();
            req.IssuedOn = DateTime.Now;
            req.AcknowledgedBy = receivedBy.Trim();
            db.SaveChanges();

            return RedirectToAction("Receipt", new { id });
        }

        public ActionResult Receipt(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var req = db.Requisitions
                        .Include(r => r.Lines.Select(l => l.Item))
                        .SingleOrDefault(r => r.Id == id);

            if (req == null) return HttpNotFound();
            if (req.Status != RequisitionStatus.Issued && req.Status != RequisitionStatus.Closed)
                return RedirectToAction("Details", new { id });

            ViewBag.IssuedBy = db.Users.Where(u => u.Id == req.IssuedById)
                                      .Select(u => u.UserName).FirstOrDefault();
            return View(req);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "InventoryStaff")]
        public ActionResult CheckStock(int id)
        {
            var req = db.Requisitions
                        .Include(r => r.Lines.Select(l => l.Item))
                        .SingleOrDefault(r => r.Id == id);

            if (req == null) return HttpNotFound();
            if (req.Status != RequisitionStatus.Approved)
                return RedirectToAction("Details", new { id });

            var userId = User.Identity.GetUserId();

            foreach (var line in req.Lines)
            {
                var available = Math.Max(0, line.Item.QtyAvailable);
                var reserve = Math.Min(line.QtyRequested, available);

                line.QtyReserved = reserve;
                line.Item.QtyReserved += reserve;

                if (reserve > 0)
                {
                    db.StockMovements.Add(new StockMovement
                    {
                        ItemId = line.ItemId,
                        Qty = reserve,
                        Type = "Reserve",
                        RequisitionId = req.Id,
                        UserId = userId
                    });
                }
            }

            req.Status = req.Lines.All(l => l.QtyShort <= 0)
                ? RequisitionStatus.Reserved           // enough stock: go to issuing
                : RequisitionStatus.AwaitingPurchase;  // shortage: Co-Owner 2 prepares purchase requisition

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "Stock was changed by someone else. Please try again.";
            }

            return RedirectToAction("Details", new { id });
        }

        // ------------------------------------------------------------------
        // SHORTAGE BRANCH
        // ------------------------------------------------------------------

        // Co-Owner 2: prepare the Purchase Requisition for the net shortage
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "CoOwner2")]
        public ActionResult PreparePurchaseRequisition(int id)
        {
            var req = db.Requisitions
                        .Include(r => r.Lines.Select(l => l.Item))
                        .SingleOrDefault(r => r.Id == id);

            if (req == null) return HttpNotFound();
            if (req.Status != RequisitionStatus.AwaitingPurchase)
                return RedirectToAction("Details", new { id });

            // purchase only the net shortage
            var total = req.Lines.Where(l => l.QtyShort > 0)
                                 .Sum(l => l.QtyShort * l.Item.UnitCost);

            db.PurchaseRequisitions.Add(new PurchaseRequisition
            {
                RequisitionId = req.Id,
                EstimatedTotal = total,
                Status = "Pending"
            });
            req.Status = RequisitionStatus.PendingBudget;
            db.SaveChanges();

            return RedirectToAction("PurchaseTicket", new { id });
        }

        // Purchase Requisition Ticket (Main Owner decides here)
        public ActionResult PurchaseTicket(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var req = db.Requisitions
                        .Include(r => r.Lines.Select(l => l.Item))
                        .SingleOrDefault(r => r.Id == id);

            if (req == null) return HttpNotFound();

            var pr = db.PurchaseRequisitions
                       .Where(p => p.RequisitionId == id)
                       .OrderByDescending(p => p.Id)
                       .FirstOrDefault();

            if (pr == null) return RedirectToAction("Details", new { id });

            ViewBag.Pr = pr;
            return View(req);
        }

        // Main Owner: approve budget, or reject (Notice of Hold)
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "MainOwner")]
        public ActionResult BudgetDecision(int id, string decision, string remarks)
        {
            var req = db.Requisitions
                        .Include(r => r.Lines.Select(l => l.Item))
                        .SingleOrDefault(r => r.Id == id);

            if (req == null) return HttpNotFound();
            if (req.Status != RequisitionStatus.PendingBudget)
                return RedirectToAction("Details", new { id });

            var pr = db.PurchaseRequisitions
                       .Where(p => p.RequisitionId == id)
                       .OrderByDescending(p => p.Id)
                       .FirstOrDefault();

            if (pr == null) return HttpNotFound();

            var userId = User.Identity.GetUserId();

            if (decision == "approve")
            {
                pr.Status = "Approved";
                req.Status = RequisitionStatus.BudgetApproved;   // purchasing and delivery take over from here
            }
            else if (decision == "reject")
            {
                if (string.IsNullOrWhiteSpace(remarks))
                {
                    TempData["Error"] = "Please enter a reason for rejecting the budget.";
                    return RedirectToAction("PurchaseTicket", new { id });
                }

                pr.Status = "Rejected";

                // Notice of Hold: release the partial stock that was reserved
                foreach (var line in req.Lines)
                {
                    var released = line.QtyReserved;
                    if (released > 0)
                    {
                        line.Item.QtyReserved -= released;
                        line.QtyReserved = 0;

                        db.StockMovements.Add(new StockMovement
                        {
                            ItemId = line.ItemId,
                            Qty = -released,
                            Type = "Release",
                            RequisitionId = req.Id,
                            UserId = userId
                        });
                    }
                }

                req.Status = RequisitionStatus.OnHold;
                req.ReviewRemarks = remarks;
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            pr.DecidedById = userId;
            pr.DecidedOn = DateTime.Now;
            pr.Remarks = remarks;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "Stock was changed by someone else. Please try again.";
            }

            return RedirectToAction("Details", new { id });
        }

        // Inventory Staff: "Allocate Received Items to Requisition".
        // Run after the delivery is received and the stock balance is updated.
        // Moves the requisition back to Reserved so it can be issued and closed.
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "InventoryStaff")]
        public ActionResult AllocateReceived(int id)
        {
            var req = db.Requisitions
                        .Include(r => r.Lines.Select(l => l.Item))
                        .SingleOrDefault(r => r.Id == id);

            if (req == null) return HttpNotFound();

            // BudgetApproved is allowed for now so you can test without the delivery module.
            // Later, remove it so this only runs after the delivery module sets PurchaseOrdered/Received.
            if (req.Status != RequisitionStatus.BudgetApproved &&
                req.Status != RequisitionStatus.PurchaseOrdered &&
                req.Status != RequisitionStatus.Received)
                return RedirectToAction("Details", new { id });

            var userId = User.Identity.GetUserId();

            foreach (var line in req.Lines)
            {
                if (line.QtyShort <= 0) continue;

                var available = Math.Max(0, line.Item.QtyAvailable);
                var add = Math.Min(line.QtyShort, available);
                if (add <= 0) continue;

                line.QtyReserved += add;
                line.Item.QtyReserved += add;

                db.StockMovements.Add(new StockMovement
                {
                    ItemId = line.ItemId,
                    Qty = add,
                    Type = "Reserve",
                    RequisitionId = req.Id,
                    UserId = userId
                });
            }

            if (req.Lines.All(l => l.QtyShort <= 0))
                req.Status = RequisitionStatus.Reserved;   // continues to issuance slip, issue, deduct and close
            else
                TempData["Error"] = "Some items are still short. Wait for the remaining delivery, then allocate again.";

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "Stock was changed by someone else. Please try again.";
            }

            return RedirectToAction("Details", new { id });
        }

        // ------------------------------------------------------------------

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "InventoryStaff")]
        public ActionResult CloseRequisition(int id)
        {
            var req = db.Requisitions
                        .Include(r => r.Lines.Select(l => l.Item))
                        .SingleOrDefault(r => r.Id == id);

            if (req == null) return HttpNotFound();
            if (req.Status != RequisitionStatus.Issued)
                return RedirectToAction("Details", new { id });

            var userId = User.Identity.GetUserId();

            foreach (var line in req.Lines)
            {
                line.Item.QtyOnHand -= line.QtyReserved;
                line.Item.QtyReserved -= line.QtyReserved;

                db.StockMovements.Add(new StockMovement
                {
                    ItemId = line.ItemId,
                    Qty = -line.QtyReserved,
                    Type = "Issue",
                    RequisitionId = req.Id,
                    UserId = userId
                });
            }

            req.Status = RequisitionStatus.Closed;
            req.ClosedOn = DateTime.Now;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "Stock was changed by someone else. Please try again.";
            }

            return RedirectToAction("Details", new { id });
        }

        private void LoadItems()
        {
            var items = db.Items.OrderBy(i => i.Name)
                          .Select(i => new { i.Id, Label = i.Name + " (" + i.Unit + ")" })
                          .ToList();
            ViewBag.Items = new SelectList(items, "Id", "Label");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}