using GalacticTrader.Desktop.Api;
using System.Windows.Controls;

namespace GalacticTrader.Desktop.Modules;

public partial class SettingsPanel : UserControl
{
    public SettingsPanel(DesktopSession session)
    {
        InitializeComponent();
        HeaderText.Text = $"Settings - {session.Username}";
    }
}
