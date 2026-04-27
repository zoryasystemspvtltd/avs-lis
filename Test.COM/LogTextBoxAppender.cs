using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Repository.Hierarchy;

namespace Test.COM
{
    public class LogTextBoxAppender : AppenderSkeleton
    {
        private TextBox _textBox;
        public string FormName { get; set; }
        public string TextBoxName { get; set; }

        protected override void Append(LoggingEvent loggingEvent)
        {
            if (_textBox == null)
            {
                if (string.IsNullOrEmpty(FormName) || string.IsNullOrEmpty(TextBoxName))
                    return;

                Form form = Application.OpenForms[FormName];
                if (form == null)
                    return;
                var textBoxCollection = GetTextBox(form);
                _textBox = textBoxCollection.FirstOrDefault(t => t.Name.Equals(TextBoxName)) as TextBox;

                if (_textBox == null)
                    return;

                form.FormClosing += (s, e) => _textBox = null;
            }

            if (_textBox.InvokeRequired)
            {
                _textBox.Invoke(new Action(() =>
                    _textBox.AppendText(loggingEvent.RenderedMessage + Environment.NewLine)
                ));
            }
            else
            {
                _textBox.AppendText(loggingEvent.RenderedMessage + Environment.NewLine);
            }
        }

        public IEnumerable<Control> GetTextBox(Control control)
        {
            Type type = typeof(TextBox);
            var controls = control.Controls.Cast<Control>();

            return controls.SelectMany(ctrl => GetTextBox(ctrl))
                                      .Concat(controls)
                                      .Where(c => c.GetType() == type);
        }
    }
}
