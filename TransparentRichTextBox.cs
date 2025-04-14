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
                this.BackColor = Color.Transparent;

                // 防止父容器重绘影响
                this.SetStyle(ControlStyles.ResizeRedraw, false);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                // 双缓冲绘制
                using (BufferedGraphicsContext context = BufferedGraphicsManager.Current)
                {
                    using (BufferedGraphics buffer = context.Allocate(e.Graphics, this.ClientRectangle))
                    {
                        buffer.Graphics.Clear(this.BackColor);
                        base.OnPaint(new PaintEventArgs(buffer.Graphics, e.ClipRectangle));
                        buffer.Render(e.Graphics);
                    }
                }
            }

            protected override void WndProc(ref Message m)
            {
                const int WM_ERASEBKGND = 0x0014;
                if (m.Msg == WM_ERASEBKGND)
                {
                    // 不处理背景擦除消息，避免背景重绘
                    return;
                }
                base.WndProc(ref m);
            }
        }
    }

}