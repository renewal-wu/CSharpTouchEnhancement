﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace VisualToolkit
{
    public static class VisualTreeManager
    {
        public static DependencyObject ParentEx(this DependencyObject item)
        {
            return VisualTreeHelper.GetParent(item);
        }

        public static IEnumerable<DependencyObject> Ancestors(this DependencyObject item)
        {
            var parent = item.ParentEx();
            while (parent != null)
            {
                yield return parent;
                parent = parent.ParentEx();
            }
        }

        public static IEnumerable<DependencyObject> AncestorsAndSelf(this DependencyObject item)
        {
            yield return item;

            foreach (var ancestor in item.Ancestors())
            {
                yield return ancestor;
            }
        }

        public static IEnumerable<T> Ancestors<T>(this DependencyObject item)
        {
            return item.Ancestors().Where(i => i is T).Cast<T>();
        }

        public static IEnumerable<T> AncestorsAndSelf<T>(this DependencyObject item)
        {
            return item.AncestorsAndSelf().Where(i => i is T).Cast<T>();
        }

        public static List<T> GetVisualChildCollection<T>(this DependencyObject parent) where T : DependencyObject
        {
            List<T> visualCollection = new List<T>();
            GetVisualChildCollection(parent, visualCollection);
            return visualCollection;
        }

        public static void GetVisualChildCollection<T>(this DependencyObject parent, List<T> visualCollection) where T : DependencyObject
        {
            int count = 0;

            try
            {
                count = VisualTreeHelper.GetChildrenCount(parent);
            }
            catch (Exception)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                try
                {
                    DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                    if (child is Panel && child is T)
                    {
                        // 當目標 Control 為 Panel 時，除了要把它加進 Collection 內之外，還要繼續檢查它的 child
                        T gottenChild = child as T;
                        if (gottenChild != null)
                        {
                            visualCollection.Add(gottenChild);
                        }
                        GetVisualChildCollection(child, visualCollection);
                    }
                    if (child is T)
                    {
                        visualCollection.Add(child as T);
                    }
                    else if (child != null)
                    {
                        GetVisualChildCollection(child, visualCollection);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public static List<Control> AllChildren(this DependencyObject parent)
        {
            var list = new List<Control>();

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is Control)
                {
                    list.Add(child as Control);
                }
                list.AddRange(AllChildren(child));
            }

            return list;
        }

        public static T GetChild<T>(this DependencyObject parentContainer, string controlName)
        {
            var childControls = AllChildren(parentContainer);
            var control = childControls.OfType<Control>().Where(x => x.Name == controlName).Cast<T>().First();

            return control;
        }

        public static T GetParent<T>(this FrameworkElement element, string message = null) where T : DependencyObject
        {
            var parent = element.Parent as T;

            if (parent == null)
            {
                if (message == null)
                {
                    message = "Parent element should not be null! Check the default Generic.xaml.";
                }

                throw new NullReferenceException(message);
            }

            return parent;
        }

        public static T GetChild<T>(this Border element, string message = null) where T : DependencyObject
        {
            var child = element.Child as T;

            if (child == null)
            {
                if (message == null)
                {
                    message = $"{nameof(Border)}'s child should not be null! Check the default Generic.xaml.";
                }

                throw new NullReferenceException(message);
            }

            return child;
        }

        public static bool IsChildOf(this FrameworkElement element, FrameworkElement targetParent)
        {
            if (element == null || targetParent == null)
            {
                return false;
            }

            while (element != null)
            {
                var parent = element.ParentEx() as FrameworkElement;
                if (parent == targetParent)
                {
                    return true;
                }

                element = parent;
            }

            return false;
        }
    }
}