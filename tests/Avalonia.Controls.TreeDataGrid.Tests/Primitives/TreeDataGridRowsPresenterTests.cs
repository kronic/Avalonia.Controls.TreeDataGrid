﻿using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.TreeDataGridTests.Primitives
{
    public class TreeDataGridRowsPresenterTests
    {
        [Fact]
        public void Creates_Initial_Rows()
        {
            using var app = App();

            var (target, scroll, _) = CreateTarget();

            Assert.Equal(new Size(100, 1000), scroll.Extent);
            AssertRowIndexes(target, 0, 10);
            AssertRecyclable(target, 0);
        }

        [Fact]
        public void Scrolls_Down_One_Row()
        {
            using var app = App();

            var (target, scroll, _) = CreateTarget();

            scroll.Offset = new Vector(0, 10);
            Layout(target);

            AssertRowIndexes(target, 1, 10);
            AssertRecyclable(target, 0);
        }

        [Fact]
        public void Scrolls_Down_More_Than_A_Page()
        {
            using var app = App();

            var (target, scroll, _) = CreateTarget();

            scroll.Offset = new Vector(0, 200);
            Layout(target);

            AssertRowIndexes(target, 20, 10);
            AssertRecyclable(target, 0);
        }

        [Fact]
        public void Scrolls_Up_More_Than_A_Page()
        {
            using var app = App();

            var (target, scroll, _) = CreateTarget();

            scroll.Offset = new Vector(0, 200);
            Layout(target);

            scroll.Offset = new Vector(0, 0);
            Layout(target);

            AssertRowIndexes(target, 0, 10);
            AssertRecyclable(target, 0);
        }

        [Fact]
        public void Handles_Inserted_Row()
        {
            using var app = App();

            var (target, _, items) = CreateTarget();

            Assert.Equal(10, target.RealizedElements.Count());

            items.Insert(2, new Model { Id = 100, Title = "New" });

            Assert.Equal(11, target.RealizedElements.Count());

            var indexes = GetRealizedRowIndexes(target);

            // Blank space inserted in realized elements and subsequent row indexes updated.
            Assert.Equal(new[] { 0, 1, -1, 3, 4, 5, 6, 7, 8, 9, 10 }, indexes);

            var elements = target.RealizedElements.ToList();
            Layout(target);

            indexes = GetRealizedRowIndexes(target);

            // After layout an element for the new row is created.
            Assert.Equal(Enumerable.Range(0, 10), indexes);

            // But apart from the new row and the removed last row, all existing elements should be the same.
            elements[2] = target.RealizedElements.ElementAt(2);
            elements.RemoveAt(elements.Count - 1);
            Assert.Equal(elements, target.RealizedElements);
        }

        [Fact]
        public void Handles_Removed_Row()
        {
            using var app = App();

            var (target, _, items) = CreateTarget();

            Assert.Equal(10, target.RealizedElements.Count());

            var toRecycle = target.RealizedElements.ElementAt(2);
            items.RemoveAt(2);

            var indexes = GetRealizedRowIndexes(target);

            // Item removed from realized elements and subsequent row indexes updated.
            Assert.Equal(Enumerable.Range(0, 9), indexes);

            var elements = target.RealizedElements.ToList();
            Layout(target);

            indexes = GetRealizedRowIndexes(target);

            // After layout an element for the newly visible last row is created and indexes updated.
            Assert.Equal(Enumerable.Range(0, 10), indexes);

            // And the removed row should now have been recycled as the last row.
            elements.Add(toRecycle);
            Assert.Equal(elements, target.RealizedElements);
        }

        [Fact]
        public void Handles_Unrealized_Rows_Being_Removed_From_End()
        {
            using var app = App();

            var (target, scroll, items) = CreateTarget();

            Assert.Equal(new Size(100, 1000), scroll.Extent);
            AssertRowIndexes(target, 0, 10);
            AssertRecyclable(target, 0);

            items.RemoveRange(90, 10);

            AssertRowIndexes(target, 0, 10);
            AssertRecyclable(target, 0);
        }

        [Fact]
        public void Handles_Unrealized_Rows_Being_Removed_From_Start()
        {
            using var app = App();

            var (target, scroll, items) = CreateTarget();

            Assert.Equal(new Size(100, 1000), scroll.Extent);
            scroll.Offset = new Vector(0, 900);
            Layout(target);

            AssertRowIndexes(target, 90, 10);
            AssertRecyclable(target, 0);

            items.RemoveRange(0, 10);

            AssertRowIndexes(target, 80, 10);
            AssertRecyclable(target, 0);
        }

        [Fact]
        public void Realized_Children_Should_Not_Be_Removed()
        {
            using var app = App();

            var (target, _, items) = CreateTarget();

            Assert.Equal(100, target!.Items!.Count);
            Assert.Equal(10, target.RealizedElements.Count);

            items.RemoveRange(7, 93);
            Layout(target);
            var children = target.GetVisualChildren();

            for (int i = 0; i < children.Count(); i++)
            {
                Assert.Equal(children.ElementAt(i), target.RealizedElements[i]);
            }  
        }

        [Fact]
        public void Should_Remove_Logical_And_Visual_Children_On_Empty_Collection_Assignment_To_Items()
        {
            using var app = App();

            var (target, _, items) = CreateTarget();
            Layout(target);
            Assert.Equal(100, items.Count);
            items.RemoveRange(1, 99);
            Layout(target);
            Assert.Single(target.Items);
            Assert.Single(target.GetLogicalChildren());
            Assert.Single(target.GetVisualChildren());

            target.Items = new AnonymousSortableRows<Model>(ItemsSourceViewFix<Model>.Empty, null);
            Layout(target);
            Assert.Empty(target.Items);

            Assert.Empty(target.GetVisualChildren());
            Assert.Empty(target.GetLogicalChildren());

            target.Items = new AnonymousSortableRows<Model>(new ItemsSourceViewFix<Model>(Enumerable.Range(0, 5)
                .Select(x => new Model { Id = x, Title = "Item " + x, })), null);
            Layout(target);
            Assert.Equal(5, target.Items.Count);

            Assert.Equal(5, target.GetVisualChildren().Count());
            Assert.Equal(5, target.GetLogicalChildren().Count());
        }

        [Fact]
        public void Handles_Removed_And_Reinserted_Row()
        {
            using var app = App();

            var (target, _, items) = CreateTarget();

            Assert.Equal(10, target.RealizedElements.Count());

            var toRecycle = target.RealizedElements.ElementAt(0);
            var item = items[0];
            items.RemoveAt(0);

            var indexes = GetRealizedRowIndexes(target);

            // Item removed from realized elements and subsequent row indexes updated.
            Assert.DoesNotContain(toRecycle, target.RealizedElements);
            Assert.Equal(Enumerable.Range(0, 9), indexes);

            items.Insert(0, item);

            // Row indexes updated.
            indexes = GetRealizedRowIndexes(target);
            Assert.Equal(Enumerable.Range(1, 9), indexes);

            var elements = target.RealizedElements.ToList();
            Layout(target);

            indexes = GetRealizedRowIndexes(target);

            // After layout an element for the newly visible last row is created and indexes updated.
            Assert.Equal(Enumerable.Range(0, 10), indexes);

            // And the removed row should now have been recycled as the first row.
            elements.Insert(0, toRecycle);
            Assert.Equal(elements, target.RealizedElements);
        }

        [Fact]
        public void Handles_Removing_Row_Range_That_Spans_Realized_And_Unrealized_Elements()
        {
            using var app = App();

            var (target, scroll, items) = CreateTarget();

            // Scroll down one item.
            scroll.Offset = new Vector(0, 10);
            Layout(target);

            Assert.Equal(10, target.RealizedElements.Count());

            var toRecycle = target.RealizedElements.Skip(4).Take(6).ToList();
            items.RemoveRange(5, 10);

            var indexes = GetRealizedRowIndexes(target);

            // Item removed from realized elements and subsequent row indexes updated.
            Assert.Equal(Enumerable.Range(1, 4), indexes);

            var elements = target.RealizedElements.ToList();
            Layout(target);

            indexes = GetRealizedRowIndexes(target);

            // After layout an element for the newly visible last row is created and indexes updated.
            Assert.Equal(Enumerable.Range(1, 10), indexes);

            // And the removed row should now have been recycled as the last row.
            elements.AddRange(toRecycle);
            Assert.Equal(elements, target.RealizedElements);
        }

        [Fact]
        public void Handles_Removing_All_Rows_When_Scrolled()
        {
            using var app = App();

            var (target, scroll, items) = CreateTarget();

            // Scroll down one item.
            scroll.Offset = new Vector(0, 10);
            Layout(target);

            Assert.Equal(10, target.RealizedElements.Count());

            // Remove all items using RemoveRange.
            items.RemoveRange(0, items.Count);

            // All items removed
            Assert.Empty(target.RealizedElements);
        }

        [Fact]
        public void Handles_Removing_Row_Range_That_Invalidates_Current_Viewport()
        {
            using var app = App();

            var (target, scroll, items) = CreateTarget();

            // Scroll down ten items.
            scroll.Offset = new Vector(0, 100);
            Layout(target);

            Assert.Equal(10, target.RealizedElements.Count());

            // Remove all but the first five items.
            items.RemoveRange(5, 95);

            Layout(target);

            // The target bounds should be updated, which will cause the scrollviewer to scroll back up.
            Assert.Equal(new Size(100, 100), target.Bounds.Size);
        }

        [Fact]
        public void Updates_Star_Column_ActualWidth()
        {
            using var app = App();

            var columns = new ColumnList<Model>();
            columns.Add(new TextColumn<Model, int>("ID", x => x.Id, new GridLength(1, GridUnitType.Star)));
            columns.Add(new TextColumn<Model, string?>("Title", x => x.Title, new GridLength(1, GridUnitType.Star)));

            var (target, _, _) = CreateTarget(columns: columns);

            foreach (var column in columns)
            {
                Assert.Equal(50, column.ActualWidth);
            }
        }

        [Fact]
        public void Brings_Next_Item_Into_View()
        {
            using var app = App();

            var (target, scroll, _) = CreateTarget();

            target.BringIntoView(10);

            AssertRowIndexes(target, 1, 10);
        }

        [Fact]
        public void Handles_Bringing_Item_Into_View_Which_Will_Already_Be_In_View_When_Created()
        {
            using var app = App();

            var (target, scroll, _) = CreateTarget();

            // Clear the items and do a layout to simulate starting from an empty state.
            var items = target.Items;
            target.Items = null;
            Layout(target);

            // Assign the items.
            target.Items = items;

            // Now bring the first item into view before it's created. There was an issue here where
            // the presenter will wait for a viewport update which will never come because the item
            // will be placed in the existing viewport.
            target.BringIntoView(0);

            AssertRowIndexes(target, 0, 10);
        }

        private static void AssertRowIndexes(TreeDataGridRowsPresenter? target, int firstRowIndex, int rowCount)
        {
            Assert.NotNull(target);

            var rowIndexes = target.GetLogicalChildren()
                .Cast<TreeDataGridRow>()
                .Where(x => x.IsVisible)
                .Select(x => x.RowIndex)
                .OrderBy(x => x)
                .ToList();

            Assert.Equal(
                Enumerable.Range(firstRowIndex, rowCount),
                rowIndexes);

            rowIndexes = target!.RealizedElements
                .Cast<TreeDataGridRow>()
                .Where(x => x.IsVisible)
                .Select(x => x.RowIndex)
                .OrderBy(x => x)
                .ToList();

            Assert.Equal(
                Enumerable.Range(firstRowIndex, rowCount),
                rowIndexes);
        }

        private static void AssertRecyclable(TreeDataGridRowsPresenter? target, int count)
        {
            Assert.NotNull(target);

            var recyclableRows = target.GetLogicalChildren()
                .Cast<TreeDataGridRow>()
                .Where(x => !x.IsVisible)
                .ToList();
            Assert.Equal(count, recyclableRows.Count);
        }

        private static List<int> GetRealizedRowIndexes(TreeDataGridRowsPresenter? target)
        {
            Assert.NotNull(target);

            return target!.RealizedElements
                .Cast<TreeDataGridRow?>()
                .Select(x => x?.RowIndex ?? -1)
                .ToList();
        }

        private static (TreeDataGridRowsPresenter, ScrollViewer, AvaloniaList<Model>) CreateTarget(
            IColumns? columns = null)
        {
            var items = new AvaloniaList<Model>(Enumerable.Range(0, 100).Select(x =>
                new Model
                {
                    Id = x,
                    Title = "Item " + x,
                }));

            var itemsView = new ItemsSourceViewFix<Model>(items);
            var rows = new AnonymousSortableRows<Model>(itemsView, null);

            var target = new TreeDataGridRowsPresenter
            {
                ElementFactory = new TreeDataGridElementFactory(),
                Items = rows,
                Columns = columns,
            };

            var scrollViewer = new ScrollViewer
            {
                Template = TestTemplates.ScrollViewerTemplate(),
                Content = target,
            };

            var root = new TestRoot
            {
                Styles =
                {
                    new Style(x => x.OfType<TreeDataGridRow>())
                    {
                        Setters =
                        {
                            new Setter(TreeDataGridRow.HeightProperty, 10.0),
                        }
                    }
                },
                Child = scrollViewer,
            };

            root.LayoutManager.ExecuteInitialLayoutPass();
            return (target, scrollViewer, items);
        }

        private static void Layout(TreeDataGridRowsPresenter target)
        {
            var root = (ILayoutRoot)target.GetVisualRoot();
            root.LayoutManager.ExecuteLayoutPass();
        }

        private static IDisposable App()
        {
            var scope = AvaloniaLocator.EnterScope();
            AvaloniaLocator.CurrentMutable.Bind<IStyler>().ToConstant(new Styler());
            return scope;
        }

        private class Model
        {
            public int Id { get; set; }
            public string? Title { get; set; }
        }
    }
}
