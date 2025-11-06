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
                string json;
                using (var webClient = new WebClient())
                {
                    json = webClient.DownloadString(JsonUrl);
                }

                var root = JsonConvert.DeserializeObject<RootObject>(json);

                using (var db = new AppContext())
                {
                    db.Database.Delete();
                    db.Database.Create();

                    // Authors
                    foreach (var author in root.authors)
                    {
                        if (string.IsNullOrEmpty(author.Id) || string.IsNullOrEmpty(author.Name))
                        {
                            LogInvalid("Author", author.Id ?? "NULL");
                            continue;
                        }
                        db.Authors.Add(author);
                    }

                    // Books
                    foreach (var book in root.books)
                    {
                        if (string.IsNullOrEmpty(book.Isbn) || string.IsNullOrEmpty(book.Title) || string.IsNullOrEmpty(book.AuthorId))
                        {
                            LogInvalid("Book", book.Isbn ?? "NULL");
                            continue;
                        }
                        db.Books.Add(book);
                    }

                    // Customers
                    foreach (var order in root.orders)
                    {
                        var c = order.customer;
                        if (c == null || string.IsNullOrEmpty(c.Id) || string.IsNullOrEmpty(c.Name))
                        {
                            LogInvalid("Customer", c?.Id ?? "NULL");
                            continue;
                        }
                        if (!db.Customers.Any(x => x.Id == c.Id))
                            db.Customers.Add(c);
                    }

                    // Orders + OrderItems + Payments
                    foreach (var order in root.orders)
                    {
                        if (order.customer == null) continue;

                        var newOrder = new Order
                        {
                            Id = order.id,
                            CustomerId = order.customer.Id,
                            OrderDate = DateTime.TryParse(order.date, out DateTime dt) ? dt : DateTime.MinValue,
                            IsPaid = order.status == "PAID" || order.status == "COMPLETED"
                        };

                        newOrder.OrderItems = new List<OrderItem>();
                        foreach (var item in order.items)
                        {
                            var oi = new OrderItem
                            {
                                OrderId = newOrder.Id,
                                BookId = item.isbn,
                                Quantity = item.qty > 0 ? item.qty : 0,
                                UnitPrice = item.unitPrice,
                                Discount = item.discount
                            };
                            newOrder.OrderItems.Add(oi);
                            db.OrderItems.Add(oi);
                        }

                        if (order.payment != null)
                        {
                            var pay = new Payment
                            {
                                Id = Guid.NewGuid().ToString(),
                                OrderId = newOrder.Id,
                                Amount = order.items.Sum(i => i.qty * i.unitPrice - i.discount),
                                Method = order.payment.method,
                                Captured = order.status == "PAID"
                            };
                            newOrder.Payment = pay;
                            db.Payments.Add(pay);
                        }

                        db.Orders.Add(newOrder);
                    }

                    db.SaveChanges();
                }

                Console.WriteLine("Import completed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error importing data: " + ex.Message);
            }
        }

        private static void LogInvalid(string type, string id)
        {
            File.AppendAllText(InvalidLogFile, $"{type} with ID {id} is invalid\n");
        }

        // RootObject a JSON szerkezethez
        private class RootObject
        {
            public List<Author> authors { get; set; }
            public List<Book> books { get; set; }
            public List<OrderJson> orders { get; set; }
        }

        private class OrderJson
        {
            public string id { get; set; }
            public string date { get; set; }
            public Customer customer { get; set; }
            public List<OrderItemJson> items { get; set; }
            public PaymentJson payment { get; set; }
            public string status { get; set; }
        }

        private class OrderItemJson
        {
            public string isbn { get; set; }
            public int qty { get; set; }
            public decimal unitPrice { get; set; }
            public decimal discount { get; set; }
        }

        private class PaymentJson
        {
            public string method { get; set; }
        }
    }
}
