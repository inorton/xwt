// 
// Table.cs
//  
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;

using Xwt.Backends;
using System.Windows.Markup;
using Xwt.Drawing;

namespace Xwt
{
	[BackendType (typeof(IBoxBackend))]
	public class Table: Widget
	{
		ChildrenCollection<TablePlacement> children;
		double defaultRowSpacing = 6;
		double defaultColSpacing = 6;
		Dictionary<int,double> rowSpacing;
		Dictionary<int,double> colSpacing;
		
		protected new class WidgetBackendHost: Widget.WidgetBackendHost, ICollectionEventSink<TablePlacement>, IContainerEventSink<TablePlacement>
		{
			public void AddedItem (TablePlacement item, int index)
			{
				((Table)Parent).OnAdd (item.Child, item);
			}

			public void RemovedItem (TablePlacement item, int index)
			{
				((Table)Parent).OnRemove (item.Child);
			}

			public void ChildChanged (TablePlacement child, string hint)
			{
				((Table)Parent).OnChildChanged (child, hint);
			}

			public void ChildReplaced (TablePlacement child, Widget oldWidget, Widget newWidget)
			{
				((Table)Parent).OnReplaceChild (child, oldWidget, newWidget);
			}
		}
		
		protected override BackendHost CreateBackendHost ()
		{
			return new WidgetBackendHost ();
		}
		
		IBoxBackend Backend {
			get { return (IBoxBackend) BackendHost.Backend; }
		}
		
		public Table ()
		{
			children = new ChildrenCollection<TablePlacement> ((WidgetBackendHost)BackendHost);
		}
		
		[DefaultValue(6)]
		public double DefaultRowSpacing {
			get { return defaultRowSpacing; }
			set { defaultRowSpacing = value; OnPreferredSizeChanged (); }
		}
		
		[DefaultValue(6)]
		public double DefaultColumnSpacing {
			get { return defaultColSpacing; }
			set { defaultColSpacing = value; OnPreferredSizeChanged (); }
		}
		
		public void SetRowSpacing (int row, double spacing)
		{
			if (rowSpacing == null)
				rowSpacing = new Dictionary<int, double> ();
			rowSpacing [row] = spacing;
			OnPreferredSizeChanged ();
		}
		
		public void SetColumnSpacing (int col, double spacing)
		{
			if (colSpacing == null)
				colSpacing = new Dictionary<int, double> ();
			colSpacing [col] = spacing;
			OnPreferredSizeChanged ();
		}
		
		public ChildrenCollection<TablePlacement> Placements {
			get { return children; }
		}
		
		public IEnumerable<Widget> Children {
			get { return children.Select (c => c.Child); }
		}
		
		[Obsolete ("Use the Add method")]
		public void Attach (Widget widget, int left, int top)
		{
			Attach (widget, left, top, null, null);
		}
		
		[Obsolete ("Use the Add method")]
		public void Attach (Widget widget, int left, int top, AttachOptions? xOptions, AttachOptions? yOptions)
		{
			Attach (widget, left, left + 1, top, top + 1, xOptions, yOptions);
		}

		[Obsolete ("Use the Add method")]
		public void Attach (Widget widget, int left, int right, int top, int bottom)
		{
			Attach (widget, left, right, top, bottom, (AttachOptions?)null, (AttachOptions?)null);
		}
		
		public void Add (Widget widget, int left, int top, int rowspan = 1, int colspan = 1, bool hexpand = false, bool vexpand = false, WidgetPlacement hpos = WidgetPlacement.Fill, WidgetPlacement vpos = WidgetPlacement.Fill, double marginLeft = -1, double marginTop = -1, double marginRight = -1, double marginBottom = -1, double margin = -1)
		{
			if (vpos != default (WidgetPlacement))
				widget.VerticalPlacement = vpos;
			if (hpos != default (WidgetPlacement))
				widget.HorizontalPlacement = hpos;

			widget.ExpandHorizontal = hexpand;
			widget.ExpandVertical = vexpand;

			if (margin != -1)
				widget.Margin = margin;
			if (marginLeft != -1)
				widget.MarginLeft = marginLeft;
			if (marginTop != -1)
				widget.MarginTop = marginTop;
			if (marginTop != -1)
				widget.MarginRight = marginRight;
			if (marginBottom != -1)
				widget.MarginBottom = marginBottom;
			
			var p = new TablePlacement ((WidgetBackendHost)BackendHost, widget) {
				Left = left,
				Right = left + colspan,
				Top = top,
				Bottom = top + rowspan
			};
			children.Add (p);
		}

		public void Attach (Widget widget, int left, int right, int top, int bottom, AttachOptions? xOptions = null, AttachOptions? yOptions = null)
		{
			if (xOptions != null) {
				widget.ExpandHorizontal = (xOptions.Value & AttachOptions.Expand) != 0;
				if ((xOptions.Value & AttachOptions.Fill) != 0)
					widget.HorizontalPlacement = WidgetPlacement.Fill;
				else
					widget.HorizontalPlacement = WidgetPlacement.Center;
			}
			if (yOptions != null) {
				widget.ExpandVertical = (yOptions.Value & AttachOptions.Expand) != 0;
				if ((yOptions.Value & AttachOptions.Fill) != 0)
					widget.VerticalPlacement = WidgetPlacement.Fill;
				else
					widget.VerticalPlacement = WidgetPlacement.Center;
			}

			var p = new TablePlacement ((WidgetBackendHost)BackendHost, widget) {
				Left = left,
				Right = right,
				Top = top,
				Bottom = bottom
			};
			children.Add (p);
		}
		
		public bool Remove (Widget widget)
		{
			for (int n=0; n<children.Count; n++) {
				if (children[n].Child == widget) {
					children.RemoveAt (n);
					return true;
				}
			}
			return false;
		}

		public void InsertRow (int top, int bottom)
		{
			var potentials = children.Where (c => c.Top >= top);
			var shift = bottom - top;
			foreach (var toShift in potentials) {
				toShift.Top += shift;
				toShift.Bottom += shift;
			}
		}
		
		/// <summary>
		/// Removes all children
		/// </summary>
		public void Clear ()
		{
			children.Clear ();
		}
		
		void OnAdd (Widget child, TablePlacement placement)
		{
			RegisterChild (child);
			Backend.Add ((IWidgetBackend)GetBackend (child));
			OnPreferredSizeChanged ();
		}
		
		void OnRemove (Widget child)
		{
			UnregisterChild (child);
			Backend.Remove ((IWidgetBackend)GetBackend (child));
			OnPreferredSizeChanged ();
		}
		
		void OnChildChanged (TablePlacement placement, object hint)
		{
			OnPreferredSizeChanged ();
		}
		
		internal protected virtual void OnReplaceChild (TablePlacement placement, Widget oldWidget, Widget newWidget)
		{
			if (oldWidget != null)
				OnRemove (oldWidget);
			OnAdd (newWidget, placement);
		}
		
		protected override void OnReallocate ()
		{
			var size = Backend.Size;
			TablePlacement[] visibleChildren = VisibleChildren ();
			var childrenSizes = GetPreferredChildrenSizes (visibleChildren, false, false);
			var w = CalcPreferredCellSizes (visibleChildren, childrenSizes, Orientation.Horizontal);
			CalcCellSizes (w, size.Width, true);
			childrenSizes = GetPreferredChildrenSizes (visibleChildren, true, false);
			var h = CalcPreferredCellSizes (visibleChildren, childrenSizes, Orientation.Vertical);
			CalcCellSizes (h, size.Height, true);

			IWidgetBackend[] widgets = new IWidgetBackend [visibleChildren.Length];
			Rectangle[] rects = new Rectangle [visibleChildren.Length];
			for (int n=0; n<visibleChildren.Length; n++) {
				var bp = visibleChildren [n];
				widgets [n] = (IWidgetBackend)GetBackend (bp.Child);
				var margin = bp.Child.Margin;
				rects [n] = new Rectangle (bp.NextX + margin.Left, bp.NextY + margin.Top, bp.NextWidth - margin.HorizontalSpacing, bp.NextHeight - margin.VerticalSpacing).Round ().WithPositiveSize ();
			}
			
			Backend.SetAllocation (widgets, rects);
			
			if (!BackendHost.EngineBackend.HandlesSizeNegotiation) {
				foreach (var bp in visibleChildren)
					bp.Child.Surface.Reallocate ();
			}
		}
		
		double GetSpacing (int cell, Orientation orientation)
		{
			double sp;
			if (orientation == Orientation.Vertical) {
				if (rowSpacing != null && rowSpacing.TryGetValue (cell, out sp))
					return sp;
				else
					return defaultRowSpacing;
			} else {
				if (colSpacing != null && colSpacing.TryGetValue (cell, out sp))
					return sp;
				else
					return defaultColSpacing;
			}
		}

		TablePlacement[] VisibleChildren ()
		{
			TablePlacement[] result = new TablePlacement[children.Count];

			int j = 0;
			for (int i = 0; i < children.Count; i++) {
				var item = children[i];
				if (item.Child.Visible) {
					result[j] = item;
					j++;
				}
			}

			if (j != children.Count) {
				Array.Resize (ref result, j);
			}

			return result;
		}

		// Get the preferred size of each child widget, including the margins
		Size[] GetPreferredChildrenSizes (TablePlacement[] visibleChildren, bool useWidthConstraint, bool useHeightConstraint)
		{
			var sizes = new Size [visibleChildren.Length];
			for (int n=0; n<visibleChildren.Length; n++) {
				var bp = visibleChildren[n];
				Size s;
				if (useWidthConstraint)
					s = bp.Child.Surface.GetPreferredSize (SizeConstraint.WithSize (bp.NextWidth - bp.Child.Margin.HorizontalSpacing), SizeConstraint.Unconstrained);
				else if (useHeightConstraint)
					s = bp.Child.Surface.GetPreferredSize (SizeConstraint.Unconstrained, SizeConstraint.WithSize (bp.NextHeight - bp.Child.Margin.VerticalSpacing));
				else
					s = bp.Child.Surface.GetPreferredSize (SizeConstraint.Unconstrained, SizeConstraint.Unconstrained);
				s.Width += bp.Child.Margin.HorizontalSpacing;
				s.Height += bp.Child.Margin.VerticalSpacing;
				sizes [n] = s;
			}
			return sizes;
		}

		/// <summary>
		/// Calculates the preferred size of each cell (either height or width, depending on the provided orientation) 
		/// </summary>
		/// <param name="mode">Mode.</param>
		/// <param name="orientation">Wether we are calculating the vertical size or the horizontal size</param>
		/// <param name="visibleChildren">List of children that are visible, and for which the size is being calculated.</param>
		/// <param name="fixedSizesByCell">Cells which have a fixed size</param>
		/// <param name="cellsWithExpand">Cells which are expandable.</param>
		/// <param name="sizes">Calculated size of each cell</param>
		/// <param name="spacing">Spacing to use for each cell</param>
		CellSizeVector CalcPreferredCellSizes (TablePlacement[] visibleChildren, Size[] childrenSizes, Orientation orientation)
		{
			Dictionary<int,double> fixedSizesByCell;
			HashSet<int> cellsWithExpand;
			double[] sizes;
			double spacing;

			int lastCell = 0;

			fixedSizesByCell = new Dictionary<int, double> ();
			cellsWithExpand = new HashSet<int> ();
			HashSet<int> cellsWithWidget = new HashSet<int> ();
			sizes = new double [visibleChildren.Length];

			// Get the size of each widget and store the fixed sizes for widgets which don't span more than one cell

			for (int n=0; n<visibleChildren.Length; n++) {
				var bp = visibleChildren[n];
				int start = GetStartAttach (bp, orientation);
				int end = GetEndAttach (bp, orientation);

				if (end > lastCell)
					lastCell = end;

				// Check if the cell is expandable and store the value
				bool expand = bp.Child.ExpandsForOrientation (orientation);
				for (int i=start; i < end; i++) {
					cellsWithWidget.Add (i);
					if (expand)
						cellsWithExpand.Add (i);
				}

				double s = orientation == Orientation.Vertical ? childrenSizes[n].Height : childrenSizes[n].Width;
				sizes [n] = s;

				if (end == start + 1) {
					// The widget only takes one cell. Store its size if it is the biggest
					bool changed = false;
					double fs;
					if (!fixedSizesByCell.TryGetValue (start, out fs))
						changed = true;
					if (s > fs) { 
						fs = s;
						changed = true;
					}
					if (changed)
						fixedSizesByCell [start] = fs;
				}
			}

			// For widgets that span more than one cell, calculate the floating size, that is, the size
			// which is not taken by other fixed size widgets

			List<TablePlacement> widgetsToAdjust = new List<TablePlacement> ();
			Dictionary<TablePlacement,double[]> growSizes = new Dictionary<TablePlacement, double[]> ();

			for (int n=0; n<visibleChildren.Length; n++) {
				var bp = visibleChildren[n];
				int start = GetStartAttach (bp, orientation);
				int end = GetEndAttach (bp, orientation);
				if (end == start + 1)
					continue;
				widgetsToAdjust.Add (bp);

				double fixedSize = 0;

				// We are going to calculate the spacing included in the widget's span of cells
				// (there is spacing between each cell)
				double spanSpacing = 0;

				for (int c = start; c < end; c++) {
					double fs;
					fixedSizesByCell.TryGetValue (c, out fs);
					fixedSize += fs;
					if (c != start && c != end)
						spanSpacing += GetSpacing (c, orientation);
				}

				// sizeToGrow is the size that the whole cell span has to grow in order to fit
				// this widget. We substract the spacing between cells because that space will
				// be used by the widget, so we don't need to allocate more size for it

				double sizeToGrow = sizes [n] - fixedSize - spanSpacing;

				double sizeToGrowPart = sizeToGrow / (end - start);

				// Split the size to grow between the cells of the widget. We need to know how much size the widget
				// requires for each cell it covers.

				double[] widgetGrowSizes = new double [end - start];
				for (int i=0; i<widgetGrowSizes.Length; i++)
					widgetGrowSizes [i] = sizeToGrowPart;
				growSizes[bp] = widgetGrowSizes;
			}

			// Now size-to-grow values have to be adjusted. For example, let's say widget A requires 100px for column 1 and 100px for column 2, and widget B requires
			// 60px for column 2 and 60px for column 3. So the widgets are overlapping at column 2. Since A requires at least 100px in column 2, it means that B can assume
			// that it will have 100px available in column 2, which means 40px more than it requested. Those extra 40px can then be substracted from the 60px that
			// it required for column 3.

			foreach (var n in cellsWithWidget) {
				double maxv = 0;
				TablePlacement maxtNatural = null;

				// Find the widget that requires the maximum size for this cell
				foreach (var bp in widgetsToAdjust) {
					// could be expressed as where clause, but this is faster and performance matters here
					if (GetStartAttach (bp, orientation) <= n && GetEndAttach (bp, orientation) > n) {
						double cv = growSizes[bp][n - GetStartAttach (bp, orientation)];
						if (cv > maxv) {
							maxv = cv;
							maxtNatural = bp;
						}
					}
				}

				// Adjust the required size of all widgets of the cell (excluding the widget with the max size)
				foreach (var bp in widgetsToAdjust) {
					if (GetStartAttach (bp, orientation) <= n && GetEndAttach (bp, orientation) > n) {
						double[] widgetGrows = growSizes[bp];
						int cellIndex = n - GetStartAttach (bp, orientation);
						if (bp != maxtNatural) {
							double cv = widgetGrows[cellIndex];
							double splitExtraSpace = (maxv - cv) / (widgetGrows.Length - 1);
							for (int i=0; i<widgetGrows.Length; i++)
								widgetGrows[i] -= splitExtraSpace;
						}
					}
				}
			}

			// Find the maximum size-to-grow for each cell

			Dictionary<int,double> finalGrowTable = new Dictionary<int, double> ();

			foreach (var bp in widgetsToAdjust) {
				int start = GetStartAttach (bp, orientation);
				int end = GetEndAttach (bp, orientation);
				double[] widgetGrows = growSizes[bp];
				for (int n=start; n<end; n++) {
					double curGrow;
					finalGrowTable.TryGetValue (n, out curGrow);
					var val = widgetGrows [n - start];
					if (val > curGrow)
						curGrow = val;
					finalGrowTable [n] = curGrow;
				}
			}

			// Add the final size-to-grow to the fixed sizes calculated at the begining

			foreach (var it in finalGrowTable) {
				double ws;
				fixedSizesByCell.TryGetValue (it.Key, out ws);
				fixedSizesByCell [it.Key] = it.Value + ws;
			}

			spacing = 0;
			for (int n=1; n<lastCell; n++) {
				if (cellsWithWidget.Contains (n))
					spacing += GetSpacing (n, orientation);
			}

			return new CellSizeVector () {
				visibleChildren = visibleChildren,
				fixedSizesByCell = fixedSizesByCell,
				cellsWithExpand = cellsWithExpand,
				sizes = sizes,
				spacing = spacing,
				orientation = orientation
			};
		}

		/// <summary>
		/// Calculates size of each cell, taking into account their preferred size, expansion/fill requests, and the available space.
		/// Calculation is done only for the provided orientation (either height or width).
		/// </summary>
		/// <param name="mode">Mode.</param>
		/// <param name="availableSize">Total size available</param>
		/// <param name="calcOffsets"></param>
		void CalcCellSizes (CellSizeVector cellSizes, double availableSize, bool calcOffsets)
		{
			TablePlacement[] visibleChildren = cellSizes.visibleChildren;
			Dictionary<int,double> fixedSizesByCell = cellSizes.fixedSizesByCell;
			double[] sizes = cellSizes.sizes;
			double spacing = cellSizes.spacing;
			Orientation orientation = cellSizes.orientation;

			// Get the total natural size
			double naturalSize = fixedSizesByCell.Values.Sum ();

			double remaining = availableSize - naturalSize - spacing;

			if (availableSize - spacing <= 0) {
				foreach (var i in fixedSizesByCell.Keys.ToArray ())
					fixedSizesByCell [i] = 0;
			}
			else if (remaining < 0) {
				// The box is not big enough to fit the widgets using its natural size.
				// We have to shrink the cells. We do a proportional reduction

				// List of cell indexes that we have to shrink
				var toShrink = new List<int> (fixedSizesByCell.Keys);

				// The total amount we have to shrink
				double splitSize = (availableSize - spacing) / toShrink.Count;

				// We have to reduce all cells proportionally, but if a cell is much bigger that
				// its proportionally allocated space, then we reduce this one before the others

				var smallCells = fixedSizesByCell.Where (c => c.Value < splitSize);
				var belowSplitSize = smallCells.Sum (c => splitSize - c.Value);

				var bigCells = fixedSizesByCell.Where (c => c.Value > splitSize);
				var overSplitSize = bigCells.Sum (c => c.Value - splitSize);

				ReduceProportional (fixedSizesByCell, bigCells.Select (c => c.Key), overSplitSize - belowSplitSize);

				var newNatural = fixedSizesByCell.Sum (c => c.Value);
				ReduceProportional (fixedSizesByCell, fixedSizesByCell.Keys, (availableSize - spacing) - newNatural);

				RoundSizes (fixedSizesByCell);
			}
			else {
				// Distribute remaining space among the extensible widgets
				HashSet<int> cellsWithExpand = cellSizes.cellsWithExpand;
				int nexpands = cellsWithExpand.Count;
				var expandRemaining = new SizeSplitter (remaining, nexpands);
				foreach (var c in cellsWithExpand) {
					double ws;
					fixedSizesByCell.TryGetValue (c, out ws);
					ws += expandRemaining.NextSizePart ();
					fixedSizesByCell [c] = ws;
				}
			}

			// Calculate the offset of each widget, relative to the cell (so 0 means at the left/top of the cell).

			for (int n=0; n<visibleChildren.Length; n++) {
				var bp = visibleChildren[n];
				double allocatedSize = 0;
				double cellOffset = 0;

				int start = GetStartAttach (bp, orientation);
				int end = GetEndAttach (bp, orientation);
				for (int i=start; i<end; i++) {
					double ws;
					fixedSizesByCell.TryGetValue (i, out ws);
					allocatedSize += ws;
					if (i != start)
						allocatedSize += GetSpacing (i, orientation);
				}

				var al = bp.Child.AlignmentForOrientation (orientation);
				if (al != WidgetPlacement.Fill) {
					double s = sizes[n];
					if (s < allocatedSize) {
						cellOffset = (allocatedSize - s) * al.GetValue ();
						allocatedSize = s;
					}
				}

				// cellOffset is the offset of the widget inside the cell. We store it in NextX/Y, and
				// will be used below to calculate the total offset of the widget

				if (orientation == Orientation.Vertical) {
					bp.NextHeight = allocatedSize;
					bp.NextY = cellOffset;
				}	
				else {
					bp.NextWidth = allocatedSize;
					bp.NextX = cellOffset;
				}
			}

			if (calcOffsets) {
				// Calculate the final offset of each widget, relative to the table origin
				var sortedChildren = visibleChildren.OrderBy (c => GetStartAttach (c, orientation)).ToArray();
				var cells = fixedSizesByCell.OrderBy (c => c.Key);
				double offset = 0;
				int n = 0;
				foreach (var c in cells) {
					if (c.Key > 0)
						offset += GetSpacing (c.Key, orientation);
					while (n < sortedChildren.Length && GetStartAttach (sortedChildren[n], orientation) == c.Key) {
						// In the loop above we store the offset of the widget inside the cell in the NextX/Y field
						// so now we have to add (not just assign) the offset of the cell to NextX/Y
						if (orientation == Orientation.Vertical)
							sortedChildren[n].NextY += offset;
						else
							sortedChildren[n].NextX += offset;
						n++;
					}
					offset += c.Value;
				}
			}
		}

		void ReduceProportional (Dictionary<int,double> sizes, IEnumerable<int> indexes, double amount)
		{
			var total = indexes.Sum (i => sizes[i]); 
			foreach (var i in indexes.ToArray ()) {
				double size = sizes [i]; 
				var am = amount * (size / total); 
				sizes [i] = size - am;
			}
		}

		void RoundSizes (Dictionary<int,double> sizes)
		{
			double rem = 0;
			for (int i = 0; i < sizes.Count; i++) {
				var kvp = sizes.ElementAt (i);
				double size = Math.Floor (kvp.Value);
				rem += kvp.Value - size;
				sizes[kvp.Key] = size;
			}
			while (rem > 0) {
				for (int i = 0; i < sizes.Count; i++) {
					var kvp = sizes.ElementAt (i);
					sizes[kvp.Key] = kvp.Value - 1;
					if (--rem <= 0)
						break;
				}
			}
		}

		class CellSizeVector
		{
			internal TablePlacement[] visibleChildren;
			internal Dictionary<int,double> fixedSizesByCell;
			internal HashSet<int> cellsWithExpand;
			internal double[] sizes;
			internal double spacing;
			internal Orientation orientation;
			double totalSize = -1;

			public double TotalSize {
				get {
					if (totalSize == -1) {
						totalSize = spacing;
						foreach (var s in fixedSizesByCell.Values)
							totalSize += s;
					}
					return totalSize;
				}
			}
		}
		
		protected override Size OnGetPreferredSize (SizeConstraint widthConstraint, SizeConstraint heightConstraint)
		{
			TablePlacement[] visibleChildren = VisibleChildren ();

			if (!widthConstraint.IsConstrained && !heightConstraint.IsConstrained) {
				var childrenSizes = GetPreferredChildrenSizes (visibleChildren, false, false);
				var w = CalcPreferredCellSizes (visibleChildren, childrenSizes, Orientation.Horizontal);
				var h = CalcPreferredCellSizes (visibleChildren, childrenSizes, Orientation.Vertical);
				return new Size (w.TotalSize, h.TotalSize);
			}
			else if (!widthConstraint.IsConstrained) {
				var childrenSizes = GetPreferredChildrenSizes (visibleChildren, false, false);
				var h = CalcPreferredCellSizes (visibleChildren, childrenSizes, Orientation.Vertical);
				CalcCellSizes (h, heightConstraint.AvailableSize, false);

				childrenSizes = GetPreferredChildrenSizes (visibleChildren, false, true);
				var w = CalcPreferredCellSizes (visibleChildren, childrenSizes, Orientation.Horizontal);
				return new Size (w.TotalSize, heightConstraint.AvailableSize);
			}
			else if (!heightConstraint.IsConstrained) {
				var childrenSizes = GetPreferredChildrenSizes (visibleChildren, false, false);
				var w = CalcPreferredCellSizes (visibleChildren, childrenSizes, Orientation.Horizontal);
				CalcCellSizes (w, widthConstraint.AvailableSize, false);

				childrenSizes = GetPreferredChildrenSizes (visibleChildren, true, false);
				var h = CalcPreferredCellSizes (visibleChildren, childrenSizes, Orientation.Vertical);
				return new Size (widthConstraint.AvailableSize, h.TotalSize);
			}
			else {
				var childrenSizes = GetPreferredChildrenSizes (visibleChildren, false, false);
				var width = CalcPreferredCellSizes (visibleChildren, childrenSizes, Orientation.Horizontal);
				var height = CalcPreferredCellSizes (visibleChildren, childrenSizes, Orientation.Vertical);

				if (width.TotalSize <= widthConstraint.AvailableSize)
					return new Size (width.TotalSize, height.TotalSize);

				CalcCellSizes (width, widthConstraint.AvailableSize, false);
				childrenSizes = GetPreferredChildrenSizes (visibleChildren, true, false);
				height = CalcPreferredCellSizes (visibleChildren, childrenSizes, Orientation.Vertical);
				return new Size (widthConstraint.AvailableSize, Math.Min (heightConstraint.AvailableSize, height.TotalSize));
			}
		}

		int GetStartAttach (TablePlacement tp, Orientation orientation)
		{
			if (orientation == Orientation.Vertical)
				return tp.Top;
			else
				return tp.Left;
		}
		
		int GetEndAttach (TablePlacement tp, Orientation orientation)
		{
			if (orientation == Orientation.Vertical)
				return tp.Bottom;
			else
				return tp.Right;
		}
	}
	
	[ContentProperty("Child")]
	public class TablePlacement
	{
		IContainerEventSink<TablePlacement> parent;
		int left, right, top, bottom;
		Widget child;
		
		internal TablePlacement (IContainerEventSink<TablePlacement> parent, Widget child)
		{
			this.parent = parent;
			this.child = child;
		}
		
		internal double NextWidth;
		internal double NextHeight;
		internal double NextX;
		internal double NextY;
		
		public int Left {
			get {
				return left;
			}
			set {
				left = value;
				parent.ChildChanged (this, "Left");
			}
		}
		
		public int Right {
			get {
				return right;
			}
			set {
				right = value;
				parent.ChildChanged (this, "Right");
			}
		}
		
		public int Top {
			get {
				return top;
			}
			set {
				top = value;
				parent.ChildChanged (this, "Top");
			}
		}
		
		public int Bottom {
			get {
				return bottom;
			}
			set {
				bottom = value;
				parent.ChildChanged (this, "Bottom");
			}
		}
		
		public Widget Child {
			get { return child; }
			set {
				var old = child;
				child = value;
				parent.ChildReplaced (this, old, value);
			}
		}
	}
	
	[Flags]
	public enum AttachOptions
	{
		Expand = 1,
		Fill = 2,
		Shrink = 4
	}
}

