﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Pulsar4X.ECSLib;

namespace ModdingTools.JsonDataEditor
{
    public partial class InstallationsWindow : UserControl
    {
        BindingList<DataHolder> AllInstallations { get; set; }
        
        InstallationSD SelectedInstallation
        {
            get { return installationUC1.StaticData; }
            set 
            { 
                installationUC1.StaticData = value;
                genericDataUC1.Description = value.Description;
                abilitiesListUC1.AbilityAmount = value.BaseAbilityAmounts;
                //techRequirementsUC1.RequredTechs = value.TechRequirements;
                //mineralsCostsUC1.MineralCosts = value.ResourceCosts;
            }
        }
        public InstallationsWindow()
        {
            InitializeComponent();
            UpdateInstallationlist();            
            Data.InstallationData.ListChanged += UpdateInstallationlist;
            listBox_AllInstalations.DataSource = AllInstallations;
        }

        private void UpdateInstallationlist()
        {
            AllInstallations = new BindingList<DataHolder>(Data.InstallationData.GetDataHolders().ToList());
        }


        private void mainMenuButton_Click(object sender, EventArgs e)
        {
            Data.MainWindow.SetMode(WindowModes.LoadingWindow);
        }

        private void listBox_AllInstalations_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            DataHolder selectedItem = (DataHolder)listBox_AllInstalations.SelectedItem;
            genericDataUC1.Item(selectedItem);
            SelectedInstallation = Data.InstallationData.Get(selectedItem.Guid);
        }
    }
}
