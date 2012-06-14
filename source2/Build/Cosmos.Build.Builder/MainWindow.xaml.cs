﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Security.Permissions;
using System.Windows.Threading;
using System.IO;

namespace Cosmos.Build.Builder {
  public partial class MainWindow : Window {
    public MainWindow() {
      InitializeComponent();
      mApp = (App)Application.Current;
    }

    bool mPreventAutoClose = false;
    App mApp;
    TextBlock mSection;
    TextBlock mContent;
    StringBuilder mClipboard = new StringBuilder();
    DispatcherTimer mCloseTimer;

    public void Build() {
      // TODO: Check for Inno, VS SDK SP1, other prereqs

      string xAppPath = System.AppDomain.CurrentDomain.BaseDirectory;
      string xCosmosPath = Path.GetFullPath(xAppPath + @"..\..\..\..\..\");

      var xTask = new CosmosTask(xCosmosPath);
      xTask.Log.LogLine += new Installer.Log.LogLineHandler(Log_LogLine);
      xTask.Log.LogSection += new Installer.Log.LogSectionHandler(Log_LogSection);
      xTask.Log.LogError += new Installer.Log.LogErrorHandler(Log_LogError);
      xTask.ResetHive = mApp.Args.Contains("-RESETHIVE");

      var xThread = new System.Threading.Thread(delegate() {
        xTask.Run();
        ThreadDone();
      });
      xThread.Start();
    }

    void ThreadDone() {
      Dispatcher.Invoke(DispatcherPriority.Normal, (Action)delegate() {
        if (mApp.Args.Contains("-STAYOPEN") == false) {
          mCloseTimer = new DispatcherTimer();
          mCloseTimer.Interval = TimeSpan.FromSeconds(5);
          mCloseTimer.Tick += delegate {
            mCloseTimer.Stop();
            if (!mPreventAutoClose) {
              Close();
            }
          };
          mCloseTimer.Start();
        }
      });
    }

    void Log_LogError() {
      Dispatcher.Invoke(DispatcherPriority.Normal, (Action)delegate() {
        tblkCurrent.Text = "";

        mSection.Foreground = Brushes.Red;
        mContent.Visibility = Visibility.Visible;
        mPreventAutoClose = true;
      });
    }

    void Log_LogSection(string aLine) {
      Dispatcher.Invoke(DispatcherPriority.Normal, (Action)delegate() {
        tblkCurrent.Text = "";

        mClipboard.AppendLine();
        mClipboard.AppendLine(new string('=', aLine.Length));
        mClipboard.AppendLine(aLine);
        mClipboard.AppendLine(new string('=', aLine.Length));
        mClipboard.AppendLine();

        mSection = new TextBlock();
        mSection.Text = aLine;
        mSection.Background = Brushes.LightGray;
        mSection.Foreground = Brushes.Green;
        mSection.FontSize = 18;
        mSection.FontWeight = FontWeights.Bold;
        mSection.MouseUp += new MouseButtonEventHandler(mSection_MouseUp);
        spnlLog.Children.Add(mSection);

        mContent = new TextBlock();
        mContent.Visibility = Visibility.Collapsed;
        spnlLog.Children.Add(mContent);
        mSection.Tag = mContent;
      });
    }

    void mSection_MouseUp(object sender, MouseButtonEventArgs e) {
      var xSection = (TextBlock)sender;
      var xContent = (TextBlock)xSection.Tag;
      xContent.Visibility = xContent.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
      mPreventAutoClose = true;
    }

    void Log_LogLine(string aLine) {
      Dispatcher.Invoke(DispatcherPriority.Normal, (Action)delegate() {
        tblkCurrent.Text = aLine;
        mClipboard.AppendLine(aLine);

        mContent.Inlines.Add(aLine);
        mContent.Inlines.Add(new LineBreak());
      });      
    }

    void Window_Loaded(object sender, RoutedEventArgs e) {
      Build();
    }

    void butnCopy_Click(object sender, RoutedEventArgs e) {
      mPreventAutoClose = true;
      Clipboard.SetText(mClipboard.ToString());
    }

  }
}