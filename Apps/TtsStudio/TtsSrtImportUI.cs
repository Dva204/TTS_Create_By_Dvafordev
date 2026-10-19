using System.Drawing;
using TTS_Create_By_DvaForDev.Services;

namespace TTS_Create_By_DvaForDev;

public partial class Form1
{
    private Label? _importedSrtInfo;
    private void InitializeSubtitleBatchImport()
    {
        var toolbar=Controls.Find("pnlToolbar",true).OfType<FlowLayoutPanel>().FirstOrDefault();
        var grid=Controls.Find("dgvJobs",true).OfType<DataGridView>().FirstOrDefault();
        if(toolbar is null || grid is null)return;
        var txt=toolbar.Controls.Find("btnImportFile",true).OfType<Button>().FirstOrDefault();
        if(txt is not null){txt.Text="Nhập TXT";txt.Width=94;}
        var srt=new Button {
            Name="btnImportSrt",Text="Nhập SRT",Width=98,Height=29,
            FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(231,247,240),
            ForeColor=Color.FromArgb(24,103,76),Margin=new Padding(0,0,7,0)
        };
        srt.FlatAppearance.BorderColor=Color.FromArgb(189,220,206);
        srt.Click += (_,_) =>
        {
            using var dialog=new OpenFileDialog {
                Filter="Phụ đề SRT (*.srt)|*.srt",
                Title="Nhập SRT có mốc thời gian và khoảng nghỉ"
            };
            if(dialog.ShowDialog(this)!=DialogResult.OK)return;
            try { ImportTimedSrt(dialog.FileName); }
            catch(Exception ex){ShowError(ex);}
        };
        toolbar.Controls.Add(srt);
        if(txt is not null)toolbar.Controls.SetChildIndex(srt,toolbar.Controls.GetChildIndex(txt)+1);
        _importedSrtInfo=new Label {
            Name="lblSrtImportInfo",Text="TXT / SRT",AutoSize=true,
            Margin=new Padding(12,6,0,0),ForeColor=Color.FromArgb(42,109,88)
        };
        toolbar.Controls.Add(_importedSrtInfo);
        grid.Columns.Add(new DataGridViewTextBoxColumn {
            Name="SrtStart",HeaderText="SRT từ",ReadOnly=true,
            FillWeight=72,MinimumWidth=90
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn {
            Name="SrtEnd",HeaderText="SRT đến",ReadOnly=true,
            FillWeight=72,MinimumWidth=90
        });
    }
}
