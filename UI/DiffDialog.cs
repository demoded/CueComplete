using System.Text;
using CueComplete.Core;
using Terminal.Gui;

namespace CueComplete.UI;

public class DiffDialog : Dialog
{
    private readonly HeaderDiffResult _diffResult;
    private readonly TextView _diffTextView;

    public TextView DiffTextView => _diffTextView;

    public DiffDialog(HeaderDiffResult diffResult) : base($"Diff: {Path.GetFileName(diffResult.CueFilePath)}")
    {
        _diffResult = diffResult;

        Width = Dim.Percent(88);
        Height = Dim.Percent(85);

        _diffTextView = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
            ReadOnly = true,
            WordWrap = false,
            Text = FormatDiffText(diffResult)
        };

        Add(_diffTextView);

        var closeButton = new Button("Close (Esc)", is_default: true);
        closeButton.Clicked += () => Application.RequestStop();
        AddButton(closeButton);
    }

    public override bool ProcessKey(KeyEvent keyEvent)
    {
        if (keyEvent.Key == Key.Esc || keyEvent.Key == (Key)'q' || keyEvent.Key == (Key)'Q' || keyEvent.Key == Key.Enter)
        {
            Application.RequestStop();
            return true;
        }
        return base.ProcessKey(keyEvent);
    }

    public static string FormatDiffText(HeaderDiffResult diff)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"CUE File:    {diff.CueFilePath}");
        sb.AppendLine($"Backup File: {diff.BackupFilePath}");
        sb.AppendLine();
        sb.AppendLine(string.Format("{0,-10} {1,-32} {2,-32} {3}", "Field", "Backup (.bak)", "Current (.cue)", "Status"));
        sb.AppendLine(new string('-', 86));

        foreach (var field in diff.Fields)
        {
            string status = field.Kind switch
            {
                DiffKind.Modified => "[MODIFIED]",
                DiffKind.Added => "[ADDED]",
                DiffKind.Removed => "[REMOVED]",
                _ => string.Empty
            };

            string backupVal = Truncate(field.BackupValue, 32);
            string currentVal = Truncate(field.CurrentValue, 32);

            sb.AppendLine(string.Format("{0,-10} {1,-32} {2,-32} {3}", field.FieldName, backupVal, currentVal, status));
        }

        sb.AppendLine(new string('-', 86));
        if (diff.TotalChanges == 0)
        {
            sb.AppendLine("All header fields are identical.");
        }
        else
        {
            sb.AppendLine($"Total differences: {diff.TotalChanges} field(s) changed.");
        }

        return sb.ToString();
    }

    private static string Truncate(string? val, int maxLen)
    {
        if (string.IsNullOrEmpty(val)) return string.Empty;
        return val.Length <= maxLen ? val : val.Substring(0, maxLen - 3) + "...";
    }
}
