using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace DWDWeatherBand
{
    public enum BrushType
    {
        SolidColorBrush,
        LinearGradientBrush
    }

    public interface IDrawData
    {
        void SetData(KeyValuePair<double, double>[] newData);
        void ClearData();
        double[] YScaleNames(int amount);
        double[] XScaleNames(int amount);
        bool GetValue(double xPosition, out double value, int roundDigits = 2);
        bool GetValueLocal(double position, out double value, int roundDigits = 2);
        double GetPosition(double xPosition);
    }

    public class VirtualData : IDrawData
    {
        private KeyValuePair<double, double>[] data = new KeyValuePair<double, double>[0];
        double gridHeight;
        double gridWidth;
        public double MinDataValueX = 0;
        public double MaxDataValueX = 1;
        public double MinDataValueY = 0;
        public double MaxDataValueY = 1;
        public VirtualData(FrameworkElement Grid) 
        {
            gridHeight = Grid.ActualHeight;
            gridWidth = Grid.ActualWidth;
        }

        public void ClearData()
        {
            data = new KeyValuePair<double, double>[0];
            throw new NotImplementedException();
        }

        public double GetPosition(double xPosition)
        {
            return xPosition / gridWidth * (MaxDataValueX - MinDataValueX) + MinDataValueX;
        }

        public bool GetValue(double xPosition, out double value, int roundDigits = 2)
        {
            value = 0;
            if (data.Length == 0)
            {
                return false;
            }

            double position = xPosition / gridWidth * (MaxDataValueX - MinDataValueX) + MinDataValueX;
            return GetValueLocal(position, out value, roundDigits);
        }

        public bool GetValueLocal(double position, out double value, int roundDigits = 2)
        {
            value = 0;
            double first = data[0].Key;
            double last = data[data.Length - 1].Key;
            if (data.Length == 1)
            {
                if (Math.Round(position, roundDigits) == Math.Round(first, roundDigits))
                {
                    value = data[0].Value;
                    return true;
                }
                return false;
            }

            if (position < first || position > last)
            {
                return false;
            }

            int half = (int)(data.Length * 0.5);
            int index = half;
            double low;
            double high;
            while ((low = data[index - 1].Key) > position || (high = data[index].Key) < position)
            {
                half = Math.Max((int)(half * 0.5), 1);
                index += half * (low > position ? -1 : 1);
            }
            double lowvalue = data[index - 1].Value;
            double highvalue = data[index].Value;
            value = (highvalue - lowvalue) * (position - low) / (high - low) + lowvalue;
            return true;
        }

        public void SetData(KeyValuePair<double, double>[] newData)
        {
            data = new KeyValuePair<double, double>[newData.Length];
            for (int i = 0; i < newData.Length; i++)
            {
                data[i] = new KeyValuePair<double, double>(newData[i].Key, newData[i].Value);
            }
        }

        public double[] XScaleNames(int amount)
        {
            double[] names = new double[amount];
            double step = (MaxDataValueX - MinDataValueX) / (amount - 1);
            for (int i = 0; i < amount; i++)
            {
                names[i] = i * step + MinDataValueX;
            }
            return names;
        }

        public double[] YScaleNames(int amount)
        {
            double[] names = new double[amount];
            double step = (MaxDataValueY - MinDataValueY) / (amount - 1);
            for (int i = 0; i < amount; i++)
            {
                names[i] = i * step + MinDataValueY;
            }
            return names;
        }
    }

    public class DrawDataAsPath : IDrawData
    {
        private readonly Path graph;
        private readonly BrushType brushType;
        private readonly string startData = "M";
        private KeyValuePair<double, double>[] data = new KeyValuePair<double, double>[0];
        private readonly double gridHeight;
        private readonly double gridWidth;

        public KeyValuePair<double, double>[] Data { get { return data; } }
        public BrushType BrushType { get { return brushType; } }
        public double MinDataValueX = 0;
        public double MaxDataValueX = 1;
        public double MinDataValueY = 0;
        public double MaxDataValueY = 1;

        public DrawDataAsPath(FrameworkElement Grid, Path Graph, BrushType BrushType)
        {
            graph = Graph;
            brushType = BrushType;
            gridHeight = Grid.ActualHeight;
            gridWidth = Grid.ActualWidth;

            if (brushType == BrushType.LinearGradientBrush)
            {
                double outOffset = Math.Min(-10 * graph.StrokeThickness, -10);
                CultureInfo culture = CultureInfo.InvariantCulture;
                startData = $"M {outOffset.ToString(culture)},0 L {outOffset.ToString(culture)},{gridHeight.ToString(culture)} M";
            }
        }

        private void UpdateGraph()
        {
            if (data.Length == 0)
            {
                return;
            }

            string pathData = startData;

            CultureInfo culture = CultureInfo.InvariantCulture;
            foreach (KeyValuePair<double, double> item in data)
            {
                double x = (item.Key - MinDataValueX) * gridWidth / (MaxDataValueX - MinDataValueX);
                double y = gridHeight * (1 - (item.Value - MinDataValueY) / (MaxDataValueY - MinDataValueY));
                pathData += $" {x.ToString(culture)},{y.ToString(culture)} {(x + graph.StrokeThickness * 0.5).ToString(culture)},{y.ToString(culture)}";
            }
            graph.Data = Geometry.Parse(pathData);
        }
        public void SetData(KeyValuePair<double, double>[] newData)
        {
            data = new KeyValuePair<double, double>[newData.Length];
            for (int i = 0; i < newData.Length; i++)
            {
                data[i] = new KeyValuePair<double, double>(newData[i].Key, newData[i].Value);
            }
            UpdateGraph();
        }

        public void ClearData()
        {
            data = new KeyValuePair<double, double>[0];
            UpdateGraph();
        }

        public double[] YScaleNames(int amount)
        {
            double[] names = new double[amount];
            double step = (MaxDataValueY - MinDataValueY) / (amount - 1);
            for (int i = 0; i < amount; i++)
            {
                names[i] = i * step + MinDataValueY;
            }
            return names;
        }

        public double[] XScaleNames(int amount)
        {
            double[] names = new double[amount];
            double step = (MaxDataValueX - MinDataValueX) / (amount - 1);
            for (int i = 0; i < amount; i++)
            {
                names[i] = i * step + MinDataValueX;
            }
            return names;
        }

        public bool GetValue(double xPosition, out double value, int roundDigits = 2)
        {
            value = 0;
            if (data.Length == 0)
            {
                return false;
            }
            double position = xPosition / gridWidth * (MaxDataValueX - MinDataValueX) + MinDataValueX;
            return GetValueLocal(position, out value, roundDigits);
        }

        public double GetPosition(double xPosition)
        {
            return xPosition / gridWidth * (MaxDataValueX - MinDataValueX) + MinDataValueX;
        }

        public bool GetValueLocal(double position, out double value, int roundDigits = 2)
        {
            value = 0;
            double first = data[0].Key;
            double last = data[data.Length - 1].Key;
            if (data.Length == 1)
            {
                if (Math.Round(position, roundDigits) == Math.Round(first, roundDigits))
                {
                    value = data[0].Value;
                    return true;
                }
                return false;
            }

            if (position < first || position > last)
            {
                return false;
            }

            int half = (int)(data.Length * 0.5);
            int index = half;
            double low;
            double high;
            while ((low = data[index - 1].Key) > position || (high = data[index].Key) < position)
            {
                half = Math.Max((int)(half * 0.5), 1);
                index += half * (low > position ? -1 : 1);
            }
            double lowvalue = data[index - 1].Value;
            double highvalue = data[index].Value;
            value = (highvalue - lowvalue) * (position - low) / (high - low) + lowvalue;
            return true;
        }
    }
    public class DrawDataAsRectangle : IDrawData
    {
        private readonly BrushType brushType;
        private KeyValuePair<double, double>[] data = new KeyValuePair<double, double>[0];
        private readonly double gridHeight;
        private readonly double gridWidth;
        private readonly Rectangle reference;
        private readonly Panel grid;
        private readonly double defaultWidth;
        public KeyValuePair<double, double>[] Data { get { return data; } }
        public BrushType BrushType { get { return brushType; } }
        public double MinDataValueX = 0;
        public double MaxDataValueX = 1;
        public double MinDataValueY = 0;
        public double MaxDataValueY = 1;


        public DrawDataAsRectangle(Panel Grid, Rectangle Reference, BrushType BrushType)
        {
            reference = Reference;
            defaultWidth = Reference.Width;
            brushType = BrushType;
            grid = Grid;
            gridHeight = Grid.ActualHeight;
            gridWidth = Grid.ActualWidth;

            if (brushType == BrushType.LinearGradientBrush)
            {
                SetGradientReference();
            }
        }

        private Rectangle GetRectangle()
        {
            return new Rectangle()
            {
                AllowDrop = reference.AllowDrop,
                BindingGroup = reference.BindingGroup,
                CacheMode = reference.CacheMode,
                Clip = reference.Clip,
                ClipToBounds = reference.ClipToBounds,
                ContextMenu = reference.ContextMenu,
                Cursor = reference.Cursor,
                DataContext = reference.DataContext,
                Effect = reference.Effect,
                Fill = reference.Fill,
                FlowDirection = reference.FlowDirection,
                Focusable = reference.Focusable,
                FocusVisualStyle = reference.FocusVisualStyle,
                ForceCursor = reference.ForceCursor,
                HorizontalAlignment = HorizontalAlignment.Left,
                InputScope = reference.InputScope,
                IsEnabled = reference.IsEnabled,
                IsHitTestVisible = false,
                IsManipulationEnabled = reference.IsManipulationEnabled,
                Language = reference.Language,
                LayoutTransform = reference.LayoutTransform,
                Opacity = reference.Opacity,
                OpacityMask = reference.OpacityMask,
                OverridesDefaultStyle = reference.OverridesDefaultStyle,
                RadiusX = reference.RadiusX,
                RadiusY = reference.RadiusY,
                RenderSize = reference.RenderSize,
                RenderTransform = reference.RenderTransform,
                RenderTransformOrigin = reference.RenderTransformOrigin,
                Resources = reference.Resources,
                SnapsToDevicePixels = reference.SnapsToDevicePixels,
                Stretch = reference.Stretch,
                Stroke = reference.Stroke,
                StrokeDashArray = reference.StrokeDashArray,
                StrokeDashCap = reference.StrokeDashCap,
                StrokeDashOffset = reference.StrokeDashOffset,
                StrokeLineJoin = reference.StrokeLineJoin,
                StrokeEndLineCap = reference.StrokeEndLineCap,
                StrokeMiterLimit = reference.StrokeMiterLimit,
                StrokeStartLineCap = reference.StrokeStartLineCap,
                StrokeThickness = reference.StrokeThickness,
                Style = reference.Style,
                Tag = reference.Tag,
                ToolTip = reference.ToolTip,
                UseLayoutRounding = reference.UseLayoutRounding,
                VerticalAlignment = VerticalAlignment.Bottom
            };
        }

        private void SetGradientReference()
        {
            Rectangle rect = GetRectangle();
            double outOffset = Math.Min(-10 * reference.StrokeThickness, -10) - rect.Width;
            rect.Margin = new Thickness(outOffset, 0, 0, 0);
            rect.Height = gridHeight;
            grid.Children.Add(rect);
        }
        private void UpdateGraph()
        {
            if (data.Length == 0)
            {
                return;
            }

            grid.Children.Clear();

            if (brushType == BrushType.LinearGradientBrush)
            {
                SetGradientReference();
            }

            KeyValuePair<double, double> item = data[0];
            double width = defaultWidth;
            if (data.Length > 1)
            {
                KeyValuePair<double, double> second = data[1];
                width = Math.Abs((second.Key - item.Key) * gridWidth / (MaxDataValueX - MinDataValueX));
            }
            double x = (item.Key - MinDataValueX) * gridWidth / (MaxDataValueX - MinDataValueX) - width * 0.5;
            double y = (item.Value - MinDataValueY) * gridHeight / (MaxDataValueY - MinDataValueY);
            Rectangle rect = GetRectangle();
            rect.Margin = new Thickness(x, 0, 0, 0);
            rect.Width = width;
            rect.Height = y;
            grid.Children.Add(rect);
            for (int i = 1; i < data.Length - 1; i++)
            {
                item = data[i];
                KeyValuePair<double, double> last = data[i - 1];
                KeyValuePair<double, double> next = data[i + 1];
                width = Math.Abs((last.Key - next.Key) * 0.5 * gridWidth / (MaxDataValueX - MinDataValueX));
                x = (item.Key - MinDataValueX - Math.Abs(last.Key - item.Key) * 0.5) * gridWidth / (MaxDataValueX - MinDataValueX);
                y = (item.Value - MinDataValueY) * gridHeight / (MaxDataValueY - MinDataValueY);
                rect = GetRectangle();
                rect.Margin = new Thickness(x, 0, 0, 0);
                rect.Width = width;
                rect.Height = y;
                grid.Children.Add(rect);
            }
            if (data.Length <= 1)
            {
                return;
            }
            item = data[data.Length - 1];
            KeyValuePair<double, double> secondLast = data[data.Length - 2];
            width = Math.Abs((item.Key - secondLast.Key) * gridWidth / (MaxDataValueX - MinDataValueX));
            x = (item.Key - MinDataValueX) * gridWidth / (MaxDataValueX - MinDataValueX) - width * 0.5;
            y = (item.Value - MinDataValueY) * gridHeight / (MaxDataValueY - MinDataValueY);
            rect = GetRectangle();
            rect.Margin = new Thickness(x, 0, 0, 0);
            rect.Width = width;
            rect.Height = y;
            grid.Children.Add(rect);
        }

        public void SetData(KeyValuePair<double, double>[] newData)
        {
            data = new KeyValuePair<double, double>[newData.Length];
            for (int i = 0; i < newData.Length; i++)
            {
                data[i] = new KeyValuePair<double, double>(newData[i].Key, newData[i].Value);
            }
            UpdateGraph();
        }

        public void ClearData()
        {
            data = new KeyValuePair<double, double>[0];
            UpdateGraph();
        }

        public double[] YScaleNames(int Amount)
        {
            double[] names = new double[Amount];
            double step = (MaxDataValueY - MinDataValueY) / (Amount - 1);
            for (int i = 0; i < Amount; i++)
            {
                names[i] = i * step + MinDataValueY;
            }
            return names;
        }

        public double[] XScaleNames(int Amount)
        {
            double[] names = new double[Amount];
            double step = (MaxDataValueX - MinDataValueX) / (Amount - 1);
            for (int i = 0; i < Amount; i++)
            {
                names[i] = i * step + MinDataValueX;
            }
            return names;
        }

        public bool GetValue(double xPosition, out double value, int roundDigits = 2)
        {
            value = 0;
            if (data.Length == 0)
            {
                return false;
            }

            double position = xPosition / gridWidth * (MaxDataValueX - MinDataValueX) + MinDataValueX;
            return GetValueLocal(position, out value, roundDigits);
        }

        public double GetPosition(double xPosition)
        {
            return xPosition / gridWidth * (MaxDataValueX - MinDataValueX) + MinDataValueX;
        }

        public bool GetValueLocal(double position, out double value, int roundDigits = 2)
        {
            value = 0;
            double first = data[0].Key;
            double last = data[data.Length - 1].Key;
            if (data.Length == 1)
            {
                if (Math.Round(position, roundDigits) == Math.Round(first, roundDigits))
                {
                    value = data[0].Value;
                    return true;
                }
                return false;
            }

            if (position < first || position > last)
            {
                return false;
            }

            int half = (int)(data.Length * 0.5);
            int index = half;
            double low;
            double high;
            while ((low = data[index - 1].Key) > position || (high = data[index].Key) < position)
            {
                half = Math.Max((int)(half * 0.5), 1);
                index += half * (low > position ? -1 : 1);
            }
            double lowvalue = data[index - 1].Value;
            double highvalue = data[index].Value;
            value = (highvalue - lowvalue) * (position - low) / (high - low) + lowvalue;
            return true;
        }
    }
}
