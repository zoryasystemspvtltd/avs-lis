using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using log4net.Appender;
using log4net.Core;
using System.IO;

namespace LisTCPIPConsole
{
    public class LogTextBoxAppender : AppenderSkeleton
    {
        private RichTextBox _textBox;
        private Form _Form;

        public string FormName { get; set; }
        public string TextBoxName { get; set; }

        protected override void Append(LoggingEvent loggingEvent)
        {
            if (_textBox == null)
            {
                if (String.IsNullOrEmpty(FormName) ||
                    String.IsNullOrEmpty(TextBoxName))
                    return;

                _Form = Application.OpenForms[FormName];
                if (_Form == null)
                    return;

                var textBoxCollection = GetTextBox(_Form);
                _textBox = textBoxCollection.FirstOrDefault(t => t.Name.Equals(TextBoxName)) as RichTextBox;

                if (_textBox == null)
                    return;

                _Form.FormClosing += (s, e) => _textBox = null;
            }

            Action operation = () =>
            {
                if (FormWindowState.Minimized != _Form.WindowState)
                {
                    System.Drawing.Color text_color = System.Drawing.Color.White;

                    switch (loggingEvent.Level.DisplayName.ToUpper())
                    {
                        case "FATAL":
                            text_color = System.Drawing.Color.Red;
                            break;

                        case "ERROR":
                            text_color = System.Drawing.Color.Orange;
                            break;

                        case "WARN":
                            text_color = System.Drawing.Color.Yellow;
                            break;

                        case "INFO":
                            text_color = System.Drawing.Color.White;
                            break;

                        case "DEBUG":
                            text_color = System.Drawing.Color.Green;
                            break;

                        default:
                            text_color = System.Drawing.Color.White;
                            break;
                    }
                    if (_textBox != null)
                    {
                        _textBox.SelectionColor = text_color;
                        using (var sw = new StringWriter())
                        {
                            // RenderLoggingEvent writes the formatted event to the provided TextWriter.
                            RenderLoggingEvent(sw, loggingEvent);
                            _textBox.AppendText(sw.ToString());
                        }
                    }
                }
            };
            this._textBox.Invoke(operation);
        }

        public IEnumerable<Control> GetTextBox(Control control)
        {
            Type type = typeof(RichTextBox);
            var controls = control.Controls.Cast<Control>();

            return controls.SelectMany(ctrl => GetTextBox(ctrl))
                                      .Concat(controls)
                                      .Where(c => c.GetType() == type);
        }
    }
}
