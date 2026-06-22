using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Revit.UI;

namespace ISPG.Conversion.Helpers
{
    /// <summary>
    /// Simple option item for dialog selection
    /// </summary>
    public class OptionItem<T>
    {
        public string Label { get; set; }
        public T Value { get; set; }

        public OptionItem(string label, T value)
        {
            Label = label;
            Value = value;
        }

        public override string ToString() => Label;
    }

    /// <summary>
    /// UI utility methods for dialogs and user interaction
    /// </summary>
    public static class UIHelper
    {
        /// <summary>
        /// Shows a simple message alert
        /// </summary>
        public static void Alert(string message, string title = "ISPG Conversion")
        {
            TaskDialog.Show(title, message);
        }

        /// <summary>
        /// Ask user to select from a list of options
        /// </summary>
        public static T AskForOption<T>(string title, string prompt, List<OptionItem<T>> options)
        {
            if (options == null || options.Count == 0)
                return default(T);

            // Use a simple form with ComboBox
            using (var form = new System.Windows.Forms.Form())
            {
                form.Text = title;
                form.Size = new System.Drawing.Size(400, 150);
                form.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;

                var label = new System.Windows.Forms.Label
                {
                    Text = prompt,
                    Location = new System.Drawing.Point(10, 10),
                    Size = new System.Drawing.Size(360, 20)
                };

                var comboBox = new System.Windows.Forms.ComboBox
                {
                    Location = new System.Drawing.Point(10, 40),
                    Size = new System.Drawing.Size(360, 25),
                    DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
                };

                foreach (var option in options)
                {
                    comboBox.Items.Add(option);
                }

                if (comboBox.Items.Count > 0)
                    comboBox.SelectedIndex = 0;

                var okButton = new System.Windows.Forms.Button
                {
                    Text = "OK",
                    DialogResult = System.Windows.Forms.DialogResult.OK,
                    Location = new System.Drawing.Point(200, 75),
                    Size = new System.Drawing.Size(80, 25)
                };

                var cancelButton = new System.Windows.Forms.Button
                {
                    Text = "Cancel",
                    DialogResult = System.Windows.Forms.DialogResult.Cancel,
                    Location = new System.Drawing.Point(290, 75),
                    Size = new System.Drawing.Size(80, 25)
                };

                form.Controls.Add(label);
                form.Controls.Add(comboBox);
                form.Controls.Add(okButton);
                form.Controls.Add(cancelButton);
                form.AcceptButton = okButton;
                form.CancelButton = cancelButton;

                if (form.ShowDialog() == System.Windows.Forms.DialogResult.OK && comboBox.SelectedItem != null)
                {
                    return ((OptionItem<T>)comboBox.SelectedItem).Value;
                }

                return default(T);
            }
        }

        /// <summary>
        /// Ask user to input a string
        /// </summary>
        public static string AskForString(string title, string prompt, string defaultValue = "")
        {
            using (var form = new System.Windows.Forms.Form())
            {
                form.Text = title;
                form.Size = new System.Drawing.Size(400, 150);
                form.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;

                var label = new System.Windows.Forms.Label
                {
                    Text = prompt,
                    Location = new System.Drawing.Point(10, 10),
                    Size = new System.Drawing.Size(360, 20)
                };

                var textBox = new System.Windows.Forms.TextBox
                {
                    Text = defaultValue,
                    Location = new System.Drawing.Point(10, 40),
                    Size = new System.Drawing.Size(360, 25)
                };

                var okButton = new System.Windows.Forms.Button
                {
                    Text = "OK",
                    DialogResult = System.Windows.Forms.DialogResult.OK,
                    Location = new System.Drawing.Point(200, 75),
                    Size = new System.Drawing.Size(80, 25)
                };

                var cancelButton = new System.Windows.Forms.Button
                {
                    Text = "Cancel",
                    DialogResult = System.Windows.Forms.DialogResult.Cancel,
                    Location = new System.Drawing.Point(290, 75),
                    Size = new System.Drawing.Size(80, 25)
                };

                form.Controls.Add(label);
                form.Controls.Add(textBox);
                form.Controls.Add(okButton);
                form.Controls.Add(cancelButton);
                form.AcceptButton = okButton;
                form.CancelButton = cancelButton;

                if (form.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    return textBox.Text;
                }

                return null;
            }
        }

        public static string AskForFile(string title, string filter)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = title,
                Filter = filter,
                Multiselect = false
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                return dialog.FileName;
            }

            return null;
        }

        public static string AskForSaveFile(string title, string filter, string defaultFileName = "")
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                Title = title,
                Filter = filter,
                FileName = defaultFileName
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                return dialog.FileName;
            }

            return null;
        }
    }
}
