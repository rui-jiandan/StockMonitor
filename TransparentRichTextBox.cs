// TransparentRichTextBox.cs 文件
using System;
using System.Drawing;
using System.Runtime.InteropServices;
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



            private const int WM_VSCROLL = 0x0115;
            private const int SB_THUMBPOSITION = 4;

            [DllImport("user32.dll")]
            private static extern int GetScrollPos(IntPtr hWnd, int nBar);

            [DllImport("user32.dll")]
            private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

            public int GetScrollPosition()
            {
                return GetScrollPos(this.Handle, 1); // 1 表示垂直滚动条
            }

            public void SetScrollPosition(int position)
            {
                SendMessage(this.Handle, WM_VSCROLL, SB_THUMBPOSITION + 0x10000 * position, 0);
            }
        }
    }

}