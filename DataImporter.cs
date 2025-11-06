using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json;

namespace UMFST.MIP.Variant1
{
    public static class DataImporter
    {
        private const string JsonUrl = "https://cdn.shopify.com/s/files/1/0883/3282/8936/files/data_bookstore_final.json?v=1762418524";
        private const string InvalidLogFile = "invalid_bookstore.txt";

        public static void ImportData()
        {
            try
            {
                // JSON letöltése
                string json;
                using (var webClient = new WebClient())
                {
                    json = webClient.DownloadString(JsonUrl);
                }

                var root = JsonConvert.DeserializeObject<RootObject>(json);

                using (var db = new AppContext())
                {
                    // DB reset
                    db.Database.Delete();
                    db.Database.Create();

                    // Authors
                    foreach (var author in root.Authors)
                    {
                        if (string.IsNullOrWhiteSpace(author.Id) || string.IsNullOrWhiteSpace(author.Name))
                        {
                            LogInvalid("Author", author.Id ?? "null");
                            continue;
                        }
                        db.Authors.Add(new Author
                        {
                            Id = author.Id,
                            Name = author.Name,
                            Country = author.Country
                        });
                    }
                    db.SaveChanges();

                    // Books
                    foreach (var book in root.Books)
                    {
                        if (string.IsNullOrWhiteSpace(book.Isbn) || string.IsNullOrWhiteSpace(book.Title))
                        {
                            LogInvalid("Book", book.Isbn ?? "null");
                            continue;
                        }

                        // Ellenőrzés: Author létezik
                        var authorExists = db.Authors.Any(a => a.Id == book.AuthorId);
                        if (!authorExists)
                        {
                            LogInvalid("Book", book.Isbn + " (Author not found)");
                            continue;
                        }

                        db.Books.Add(new Book
                        {
                            Isbn = book.Isbn,
                            Title = book.Title,
                            Price = book.Price,
                            Stock = book.Stock,
                            Category = book.Categories != null ? string.Join(", ", book.Categories) : "",
                            AuthorId = book.AuthorId
                        });
                    }
                    db.SaveChanges();

                  
                    db.SaveChanges();

                    // Orders + OrderItems + Payments
                    foreach (var order in root.Orders)
                    {
                        if (string.IsNullOrWhiteSpace(order.Id) || string.IsNullOrWhiteSpace(order.CustomerId))
                        {
                            LogInvalid("Order", order.Id ?? "null");
                            continue;
                        }

                        var customerExists = db.Customers.Any(c => c.Id == order.CustomerId);
                        if (!customerExists)
                        {
                            LogInvalid("Order", order.Id + " (Customer not found)");
                            continue;
                        }

                        DateTime? orderDate = null;
                        if (!string.IsNullOrWhiteSpace(order.Date))
                        {
                            if (DateTime.TryParse(order.Date, out var parsedDate))
                            {
                                orderDate = parsedDate;
                            }
                        }

                        var newOrder = new Order
                        {
                            Id = order.Id,
                            CustomerId = order.CustomerId,
                            OrderDate = orderDate,
                            IsPaid = order.Status != null && order.Status.ToLower() == "paid"
                        };
                        db.Orders.Add(newOrder);
                        db.SaveChanges();

                        foreach (var item in order.Items)
                        {
                            if (string.IsNullOrWhiteSpace(item.Isbn))
                            {
                                LogInvalid("OrderItem", "null");
                                continue;
                            }

                            var bookExists = db.Books.Any(b => b.Isbn == item.Isbn);
                            if (!bookExists)
                            {
                                LogInvalid("OrderItem", item.Isbn + " (Book not found)");
                                continue;
                            }

                            db.OrderItems.Add(new OrderItem
                            {
                                OrderId = newOrder.Id,
                                BookId = item.Isbn,
                                Quantity = item.Qty,
                                UnitPrice = item.UnitPrice,
                                Discount = item.Discount
                            });
                        }

                        if (order.Payment != null)
                        {
                            db.Payments.Add(new Payment
                            {
                                Id = order.Payment.Id,
                                OrderId = newOrder.Id,
                                Method = order.Payment.Method,
                                Amount = order.Payment.Amount,
                                Captured = order.Payment.Captured
                            });
                        }

                        db.SaveChanges();
                    }
                }

                Console.WriteLine("Import completed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Hiba az importálás közben: " + ex.Message);
            }
        }

        private static void LogInvalid(string type, string id)
        {
            File.AppendAllText(InvalidLogFile, $"{type} with ID {id} is invalid\n");
        }

        // 🔹 RootObject a JSON szerkezethez
        private class RootObject
        {
            public List<AuthorJson> Authors { get; set; }
            public List<BookJson> Books { get; set; }
            public List<CustomerJson> Customers { get; set; }
            public List<OrderJson> Orders { get; set; }
        }

        // JSON DTO-k
        private class AuthorJson { public string Id; public string Name; public string Country; }
        private class BookJson { public string Isbn; public string Title; public string AuthorId; public decimal Price; public int Stock; public List<string> Categories; }
        private class CustomerJson { public string Id; public string Name; public string Email; }
        private class OrderJson
        {
            public string Id;
            public string CustomerId;
            public DateTime OrderDate;
            public string Date;
            public string Status;
            public List<OrderItemJson> Items;
            public PaymentJson Payment;
        }
        private class OrderItemJson { public string Isbn; public int Qty; public decimal UnitPrice; public decimal Discount; }
        private class PaymentJson { public string Id; public string Method; public decimal Amount; public bool Captured; }
    }
}
