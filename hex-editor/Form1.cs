using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Data;
using System.IO;
using System.Text;
using System.Drawing.Printing;


namespace hex_editor
{
    public partial class MainForm : Form
    {

        Encoding ascii = Encoding.ASCII;
        private const int BytesPerRow = 16;

        public MainForm()
        {
            InitializeComponent();

            richTextBox1.SelectionChanged += RichTextBox1_SelectionChanged;
            dataGridView1.CellClick += DataGridView1_CellClick;
            richTextBox1.TextChanged += RichTextBox1_TextChanged;
            undoToolStripMenuItem.Enabled = false;

            openFileDialog1.FileName = "";
            openFileDialog1.DefaultExt = "*.*";
            openFileDialog1.Filter = "All files(*.*)|*.*";
            saveFileDialog1.Filter = "All files(*.*)|*.*";
        }
        void OpenFileToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.Cancel)
                return;

            string filename = openFileDialog1.FileName;
            byte[] fileContent;

            try
            {
                fileContent = File.ReadAllBytes(filename);
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while reading the file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            StringBuilder sb = new StringBuilder();
            foreach (byte b in fileContent)
            {
                sb.Append(Convert.ToChar(b));
            }

            richTextBox1.Text = sb.ToString();


            dataGridView1.ColumnCount = BytesPerRow;
            for (int i = 0; i < BytesPerRow; i++)
            {
                string str = "";
                dataGridView1.Rows.Add();

                dataGridView1.Columns[i].Width = 25;
                str = Convert.ToString(i, BytesPerRow);
                dataGridView1.Columns[i].HeaderText = str.ToUpper();
                dataGridView1.Columns[i].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dataGridView1.Columns[i].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            Rewrite();
        }

        /*   private DataGridView GetDataGridView1()
           {
               return dataGridView1;
           }*/

        void Rewrite()
        {
            dataGridView1.Rows.Clear();

            int rowCount = (richTextBox1.TextLength + 15) / 16;
            dataGridView1.ColumnCount = BytesPerRow;
            for (int i = 0; i < dataGridView1.ColumnCount; i++)
            {
                dataGridView1.Columns[i].Width = 25;
                string str = Convert.ToString(i, BytesPerRow);
                dataGridView1.Columns[i].HeaderText = str.ToUpper();
            }

            for (int i = 0; i < rowCount; i++)
            {
                dataGridView1.Rows.Add();
                dataGridView1.Rows[i].Height = 15;
                dataGridView1.RowHeadersWidth = 90;
                string s = Convert.ToString(i, BytesPerRow).ToUpper();
                dataGridView1.Rows[i].HeaderCell.Value = s.PadLeft(5, '0') + '0';
                dataGridView1.Rows[i].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;

                for (int j = 0; j < BytesPerRow; j++)
                {
                    int index = i * BytesPerRow + j;
                    if (index < richTextBox1.TextLength)
                    {
                        string ch = richTextBox1.Text[index].ToString();
                        byte[] encodedBytes = Encoding.Default.GetBytes(ch);
                        dataGridView1.Rows[i].Cells[j].Value = BitConverter.ToString(encodedBytes);
                    }
                    else
                    {
                        dataGridView1.Rows[i].Cells[j].Value = string.Empty;
                    }
                }
            }
        }
        private void RichTextBox1_TextChanged(object sender, EventArgs e)
        {
            undoToolStripMenuItem.Enabled = richTextBox1.CanUndo;
            /*   if (dataGridView1.Rows.Count > 5)
               {
                   Rewrite();
               }

               undoToolStripMenuItem.Enabled = richTextBox1.CanUndo;
             */
            Rewrite();
        }
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.Cancel)
                return;

            string filename = saveFileDialog1.FileName;

            try
            {
                File.WriteAllText(filename, richTextBox1.Text);

                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < dataGridView1.Columns.Count; i++)
                {
                    sb.Append(dataGridView1.Columns[i].HeaderText);
                    if (i < dataGridView1.Columns.Count - 1)
                        sb.Append("\t");
                }
                sb.AppendLine();

                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    for (int i = 0; i < row.Cells.Count; i++)
                    {
                        if (row.Cells[i].Value != null)
                        {
                            sb.Append(row.Cells[i].Value.ToString());
                        }
                        if (i < row.Cells.Count - 1)
                            sb.Append("\t");
                    }
                    sb.AppendLine();
                }

                File.AppendAllText(filename, sb.ToString());

                MessageBox.Show("Saved successfully", "Saving", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while saving the file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void RichTextBox1_SelectionChanged(object sender, EventArgs e)
        {
            if (richTextBox1.SelectionLength > 0)
            {
                int startIndex = richTextBox1.SelectionStart;
                int endIndex = startIndex + richTextBox1.SelectionLength - 1;

                int startRow = startIndex / BytesPerRow;
                int startColumn = startIndex % BytesPerRow;
                int endRow = endIndex / BytesPerRow;
                int endColumn = endIndex % BytesPerRow;

                dataGridView1.ClearSelection();

                for (int row = startRow; row <= endRow; row++)
                {
                    for (int column = (row == startRow ? startColumn : 0);
                         column <= (row == endRow ? endColumn : 15);
                         column++)
                    {
                        dataGridView1.Rows[row].Cells[column].Selected = true;
                    }
                }
            }
        }
        private void DataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                int startIndex = e.RowIndex * BytesPerRow + e.ColumnIndex;
                int endIndex = startIndex;

                if (dataGridView1.SelectionMode == DataGridViewSelectionMode.CellSelect)
                {
                    foreach (DataGridViewCell cell in dataGridView1.SelectedCells)
                    {
                        int cellIndex = cell.RowIndex * BytesPerRow + cell.ColumnIndex;
                        if (cellIndex < startIndex)
                            startIndex = cellIndex;
                        if (cellIndex > endIndex)
                            endIndex = cellIndex;
                    }
                }
                richTextBox1.SelectionStart = startIndex;
                richTextBox1.SelectionLength = endIndex - startIndex + 1;
                richTextBox1.Focus();
            }
        }
        private void info_ToolStripMenuItem1_Click_1(object sender, EventArgs e)
        {
            MessageBox.Show("Hex Editor is a program for viewing and editing binary files.\n\n\n" +
                "Developer: Soroka Victoria\n" +
                "Kyiv, 2024", "About the Program");
        }
        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();

            if (richTextBox1.SelectedText.Length > 0)
            {
                sb.AppendLine(richTextBox1.SelectedText);
            }

            if (dataGridView1.SelectedCells.Count > 0)
            {
                foreach (DataGridViewCell cell in dataGridView1.SelectedCells)
                {
                    if (cell.Value != null)
                    {
                        sb.Append(cell.Value.ToString());
                        sb.Append("\t");
                    }
                }
                sb.AppendLine();
            }

            if (sb.Length > 0)
            {
                Clipboard.SetText(sb.ToString());
                MessageBox.Show("Data is copied to the clipboard", "Copy", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("No dedicated data to copy", "Copy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private void fontToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FontDialog fontDialog = new FontDialog();
            if (fontDialog.ShowDialog() == DialogResult.OK)
            {
                richTextBox1.Font = fontDialog.Font;
            }
        }
        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(richTextBox1.SelectedText);
            richTextBox1.SelectedText = string.Empty;
        }
        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int selectionStart = richTextBox1.SelectionStart;
            richTextBox1.SelectedText = Clipboard.GetText();
            richTextBox1.SelectionStart = selectionStart;
            Rewrite();
        }
        private void colorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog();
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                richTextBox1.ForeColor = colorDialog.Color;
            }
        }
        private void backgroungColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog();
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                richTextBox1.BackColor = colorDialog.Color;
            }
        }
        public string RichTextBoxText
        {
            get { return richTextBox1.Text; }
        }
        //   public object RichTextBox1 { get; internal set; }

        /*    public void SetSelectionInRichTextBox(int start, int length)
            {
                richTextBox1.SelectionStart = start;
                richTextBox1.SelectionLength = length;
                richTextBox1.ScrollToCaret();
            }  */

        private void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
        {

            int startX = e.MarginBounds.Left;
            int startY = e.MarginBounds.Top + (int)e.Graphics.MeasureString(richTextBox1.Text, richTextBox1.Font).Height;

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                startX = e.MarginBounds.Left;
                foreach (DataGridViewCell cell in row.Cells)
                {
                    if (cell.Value != null)
                    {
                        e.Graphics.DrawString(cell.Value.ToString(), cell.InheritedStyle.Font, Brushes.Black, startX, startY);
                    }
                    startX += cell.Size.Width;
                }
                startY += row.Height;
            }

            e.HasMorePages = false;
        }
        private void printToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == DialogResult.OK)
            {
                PrintDocument printDocument = new PrintDocument();
                printDocument.PrintPage += PrintDocument_PrintPage;
                printDocument.Print();
            }
        }
        private void clearToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            richTextBox1.Clear();
            dataGridView1.Rows.Clear();
        }

        private void undoToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            richTextBox1.Undo();
            Rewrite();
        }
    }
}