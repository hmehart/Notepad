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
using NotepadTheNextVersion.Models;
using System.IO.IsolatedStorage;
using NotepadTheNextVersion.Enumerations;
using Microsoft.Phone.Tasks;
using System.Text;
using NotepadTheNextVersion.StaticClasses;

namespace NotepadTheNextVersion.Views
{
    public partial class ExportAll : PhoneApplicationPage
    {
        private TextBox ImportBox;

        public ExportAll()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(ExportAll_Loaded);
        }

        void ExportAll_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateExport();
            UpdateImport();
        }

        #region Private Helpers

        private void UpdateExport()
        {
            ExportPanel.Children.Add(new TextBlock()
            {
                Text = "Export puts the text of each of your documents into an email" +
                       " for you. The text is structured so that it can be imported into Notepad later" +
                       " using the Import tool.",
                Margin = new Thickness(12, 0, 0, 30),
                TextWrapping = TextWrapping.Wrap,
            });

            Button b = new Button();
            b.Content = "Export";
            b.HorizontalAlignment = HorizontalAlignment.Left;
            b.Click += new RoutedEventHandler(Export_Click);
            ExportPanel.Children.Add(b);
        }

        private void UpdateImport()
        {
            ImportScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;

            StackPanel ImportPanel = new StackPanel();
            ImportScrollViewer.Content = ImportPanel;
            
            ImportPanel.Children.Add(new TextBlock()
            {
                Text = "Import creates folders and files based on the text you provide. For " +
                       "Import to work correctly, you should provide only the text created by" +
                       " the Export tool or by Send as... (email only) in the Document Editor tool.",
                Margin = new Thickness(12, 0, 0, 30),
                TextWrapping = TextWrapping.Wrap,
            });

            ImportBox = new TextBox();
            ImportBox.AcceptsReturn = true;
            ImportBox.SizeChanged+=new SizeChangedEventHandler(ImportBox_SizeChanged);
            ImportPanel.Children.Add(ImportBox);

            Button b = new Button();
            b.Content = "Import";
            b.HorizontalAlignment = HorizontalAlignment.Left;
            b.Click += new RoutedEventHandler(Import_Click);
            ImportPanel.Children.Add(b);
        }

        private string GetDocName(string path)
        {
            string[] pathArray = path.Split(new string[] { "\\", "/" }, StringSplitOptions.RemoveEmptyEntries);
            pathArray[0] = (string)SettingsUtils.GetSetting(Setting.RootDirectoryName);
            return NotepadTheNextVersion.Models.Path.Combine(pathArray);
        }

        #endregion

        #region Event Handlers

        void ImportBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ImportScrollViewer.DesiredSize.Height > 525)
                ImportScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            else
                ImportScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        }

        void Import_Click(object sender, RoutedEventArgs e)
        {
            string[] docs = ImportBox.Text.Split(new string[] { "====", }, StringSplitOptions.RemoveEmptyEntries);
            if (docs.Length % 2 != 0)
            {
                MessageBox.Show("The input you supplied was invalid. If your input was very long, it may have been truncated." + 
                    " Try breaking your current input into several inputs.", "Invalid input", MessageBoxButton.OK);
                return;
            }

            for (int i = 0; i < docs.Length; i += 2)
            {
                string docName = GetDocName(docs[i].Trim());
                string docText = docs[i + 1].Trim();
                Document newDoc = Utils.CreateFileFromString(docName);
                newDoc.Text = docText;
                newDoc.Save();
            }

            NavigationService.GoBack();
        }

        void Export_Click(object sender, RoutedEventArgs e)
        {
            EmailComposeTask t = new EmailComposeTask();
            StringBuilder b = new StringBuilder();
            IList<Document> docs = Utils.GetAllDocuments(PathBase.Root);
            foreach (Document d in docs)
                b.Append(String.Format("==== {0} ====\n\n{1}\n\n", d.Path.PathString, d.Text));
            t.Body = b.ToString();
            t.Show();
        }

        #endregion
    }
}