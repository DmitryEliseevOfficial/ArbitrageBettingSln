using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace EditMaps
{
   
    public class ListBoxExtenders : DependencyObject
    {
        public static readonly DependencyProperty AutoScrollToEndProperty = DependencyProperty.RegisterAttached("AutoScrollToEnd", typeof(bool), typeof(ListBoxExtenders), new UIPropertyMetadata(default(bool), OnAutoScrollToEndChanged));


        public static bool GetAutoScrollToEnd(DependencyObject obj)
        {
            return (bool)obj.GetValue(AutoScrollToEndProperty);
        }


        public static void SetAutoScrollToEnd(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoScrollToEndProperty, value);
        }

     
        public static void OnAutoScrollToEndChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
        {
            var listBox = s as ListBox;
            var listBoxItems = listBox.Items;
            var data = listBoxItems.SourceCollection as INotifyCollectionChanged;

            var scrollToEndHandler = new System.Collections.Specialized.NotifyCollectionChangedEventHandler(
                (s1, e1) =>
                {
                    if (listBox.Items.Count > 0)
                    {
                        object lastItem = listBox.Items[listBox.Items.Count - 1];
                        listBoxItems.MoveCurrentTo(lastItem);
                        listBox.ScrollIntoView(lastItem);
                    }
                });

            if ((bool)e.NewValue)
                data.CollectionChanged += scrollToEndHandler;
            else
                data.CollectionChanged -= scrollToEndHandler;
        }
    }
}
