using System;
using System.ComponentModel;
using System.Windows.Forms;
using OmniSight.Base.Application;

namespace OmniSight.Base.UI
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        internal MainForm(Machine machine) : this()
        {
            Machine = machine ?? throw new ArgumentNullException(nameof(machine));
            Text = BuildInfo.AppId;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Machine Machine { get; }
    }
}
