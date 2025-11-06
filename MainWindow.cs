using System;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Data.Entity;

namespace UMFST.MIP.Variant1
{
    public partial class MainWindow : Form
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadBooks();
            LoadOrders();
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            LoadBooks();
            LoadOrders();
        }

        private void LoadBooks()
        {
            using (var db = new AppContext())
            {
                var books = db.Books.Include("Author").Select(b => new
                {
                    b.Isbn,
                    b.Title,
                    Author = b.Author != null ? b.Author.Name : "",
                    b.Category,
                    b.Price,
                    b.Stock
                }).ToList();

                dgvBooks.DataSource = books;
            }
        }

        private void btnFilter_Click(object sender, EventArgs e)
        {
            string keyword = txtSearch.Text.ToLower();
            using (var db = new AppContext())
            {
                var filtered = db.Books.Include("Author")
                    .Where(b => b.Title.ToLower().Contains(keyword) || (b.Author != null && b.Author.Name.ToLower().Contains(keyword)))
                    .Select(b => new
                    {
                        b.Isbn,
                        b.Title,
                        Author = b.Author != null ? b.Author.Name : "",
                        b.Category,
                        b.Price,
                        b.Stock
                    }).ToList();

                dgvBooks.DataSource = filtered;
            }
        }

        private void btnRestock_Click(object sender, EventArgs e)
        {
            if (dgvBooks.CurrentRow == null) return;

            string isbn = dgvBooks.CurrentRow.Cells["Isbn"].Value.ToString();

            using (var db = new AppContext())
            {
                var book = db.Books.Find(isbn);
                if (book != null)
                {
                    book.Stock += 10;
                    db.SaveChanges();
                    LoadBooks();
                }
            }
        }

        private void LoadOrders()
        {
            using (var db = new AppContext())
            {
                var orders = db.Orders.Include("Customer").Include("OrderItems.Book").Include("Payment")
                    .Select(o => new
                    {
                        o.Id,
                        Customer = o.Customer != null ? o.Customer.Name : "",
                        o.OrderDate,
                        o.IsPaid,
                        TotalAmount = o.OrderItems.Sum(oi => oi.Quantity * oi.UnitPrice - oi.Discount)
                    }).ToList();

                dgvOrders.DataSource = orders;
            }
        }

        private void dgvOrders_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvOrders.CurrentRow == null) return;

            string orderId = dgvOrders.CurrentRow.Cells["Id"].Value.ToString();

            using (var db = new AppContext())
            {
                var items = db.OrderItems.Include("Book")
                    .Where(oi => oi.OrderId == orderId)
                    .Select(oi => new
                    {
                        oi.Id,
                        Book = oi.Book != null ? oi.Book.Title : "",
                        oi.Quantity,
                        oi.UnitPrice,
                        oi.Discount,
                        Total = (oi.Quantity * oi.UnitPrice - oi.Discount)
                    }).ToList();

                dgvOrderItems.DataSource = items;
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            using (var db = new AppContext())
            {
                var totalSales = db.Orders.Where(o => o.IsPaid)
                    .Sum(o => (decimal?)o.OrderItems.Sum(oi => oi.Quantity * oi.UnitPrice - oi.Discount)) ?? 0;

                var lowStockBooks = db.Books.Where(b => b.Stock < 5)
                    .Select(b => new { b.Title, b.Stock }).ToList();

                var bestSelling = db.OrderItems.GroupBy(oi => oi.BookId)
                    .Select(g => new { BookId = g.Key, QuantitySold = g.Sum(oi => oi.Quantity) })
                    .OrderByDescending(g => g.QuantitySold)
                    .FirstOrDefault();

                string bestSellingTitle = "";
                int bestSellingQty = 0;

                if (bestSelling != null)
                {
                    var book = db.Books.Find(bestSelling.BookId);
                    bestSellingTitle = book?.Title ?? "";
                    bestSellingQty = bestSelling.QuantitySold;
                }

                var report = new System.Text.StringBuilder();
                report.AppendLine("BOOKVERSE REPORT (2025)");
                report.AppendLine("=======================");
                report.AppendLine($"Total sales: {totalSales} EUR");
                report.AppendLine("Books below stock threshold (5):");
                foreach (var b in lowStockBooks)
                    report.AppendLine($"- {b.Title} ({b.Stock} left)");
                report.AppendLine($"Best-selling: {bestSellingTitle} ({bestSellingQty} units sold)");

                rtbReport.Text = report.ToString();
                File.WriteAllText("sales_report.txt", report.ToString());

                MessageBox.Show("Report exported to sales_report.txt");
            }
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Biztosan újra akarod tölteni az adatbázist? Minden adat törlődik!",
                "Reset Database",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (result == DialogResult.Yes)
            {
                try
                {
                    DataImporter.ImportData();
                    LoadBooks();
                    LoadOrders();
                    rtbReport.Clear();
                    MessageBox.Show("Database reset and reimport completed.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Hiba a DB reset közben: " + ex.Message);
                }
            }
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            try
            {
                DataImporter.ImportData();
                LoadBooks();
                LoadOrders();
                rtbReport.Clear();
                MessageBox.Show("Import completed successfully!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hiba az importálás közben: " + ex.Message);
            }
        }
    }
}
