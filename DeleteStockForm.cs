using StockMonitor.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace StockMonitor
{
    public class DeleteStockForm : Form
    {
        private List<StockConfig> stocks;
        private ComboBox stockComboBox;
        private Button okButton;
        private Button cancelButton;

        public DeleteStockForm(List<StockConfig> stocks)
        {
            this.stocks = stocks;
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            stockComboBox = new ComboBox();
            okButton = new Button();
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
            }
            this.Controls.Add(stockComboBox);

            okButton.Location = new Point(10, 40);
            okButton.Size = new Size(80, 25);
            okButton.Text = "确定";
            okButton.Click += OkButton_Click;
            this.Controls.Add(okButton);

            cancelButton.Location = new Point(100, 40);
            cancelButton.Size = new Size(80, 25);
            cancelButton.Text = "取消";
            cancelButton.Click += CancelButton_Click;
            this.Controls.Add(cancelButton);

            this.Size = new Size(230, 100);
            this.Text = "删除股票";
        }

        private void OkButton_Click(object sender, EventArgs e)
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

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}