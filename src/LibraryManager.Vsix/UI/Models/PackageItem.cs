using System;
using System.Collections.Generic;

namespace Microsoft.Web.LibraryManager.Vsix.UI.Models
{
    internal class PackageItem : BindableBase
    {
        private readonly HashSet<string> _selectedFiles;
        private IReadOnlyList<PackageItem> _children;
        private bool? _isChecked;
        private bool _isExpanded;
        private bool _isMain;
        private bool _isUpdatingParentCheckedStates;
        private PackageItemType _itemType;
        private string _name;

        public PackageItem(InstallDialogViewModel parent, PackageItem parentNode, HashSet<string> selectedFiles)
        {
            _selectedFiles = selectedFiles;
            Children = Array.Empty<PackageItem>();
            ParentModel = parent;
            Parent = parentNode;
        }

        public Func<bool> CanUpdateInstallStatus { get; set; }

        public IReadOnlyList<PackageItem> Children
        {
            get { return _children; }
            set { Set(ref _children, value); }
        }

        public string FullPath { get; set; }

        public bool? IsChecked
        {
            get
            {
                if (FileSelection.InstallationType == FileSelectionType.InstallAllLibraryFiles)
                {
                    _isChecked = true;
                }

                return _isChecked;
            }
            set
            {
                if (FileSelection.InstallationType == FileSelectionType.InstallAllLibraryFiles)
                {
                    value = true;
                }

                if (Set(ref _isChecked, value))
                {
                    if (value.HasValue && !_isUpdatingParentCheckedStates)
                    {
                        foreach (PackageItem child in Children)
                        {
                            child.IsChecked = value;
                        }

                        if (ItemType == PackageItemType.File)
                        {
                            if (value.GetValueOrDefault())
                            {
                                _selectedFiles.Add(FullPath);
                            }
                            else
                            {
                                _selectedFiles.Remove(FullPath);
                            }

                            if (CanUpdateInstallStatus())
                            {
                                ParentModel.InstallPackageCommand.CanExecute(null);
                            }
                        }
                    }

                    Parent?.UpdateCheckedStateForChildCheckedStateChange();
                }
            }
        }

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { Set(ref _isExpanded, value); }
        }

        public bool IsMain
        {
            get { return _isMain; }
            set
            {
                if (Set(ref _isMain, value) && value)
                {
                    IsChecked = true;
                }
            }
        }

        public PackageItemType ItemType
        {
            get { return _itemType; }
            set { Set(ref _itemType, value); }
        }

        public string Name
        {
            get { return _name; }
            set { Set(ref _name, value, StringComparer.Ordinal); }
        }

        public PackageItem Parent { get; }

        public InstallDialogViewModel ParentModel { get; }

        private void UpdateCheckedStateForChildCheckedStateChange()
        {
            //Should never happen
            if (Children.Count == 0)
            {
                return;
            }

            _isUpdatingParentCheckedStates = true;
            if (!Children[0].IsChecked.HasValue)
            {
                IsChecked = null;
                _isUpdatingParentCheckedStates = false;
                return;
            }

            bool baseState = Children[0].IsChecked.Value;

            for (int i = 1; i < Children.Count; ++i)
            {
                if (Children[i].IsChecked.GetValueOrDefault(!baseState) ^ baseState)
                {
                    IsChecked = null;
                    _isUpdatingParentCheckedStates = false;
                    return;
                }
            }

            IsChecked = baseState;
            _isUpdatingParentCheckedStates = false;
        }
    }
}
