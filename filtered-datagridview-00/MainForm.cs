using SQLite;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace filtered_datagridview_00
{
    public partial class MainForm : Form
    {
        public MainForm() => InitializeComponent();
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            dataGridView.Query();
        }
    }
    class FilteredDataGridView : DataGridView
    {
        public FilteredDataGridView()
        {
            MockDatabase.CreateTable<Record>();
            // Add a few mock items
            MockDatabase.Insert(new Record { Description = "The Quick Brown" });
            MockDatabase.Insert(new Record { Description = "Quick Brown Fox" });
            MockDatabase.Insert(new Record { Description = "Brown Fox Jumps" });
            MockDatabase.Insert(new Record { Description = "Fox Jumps Over" });
            MockDatabase.Insert(new Record { Description = "Jumps Over The" });
            MockDatabase.Insert(new Record { Description = "Over The Lazy" });
            MockDatabase.Insert(new Record { Description = "The Lazy Dog" });

            #region G L Y P H S
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts", "glyphs.ttf");
            privateFontCollection.AddFontFile(path);
            var fontFamily = privateFontCollection.Families[0];
            Glyphs = new Font(fontFamily, 9F);
            #endregion G L Y P H S            
        }
        private readonly Record _filterRow = new Record { Code = string.Empty };
        public SQLiteConnection MockDatabase { get; } = new SQLiteConnection(":memory:");
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (!DesignMode)
            {
                DataSource = Records;
                Records.ListChanged += (sender, e) =>
                {
                    var row0 = Records.FirstOrDefault();
                    if((row0 == null) || (!ReferenceEquals(row0, _filterRow)))
                    {
                        Records.Insert(0, _filterRow);
                    }
                };

                #region F O R M A T    C O L U M N S
                Records.Add(new Record()); // <- Auto generate cells
                Columns[nameof(Record.Code)].Width = 150;
                Columns[nameof(Record.Description)].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                Columns[nameof(Record.Description)].HeaderText = "Formal Title";
                Records.Clear();
                #endregion F O R M A T    C O L U M N S

                foreach(DataGridViewCell cell in Rows[0].Cells)
                {
                    var column = Columns[cell.ColumnIndex];
                    var cellLocation = GetCellDisplayRectangle(
                                cell.ColumnIndex,
                                cell.RowIndex,
                                cutOverflow: true);
                    var clearLabel = new ClearLabel()
                    {
                        Name = $"clear{column.Name}",
                        Font = Glyphs,
                        Visible = false,
                    };
                    clearLabel.Location = new Point(
                        cellLocation.X + cellLocation.Width - clearLabel.Width - 4,
                        cellLocation.Y);
                    clearLabel.Click += onAnyClickClearLabel;
                    Controls.Add(clearLabel);
                }
            }
        }

        private void onAnyClickClearLabel(object? sender, EventArgs e)
        {
            if (sender is ClearLabel clearLabel)
            {
                EndEdit();
                var pi = typeof(Record).GetProperty(clearLabel.Name.Substring(5));
                pi?.SetValue(_filterRow, string.Empty);
                InvalidateRow(0);
                Query();
                clearLabel.Visible = false;
            }
        }
        private readonly Font Glyphs;
        PrivateFontCollection privateFontCollection = new ();
        public BindingList<Record> Records { get; } = new BindingList<Record>();
        private static PropertyInfo[] _pis = typeof(Record).GetProperties();
        public void Query()
        {
            // [Careful]
            // Having "35000 records" requires some paging strategies.
            // [Quick and dirty]
            // Get enough to fill the visible rows.
            // Then add to the recordset as DGV is scrolled or paged down.
            const string DEFAULT = "SELECT * FROM records LIMIT 100";
            string sql;
            string? value;
            var builder = new List<string>();
            foreach (PropertyInfo pi in _pis)
            {
                value = pi.GetValue(_filterRow)?.ToString();
                if (!string.IsNullOrEmpty(value))
                {
                    builder.Add($"{pi.Name} LIKE '%{value}%'");
                }
            }
            if (builder.Any())
            {
                string where = $"WHERE {string.Join(" AND ", builder)}";
                sql = $"SELECT * FROM records {where}";
            }
            else sql = DEFAULT;
            foreach (var record in Records.Skip(1).ToArray())
            {
                Records.Remove(record);
            }
            foreach (var record in MockDatabase.Query<Record>(sql))
            {
                Records.Add(record);
            }
        }    
        protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e)
        {
            base.OnCellPainting(e);
            int col = e.ColumnIndex, row = e.RowIndex;
            if (isFilterCell(col, row))
            {
                using (var brush = new SolidBrush(FilterBackColor))
                {
                    e.Graphics.FillRectangle(brush, e.CellBounds);
                }
                Rectangle rect = e.CellBounds;
                rect.Inflate(new Size(-1, -1));
                using (var pen = new Pen(Color.Black, 1))
                {
                    // https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.datagridview.cellpainting?view=windowsdesktop-7.0#examples
                    e.Graphics.DrawLine(
                        pen, 
                        e.CellBounds.Right - 1, e.CellBounds.Top,
                        e.CellBounds.Right - 1, e.CellBounds.Bottom);
                }
                using (var pen = new Pen(Color.Black, 2))
                {
                    e.Graphics.DrawLine(
                        pen,
                        e.CellBounds.Left,  e.CellBounds.Bottom - 1,
                        e.CellBounds.Right - 1, e.CellBounds.Bottom - 1);
                }
                if (col == -1)
                {
                    using (var pen = new Pen(Color.Black, 1))
                    {
                        // https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.datagridview.cellpainting?view=windowsdesktop-7.0#examples
                        e.Graphics.DrawLine(
                            pen,
                            e.CellBounds.Left, e.CellBounds.Top,
                            e.CellBounds.Left, e.CellBounds.Bottom);
                    }
                    using (var brush = new SolidBrush(Color.CadetBlue))
                    {
                        var meas = e.Graphics.MeasureString("\uE800", Glyphs);
                        var padLeft = (int)((e.CellBounds.Width - meas.Width) / 2F);
                        var padTop = (int)((e.CellBounds.Height - meas.Height) / 1.8F);
                        var offset = new Point(e.CellBounds.Location.X + padLeft, e.CellBounds.Location.Y + padTop);
                        e.Graphics.DrawString("\uE800", Glyphs, brush, offset);
                    }
                }
                else
                {
                    using (var brush = new SolidBrush(Color.Gray))
                    {
                        var value = this[col, row].Value?.ToString();
                        if (string.IsNullOrWhiteSpace(value))
                        {
                            value = "Filter";
                        }
                        e.Graphics.DrawString(
                            value,
                            Font,
                            brush,
                            new Point(e.CellBounds.Location.X + 2, e.CellBounds.Location.Y + 2));
                    }
                }
                e.Handled = true;
            }
        }
        bool isFilterCell(int columnIndex, int rowIndex)
        {
            if (rowIndex == -1) return false;
            if(rowIndex >= Records.Count) return false;
            return ReferenceEquals(Records[rowIndex], _filterRow);
        }
        int _keyCount = 0;
        TextBox? _editor = null;
        ClearLabel? _editorClear = null;
        bool _isEnterOrClear = false;
        public static Color FilterBackColor = Color.White;
        protected override void OnEditingControlShowing(DataGridViewEditingControlShowingEventArgs e)
        {
            base.OnEditingControlShowing(e);
            if (e.Control is TextBox textBox)
            {
                _editor = textBox;
                _editor.TextChanged -= localOnTextChanged;
                _editor.TextChanged += localOnTextChanged;
                _editor.PreviewKeyDown -= localOnKeyDown;
                _editor.PreviewKeyDown += localOnKeyDown;

                _editor.Controls.Clear();
                void localOnTextChanged(object? sender, EventArgs unused)
                {
                    var _editorClear = Controls[$"clear{Columns[CurrentCell.ColumnIndex].Name}"];
                    _editorClear.Visible = !string.IsNullOrWhiteSpace(_editor.Text);
                    _editorClear.BringToFront();
                    var captureCount = ++_keyCount;
                    // Watchdog timer resets with every new keystroke.
                    Task
                        .Delay(250)
                        .GetAwaiter()
                        .OnCompleted(() =>
                        {
                            if (!_isEnterOrClear)
                            {
                                // if ((_keyCount == captureCount) && !string.IsNullOrWhiteSpace(textBox.Text))
                                if (_keyCount == captureCount)
                                {
                                    try
                                    {
                                        // No new keystrokes received during the settle delay.
                                        var currentCellB4 = new Point(CurrentCell.ColumnIndex, CurrentCell.RowIndex);
                                        CurrentCell.Value = _editor.Text;
                                        Query();
                                        _editor.Select(_editor.TextLength, 0);
                                    }
                                    finally
                                    {
                                        textBox.UseWaitCursor = false;
                                    }
                                }
                            }
                        });
                }
            }
            void localOnKeyDown(object? sender, PreviewKeyDownEventArgs e)
            {
                if(e.KeyData == Keys.Enter) 
                {
                    EndEdit();
                    _isEnterOrClear = true;
                    Query();
                }
                else
                {
                    _isEnterOrClear = false;
                }
            }
        }


        private bool _readOnlyB4;
        protected override void OnCellMouseDown(DataGridViewCellMouseEventArgs e)
        {
            base.OnCellMouseDown(e);
            if (isFilterCell(e.ColumnIndex, e.RowIndex))
            {
                if (e.ColumnIndex.Equals(-1))
                {
                    EndEdit();
                    foreach (var pi in _pis)
                    {
                        pi.SetValue(_filterRow, string.Empty);
                    }
                    Query();
                }
                else
                {
                    _readOnlyB4 = ReadOnly;
                    ReadOnly = false;
                    BeginEdit(selectAll: true);
                }
            }
        }
        protected override void OnCellEndEdit(DataGridViewCellEventArgs e)
        {
            base.OnCellEndEdit(e);
            ReadOnly = _readOnlyB4;
        }
    }

    [Table("records")]
    class Record
    {
        public string Code { get; set; } = Guid.NewGuid().ToString().ToUpper().Substring(0, 12);
        string _formalTitle = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
    class ClearLabel : Label
    {
        public ClearLabel()
        {
            Width = Height = 30;
        }
        Point _location = new Point();
        public Point location
        {
            get => _location;
            set
            {
                if (!Equals(_location, value))
                {
                    _location = value;
                }
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var brush = new SolidBrush(FilteredDataGridView.FilterBackColor))
            {
                e.Graphics.FillRectangle(brush, e.ClipRectangle);
            }
            using (var brush = new SolidBrush(Color.Black))
            {
                var meas = e.Graphics.MeasureString("\uE800", Font);
                var offset = new Point(
                    e.ClipRectangle.Location.X, 
                    e.ClipRectangle.Location.Y + 6);
                e.Graphics.DrawString("\uE800", Font, brush, offset);
            }
        }
    }
}