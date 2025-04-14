using StockMonitor.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace StockMonitor
{
    public class EditStockForm : Form
    {
        private List<StockConfig> stocks;
        private ComboBox stockComboBox;
        private TextBox positionTextBox;
        private TextBox costTextBox;
        private Button okButton;
        private Button cancelButton;

        public EditStockForm(List<StockConfig> stocks)
        {
            this.stocks = stocks;
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            stockComboBox = new ComboBox();
            positionTextBox = new TextBox();
            costTextBox = new TextBox();
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

            positionTextBox.Location = new Point(10, 40);
            positionTextBox.Size = new Size(200, 20);
            this.Controls.Add(positionTextBox);

            costTextBox.Location = new Point(10, 70);
            costTextBox.Size = new Size(200, 20);
            this.Controls.Add(costTextBox);

            okButton.Location = new Point(10, 100);
            okButton.Size = new Size(80, 25);
            okButton.Text = "确定";
            okButton.Click += OkButton_Click;
            this.Controls.Add(okButton);

            cancelButton.Location = new Point(100, 100);
            cancelButton.Size = new Size(80, 25);
            cancelButton.Text = "取消";
            cancelButton.Click += CancelButton_Click;
            this.Controls.Add(cancelButton);

            this.Size = new Size(230, 150);
            this.Text = "编辑股票";
        }

        private void OkButton_Click(object sender, EventArgs e)
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
                        MessageBox.Show("持仓和成本必须为有效的数字！");
                    }
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