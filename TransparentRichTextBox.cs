// TransparentRichTextBox.cs 文件
using System;
using System.Drawing;
using System.Windows.Forms;

namespace StockMonitor
{
    public partial class MainForm
    {
        public class TransparentRichTextBox : RichTextBox
        {
            public TransparentRichTextBox()
            {
                // 启用双缓冲和其他样式
                this.SetStyle(ControlStyles.OptimizedDoubleBuffer |
                              ControlStyles.AllPaintingInWmPaint |
                              ControlStyles.SupportsTransparentBackColor, true);

                this.BackColor = Color.FromArgb(40, Color.Black);
                this.BorderStyle = BorderStyle.None;
                this.TabStop = false;
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == 0x0014)
                {
                    // 不处理背景擦除消息，避免背景重绘
                    return;
                }
                if (m.Msg == 0x0007)
                {
                    // 不处理获得焦点的消息，避免显示输入光标
                    return;
                }
                base.WndProc(ref m);
            }
        }
    }

}