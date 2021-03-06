﻿using Microsoft.Phone.Controls;
using NotepadTheNextVersion.Models;
using System.Windows;
using System.Collections.ObjectModel;
using System.IO.IsolatedStorage;
using System;
using NotepadTheNextVersion.Enumerations;
using System.Windows.Controls;
using System.Collections.Generic;
using NotepadTheNextVersion.StaticClasses;
using Microsoft.Phone.Shell;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Threading;
using System.ComponentModel;

namespace NotepadTheNextVersion.Views
{
    public partial class Listings : PhoneApplicationPage
    {
        private Directory _curr;
        private ListingsMode _pageMode;
        private IList<object> _items;
        private bool _isUpdatingItems;
        private TextBlock _loadingNotice;
        private TextBlock _emptyNotice;
        private bool _isShowingEmptyNotice { get { return LayoutRoot.Children.Contains(_emptyNotice); } }
        private bool _isShowingLoadingNotice { get { return LayoutRoot.Children.Contains(_loadingNotice); } }

        #region Storyboard Durations (in millis)

        private static readonly int SLIDE_X_OUT_DURATION = 150;
        private static readonly int SLIDE_X_IN_DURATION = 150;
        private static readonly int FADE_IN_DURATION = 100;
        private static readonly int FADE_OUT_DURATION = 100;
        private static readonly int SLIDE_Y_IN_DURATION = 200;
        private static readonly int SWOOP_DURATION = 250;
        private static readonly ExponentialEase SLIDE_X_IN_EASE = new ExponentialEase() { EasingMode = EasingMode.EaseOut, Exponent = 3 };
        private static readonly ExponentialEase SLIDE_X_OUT_EASE = new ExponentialEase() { EasingMode = EasingMode.EaseIn, Exponent = 3 };
        private static readonly ExponentialEase SLIDE_Y_IN_EASE = new ExponentialEase() { EasingMode = EasingMode.EaseOut, Exponent = 3 };

        #endregion

        public Listings()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(Listings_Loaded);
            ContentBox.SelectionChanged += new SelectionChangedEventHandler(ContentBox_SelectionChanged);
            InitializeApplicationBar();
            SetPageMode(ListingsMode.View);
            _items = new List<object>();
            LayoutRoot.RenderTransform = new CompositeTransform();
            ContentBox.RenderTransform = new CompositeTransform();
         
            PageTitle.Visibility = Visibility.Collapsed;
        }

        void Listings_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateItems(() => GetNavigatedToStoryboard().Begin());
            UpdateView(() => GetNavigatedToStoryboard().Begin());
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (_curr == null)
                GetArgs();
            _curr = (Directory)_curr.SwapRoot();
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (_pageMode == ListingsMode.Edit)
            {
                SetPageMode(ListingsMode.View);
                e.Cancel = true;
            }
            else if (_pageMode == ListingsMode.View)
            {
                Path parent = _curr.Path.Parent;
                if (parent != null)
                {
                    Navigate(new Directory(parent),
                        GetOutStoryboardForBackwardNavigation(),
                        GetInStoryboardFromBackwardNavigation());
                    e.Cancel = true;
                }
            }
            else
                throw new Exception("Unknown page mode (should be \"edit\" or \"view\")");

            base.OnBackKeyPress(e);
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            LayoutRoot.Opacity = 0;
        }

        #region Event Handlers

        private void ContentBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isShowingEmptyNotice || _isShowingLoadingNotice)
                return;

            if (_pageMode == ListingsMode.Edit)
            {
                if (ContentBox.SelectedItems.Count == 0)
                    SetPageMode(ListingsMode.View);
                else if (ContentBox.SelectedItems.Count == 1)
                    EnableSingleSelectionAppBarItems();
                else if (ContentBox.SelectedItems.Count > 1)
                    EnableMultipleSelectionAppBarItems();
            }
            else if (_pageMode == ListingsMode.View)
            {
                if (ContentBox.SelectedIndex == -1)
                    return;

                ListingsListItem li = (ListingsListItem)ContentBox.SelectedItem;
                ContentBox.SelectedIndex = -1;
                if (li.GetType() == typeof(DocumentListItem))
                {
                    Storyboard sb = GetOutStoryboardForForwardNavigation(li);
                    sb.Completed += new EventHandler(
                        (object a, EventArgs b) => li.ActionableItem.Open(NavigationService));
                    sb.Begin();
                }
                else
                {
                    Navigate((Directory)li.ActionableItem,
                        GetOutStoryboardForForwardNavigation(li),
                        GetInStoryboardFromForwardNavigation());
                }
            }
        }

        #endregion 

        #region Private Helpers

        #region Storyboard Generators

        private Storyboard GetInStoryboardFromForwardNavigation()
        {
            // Slide in from right
            Storyboard s = new Storyboard();
            Storyboard.SetTarget(s, ContentBox);

            s.Children.Add(AnimationUtils.TranslateX(500, 0, SLIDE_X_IN_DURATION, SLIDE_X_IN_EASE));
            s.Children.Add(AnimationUtils.FadeIn(FADE_IN_DURATION));

            TextBlock append = new TextBlock()
            {
                Text = "\\" + _curr.DisplayName.ToUpper(),
                Style = (Style)App.Current.Resources["PhoneTextNormalStyle"],
                Margin = new Thickness(0, 0, 0, 0),
                RenderTransform = new CompositeTransform(),
                Opacity = 0
            };
            PathPanel.Children.Add(append);
            DoubleAnimation pathSlide = AnimationUtils.TranslateX(500, 0, SLIDE_X_IN_DURATION, SLIDE_X_IN_EASE);
            Storyboard.SetTarget(pathSlide, append);
            s.Children.Add(pathSlide);
            DoubleAnimation pathFade = AnimationUtils.FadeIn(1);
            Storyboard.SetTarget(pathFade, append);
            s.Children.Add(pathFade);

            return s;
        }

        private Storyboard GetInStoryboardFromBackwardNavigation()
        {
            // Slide in from left
            Storyboard s = new Storyboard();
            Storyboard.SetTarget(s, ContentBox);

            s.Children.Add(AnimationUtils.TranslateX(-500, 0, SLIDE_X_IN_DURATION, SLIDE_X_IN_EASE));
            s.Children.Add(AnimationUtils.FadeIn(FADE_IN_DURATION));

            return s;
        }

        private Storyboard GetOutStoryboardForBackwardNavigation()
        {
            // Slide out to right
            Storyboard s = new Storyboard();
            s.Completed += new EventHandler((object sender, EventArgs e) => PathPanel.Children.RemoveAt(PathPanel.Children.Count - 1));

            DoubleAnimation slideBoxRight = AnimationUtils.TranslateX(0, 500, SLIDE_X_OUT_DURATION, SLIDE_X_OUT_EASE);
            Storyboard.SetTarget(slideBoxRight, ContentBox);
            s.Children.Add(slideBoxRight);

            DoubleAnimation slidePathRight = AnimationUtils.TranslateX(0, 500, SLIDE_X_OUT_DURATION, SLIDE_X_OUT_EASE);
            Storyboard.SetTarget(slidePathRight, PathPanel.Children[PathPanel.Children.Count - 1]);
            s.Children.Add(slidePathRight);

            return s;
        }

        private Storyboard GetOutStoryboardForForwardNavigation(ListingsListItem selectedItem)
        {
            // Swoop selectedItem, fade out
            Storyboard s = new Storyboard();

            // Swoop
            Storyboard swoop = new Storyboard();
            Storyboard.SetTarget(swoop, selectedItem.GetAnimatedItemReference());

            swoop.Children.Add(AnimationUtils.TranslateY(0, 80, SWOOP_DURATION, new ExponentialEase() { EasingMode = EasingMode.EaseOut, Exponent = 3 }));
            swoop.Children.Add(AnimationUtils.TranslateX(0, 350, SWOOP_DURATION, new ExponentialEase() { EasingMode = EasingMode.EaseIn, Exponent = 4 }));
            s.Children.Add(swoop);

            // Fade out
            Storyboard fade = new Storyboard();
            foreach (ListingsListItem item in ContentBox.Items) // ContentBox items
                if (item != selectedItem)
                    fade.Children.Add(ApplyFadeOutAnimation(item, FADE_OUT_DURATION));
            foreach (UIElement item in selectedItem.GetNotAnimatedItemsReference()) // Elements of selectedItem
                fade.Children.Add(ApplyFadeOutAnimation(item, FADE_OUT_DURATION));
            s.Children.Add(fade);

            return s;
        }

        private Storyboard GetNavigatedToStoryboard()
        {
            // Carousel/corkscrew items in (including PageTitle)
            Storyboard s = new Storyboard();
            Storyboard.SetTarget(s, LayoutRoot);
            
            s.Children.Add(AnimationUtils.FadeIn(SLIDE_Y_IN_DURATION));
            s.Children.Add(AnimationUtils.TranslateY(350, 0, SLIDE_Y_IN_DURATION, SLIDE_Y_IN_EASE));

            if (PathPanel.Children.Count != _curr.Path.Depth + 1)
            {
                TextBlock root = new TextBlock()
                {
                    Text = _curr.DisplayName.ToUpper(),
                    Style = (Style)App.Current.Resources["PhoneTextNormalStyle"],
                    Margin = new Thickness(0, 0, 0, 0),
                    RenderTransform = new CompositeTransform()
                };
                PathPanel.Children.Add(root);
            }

            return s;
        }

        private DoubleAnimation ApplyFadeOutAnimation(UIElement item, int millis)
        {
            DoubleAnimation d = AnimationUtils.FadeOut(millis);
            Storyboard.SetTarget(d, item);
            return d;
        }

        #endregion

        private void Navigate(Directory newDirectory, Storyboard outAnimation, Storyboard inAnimation)
        {
            outAnimation.Begin();

            _curr = newDirectory;
            Action BeginInAnimation = () =>
            {
                inAnimation.Begin();
            };
            Action WorkToDo = () =>
            {
                UpdateItems(BeginInAnimation);
            };
            outAnimation.Completed += new EventHandler((object sender, EventArgs e) =>
            {
                UpdateView(BeginInAnimation);
            });
            Action<object, RunWorkerCompletedEventArgs> RunWorkerCompletedEvent = (object sender, RunWorkerCompletedEventArgs e) =>
            {
                if (outAnimation.GetCurrentState() != ClockState.Active)
                {
                    UpdateView(BeginInAnimation);
                    outAnimation.Stop();
                }
            };

            CreateNewBackgroundWorker(WorkToDo, RunWorkerCompletedEvent)
                .RunWorkerAsync();
        }

        private BackgroundWorker CreateNewBackgroundWorker(Action workToDo, Action<object, RunWorkerCompletedEventArgs> OnCompleted)
        {
            BackgroundWorker b = new BackgroundWorker();
            b.DoWork += (object sender, DoWorkEventArgs e) => { this.Dispatcher.BeginInvoke(workToDo); };
            b.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OnCompleted);
            return b;
        }

        // Reads information from the incoming UriString to fill class fields
        private void GetArgs()
        {
            string s;
            if (NavigationContext.QueryString.TryGetValue("param", out s))
            {
                Path p = Path.CreatePathFromString(s);
                _curr = new Directory(p);
            }
            else
            {
                IList<object> args = Utils.GetArguments();
                _curr = (Directory)args[0];
            }
        }

        // Changes the currently-viewed folder and updates the view
        private void UpdateItems(Action OnCompleted)
        {
            _isUpdatingItems = true;
            _items.Clear();

            // Re-fill ContentBox
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                List<string> dirs = new List<string>();
                foreach (string dir in isf.GetDirectoryNames(_curr.Path.PathString + "/*"))
                    if (!dir.StartsWith("."))
                        dirs.Add(dir);
                dirs.Sort();

                // Add directories
                foreach (string dir in dirs)
                {
                    Directory d = new Directory(_curr.Path.NavigateIn(dir));
                    ListingsListItem li = ListingsListItem.CreateListItem(d);
                    _items.Add(li);
                }

                List<string> docs = new List<string>();
                foreach (string doc in isf.GetFileNames(_curr.Path.PathString + "/*"))
                    if (!doc.StartsWith("."))
                        docs.Add(doc);
                docs.Sort();
                
                // Add documents
                foreach (string doc in docs)
                {
                    Document d = new Document(_curr.Path.NavigateIn(doc));
                    ListingsListItem li = ListingsListItem.CreateListItem(d);
                    _items.Add(li);
                }
            }

            _isUpdatingItems = false;
            if (_isShowingLoadingNotice)
                UpdateView(OnCompleted);

            RemoveNotice(Notice.Empty);
            RemoveNotice(Notice.Loading);
        }

        private void UpdateView(Action OnCompleted)
        {
            ContentBox.Items.Clear();

            if (_isUpdatingItems)
                ShowNotice(Notice.Loading);
            else if (_items.Count == 0)
                ShowNotice(Notice.Empty);
            else
                foreach (ListingsListItem li in _items)
                    ContentBox.Items.Add(li);

            DisableOrEnableScrolling();
            if (_pageMode != ListingsMode.View)
                SetPageMode(ListingsMode.View);

            OnCompleted();
        }

        private void ShowNotice(Notice notice)
        {
            if (notice == Notice.Empty)
            {
                if (_emptyNotice == null)
                {
                    _emptyNotice = CreateNoticeBlock(notice.GetText());
                }
                LayoutRoot.Children.Add(_emptyNotice);
            }
            else if (notice == Notice.Loading)
            {
                if (_loadingNotice == null)
                {
                    _loadingNotice = CreateNoticeBlock(notice.GetText());
                }
                LayoutRoot.Children.Add(_loadingNotice);
            }
            else
                throw new Exception("Unknown enum type");
        }

        private void RemoveNotice(Notice notice)
        {
            switch (notice)
            {
                case Notice.Empty:
                    LayoutRoot.Children.Remove(_emptyNotice);
                    break;
                case Notice.Loading:
                    LayoutRoot.Children.Remove(_loadingNotice);
                    break;
                default:
                    throw new Exception("Unrecognized enum type");
            }
        }

        private TextBlock CreateNoticeBlock(string Text)
        {
            TextBlock tb = new TextBlock()
            {
                Text = Text,
                Foreground = new SolidColorBrush(Colors.Gray),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(12, 0, 12, 0),
                FontSize = 24
            };
            Grid.SetRow(tb, 1);
            return tb;
        }

        private void DisableOrEnableScrolling()
        {
            ContentBox.UpdateLayout();
            if (ContentBox.Items.Count < 6)
                ScrollViewer.SetVerticalScrollBarVisibility(ContentBox, ScrollBarVisibility.Disabled);
            else
                ScrollViewer.SetVerticalScrollBarVisibility(ContentBox, ScrollBarVisibility.Visible);
        }

        private void InitializeApplicationBar()
        {
            ApplicationBar = new ApplicationBar();
            ApplicationBar.IsVisible = true;
            ApplicationBar.IsMenuEnabled = true;
        }

        private void SetPageMode(ListingsMode type)
        {
            _pageMode = type;
            if (type == ListingsMode.View)
            {
                // Swap out the icon buttons
                InitializeViewMode();

                // Change page properties
                ContentBox.SelectionMode = SelectionMode.Single;
                ContentBox.SelectedIndex = -1;
            }
            else if (type == ListingsMode.Edit)
            {
                // Swap out the icon buttons
                InitializeEditMode();

                // Change page properties
                ContentBox.SelectionMode = SelectionMode.Multiple;
                ContentBox.SelectedIndex = -1;
            }
        }

        private IList<ApplicationBarIconButton> ViewListButtons;
        private IList<ApplicationBarMenuItem> ViewListItems;

        // Returns a list of icon buttons for the application bar's "view" setting
        private void InitializeViewMode()
        {
            // Lazy initialization
            if (ViewListButtons == null) // && ViewListItems == null
            {
                ViewListButtons = new List<ApplicationBarIconButton>();
                ViewListButtons.Add(Utils.createIconButton("new", App.AddIcon, (object Sender, EventArgs e) =>
                {
                    Utils.SetArguments(_curr);
                    NavigationService.Navigate(App.AddNewItem);
                }));
                ViewListButtons.Add(Utils.createIconButton("edit", App.EditIcon, (object sender, EventArgs e) => { SetPageMode(ListingsMode.Edit); }));

                ViewListItems = new List<ApplicationBarMenuItem>();
                ViewListItems.Add(Utils.createMenuItem("search", (object sender, EventArgs e) => { NavigationService.Navigate(App.Search); }));
                ViewListItems.Add(Utils.createMenuItem("settings", (object sender, EventArgs e) => { NavigationService.Navigate(App.Settings); }));
                ViewListItems.Add(Utils.createMenuItem("trash", (object sender, EventArgs e) => { NavigationService.Navigate(App.Trash); }));
                ViewListItems.Add(Utils.createMenuItem("import+export", (object sender, EventArgs e) => { NavigationService.Navigate(App.ExportAll); }));
                ViewListItems.Add(Utils.createMenuItem("about+tips", (object sender, EventArgs e) => { NavigationService.Navigate(App.AboutAndTips); }));
            }

            ApplicationBar.Buttons.Clear();
            foreach (ApplicationBarIconButton b in ViewListButtons)
                ApplicationBar.Buttons.Add(b);

            ApplicationBar.MenuItems.Clear();
            foreach (ApplicationBarMenuItem i in ViewListItems)
                ApplicationBar.MenuItems.Add(i);
        }

        private IList<ApplicationBarIconButton> EditListButtons;
        private IList<ApplicationBarMenuItem> EditListItems;

        // Returns a list of icon buttons for the application bar's "edit" setting
        private void InitializeEditMode()
        {
            // Lazy initialization
            if (EditListButtons == null) // && EditListItems == null
            {
                EditListButtons = new List<ApplicationBarIconButton>();
                EditListButtons.Add(Utils.createIconButton("delete", App.DeleteIcon, (object sender, EventArgs e) =>
                {
                    IList<ListingsListItem> deletedItems = new List<ListingsListItem>();
                    foreach (ListingsListItem li in ContentBox.SelectedItems)
                    {
                        li.ActionableItem.Delete();
                        deletedItems.Add(li);
                    }
                    BeginDeleteAnimations(deletedItems);
                    SetPageMode(ListingsMode.View);
                }));
                EditListButtons.Add(Utils.createIconButton("pin", App.PinIcon, (object sender, EventArgs e) =>
                {
                    IActionable a = (ContentBox.SelectedItem as ListingsListItem).ActionableItem;
                    a.TogglePin();
                }));

                EditListItems = new List<ApplicationBarMenuItem>();
                EditListItems.Add(Utils.createMenuItem("move", (object sender, EventArgs e) =>
                {
                    IList<IActionable> args = new List<IActionable>();
                    foreach (ListingsListItem li in ContentBox.SelectedItems)
                        args.Add(li.ActionableItem);

                    Utils.SetArguments(args);
                    NavigationService.Navigate(App.MoveItem);
                }));
                EditListItems.Add(Utils.createMenuItem("rename", (object sender, EventArgs e) =>
                {
                    IActionable a = (ContentBox.SelectedItem as ListingsListItem).ActionableItem;
                    a.NavToRename(NavigationService);
                }));
            }

            ApplicationBar.Buttons.Clear();
            foreach (ApplicationBarIconButton b in EditListButtons)
                ApplicationBar.Buttons.Add(b);
            
            ApplicationBar.MenuItems.Clear();
            foreach (ApplicationBarMenuItem i in EditListItems)
                ApplicationBar.MenuItems.Add(i);

            DisableAllAppBarItems();
        }

        private void BeginDeleteAnimations(IList<ListingsListItem> deletedItems)
        {
            ListingsListItem lastDeletedItem = deletedItems[deletedItems.Count - 1];
            IList<ListingsListItem> previousItems = new List<ListingsListItem>();
            int i = 0;
            while (i < ContentBox.Items.Count)
            {
                ListingsListItem item = (ListingsListItem)ContentBox.Items[i];
                i++;
                if (item != deletedItems[0])
                    previousItems.Add(item);
                if (item == lastDeletedItem)
                    break;
            }

            double height = 0;
            foreach (ListingsListItem item in deletedItems)
            {
                // 25 is the bottom margin on a ListingsListItem. Not sure why, but
                // item.Margin.Bottom returns 0.0
                height += item.DesiredSize.Height;
            }

            Storyboard s = new Storyboard();
            while (i < ContentBox.Items.Count)
            {
                DoubleAnimation d = AnimationUtils.TranslateY(height, 0, 110, new ExponentialEase() { EasingMode = EasingMode.EaseIn, Exponent = 3 });
                Storyboard.SetTarget(d, (UIElement)ContentBox.Items[i]);
                s.Children.Add(d);
                ((UIElement)ContentBox.Items[i]).RenderTransform = new CompositeTransform();
                i++;
            }

            foreach (ListingsListItem item in deletedItems)
                ContentBox.Items.Remove(item);

            s.Begin();
            s.Completed += (object sender, EventArgs e) =>
            {
                if (ContentBox.Items.Count == 0)
                    ShowNotice(Notice.Empty);
            };
        }

        private void DisableAllAppBarItems()
        {
            foreach (ApplicationBarIconButton b in ApplicationBar.Buttons)
                b.IsEnabled = false;

            foreach (ApplicationBarMenuItem i in ApplicationBar.MenuItems)
                i.IsEnabled = false;
        }

        private void EnableSingleSelectionAppBarItems()
        {
            (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = true;
            (ApplicationBar.Buttons[1] as ApplicationBarIconButton).IsEnabled = true;

            (ApplicationBar.MenuItems[0] as ApplicationBarMenuItem).IsEnabled = true;
            (ApplicationBar.MenuItems[1] as ApplicationBarMenuItem).IsEnabled = true;
        }

        private void EnableMultipleSelectionAppBarItems()
        {
            (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = true;
            (ApplicationBar.Buttons[1] as ApplicationBarIconButton).IsEnabled = false;

            (ApplicationBar.MenuItems[0] as ApplicationBarMenuItem).IsEnabled = true;
            (ApplicationBar.MenuItems[1] as ApplicationBarMenuItem).IsEnabled = false;
        }

        #endregion 
    }
}