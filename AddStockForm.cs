using System;
using System.Drawing;
using System.Windows.Forms;

namespace StockMonitor
{
    public class AddStockForm : Form
    {
        private TextBox stockCodeTextBox;
        private TextBox positionTextBox;
        private TextBox costTextBox;
        private Button okButton;
        private Button cancelButton;

        public string StockCode
        {
            get => stockCodeTextBox.Text;
            set => stockCodeTextBox.Text = value;
        }

        public int Position
        {
            get
            {
                if(int.TryParse(positionTextBox.Text, out var result))
                {
                    return result;
                }
                else
                {
                    return 0;
                }                
            }
            set => positionTextBox.Text = value.ToString();
        }

        public decimal Cost
        {
            get
            {
                if (int.TryParse(costTextBox.Text, out var result))
                {
                    return result;
                }
                else
                {
                    return 0;
                }
            }
            set => costTextBox.Text = value.ToString();
        }

        public AddStockForm()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            stockCodeTextBox = new TextBox();
            positionTextBox = new TextBox();
            costTextBox = new TextBox();
            okButton = new Button();
            cancelButton = new Button();

            stockCodeTextBox.Location = new Point(10, 10);
            stockCodeTextBox.Size = new Size(200, 20);
            this.Controls.Add(stockCodeTextBox);

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

            this.Size = new Size(230, 200);
            this.Text = "添加股票";
            this.ShowInTaskbar = false;
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}