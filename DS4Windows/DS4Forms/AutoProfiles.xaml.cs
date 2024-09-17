﻿/*
DS4Windows
Copyright (C) 2023  Travis Nickles

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using Ookii.Dialogs.Wpf;
using DS4WinWPF.DS4Forms.ViewModels;
using Microsoft.Win32;
using System.Collections.Specialized;
using System.Collections;
using System.Windows.Controls.Primitives;

namespace DS4WinWPF.DS4Forms
{
    /// <summary>
    /// Interaction logic for AutoProfiles.xaml
    /// </summary>
    public partial class AutoProfiles : UserControl
    {
        protected String m_Profile = DS4Windows.Global.appdatapath + "\\Auto Profiles.xml";
        public const string steamCommx86Loc = @"C:\Program Files (x86)\Steam\steamapps\common";
        public const string steamCommLoc = @"C:\Program Files\Steam\steamapps\common";
        private string steamgamesdir;
        private AutoProfilesViewModel autoProfVM;
        private AutoProfileHolder autoProfileHolder;
        private ProfileList profileList;
        private bool autoDebug;

        public AutoProfileHolder AutoProfileHolder { get => autoProfileHolder;
            set => autoProfileHolder = value; }
        public AutoProfilesViewModel AutoProfVM { get => autoProfVM; }
        public bool AutoDebug { get => autoDebug; }
        public event EventHandler AutoDebugChanged;

        public AutoProfiles()
        {
            InitializeComponent();

            // Stop anonying warning triangle icon appearing in WPF designer mode.
            if (DesignerProperties.GetIsInDesignMode(this)) return;

            if (!File.Exists(DS4Windows.Global.appdatapath + @"\Auto Profiles.xml"))
                DS4Windows.Global.CreateAutoProfiles(m_Profile);

            //LoadP();

            if (DS4Windows.Global.UseCustomSteamFolder &&
                Directory.Exists(DS4Windows.Global.CustomSteamFolder))
                steamgamesdir = DS4Windows.Global.CustomSteamFolder;
            else if (Directory.Exists(steamCommx86Loc))
                steamgamesdir = steamCommx86Loc;
            else if (Directory.Exists(steamCommLoc))
                steamgamesdir = steamCommLoc;
            else
                addProgramsBtn.ContextMenu.Items.Remove(steamMenuItem);

            autoProfileHolder = new AutoProfileHolder();

            int currentRowCount = autoProfilesGrid.RowDefinitions.Count;
            if (currentRowCount > DS4Windows.ControlService.CURRENT_DS4_CONTROLLER_LIMIT)
            {
                for (int i = currentRowCount-1; i >= DS4Windows.ControlService.CURRENT_DS4_CONTROLLER_LIMIT; i--)
                {
                    autoProfilesGrid.RowDefinitions.RemoveAt(i);
                }
            }

            Loaded += AutoProfiles_Loaded;
            ((INotifyCollectionChanged)programListLV.Items).CollectionChanged += AutoProfiles_CollectionChanged;
        }

        private void AutoProfiles_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // This autosizes the columns to fit the contents when a new log item is added
            foreach (GridViewColumn c in ((GridView)programListLV.View).Columns)
            {
                if (double.IsNaN(c.Width))
                {
                    c.Width = c.ActualWidth;
                }
                c.Width = double.NaN;
            }
        }

        private void AutoProfiles_Loaded(object sender, RoutedEventArgs e)
        {
            // not really sure why this is not scrolled to top by default,
            // but let's just do that manually, one time only.
            if (sidebarScrollViewer.ActualHeight > 0)
            {
                sidebarScrollViewer.ScrollToTop();
                // Loaded -= AutoProfiles_Loaded;
            }
        }

        public void SetupDataContext(ProfileList profileList)
        {
            autoProfVM = new AutoProfilesViewModel(autoProfileHolder, profileList);
            programListLV.DataContext = autoProfVM;
            programListLV.ItemsSource = autoProfVM.ProgramColl;
            
            revertDefaultProfileOnUnknownCk.DataContext = autoProfVM;

            autoProfVM.SearchFinished += AutoProfVM_SearchFinished;
            autoProfVM.CurrentItemChange += AutoProfVM_CurrentItemChange;

            //autoProfilesGrid.DataContext = autoProfVM;
            outerEditControlsPanel.DataContext = autoProfVM;
            editControlsPanel.DataContext = autoProfVM.SelectedItem;
            this.profileList = profileList;

            // Sort auto profile list by application file name
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(programListLV.ItemsSource);
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription("Filename", ListSortDirection.Ascending));
            view.Refresh();

            sidebarScrollViewer.ScrollToTop();
        }

        private void AutoProfVM_CurrentItemChange(AutoProfilesViewModel sender, ProgramItem item)
        {
            if (item != null)
            {
                if (item.MatchedAutoProfile != null)
                {
                    ProfileEntity tempProf = null;
                    string profileName = item.MatchedAutoProfile.ProfileNames[0];
                    if (!string.IsNullOrEmpty(profileName) && profileName != "(none)")
                    {
                        tempProf = profileList.ProfileListCol.SingleOrDefault(x => x.Name == profileName);
                        if (tempProf != null)
                        {
                            item.SelectedIndexCon1 = profileList.ProfileListCol.IndexOf(tempProf) + 1;
                        }
                    }
                    else
                    {
                        item.SelectedIndexCon1 = 0;
                    }

                    profileName = item.MatchedAutoProfile.ProfileNames[1];
                    if (!string.IsNullOrEmpty(profileName) && profileName != "(none)")
                    {
                        tempProf = profileList.ProfileListCol.SingleOrDefault(x => x.Name == profileName);
                        if (tempProf != null)
                        {
                            item.SelectedIndexCon2 = profileList.ProfileListCol.IndexOf(tempProf) + 1;
                        }
                    }
                    else
                    {
                        item.SelectedIndexCon2 = 0;
                    }

                    profileName = item.MatchedAutoProfile.ProfileNames[2];
                    if (!string.IsNullOrEmpty(profileName) && profileName != "(none)")
                    {
                        tempProf = profileList.ProfileListCol.SingleOrDefault(x => x.Name == profileName);
                        if (tempProf != null)
                        {
                            item.SelectedIndexCon3 = profileList.ProfileListCol.IndexOf(tempProf) + 1;
                        }
                    }
                    else
                    {
                        item.SelectedIndexCon3 = 0;
                    }

                    profileName = item.MatchedAutoProfile.ProfileNames[3];
                    if (!string.IsNullOrEmpty(profileName) && profileName != "(none)")
                    {
                        tempProf = profileList.ProfileListCol.SingleOrDefault(x => x.Name == profileName);
                        if (tempProf != null)
                        {
                            item.SelectedIndexCon4 = profileList.ProfileListCol.IndexOf(tempProf) + 1;
                        }
                    }
                    else
                    {
                        item.SelectedIndexCon4 = 0;
                    }

                    if (autoProfVM.UsingExpandedControllers)
                    {
                        profileName = item.MatchedAutoProfile.ProfileNames[4];
                        if (!string.IsNullOrEmpty(profileName) && profileName != "(none)")
                        {
                            tempProf = profileList.ProfileListCol.SingleOrDefault(x => x.Name == profileName);
                            if (tempProf != null)
                            {
                                item.SelectedIndexCon5 = profileList.ProfileListCol.IndexOf(tempProf) + 1;
                            }
                        }
                        else
                        {
                            item.SelectedIndexCon5 = 0;
                        }

                        profileName = item.MatchedAutoProfile.ProfileNames[5];
                        if (!string.IsNullOrEmpty(profileName) && profileName != "(none)")
                        {
                            tempProf = profileList.ProfileListCol.SingleOrDefault(x => x.Name == profileName);
                            if (tempProf != null)
                            {
                                item.SelectedIndexCon6 = profileList.ProfileListCol.IndexOf(tempProf) + 1;
                            }
                        }
                        else
                        {
                            item.SelectedIndexCon6 = 0;
                        }

                        profileName = item.MatchedAutoProfile.ProfileNames[6];
                        if (!string.IsNullOrEmpty(profileName) && profileName != "(none)")
                        {
                            tempProf = profileList.ProfileListCol.SingleOrDefault(x => x.Name == profileName);
                            if (tempProf != null)
                            {
                                item.SelectedIndexCon7 = profileList.ProfileListCol.IndexOf(tempProf) + 1;
                            }
                        }
                        else
                        {
                            item.SelectedIndexCon7 = 0;
                        }

                        profileName = item.MatchedAutoProfile.ProfileNames[7];
                        if (!string.IsNullOrEmpty(profileName) && profileName != "(none)")
                        {
                            tempProf = profileList.ProfileListCol.SingleOrDefault(x => x.Name == profileName);
                            if (tempProf != null)
                            {
                                item.SelectedIndexCon8 = profileList.ProfileListCol.IndexOf(tempProf) + 1;
                            }
                        }
                        else
                        {
                            item.SelectedIndexCon8 = 0;
                        }
                    }
                }

                editControlsPanel.DataContext = item;
                editControlsPanel.IsEnabled = true;
            }
            else
            {
                editControlsPanel.DataContext = null;
                editControlsPanel.IsEnabled = false;

                cont1AutoProf.SelectedIndex = 0;
                cont2AutoProf.SelectedIndex = 0;
                cont3AutoProf.SelectedIndex = 0;
                cont4AutoProf.SelectedIndex = 0;
                cont5AutoProf.SelectedIndex = 0;
                cont6AutoProf.SelectedIndex = 0;
                cont7AutoProf.SelectedIndex = 0;
                cont8AutoProf.SelectedIndex = 0;
            }
        }

        private void AutoProfVM_SearchFinished(object sender, EventArgs e)
        {
            IsEnabled = true;
        }

        private void SteamMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            steamMenuItem.Visibility = Visibility.Collapsed;
            programListLV.ItemsSource = null;
            autoProfVM.SearchFinished += AppsSearchFinished;
            autoProfVM.AddProgramsFromSteam(steamgamesdir);
        }

        private void BrowseProgsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();
            if (dialog.ShowDialog() == true)
            {
                //browseProgsMenuItem.Visibility = Visibility.Collapsed;
                programListLV.ItemsSource = null;
                autoProfVM.SearchFinished += AppsSearchFinished;
                autoProfVM.AddProgramsFromDir(dialog.SelectedPath);
            }
            else
            {
                this.IsEnabled = true;
            }
        }

        private void StartMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            startMenuItem.Visibility = Visibility.Collapsed;
            programListLV.ItemsSource = null;
            autoProfVM.SearchFinished += AppsSearchFinished;
            autoProfVM.AddProgramsFromStartMenu();
        }

        private void AppsSearchFinished(object sender, EventArgs e)
        {
            autoProfVM.SearchFinished -= AppsSearchFinished;
            programListLV.ItemsSource = autoProfVM.ProgramColl;
        }

        private void AddProgramsBtn_Click(object sender, RoutedEventArgs e)
        {
            addProgramsBtn.ContextMenu.Placement = PlacementMode.Left;
            addProgramsBtn.ContextMenu.PlacementRectangle = new Rect(0, (sender as Button).ActualHeight, 0,0);
            addProgramsBtn.ContextMenu.PlacementTarget = sender as UIElement;
            addProgramsBtn.ContextMenu.IsOpen = true;
            e.Handled = true;
        }

        private void HideUncheckedBtn_Click(object sender, RoutedEventArgs e)
        {
            programListLV.ItemsSource = null;
            autoProfVM.RemoveUnchecked();
            steamMenuItem.Visibility = Visibility.Visible;
            startMenuItem.Visibility = Visibility.Visible;
            browseProgsMenuItem.Visibility = Visibility.Visible;
            programListLV.ItemsSource = autoProfVM.ProgramColl;
        }

        private void ShowAutoDebugCk_Click(object sender, RoutedEventArgs e)
        {
            bool state = showAutoDebugCk.IsChecked == true;
            autoDebug = state;
            AutoDebugChanged?.Invoke(this, EventArgs.Empty);
        }

        private void RemoveAutoBtn_Click(object sender, RoutedEventArgs e)
        {
            if (autoProfVM.SelectedItem != null)
            {
                editControlsPanel.DataContext = null;
                autoProfVM.AddExeToHIDHideWhenSaving(autoProfVM.SelectedItem, false);
                autoProfVM.RemoveAutoProfileEntry(autoProfVM.SelectedItem);
                autoProfVM.AutoProfileHolder.Save(DS4Windows.Global.appdatapath + @"\Auto Profiles.xml");
                autoProfVM.SelectedItem = null;
            }
        }

        private void SaveAutoBtn_Click(object sender, RoutedEventArgs e)
        {
            if (autoProfVM.SelectedItem != null)
            {
                if (autoProfVM.SelectedItem.MatchedAutoProfile == null)
                {
                    autoProfVM.CreateAutoProfileEntry(autoProfVM.SelectedItem);
                }
                else
                {
                    autoProfVM.PersistAutoProfileEntry(autoProfVM.SelectedItem);
                }

                autoProfVM.AddExeToHIDHideWhenSaving(autoProfVM.SelectedItem, autoProfVM.SelectedItem.Turnoff);
                autoProfVM.AutoProfileHolder.Save(DS4Windows.Global.appdatapath + @"\Auto Profiles.xml");
            }
        }

        private void BrowseAddProgMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = false;
            dialog.AddExtension = true;
            dialog.DefaultExt = ".exe";
            dialog.Filter = "Program (*.exe)|*.exe";
            dialog.Title = "Select Program";

            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (dialog.ShowDialog() == true)
            {
                programListLV.ItemsSource = null;
                autoProfVM.SearchFinished += AppsSearchFinished;
                autoProfVM.AddProgramExeLocation(dialog.FileName);
            }
            else
            {
                this.IsEnabled = true;
            }
        }

        private void MoveUpDownAutoBtn_Click(object sender, RoutedEventArgs e)
        {
            if (autoProfVM.SelectedItem != null && sender != null)
            {
                if(autoProfVM.MoveItemUpDown(autoProfVM.SelectedItem, ((sender as MenuItem).Name == "MoveUp") ? -1 : 1))
                    autoProfVM.AutoProfileHolder.Save(DS4Windows.Global.appdatapath + @"\Auto Profiles.xml");
            }
        }

        private void GridViewColumnHeader_SizeChanged(object sender, SizeChangedEventArgs sizeChangedEventArgs)
        {
            // gep todo don't keep duplicate this code, put in in a shared function or class or attached property or something
            var gvch = ((GridViewColumnHeader)sender);
            var minWidthString = gvch.Tag as string;
            var minWidth = 30;
            int.TryParse(minWidthString, out minWidth);

            if (sizeChangedEventArgs.NewSize.Width <= minWidth)
            {
                sizeChangedEventArgs.Handled = true;
                gvch.Column.Width = minWidth;
            }
        }
    }
}
