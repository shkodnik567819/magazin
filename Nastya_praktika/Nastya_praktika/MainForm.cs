using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace Nastya_praktika
{
    public static class StringExtensions
    {
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength - 3) + "...";
        }
    }

    public partial class MainForm : Form
    {
        private readonly string ConnectionString = "Host=91.233.173.91;Username=selectel;Password=selectel;Database=selectel";
        public int stuff { get; set; } // ID магазина
        private DataTable productsTable = new DataTable();
        private List<CartItem> cart = new List<CartItem>();
        private decimal totalSum = 0;
        private DataGridView cartGrid = new DataGridView();
        private Label totalLabel = new Label();
        private DataGridView productsGrid;
        private DataGridView purchasesGrid;
        private DataGridView historyGrid;
        private TextBox chequeTextBox;

        public MainForm()
        {
            InitializeComponent();
            this.Size = new Size(900, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Система продаж кондитерских изделий";
            InitializeTabs();
            this.Load += (s, e) => RefreshAllData();
        }

        private void RefreshAllData()
        {
            LoadProducts(productsGrid);
            LoadPurchasesHistory();
            LoadTransactionHistory();
        }

        private void InitializeTabs()
        {
            TabControl tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            tabControl.SelectedIndexChanged += (s, e) => RefreshAllData();
            this.Controls.Add(tabControl);

            InitializeSalesTab(tabControl);
            InitializePurchasesTab(tabControl);
            InitializeHistoryTab(tabControl);
        }

        private void InitializeSalesTab(TabControl tabControl)
        {
            TabPage salesTab = new TabPage("Продажи");
            salesTab.BackColor = Color.FromArgb(255, 253, 245);
            tabControl.TabPages.Add(salesTab);

            productsGrid = new DataGridView
            {
                Location = new Point(20, 20),
                Size = new Size(700, 200),
                BackgroundColor = Color.FromArgb(255, 253, 245),
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            salesTab.Controls.Add(productsGrid);

            Button addToCartBtn = new Button
            {
                Text = "Добавить в корзину",
                Location = new Point(20, 230),
                Size = new Size(150, 30),
                BackColor = Color.FromArgb(230, 220, 200),
                FlatStyle = FlatStyle.Flat
            };
            addToCartBtn.Click += (s, e) => AddToCart(productsGrid);
            salesTab.Controls.Add(addToCartBtn);

            Button refreshBtn = new Button
            {
                Text = "Обновить",
                Location = new Point(180, 230),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(230, 220, 200),
                FlatStyle = FlatStyle.Flat
            };
            refreshBtn.Click += (s, e) => LoadProducts(productsGrid);
            salesTab.Controls.Add(refreshBtn);

            cartGrid = new DataGridView
            {
                Location = new Point(20, 270),
                Size = new Size(700, 150),
                BackgroundColor = Color.FromArgb(255, 253, 245),
                BorderStyle = BorderStyle.FixedSingle
            };
            salesTab.Controls.Add(cartGrid);

            totalLabel = new Label
            {
                Text = "Итого: 0 руб.",
                Location = new Point(20, 430),
                Size = new Size(200, 20),
                ForeColor = Color.FromArgb(70, 70, 70)
            };
            salesTab.Controls.Add(totalLabel);

            ComboBox paymentMethod = new ComboBox
            {
                Location = new Point(20, 460),
                Size = new Size(200, 30),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            paymentMethod.Items.AddRange(new object[] { "Наличные", "Карта", "Безналичный расчет" });
            salesTab.Controls.Add(paymentMethod);

            Button payBtn = new Button
            {
                Text = "Оплатить",
                Location = new Point(240, 460),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(230, 220, 200),
                FlatStyle = FlatStyle.Flat
            };
            payBtn.Click += (s, e) => ProcessPayment(paymentMethod);
            salesTab.Controls.Add(payBtn);
        }

        private void InitializePurchasesTab(TabControl tabControl)
        {
            TabPage purchasesTab = new TabPage("Закупки");
            purchasesTab.BackColor = Color.FromArgb(255, 253, 245);
            tabControl.TabPages.Add(purchasesTab);

            Button newPurchaseBtn = new Button
            {
                Text = "Новая закупка",
                Location = new Point(20, 20),
                Size = new Size(150, 30),
                BackColor = Color.FromArgb(230, 220, 200),
                FlatStyle = FlatStyle.Flat
            };
            newPurchaseBtn.Click += (s, e) => CreateNewPurchase();
            purchasesTab.Controls.Add(newPurchaseBtn);

            purchasesGrid = new DataGridView
            {
                Location = new Point(20, 70),
                Size = new Size(700, 300),
                BackgroundColor = Color.FromArgb(255, 253, 245),
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true
            };
            purchasesTab.Controls.Add(purchasesGrid);
        }

        private void InitializeHistoryTab(TabControl tabControl)
        {
            TabPage historyTab = new TabPage("История транзакций");
            historyTab.BackColor = Color.FromArgb(255, 253, 245);
            tabControl.TabPages.Add(historyTab);

            historyGrid = new DataGridView
            {
                Location = new Point(20, 20),
                Size = new Size(700, 200),
                BackgroundColor = Color.FromArgb(255, 253, 245),
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            historyGrid.SelectionChanged += (s, e) => ShowSelectedCheque();
            historyTab.Controls.Add(historyGrid);

            chequeTextBox = new TextBox
            {
                Location = new Point(20, 230),
                Size = new Size(700, 400),
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                ReadOnly = true,
                Font = new Font("Courier New", 10),
                WordWrap = false,
                AcceptsReturn = true,
                AcceptsTab = true,
                ShortcutsEnabled = true
            };
            historyTab.Controls.Add(chequeTextBox);
        }

        private void ShowSelectedCheque()
        {
            if (historyGrid.SelectedRows.Count > 0)
            {
                DataRowView row = (DataRowView)historyGrid.SelectedRows[0].DataBoundItem;
                string chequeText = row["Чек"].ToString();

                // Форматируем текст для правильного отображения переносов строк
                chequeTextBox.Font = new Font("Courier New", 10);
                chequeTextBox.Text = FormatChequeText(chequeText);
            }
            else
            {
                chequeTextBox.Text = string.Empty;
            }
        }

        private string FormatChequeText(string originalText)
        {
            // 1. Заменяем псевдографические символы
            string formatted = originalText.Replace("║", "│")
                                         .Replace("╔", "┌")
                                         .Replace("╗", "┐")
                                         .Replace("╚", "└")
                                         .Replace("╝", "┘")
                                         .Replace("╠", "├")
                                         .Replace("╣", "┤")
                                         .Replace("╬", "┼")
                                         .Replace("╦", "┬")
                                         .Replace("╩", "┴")
                                         .Replace("═", "─");

            // 2. Убедимся, что переносы строк сохраняются
            formatted = formatted.Replace("\n", Environment.NewLine);

            return formatted;
        }

        private string FormatChequeForDisplay(string chequeText)
        {
            // Заменяем псевдографические символы на ASCII-эквиваленты
            return chequeText.Replace("║", "│")
                           .Replace("╔", "┌")
                           .Replace("╗", "┐")
                           .Replace("╚", "└")
                           .Replace("╝", "┘")
                           .Replace("╠", "├")
                           .Replace("╣", "┤")
                           .Replace("╬", "┼")
                           .Replace("╦", "┬")
                           .Replace("╩", "┴")
                           .Replace("═", "─");
        }

        private void CreateNewPurchase()
        {
            var purchaseForm = new PurchaseForm(stuff, ConnectionString);
            if (purchaseForm.ShowDialog() == DialogResult.OK)
            {
                LoadPurchasesHistory();
                LoadProducts(productsGrid);
            }
        }

        private void LoadProducts(DataGridView grid)
        {
            try
            {
                productsTable.Clear();
                using (var conn = new NpgsqlConnection(ConnectionString))
                {
                    conn.Open();
                    string query = @"SELECT p.item as Название, p.price as Цена, 
                           c.category as Категория, p.count as Количество, c.unit as Ед_изм
                           FROM candy_shop.products p
                           JOIN candy_shop.categories c ON p.category = c.id
                           WHERE p.count > 0 AND p.shop = @shopId";

                    var adapter = new NpgsqlDataAdapter(query, conn);
                    adapter.SelectCommand.Parameters.AddWithValue("@shopId", stuff);
                    adapter.Fill(productsTable);
                    grid.DataSource = productsTable;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки товаров: {ex.Message}");
            }
        }

        private void AddToCart(DataGridView productsGrid)
        {
            if (productsGrid.SelectedRows.Count == 0) return;

            DataRowView row = (DataRowView)productsGrid.SelectedRows[0].DataBoundItem;
            string name = row["Название"].ToString();
            decimal price = Convert.ToDecimal(row["Цена"]);
            string unit = row["Ед_изм"].ToString();

            var quantityForm = new QuantityForm(unit);
            if (quantityForm.ShowDialog() == DialogResult.OK)
            {
                decimal quantity = quantityForm.Quantity;
                if (quantity <= 0)
                {
                    MessageBox.Show("Введите корректное количество");
                    return;
                }

                decimal available = Convert.ToDecimal(row["Количество"]);
                if (quantity > available)
                {
                    MessageBox.Show($"Недостаточно товара на складе. Доступно: {available} {unit}");
                    return;
                }

                cart.Add(new CartItem
                {
                    Name = name,
                    Price = price,
                    Quantity = quantity,
                    Unit = unit
                });

                UpdateCart();
            }
        }

        private void UpdateCart()
        {
            var cartTable = new DataTable();
            cartTable.Columns.Add("Название");
            cartTable.Columns.Add("Количество");
            cartTable.Columns.Add("Ед. изм");
            cartTable.Columns.Add("Цена");
            cartTable.Columns.Add("Сумма");

            totalSum = 0;
            foreach (var item in cart)
            {
                decimal sum = item.Price * item.Quantity;
                totalSum += sum;
                cartTable.Rows.Add(item.Name, item.Quantity, item.Unit, item.Price, sum);
            }

            cartGrid.DataSource = cartTable;
            totalLabel.Text = $"Итого: {totalSum} руб.";
        }

        private void ProcessPayment(ComboBox paymentMethod)
        {
            if (cart.Count == 0)
            {
                MessageBox.Show("Корзина пуста");
                return;
            }

            if (paymentMethod.SelectedItem == null)
            {
                MessageBox.Show("Выберите способ оплаты");
                return;
            }

            var paymentForm = new PaymentForm(totalSum);
            if (paymentForm.ShowDialog() == DialogResult.OK)
            {
                decimal paidAmount = paymentForm.PaidAmount;
                decimal change = paidAmount - totalSum;

                if (change < 0)
                {
                    MessageBox.Show($"Недостаточно средств. Не хватает: {-change} руб.");
                    return;
                }

                string receipt = GenerateReceipt(paymentMethod.SelectedItem.ToString(), paidAmount, change);
                SaveSale(receipt);
                UpdateProductQuantities();

                MessageBox.Show($"Сдача: {change} руб.\n\nЧек:\n{receipt}", "Оплата завершена");

                cart.Clear();
                UpdateCart();
            }
        }

        private string GenerateReceipt(string paymentMethod, decimal paidAmount, decimal change)
        {
            string horizontalLine = new string('═', 42);

            string receipt = $"{horizontalLine}\n";
            receipt += $"{"ЧЕК ПРОДАЖИ".PadLeft(25).PadRight(42)}\n";
            receipt += $"{horizontalLine}\n";
            receipt += $"{"Магазин: " + stuff.ToString().PadRight(42)}\n";
            receipt += $"{"Дата: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm").PadRight(42)}\n";
            receipt += $"{"Номер: " + (DateTime.Now.Ticks % 1000000).ToString().PadRight(42)}\n";
            receipt += $"{horizontalLine}\n";
            receipt += $"{"Товар".PadRight(30)} {"Сумма".PadLeft(12)}\n";
            receipt += $"{horizontalLine}\n";

            foreach (var item in cart)
            {
                string name = item.Name.Length > 25 ? item.Name.Substring(0, 22) + "..." : item.Name;
                string line = $"{name.PadRight(30)} {(item.Price * item.Quantity).ToString("C").PadLeft(12)}\n";
                receipt += line;
            }

            receipt += $"{horizontalLine}\n";
            receipt += $"{"Итого:".PadRight(30)} {totalSum.ToString("C").PadLeft(12)}\n";
            receipt += $"{"Оплата: " + paymentMethod.PadRight(42)}\n";
            receipt += $"{"Внесено:".PadRight(30)} {paidAmount.ToString("C").PadLeft(12)}\n";
            receipt += $"{"Сдача:".PadRight(30)} {change.ToString("C").PadLeft(12)}\n";
            receipt += $"{horizontalLine}";

            return receipt;
        }

        private void SaveSale(string receipt)
        {
            try
            {
                using (var conn = new NpgsqlConnection(ConnectionString))
                {
                    conn.Open();
                    string query = @"INSERT INTO candy_shop.sales(type, shop, cheque, date) 
                                   VALUES(@type, @shop, @cheque, @date)";
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@type", "Продажа");
                        cmd.Parameters.AddWithValue("@shop", stuff);
                        cmd.Parameters.AddWithValue("@cheque", receipt);
                        cmd.Parameters.AddWithValue("@date", DateTime.Now);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения чека: {ex.Message}");
            }
        }

        private void UpdateProductQuantities()
        {
            try
            {
                using (var conn = new NpgsqlConnection(ConnectionString))
                {
                    conn.Open();
                    foreach (var item in cart)
                    {
                        string query = @"UPDATE candy_shop.products 
                               SET count = count - @quantity 
                               WHERE item = @name AND shop = @shop";
                        using (var cmd = new NpgsqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@quantity", item.Quantity);
                            cmd.Parameters.AddWithValue("@name", item.Name);
                            cmd.Parameters.AddWithValue("@shop", stuff);
                            int affected = cmd.ExecuteNonQuery();

                            if (affected == 0)
                            {
                                MessageBox.Show($"Ошибка обновления товара {item.Name}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления количества товаров: {ex.Message}");
            }
        }

        private void LoadPurchasesHistory()
        {
            try
            {
                using (var conn = new NpgsqlConnection(ConnectionString))
                {
                    conn.Open();
                    string query = @"SELECT s.date as Дата, c.contractor as Поставщик, 
                                  'Закупка #' || (s.id % 1000000) as Номер, s.cheque as Чек 
                                  FROM candy_shop.sales s
                                  JOIN candy_shop.contractors c ON s.contractors = c.id
                                  WHERE s.type = 'Закупка' AND s.shop = @shopId
                                  ORDER BY s.date DESC";

                    var adapter = new NpgsqlDataAdapter(query, conn);
                    adapter.SelectCommand.Parameters.AddWithValue("@shopId", stuff);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    purchasesGrid.DataSource = dt;
                    purchasesGrid.Columns["Чек"].Visible = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки истории закупок: {ex.Message}");
            }
        }

        private void LoadTransactionHistory()
        {
            try
            {
                using (var conn = new NpgsqlConnection(ConnectionString))
                {
                    conn.Open();
                    string query = @"SELECT date as Дата, 
                                   type as Тип, 
                                   CASE 
                                       WHEN type = 'Продажа' THEN 'Продажа #' || (id % 1000000)
                                       ELSE 'Закупка #' || (id % 1000000)
                                   END as Номер,
                                   cheque as Чек 
                                   FROM candy_shop.sales
                                   WHERE shop = @shopId
                                   ORDER BY date DESC";

                    var adapter = new NpgsqlDataAdapter(query, conn);
                    adapter.SelectCommand.Parameters.AddWithValue("@shopId", stuff);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    historyGrid.DataSource = dt;
                    historyGrid.Columns["Чек"].Visible = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки истории транзакций: {ex.Message}");
            }
        }
    }


    public class CartItem
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; }
    }

    public class QuantityForm : Form
    {
        public decimal Quantity { get; private set; }
        private TextBox quantityBox;

        public QuantityForm(string unit)
        {
            this.Text = "Введите количество";
            this.Size = new Size(300, 150);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            Label label = new Label
            {
                Text = $"Количество ({unit}):",
                Location = new Point(20, 20),
                Size = new Size(200, 20)
            };
            this.Controls.Add(label);

            quantityBox = new TextBox
            {
                Location = new Point(20, 50),
                Size = new Size(100, 20)
            };
            this.Controls.Add(quantityBox);

            Button okBtn = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(20, 80),
                Size = new Size(75, 30)
            };
            okBtn.Click += (s, e) =>
            {
                if (decimal.TryParse(quantityBox.Text, out decimal qty))
                {
                    Quantity = qty;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Введите корректное число");
                }
            };
            this.Controls.Add(okBtn);

            Button cancelBtn = new Button
            {
                Text = "Отмена",
                DialogResult = DialogResult.Cancel,
                Location = new Point(120, 80),
                Size = new Size(75, 30)
            };
            this.Controls.Add(cancelBtn);
        }
    }

    public class PaymentForm : Form
    {
        public decimal PaidAmount { get; private set; }
        private TextBox amountBox;
        private decimal totalAmount;

        public PaymentForm(decimal total)
        {
            totalAmount = total;
            this.Text = "Оплата";
            this.Size = new Size(300, 200);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            Label totalLabel = new Label
            {
                Text = $"Сумма к оплате: {total} руб.",
                Location = new Point(20, 20),
                Size = new Size(250, 20)
            };
            this.Controls.Add(totalLabel);

            Label amountLabel = new Label
            {
                Text = "Внесённая сумма:",
                Location = new Point(20, 50),
                Size = new Size(150, 20)
            };
            this.Controls.Add(amountLabel);

            amountBox = new TextBox
            {
                Location = new Point(20, 80),
                Size = new Size(100, 20)
            };
            this.Controls.Add(amountBox);

            Button okBtn = new Button
            {
                Text = "Подтвердить",
                DialogResult = DialogResult.OK,
                Location = new Point(20, 120),
                Size = new Size(100, 30)
            };
            okBtn.Click += (s, e) =>
            {
                if (decimal.TryParse(amountBox.Text, out decimal amount))
                {
                    PaidAmount = amount;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Введите корректную сумму");
                }
            };
            this.Controls.Add(okBtn);

            Button cancelBtn = new Button
            {
                Text = "Отмена",
                DialogResult = DialogResult.Cancel,
                Location = new Point(140, 120),
                Size = new Size(100, 30)
            };
            this.Controls.Add(cancelBtn);
        }
    }

    public class PurchaseForm : Form
    {
        private readonly string connectionString;
        private readonly int shopId;
        private List<PurchaseItem> purchaseItems = new List<PurchaseItem>();
        private DataGridView itemsGrid = new DataGridView();
        private decimal totalSum = 0;
        private Label totalLabel = new Label();
        private ComboBox contractorCombo = new ComboBox();
        private ComboBox categoryCombo = new ComboBox();

        public PurchaseForm(int shopId, string connectionString)
        {
            this.shopId = shopId;
            this.connectionString = connectionString;
            this.Text = "Новая закупка";
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            InitializeControls();
            LoadContractors();
            LoadCategories();
        }

        private void InitializeControls()
        {
            Label contractorLabel = new Label
            {
                Text = "Поставщик:",
                Location = new Point(20, 20),
                Size = new Size(100, 20)
            };
            this.Controls.Add(contractorLabel);

            contractorCombo = new ComboBox
            {
                Location = new Point(130, 20),
                Size = new Size(200, 30),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            this.Controls.Add(contractorCombo);

            Button newContractorBtn = new Button
            {
                Text = "Новый поставщик",
                Location = new Point(340, 20),
                Size = new Size(150, 30),
                BackColor = Color.FromArgb(230, 220, 200),
                FlatStyle = FlatStyle.Flat
            };
            newContractorBtn.Click += (s, e) => AddNewContractor();
            this.Controls.Add(newContractorBtn);

            Label categoryLabel = new Label
            {
                Text = "Категория:",
                Location = new Point(20, 60),
                Size = new Size(100, 20)
            };
            this.Controls.Add(categoryLabel);

            categoryCombo = new ComboBox
            {
                Location = new Point(130, 60),
                Size = new Size(200, 30),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            this.Controls.Add(categoryCombo);

            Button newCategoryBtn = new Button
            {
                Text = "Новая категория",
                Location = new Point(340, 60),
                Size = new Size(150, 30),
                BackColor = Color.FromArgb(230, 220, 200),
                FlatStyle = FlatStyle.Flat
            };
            newCategoryBtn.Click += (s, e) => AddNewCategory();
            this.Controls.Add(newCategoryBtn);

            Label itemLabel = new Label
            {
                Text = "Товар:",
                Location = new Point(20, 100),
                Size = new Size(100, 20)
            };
            this.Controls.Add(itemLabel);

            TextBox itemNameBox = new TextBox
            {
                Location = new Point(130, 100),
                Size = new Size(200, 30)
            };
            this.Controls.Add(itemNameBox);

            Label priceLabel = new Label
            {
                Text = "Цена:",
                Location = new Point(20, 140),
                Size = new Size(100, 20)
            };
            this.Controls.Add(priceLabel);

            TextBox priceBox = new TextBox
            {
                Location = new Point(130, 140),
                Size = new Size(100, 30)
            };
            this.Controls.Add(priceBox);

            Label quantityLabel = new Label
            {
                Text = "Количество:",
                Location = new Point(20, 180),
                Size = new Size(100, 20)
            };
            this.Controls.Add(quantityLabel);

            TextBox quantityBox = new TextBox
            {
                Location = new Point(130, 180),
                Size = new Size(100, 30)
            };
            this.Controls.Add(quantityBox);

            Button addItemBtn = new Button
            {
                Text = "Добавить товар",
                Location = new Point(340, 140),
                Size = new Size(150, 60),
                BackColor = Color.FromArgb(230, 220, 200),
                FlatStyle = FlatStyle.Flat
            };
            addItemBtn.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(itemNameBox.Text) ||
                    !decimal.TryParse(priceBox.Text, out decimal price) ||
                    !decimal.TryParse(quantityBox.Text, out decimal quantity))
                {
                    MessageBox.Show("Заполните все поля корректно");
                    return;
                }

                if (categoryCombo.SelectedItem == null)
                {
                    MessageBox.Show("Выберите категорию");
                    return;
                }

                purchaseItems.Add(new PurchaseItem
                {
                    Name = itemNameBox.Text,
                    Price = price,
                    Quantity = quantity,
                    Category = (categoryCombo.SelectedItem as DataRowView)?["category"].ToString()
                });

                totalSum += price * quantity;
                totalLabel.Text = $"Итого: {totalSum} руб.";
                UpdateItemsGrid();

                itemNameBox.Clear();
                priceBox.Clear();
                quantityBox.Clear();
            };
            this.Controls.Add(addItemBtn);

            itemsGrid = new DataGridView
            {
                Location = new Point(20, 220),
                Size = new Size(540, 150),
                BackgroundColor = Color.FromArgb(255, 253, 245),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(itemsGrid);

            totalLabel = new Label
            {
                Text = "Итого: 0 руб.",
                Location = new Point(20, 380),
                Size = new Size(200, 20),
                ForeColor = Color.FromArgb(70, 70, 70)
            };
            this.Controls.Add(totalLabel);

            Button saveBtn = new Button
            {
                Text = "Сохранить закупку",
                Location = new Point(20, 410),
                Size = new Size(150, 30),
                BackColor = Color.FromArgb(230, 220, 200),
                FlatStyle = FlatStyle.Flat
            };
            saveBtn.Click += (s, e) => SavePurchase();
            this.Controls.Add(saveBtn);

            Button cancelBtn = new Button
            {
                Text = "Отмена",
                Location = new Point(180, 410),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(230, 220, 200),
                FlatStyle = FlatStyle.Flat
            };
            cancelBtn.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            this.Controls.Add(cancelBtn);
        }

        private void LoadContractors()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT id, contractor FROM candy_shop.contractors ORDER BY contractor";
                    var adapter = new NpgsqlDataAdapter(query, conn);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    contractorCombo.DisplayMember = "contractor";
                    contractorCombo.ValueMember = "id";
                    contractorCombo.DataSource = dt;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки поставщиков: {ex.Message}");
            }
        }

        private void LoadCategories()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT id, category FROM candy_shop.categories ORDER BY category";
                    var adapter = new NpgsqlDataAdapter(query, conn);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    categoryCombo.DisplayMember = "category";
                    categoryCombo.ValueMember = "id";
                    categoryCombo.DataSource = dt;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки категорий: {ex.Message}");
            }
        }

        private void AddNewContractor()
        {
            var inputForm = new InputForm("Новый поставщик", "Введите название поставщика:");
            if (inputForm.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using (var conn = new NpgsqlConnection(connectionString))
                    {
                        conn.Open();
                        string query = "INSERT INTO candy_shop.contractors(contractor) VALUES(@name) RETURNING id";
                        using (var cmd = new NpgsqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@name", inputForm.InputText);
                            int newId = Convert.ToInt32(cmd.ExecuteScalar());

                            LoadContractors();
                            contractorCombo.SelectedValue = newId;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка добавления поставщика: {ex.Message}");
                }
            }
        }

        private void AddNewCategory()
        {
            var inputForm = new InputForm("Новая категория", "Введите название категории:");
            if (inputForm.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using (var conn = new NpgsqlConnection(connectionString))
                    {
                        conn.Open();
                        string query = "INSERT INTO candy_shop.categories(category, unit) VALUES(@category, 'шт') RETURNING id";
                        using (var cmd = new NpgsqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@category", inputForm.InputText);
                            int newId = Convert.ToInt32(cmd.ExecuteScalar());

                            LoadCategories();
                            categoryCombo.SelectedValue = newId;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка добавления категории: {ex.Message}");
                }
            }
        }

        private void UpdateItemsGrid()
        {
            var itemsTable = new DataTable();
            itemsTable.Columns.Add("Товар");
            itemsTable.Columns.Add("Цена");
            itemsTable.Columns.Add("Количество");
            itemsTable.Columns.Add("Категория");
            itemsTable.Columns.Add("Сумма");

            foreach (var item in purchaseItems)
            {
                decimal sum = item.Price * item.Quantity;
                itemsTable.Rows.Add(item.Name, item.Price, item.Quantity, item.Category, sum);
            }

            itemsGrid.DataSource = itemsTable;
        }

        private void SavePurchase()
        {
            if (contractorCombo.SelectedItem == null)
            {
                MessageBox.Show("Выберите поставщика");
                return;
            }

            if (purchaseItems.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы один товар");
                return;
            }

            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            string receipt = GeneratePurchaseReceipt();
                            int contractorId = Convert.ToInt32((contractorCombo.SelectedItem as DataRowView)["id"]);

                            string query = @"INSERT INTO candy_shop.sales(type, shop, contractors, cheque, date) 
                                          VALUES(@type, @shop, @contractor, @cheque, @date) RETURNING id";
                            int saleId;
                            using (var cmd = new NpgsqlCommand(query, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@type", "Закупка");
                                cmd.Parameters.AddWithValue("@shop", shopId);
                                cmd.Parameters.AddWithValue("@contractor", contractorId);
                                cmd.Parameters.AddWithValue("@cheque", receipt);
                                cmd.Parameters.AddWithValue("@date", DateTime.Now);
                                saleId = Convert.ToInt32(cmd.ExecuteScalar());
                            }

                            foreach (var item in purchaseItems)
                            {
                                int? productId = null;
                                string checkQuery = @"SELECT id FROM candy_shop.products 
                                                  WHERE item = @name AND shop = @shop";
                                using (var cmd = new NpgsqlCommand(checkQuery, conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@name", item.Name);
                                    cmd.Parameters.AddWithValue("@shop", shopId);
                                    var result = cmd.ExecuteScalar();
                                    if (result != null)
                                    {
                                        productId = Convert.ToInt32(result);
                                    }
                                }

                                if (productId.HasValue)
                                {
                                    string updateQuery = @"UPDATE candy_shop.products 
                                                       SET count = count + @quantity, 
                                                           price = @price,
                                                           category = (SELECT id FROM candy_shop.categories WHERE category = @category),
                                                           contractor = @contractor
                                                       WHERE id = @id";
                                    using (var cmd = new NpgsqlCommand(updateQuery, conn, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@quantity", item.Quantity);
                                        cmd.Parameters.AddWithValue("@price", item.Price);
                                        cmd.Parameters.AddWithValue("@category", item.Category);
                                        cmd.Parameters.AddWithValue("@contractor", contractorId);
                                        cmd.Parameters.AddWithValue("@id", productId.Value);
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                                else
                                {
                                    string insertQuery = @"INSERT INTO candy_shop.products(item, price, count, shop, category, contractor)
                                                       VALUES(@item, @price, @count, @shop, 
                                                           (SELECT id FROM candy_shop.categories WHERE category = @category), @contractor)";
                                    using (var cmd = new NpgsqlCommand(insertQuery, conn, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@item", item.Name);
                                        cmd.Parameters.AddWithValue("@price", item.Price);
                                        cmd.Parameters.AddWithValue("@count", item.Quantity);
                                        cmd.Parameters.AddWithValue("@shop", shopId);
                                        cmd.Parameters.AddWithValue("@category", item.Category);
                                        cmd.Parameters.AddWithValue("@contractor", contractorId);
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }

                            transaction.Commit();
                            MessageBox.Show("Закупка успешно сохранена");
                            this.DialogResult = DialogResult.OK;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            MessageBox.Show($"Ошибка сохранения закупки: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}");
            }
        }

        private string GeneratePurchaseReceipt()
        {
            string horizontalLine = new string('═', 42);

            string receipt = $"{horizontalLine}\n";
            receipt += $"{"ЧЕК ЗАКУПКИ".PadLeft(25).PadRight(42)}\n";
            receipt += $"{horizontalLine}\n";
            receipt += $"{"Магазин: " + shopId.ToString().PadRight(42)}\n";
            receipt += $"{"Дата: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm").PadRight(42)}\n";
            receipt += $"{"Номер: " + (DateTime.Now.Ticks % 1000000).ToString().PadRight(42)}\n";

            if (contractorCombo.SelectedItem != null)
            {
                DataRowView row = (DataRowView)contractorCombo.SelectedItem;
                receipt += $"{"Поставщик: " + row["contractor"].ToString().Truncate(30).PadRight(42)}\n";
            }

            receipt += $"{horizontalLine}\n";
            receipt += $"{"Товар".PadRight(19)} {"Кол-во".PadRight(10)} {"Сумма".PadLeft(10)}\n";
            receipt += $"{horizontalLine}\n";

            foreach (var item in purchaseItems)
            {
                string name = item.Name.Truncate(18).PadRight(20);
                string quantity = item.Quantity.ToString().PadRight(10);
                string sum = (item.Price * item.Quantity).ToString("C").PadLeft(12);
                receipt += $"{name}{quantity}{sum}\n";
            }

            receipt += $"{horizontalLine}\n";
            receipt += $"{"Итого:".PadRight(30)} {totalSum.ToString("C").PadLeft(12)}\n";
            receipt += $"{horizontalLine}";

            return receipt;
        }
    }

    public class PurchaseItem
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public string Category { get; set; }
    }

    public class InputForm : Form
    {
        public string InputText { get; private set; }
        private TextBox inputBox;

        public InputForm(string title, string prompt)
        {
            this.Text = title;
            this.Size = new Size(300, 150);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            Label promptLabel = new Label
            {
                Text = prompt,
                Location = new Point(20, 20),
                Size = new Size(250, 20)
            };
            this.Controls.Add(promptLabel);

            inputBox = new TextBox
            {
                Location = new Point(20, 50),
                Size = new Size(240, 30)
            };
            this.Controls.Add(inputBox);

            Button okBtn = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(20, 80),
                Size = new Size(100, 30)
            };
            okBtn.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(inputBox.Text))
                {
                    MessageBox.Show("Введите значение");
                    return;
                }
                InputText = inputBox.Text;
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            this.Controls.Add(okBtn);

            Button cancelBtn = new Button
            {
                Text = "Отмена",
                DialogResult = DialogResult.Cancel,
                Location = new Point(130, 80),
                Size = new Size(100, 30)
            };
            this.Controls.Add(cancelBtn);
        }
    }
}