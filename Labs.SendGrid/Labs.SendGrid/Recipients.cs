using SendGrid.Helpers.Mail;

namespace Labs.SendGrid;

public static class Recipients
{
    public static EmailAddress From = new EmailAddress("no-reply@thebirdcage.link");

    public static List<Personalization> Tos = new List<Personalization>
    {
        new Personalization
        {
            Tos = new List<EmailAddress>
            {
                new EmailAddress("bob.smith@gmail.com", "Bob Smith")
            },
            CustomArgs = new Dictionary<string, string>
            {
                { "tenant_id", "11" },
                { "recipient_id", "25" },
                { "run_id", "6" }
            },
            TemplateData = new
            {
                user = new {
                    name = "Bob",
                    phone = "123456"
                },
                order = new {
                    reference = "FRUIT-101",
                    currency = "GBP",
                    grandTotal = 13.25,
                    items = new List<dynamic>
                    {
                        new { name = "🍎 Apples", quantity = 6, total = 12 },
                        new { name = "🍌 Bananas", quantity = 3, total = 1.25 }
                    }
                }
            }
        },
        new Personalization
        {
            Tos = new List<EmailAddress>
            {
                new EmailAddress("alice.smith@gmail.com", "Alice Smith")
            },
            CustomArgs = new Dictionary<string, string>
            {
                { "tenant_id", "11" },
                { "recipient_id", "33" },
                { "run_id", "6" }
            },
            TemplateData = new
            {
                user = new {
                    name = "Alice",
                    phone = "123456"
                },
                order = new {
                    reference = "FRUIT-999",
                    currency = "GBP",
                    grandTotal = 3.39,
                    items = new List<dynamic>
                    {
                        new { name = "🍊 Oranges", quantity = 6, total = 2.99 },
                        new { name = "🥝 Kiwis", quantity = 3, total = 0.40 }
                    }
                }
            }
        }
    };
}
