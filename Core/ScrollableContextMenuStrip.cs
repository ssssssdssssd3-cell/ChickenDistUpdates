using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace ChickenDist.Core
{
    /// <summary>
    /// قائمة سياقية منسدلة تدعم التحرك السلس والتمرير الكامل ببكرة الماوس (Mouse Wheel Scroll)
    /// بدون الحاجة لاستخدام أزرار الأسهم في لوحة المفاتيح
    /// </summary>
    public class ScrollableContextMenuStrip : ContextMenuStrip, IMessageFilter
    {
        private static readonly MethodInfo _scrollInternalMethod = typeof(ToolStripDropDownMenu)
            .GetMethod("ScrollInternal", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(bool) }, null);

        private static readonly PropertyInfo _upScrollButtonProp = typeof(ToolStripDropDownMenu)
            .GetProperty("UpScrollButton", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly PropertyInfo _downScrollButtonProp = typeof(ToolStripDropDownMenu)
            .GetProperty("DownScrollButton", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly PropertyInfo _requiresScrollButtonsProp = typeof(ToolStripDropDownMenu)
            .GetProperty("RequiresScrollButtons", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly MethodInfo _updateScrollButtonStatusMethod = typeof(ToolStripDropDownMenu)
            .GetMethod("UpdateScrollButtonStatus", BindingFlags.NonPublic | BindingFlags.Instance);

        private bool _filterRegistered = false;

        public ScrollableContextMenuStrip()
        {
            this.Opened += delegate { RegisterFilter(); };
            this.Closed += delegate { UnregisterFilter(); };
        }

        private void RegisterFilter()
        {
            if (!_filterRegistered)
            {
                Application.AddMessageFilter(this);
                _filterRegistered = true;
            }
        }

        private void UnregisterFilter()
        {
            if (_filterRegistered)
            {
                Application.RemoveMessageFilter(this);
                _filterRegistered = false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                UnregisterFilter();
            }
            base.Dispose(disposing);
        }

        public bool PreFilterMessage(ref Message m)
        {
            const int WM_MOUSEWHEEL = 0x020A;
            if (m.Msg == WM_MOUSEWHEEL && this.Visible)
            {
                Point mousePos = Cursor.Position;
                if (this.Bounds.Contains(mousePos))
                {
                    int delta = (short)((m.WParam.ToInt64() >> 16) & 0xFFFF);
                    DoScroll(delta);
                    return true;
                }
            }
            return false;
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_MOUSEWHEEL = 0x020A;
            if (m.Msg == WM_MOUSEWHEEL)
            {
                int delta = (short)((m.WParam.ToInt64() >> 16) & 0xFFFF);
                DoScroll(delta);
                m.Result = IntPtr.Zero;
                return;
            }
            base.WndProc(ref m);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            DoScroll(e.Delta);
        }

        public void DoScroll(int delta)
        {
            if (delta == 0) return;

            try
            {
                bool req = false;
                if (_requiresScrollButtonsProp != null)
                {
                    object val = _requiresScrollButtonsProp.GetValue(this, null);
                    if (val is bool b) req = b;
                }
                if (!req) return;

                if (_updateScrollButtonStatusMethod != null)
                    _updateScrollButtonStatusMethod.Invoke(this, null);

                ToolStripItem upBtn = _upScrollButtonProp != null ? _upScrollButtonProp.GetValue(this, null) as ToolStripItem : null;
                ToolStripItem downBtn = _downScrollButtonProp != null ? _downScrollButtonProp.GetValue(this, null) as ToolStripItem : null;

                // التمرير بمعدل 3 أسطر لكل تكة من بكرة الماوس لتحقيق حركة سريعة وسلسة ومريحة
                int lines = Math.Max(1, Math.Abs(delta) / 120 * 3);
                bool scrollUp = delta > 0;

                for (int i = 0; i < lines; i++)
                {
                    if (scrollUp)
                    {
                        if (upBtn != null && !upBtn.Enabled) break;
                        if (_scrollInternalMethod != null)
                            _scrollInternalMethod.Invoke(this, new object[] { true });
                    }
                    else
                    {
                        if (downBtn != null && !downBtn.Enabled) break;
                        if (_scrollInternalMethod != null)
                            _scrollInternalMethod.Invoke(this, new object[] { false });
                    }
                    if (_updateScrollButtonStatusMethod != null)
                        _updateScrollButtonStatusMethod.Invoke(this, null);
                }
            }
            catch { }
        }
    }
}
