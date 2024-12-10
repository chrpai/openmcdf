﻿using System.Windows.Input;
using System;
using StructuredStorageExplorerPOC.Types;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using OpenMcdf;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia;
using Avalonia.Platform.Storage;
using Microsoft.VisualBasic;
using System.Collections.ObjectModel;
using System.Xml.Linq;
using StructuredStorageExplorerPOC.Models;

namespace StructuredStorageExplorerPOC.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    Window _window;
    RootStorage? _rootStorage;

    [ObservableProperty]
    private ICommand newFile;

    [ObservableProperty]
    private ICommand saveFile;

    [ObservableProperty]
    private ICommand closeCurrentFile;

    [ObservableProperty]
    private string filePath;

    [ObservableProperty]
    private bool documentLoaded;

    public ObservableCollection<Node> Nodes { get; set; }

    public MainWindowViewModel()
    {
        CloseCurrentFile = new CommandHandler(() => CloseCurrentFileAction(), true);
        NewFile = new CommandHandler(() => NewFileAction(), true);
        SaveFile = new CommandHandler(() => SaveFileAction(), true);
        DocumentLoaded = false;
        FilePath = string.Empty;
        Nodes = new ObservableCollection<Node>();
    }

    private void CloseCurrentFileAction()
    {
        FilePath = string.Empty;
        DocumentLoaded = false;
        Dispose();
    }

    private void NewFileAction()
    {
        LoadData();
   }
    public void OpenFile(string filePath)
    {
        LoadData(filePath);
    }

    private void LoadData(string filePath = null)
    {
        CloseCurrentFileAction();
        if (filePath == null)
        {
            filePath = Path.GetTempFileName();
        }
        _rootStorage = RootStorage.Open(filePath, FileMode.OpenOrCreate);
        FilePath = filePath;
        DocumentLoaded = true;

        Nodes.Clear();
        Node root = new Node(_rootStorage.EntryInfo.Name);
        Nodes.Add(root);

        AddNodes(root, _rootStorage);

    }
    private void SaveFileAction(string filePath = null)
    {
    }

    private static void AddNodes(Node node, Storage storage)
    {
        foreach (EntryInfo item in storage.EnumerateEntries())
        {
            Node childNode = new Node(item.Name);
            node.SubNodes.Add(childNode);

            if (item.Type is EntryType.Storage)
            {
                Storage subStorage = storage.OpenStorage(item.Name);
                AddNodes(childNode, subStorage);
            }
            else
            {
            }
        }
    }
    public void Dispose()
    {
        _rootStorage?.Dispose();
        _rootStorage = null;
    }

}
