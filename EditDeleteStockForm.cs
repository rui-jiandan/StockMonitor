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
        private StockCalculator Calculator;
        private ComboBox stockComboBox;
        private TextBox positionTextBox;
        private TextBox costTextBox;
        private Button editButton;
        private Button deleteButton;
        private Button addPositionButton;
        private Button reducePositionButton;

        public EditDeleteStockForm(List<StockConfig> stocks, StockCalculator calculator)
        {
            this.stocks = stocks;
            Calculator = calculator;
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            stockComboBox = new ComboBox();
            positionTextBox = new TextBox();
            costTextBox = new TextBox();
            editButton = new Button();
            deleteButton = new Button();
            addPositionButton = new Button();
            reducePositionButton = new Button();

            stockComboBox.Location = new Point(10, 10);
            stockComboBox.Size = new Size(170, 20);
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
            stockComboBox.TextChanged += StockComboBox_SelectedIndexChanged;
            this.Controls.Add(stockComboBox);

            // 创建和配置 Position Label
            Label positionLabel = new Label();
            positionLabel.Text = "持仓:";
            positionLabel.Location = new Point(10, 45);
            positionLabel.Size = new Size(50, 20);
            this.Controls.Add(positionLabel);

            positionTextBox.Location = new Point(62, 40);
            positionTextBox.Size = new Size(118, 20);
            this.Controls.Add(positionTextBox);

            // 创建和配置 Cost Label
            Label costLabel = new Label();
            costLabel.Text = "成本:";
            costLabel.Location = new Point(10, 75);
            costLabel.Size = new Size(50, 20);
            this.Controls.Add(costLabel);

            costTextBox.Location = new Point(62, 70);
            costTextBox.Size = new Size(118, 20);
            this.Controls.Add(costTextBox);

            reducePositionButton.Location = new Point(10, 100);
            reducePositionButton.Size = new Size(80, 25);
            reducePositionButton.Text = "减仓";
            reducePositionButton.Click += ReducePositionButton_Click;
            this.Controls.Add(reducePositionButton);

            addPositionButton.Location = new Point(10, 130);
            addPositionButton.Size = new Size(80, 25);
            addPositionButton.Text = "加仓";
            addPositionButton.Click += AddPositionButton_Click;
            this.Controls.Add(addPositionButton);

            editButton.Location = new Point(100, 100);
            editButton.Size = new Size(80, 25);
            editButton.Text = "增改";
            editButton.Click += EditButton_Click;
            this.Controls.Add(editButton);

            deleteButton.Location = new Point(100, 130);
            deleteButton.Size = new Size(80, 25);
            deleteButton.Text = "删除";
            deleteButton.Click += DeleteButton_Click;
            this.Controls.Add(deleteButton);

            this.Size = new Size(205, 200); // 调整窗体大小以适应新按钮
            this.Text = "编辑/删除股票";
            this.ShowInTaskbar = false;
        }

        private void StockComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ShowStockInfo();
        }

        private void ShowStockInfo()
        {
            var code = string.Empty;
            if (stockComboBox.SelectedIndex >= 0)
            {
                code= stockComboBox.SelectedItem.ToString();
                string selectedCode = stockComboBox.SelectedItem.ToString();
            }else if (!string.IsNullOrEmpty(stockComboBox.Text))
            {
                code = stockComboBox.Text.Trim();
            }
            if (string.IsNullOrEmpty(code))
                return;
            StockConfig stock = stocks.FirstOrDefault(s => s.Code == code);
            if (stock != null)
            {
                positionTextBox.Text = stock.Position.ToString();
                costTextBox.Text = stock.Cost.ToString();
            }
            else
            {
                positionTextBox.Text = "";
                costTextBox.Text = "";
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
                    Logger.LogDebug($"修改 【{stockToEdit.Code}】 {stockToEdit.Position} {stockToEdit.Cost}");
                }
            }
            else
            {
                string newCode = stockComboBox.Text.Trim();
                if (string.IsNullOrEmpty(newCode)) { return; }

                if (!int.TryParse(positionTextBox.Text, out var position))
                {
                    position = 0;
                }
                if(!decimal.TryParse(costTextBox.Text, out var cost))
                {
                    cost = 0;
                }
                stocks.Add(new StockConfig
                {
                    Code = newCode,
                    Position = position,
                    Cost = cost
                });
                Logger.LogDebug($"增加 【{newCode}】 {position} {cost}");
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            if (stockComboBox.SelectedIndex >= 0)
            {
                string selectedCode = stockComboBox.SelectedItem.ToString();
                stocks.RemoveAll(s=> s.Code == selectedCode);
                this.DialogResult = DialogResult.OK;
                this.Close();
                Logger.LogDebug($"删除 【{selectedCode}】");
            }
        }

        private void AddPositionButton_Click(object sender, EventArgs e)
        {
            ChangePosition();
        }

        private void ReducePositionButton_Click(object sender, EventArgs e)
        {
            ChangePosition(false);
        }

        private void ChangePosition(bool isadd=true)
        {
            if (stockComboBox.SelectedIndex >= 0)
            {
                string selectedCode = stockComboBox.SelectedItem.ToString();
                StockConfig stockToAddPosition = stocks.FirstOrDefault(s => s.Code == selectedCode);
                if (stockToAddPosition != null)
                {
                    if (int.TryParse(positionTextBox.Text, out var position) && decimal.TryParse(costTextBox.Text, out var cost))
                    {

                        if (isadd)
                        {
                            Calculator.AddPosition(stockToAddPosition, cost, position);
                        }
                        else
                        {
                            Calculator.ReducePosition(stockToAddPosition, cost, position);
                        }
                        Logger.LogDebug($"{(isadd ? "加仓" : "减仓")} 【{selectedCode}】 {position} {cost}");
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                }
            }
        }
    }
}