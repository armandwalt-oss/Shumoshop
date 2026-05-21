using Microsoft.AspNetCore.Mvc;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Services;
using System;
using System.Net;

namespace WebApplication1.Controllers
{
    public class ContactUsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ContactUsController> _logger;

        public ContactUsController(
            ApplicationDbContext context,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<ContactUsController> logger)
        {
            _context = context;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
        }

        // GET: ContactUs
        public ActionResult Index()
        {
            return View();
        }

        // POST: ContactUs — save + notify admin + ack the submitter.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ContactUs contact)
        {
            if (!ModelState.IsValid) return View(contact);

            contact.SubmittedDate = DateTime.Now;
            _context.Contacts.Add(contact);
            await _context.SaveChangesAsync();

            // Notify admin so they actually find out about the message.
            var adminEmail = _configuration["Email:AdminEmail"];
            if (!string.IsNullOrWhiteSpace(adminEmail))
            {
                var adminSubject = $"New contact form submission — {contact.Subject}";
                var adminBody = BuildAdminNotificationHtml(contact);
                try
                {
                    await _emailService.SendEmailAsync(adminEmail, adminSubject, adminBody);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to forward contact form to admin");
                }
            }

            // Acknowledge the submitter — best-effort.
            if (!string.IsNullOrWhiteSpace(contact.Email))
            {
                var ackSubject = "We received your message — ShumoShop";
                var ackBody = BuildSubmitterAckHtml(contact);
                try
                {
                    await _emailService.SendEmailAsync(contact.Email, ackSubject, ackBody);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send contact form acknowledgement to {Email}", contact.Email);
                }
            }

            TempData["SuccessMessage"] = "Thank you for contacting us! We'll get back to you soon.";
            return RedirectToAction("Index");
        }

        private static string BuildAdminNotificationHtml(ContactUs c) => $@"
            <html><body style='font-family:Arial,sans-serif;'>
            <h2>New contact form submission</h2>
            <p><strong>From:</strong> {WebUtility.HtmlEncode(c.Name)} &lt;{WebUtility.HtmlEncode(c.Email)}&gt;</p>
            <p><strong>Subject:</strong> {WebUtility.HtmlEncode(c.Subject)}</p>
            <p><strong>Submitted:</strong> {c.SubmittedDate:dd MMM yyyy HH:mm}</p>
            <hr/>
            <p style='white-space:pre-wrap;'>{WebUtility.HtmlEncode(c.Message)}</p>
            </body></html>";

        private static string BuildSubmitterAckHtml(ContactUs c) => $@"
            <html><body style='font-family:Arial,sans-serif;'>
            <h2>Thanks for reaching out!</h2>
            <p>Hi {WebUtility.HtmlEncode(c.Name)},</p>
            <p>We received your message and will get back to you as soon as possible.</p>
            <p><strong>Your message:</strong></p>
            <blockquote style='border-left:3px solid #0b66c3; padding-left:12px; color:#444;'>
                {WebUtility.HtmlEncode(c.Message)}
            </blockquote>
            <p>— The ShumoShop team</p>
            </body></html>";
    }
}
