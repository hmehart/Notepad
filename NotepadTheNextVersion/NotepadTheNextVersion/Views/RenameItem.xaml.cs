﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using NotepadTheNextVersion.StaticClasses;
using NotepadTheNextVersion.Models;
using System.IO.IsolatedStorage;
using System.IO;

namespace NotepadTheNextVersion.Views
{
    // This page accepts a list of items to rename, and then renames each item individually.
    public partial class RenameItem : PhoneApplicationPage
    {
        public IActionable _actionable;

        public RenameItem()
        {
            InitializeComponent();

            GetArgs();
            UpdateView();

            NewNameBox.GotFocus += new RoutedEventHandler(NewNameBox_GotFocus);
            this.Loaded += new RoutedEventHandler(RenameItem_Loaded);
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            NavigationService.RemoveBackEntry();
            if (_actionable.IsTemp &&
                _actionable.Exists())
            {
                _actionable.Delete();
            }
        }

        #region Event Handlers

        void RenameItem_Loaded(object sender, RoutedEventArgs e)
        {
            NewNameBox.Focus();
        }

        void NewNameBox_GotFocus(object sender, RoutedEventArgs e)
        {
            NewNameBox.SelectAll();
        }

        private void IconButton_Okay_Click(object sender, EventArgs e)
        {
            // Ensure the filename is unique.
            string newName = NewNameBox.Text.Trim();
            IList<string> badCharsInName = new List<string>();
            if (!Utils.IsValidFileName(newName, out badCharsInName))
            {
                AlertUserBadChars(badCharsInName);
                return;
            }
            if (!IsUniqueFileName(newName))
            {
                AlertUserDuplicateName();
                return;
            }
            if (newName.StartsWith("."))
            {
                AlertUserDotFile();
                return;
            }

            // Rename the item
            _actionable = _actionable.Rename(newName);
            _actionable.IsTemp = false;

            if (_actionable.IsTemp)
                _actionable.Open(NavigationService);
            else
                NavigationService.GoBack();
        }

        private void IconButton_Cancel_Click(object sender, EventArgs e)
        {
            NavigationService.GoBack();
        }

        private void NewNameBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                IconButton_Okay_Click(null, null);
            }
        }

        #endregion

        #region Private Helpers

        private void AlertUserBadChars(IList<string> badCharsInName)
        {
            MessageBox.Show("The following characters are invalid for use in names: " + badCharsInName.ToString(),
                "Invalid characters", MessageBoxButton.OK);
        }

        private void AlertUserDuplicateName()
        {
            MessageBox.Show("An item with the same name already exists in that location.", "Invalid name", MessageBoxButton.OK);
        }

        private void AlertUserDotFile()
        {
            MessageBox.Show("Items whose names start with '.' are hidden. You can disable this feature in settings.", "Dot file", MessageBoxButton.OKCancel);
        }

        private void UpdateView()
        {
            if (ApplicationBar == null)
                CreateAppBar();
            if (_actionable.IsTemp)
            {
                ApplicationTitle.Text = "NEW";
                PageTitle.Text = "new " + _actionable.GetType().Name.ToString().ToLower();
                NewNameBox.Text = Utils.GetNumberedName("Untitled", new Models.Directory(_actionable.Path.Parent));
            }
            else
            {
                ApplicationTitle.Text = _actionable.DisplayName.ToUpper();
                NewNameBox.Text = _actionable.DisplayName;
            }
        }

        private void CreateAppBar()
        {
            ApplicationBar = new ApplicationBar();

            ApplicationBar.IsMenuEnabled = false;
            ApplicationBar.IsVisible = true;

            ApplicationBar.Buttons.Add(Utils.createIconButton("okay", App.CheckIcon, new EventHandler(IconButton_Okay_Click)));
            ApplicationBar.Buttons.Add(Utils.createIconButton("cancel", App.CancelIcon, new EventHandler(IconButton_Cancel_Click)));
        }

        private void GetArgs()
        {
            IList<object> args = Utils.GetArguments();

            _actionable = (IActionable)args[0];
        }

        private bool IsUniqueFileName(string name)
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
            {                
                if (_actionable.GetType() == typeof(Document))
                {
                    if (!name.EndsWith(".txt"))
                        name += ".txt";
                    Models.Path newPath = _actionable.Path.NavigateIn(name);

                    if (isf.FileExists(newPath.PathString))
                    {
                        MessageBox.Show("A document with the specified name already exists.");
                        return false;
                    }
                }
                else if (_actionable.GetType() == typeof(Models.Directory))
                {
                    Models.Path newPath = _actionable.Path.NavigateIn(name);

                    if (isf.DirectoryExists(newPath.PathString))
                    {
                        MessageBox.Show("A directory with the specified name already exists.");
                        return false;
                    }
                }
                return true;
            }
        }


        #endregion
    }
}