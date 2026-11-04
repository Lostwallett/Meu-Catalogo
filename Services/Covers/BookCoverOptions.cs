using System.Net.Mail;

namespace MyCatalog.Services.Covers;

public class BookCoverOptions
{
    public bool Enabled { get; set; }
    public string ContactEmail { get; set; } = string.Empty;

    public bool HasValidContact => ContactEmail.Length <= 254 &&
        ContactEmail.IndexOfAny(['\r', '\n', '(', ')']) < 0 &&
        MailAddress.TryCreate(ContactEmail, out var address) && address.Address == ContactEmail;
}
