using System;
using System.ComponentModel;
using System.Windows.Forms;
using OmniSight.Base.Application;
using OmniSight.Base.Configuration;

namespace OmniSight.Base.UI
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        internal MainForm(Machine machine, JsonConfigProvider<SystemSettings> systemConfiguration, RuntimeHealthMonitor healthMonitor) : this()
        {
            Machine = machine ?? throw new ArgumentNullException(nameof(machine));
            SystemConfiguration = systemConfiguration ?? throw new ArgumentNullException(nameof(systemConfiguration));
            HealthMonitor = healthMonitor ?? throw new ArgumentNullException(nameof(healthMonitor));
            Text = BuildInfo.AppId;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Machine Machine { get; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public JsonConfigProvider<SystemSettings> SystemConfiguration { get; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public RuntimeHealthMonitor HealthMonitor { get; }
    }
}
