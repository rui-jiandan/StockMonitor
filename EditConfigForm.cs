using StockMonitor.Model;
using System;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace StockMonitor
{
    public partial class EditConfigForm : Form
    {
        private ConfigV1 config;

        public EditConfigForm(ConfigV1 config)
        {
            InitializeComponent();
            this.config = config;

            // 初始化控件值
            txtShowFormat.Text = config.ShowFormat;
            txtShowFormat.Multiline = true;
            txtShowFormat.Height = 40;
            txtRefreshTime.Text = config.RefreshTime.ToString();
            txtShowTodaySumFormat.Text = config.ShowTodaySumFormat;
            txtShowTodaySumFormat.Multiline = true;
            txtOrderBy.Text = config.OrderBy;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            // 保存用户输入到配置对象
            config.ShowFormat = txtShowFormat.Text;
            config.RefreshTime = int.TryParse(txtRefreshTime.Text, out int refreshTime) ? refreshTime : config.RefreshTime;
            config.ShowTodaySumFormat = txtShowTodaySumFormat.Text;
            config.OrderBy = txtOrderBy.Text;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
