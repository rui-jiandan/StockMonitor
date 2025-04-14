using StockMonitor.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace StockMonitor
{
    public partial class EditDeleteStockForm : Form
    {
        private List<StockConfig> stocks;
        private ComboBox stockComboBox;
        private TextBox positionTextBox;
        private TextBox costTextBox;
        private Button editButton;
        private Button deleteButton;
        private Button cancelButton;

        public EditDeleteStockForm(List<StockConfig> stocks)
        {
            this.stocks = stocks;
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            stockComboBox = new ComboBox();
            positionTextBox = new TextBox();
            costTextBox = new TextBox();
            editButton = new Button();
            deleteButton = new Button();
            cancelButton = new Button();

            stockComboBox.Location = new Point(10, 10);
            stockComboBox.Size = new Size(200, 20);
            foreach (StockConfig stock in stocks)
            {
                stockComboBox.Items.Add(stock.Code);
            }
            if (stockComboBox.Items.Count > 0)
            {
                stockComboBox.SelectedIndex = 0;
                ShowStockInfo();
            }
            stockComboBox.SelectedIndexChanged += StockComboBox_SelectedIndexChanged;
            this.Controls.Add(stockComboBox);

            positionTextBox.Location = new Point(10, 40);
            positionTextBox.Size = new Size(200, 20);
            this.Controls.Add(positionTextBox);

            costTextBox.Location = new Point(10, 70);
            costTextBox.Size = new Size(200, 20);
            this.Controls.Add(costTextBox);

            editButton.Location = new Point(10, 100);
            editButton.Size = new Size(80, 25);
            editButton.Text = "编辑";
            editButton.Click += EditButton_Click;
            this.Controls.Add(editButton);

            deleteButton.Location = new Point(100, 100);
            deleteButton.Size = new Size(80, 25);
            deleteButton.Text = "删除";
            deleteButton.Click += DeleteButton_Click;
            this.Controls.Add(deleteButton);

            this.Size = new Size(230, 180);
            this.Text = "编辑/删除股票";
            this.ShowInTaskbar = false;
        }

        private void StockComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ShowStockInfo();
        }

        private void ShowStockInfo()
        {
            if (stockComboBox.SelectedIndex >= 0)
            {
                string selectedCode = stockComboBox.SelectedItem.ToString();
                StockConfig stock = stocks.FirstOrDefault(s => s.Code == selectedCode);
                if (stock != null)
                {
                    positionTextBox.Text = stock.Position.ToString();
                    costTextBox.Text = stock.Cost.ToString();
                }
            }
        }

        private void EditButton_Click(object sender, EventArgs e)
        {
            if (stockComboBox.SelectedIndex >= 0)
            {
                string selectedCode = stockComboBox.SelectedItem.ToString();
                StockConfig stockToEdit = stocks.FirstOrDefault(s => s.Code == selectedCode);
                if (stockToEdit != null)
                {
                    if (int.TryParse(positionTextBox.Text, out var position) && decimal.TryParse(costTextBox.Text, out var cost))
                    {
                        stockToEdit.Position = position;
                        stockToEdit.Cost = cost;
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    else
                    {
                        stockToEdit.Position = 0;
                        stockToEdit.Cost = 0;
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                }
            }
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            if (stockComboBox.SelectedIndex >= 0)
            {
                string selectedCode = stockComboBox.SelectedItem.ToString();
                StockConfig stockToDelete = stocks.FirstOrDefault(s => s.Code == selectedCode);
                if (stockToDelete != null)
                {
                    stocks.Remove(stockToDelete);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
        }
    }
}